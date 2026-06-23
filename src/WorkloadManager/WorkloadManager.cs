using System.Fabric;
using System.Security.Cryptography;
using System.Text;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Shared.Contracts;
using Shared.Models;
using WorkloadManager.StateMachine;

namespace WorkloadManager;

internal sealed class WorkloadManager : StatefulService, IWorkloadManager
{
    public WorkloadManager(StatefulServiceContext context) : base(context)
    {
    }

    protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
    {
        return this.CreateServiceRemotingReplicaListeners();
    }

    public async Task SubmitAsync(string workloadId, int requiredCapacityUnits, CancellationToken cancellationToken)
    {
        var dict = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, Workload>>("workloads");
        var queue = await this.StateManager.GetOrAddAsync<IReliableQueue<string>>("pendingQueue");

        using (var tx = this.StateManager.CreateTransaction())
        {
            var existing = await dict.TryGetValueAsync(tx, workloadId);
            if (existing.HasValue)
            {
                // Idempotent retry: workload already exists, just return success.
                return;
            }

            var workload = new Workload
            {
                WorkloadId = workloadId,
                State = WorkloadState.Submitted,
                RequiredCapacityUnits = requiredCapacityUnits,
                CreatedUtc = DateTime.UtcNow,
                UpdatedUtc = DateTime.UtcNow,
                Attempts = 0
            };

            await dict.AddAsync(tx, workloadId, workload);
            await queue.EnqueueAsync(tx, workloadId);
            await tx.CommitAsync();
        }

        ServiceEventSource.Current.ServiceMessage(this.Context, $"Submitted workload {workloadId}");
    }

    public async Task<Workload?> GetAsync(string workloadId, CancellationToken cancellationToken)
    {
        var dict = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, Workload>>("workloads");
        using (var tx = this.StateManager.CreateTransaction())
        {
            var result = await dict.TryGetValueAsync(tx, workloadId);
            return result.HasValue ? result.Value : null;
        }
    }

    public async Task<bool> CancelAsync(string workloadId, CancellationToken cancellationToken)
    {
        var dict = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, Workload>>("workloads");
        
        using (var tx = this.StateManager.CreateTransaction())
        {
            var result = await dict.TryGetValueAsync(tx, workloadId);
            if (!result.HasValue) return false;

            var current = result.Value;
            if (current.State == WorkloadState.Completed || 
                current.State == WorkloadState.Failed || 
                current.State == WorkloadState.Cancelled)
            {
                return false;
            }

            if ((current.State == WorkloadState.Scheduled || current.State == WorkloadState.Running) && !string.IsNullOrEmpty(current.AssignedNodeId))
            {
                var capacityProxy = GetCapacityServiceProxy();
                await capacityProxy.ReleaseAsync(current.AssignedNodeId, current.RequiredCapacityUnits, cancellationToken);
            }

            var next = new Workload
            {
                WorkloadId = current.WorkloadId,
                State = WorkloadState.Cancelled,
                RequiredCapacityUnits = current.RequiredCapacityUnits,
                AssignedNodeId = string.Empty, // clear assignment
                CreatedUtc = current.CreatedUtc,
                UpdatedUtc = DateTime.UtcNow,
                Attempts = current.Attempts
            };

            await dict.SetAsync(tx, workloadId, next);
            await tx.CommitAsync();

            ServiceEventSource.Current.ServiceMessage(this.Context, $"Workload {workloadId} cancelled (Partition: {this.Context.PartitionId})");
            return true;
        }
    }

    protected override async Task RunAsync(CancellationToken cancellationToken)
    {
        var dict = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, Workload>>("workloads");
        var queue = await this.StateManager.GetOrAddAsync<IReliableQueue<string>>("pendingQueue");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                using (var tx = this.StateManager.CreateTransaction())
                {
                    var result = await queue.TryDequeueAsync(tx);
                    if (result.HasValue)
                    {
                        var workloadId = result.Value;
                        var workloadResult = await dict.TryGetValueAsync(tx, workloadId);
                        
                        if (workloadResult.HasValue)
                        {
                            var current = workloadResult.Value;
                            var oldState = current.State;

                            // Call CapacityService if needed
                            CapacityGrant grant = new CapacityGrant { Granted = false };
                            if (current.State == WorkloadState.Submitted)
                            {
                                var capacityProxy = GetCapacityServiceProxy();
                                grant = await capacityProxy.ReserveAsync(current.RequiredCapacityUnits, cancellationToken);
                            }

                            // Advance state
                            var (next, requeue) = WorkloadStateMachine.Advance(current, grant);

                            // Release capacity if terminal state
                            if ((next.State == WorkloadState.Completed || next.State == WorkloadState.Failed) && !string.IsNullOrEmpty(next.AssignedNodeId))
                            {
                                var capacityProxy = GetCapacityServiceProxy();
                                await capacityProxy.ReleaseAsync(next.AssignedNodeId, next.RequiredCapacityUnits, cancellationToken);
                            }

                            // Persist
                            await dict.SetAsync(tx, workloadId, next);

                            if (requeue)
                            {
                                await queue.EnqueueAsync(tx, workloadId);
                            }

                            await tx.CommitAsync();

                            if (oldState != next.State)
                            {
                                ServiceEventSource.Current.ServiceMessage(this.Context, 
                                    $"Workload {workloadId} transitioned {oldState} -> {next.State} (Partition: {this.Context.PartitionId})");
                            }
                        }
                    }
                }
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                ServiceEventSource.Current.ServiceMessage(this.Context, $"Error in RunAsync loop: {ex}");
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
        }
    }

    private ICapacityService GetCapacityServiceProxy()
    {
        long bucket = Math.Abs(Guid.NewGuid().GetHashCode()) % 3;
        var partitionKey = new ServicePartitionKey(bucket);
        return ServiceProxy.Create<ICapacityService>(
            new Uri("fabric:/ReliableServicesPlatform/CapacityService"),
            partitionKey);
    }
}

internal sealed class ServiceEventSource
{
    public static readonly ServiceEventSource Current = new ServiceEventSource();
    public void ServiceMessage(ServiceContext context, string message, params object[] args)
    {
        Console.WriteLine($"[WorkloadManager] {context.ServiceName} - {string.Format(message, args)}");
    }
}
