using System.Fabric;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Shared.Contracts;
using Shared.Models;

namespace CapacityService;

internal sealed class CapacityService : StatefulService, ICapacityService
{
    private const int DefaultNodeCount = 4;
    private const int MaxUnitsPerNode = 100;

    public CapacityService(StatefulServiceContext context) : base(context)
    {
    }

    protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
    {
        return this.CreateServiceRemotingReplicaListeners();
    }

    protected override async Task RunAsync(CancellationToken cancellationToken)
    {
        var dict = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, int>>("nodeCapacity");

        using (var tx = this.StateManager.CreateTransaction())
        {
            var count = await dict.GetCountAsync(tx);
            if (count == 0)
            {
                // Seed some simulated nodes
                for (int i = 0; i < DefaultNodeCount; i++)
                {
                    await dict.AddAsync(tx, $"node-{i}", MaxUnitsPerNode);
                }
                await tx.CommitAsync();
                ServiceEventSource.Current.ServiceMessage(this.Context, $"CapacityService seeded {DefaultNodeCount} simulated nodes.");
            }
        }
    }

    public async Task<CapacityGrant> ReserveAsync(int requiredUnits, CancellationToken cancellationToken)
    {
        var dict = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, int>>("nodeCapacity");

        using (var tx = this.StateManager.CreateTransaction())
        {
            var enumerable = await dict.CreateEnumerableAsync(tx);
            using (var enumerator = enumerable.GetAsyncEnumerator())
            {
                while (await enumerator.MoveNextAsync(cancellationToken))
                {
                    var kvp = enumerator.Current;
                    if (kvp.Value >= requiredUnits)
                    {
                        await dict.SetAsync(tx, kvp.Key, kvp.Value - requiredUnits);
                        await tx.CommitAsync();
                        return new CapacityGrant { Granted = true, NodeId = kvp.Key };
                    }
                }
            }
        }

        return new CapacityGrant { Granted = false };
    }

    public async Task ReleaseAsync(string nodeId, int units, CancellationToken cancellationToken)
    {
        var dict = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, int>>("nodeCapacity");

        using (var tx = this.StateManager.CreateTransaction())
        {
            var current = await dict.TryGetValueAsync(tx, nodeId);
            if (current.HasValue)
            {
                var newValue = Math.Min(MaxUnitsPerNode, current.Value + units);
                await dict.SetAsync(tx, nodeId, newValue);
                await tx.CommitAsync();
            }
        }
    }

    public async Task<int> AvailableUnitsAsync(CancellationToken cancellationToken)
    {
        var dict = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, int>>("nodeCapacity");
        int total = 0;

        using (var tx = this.StateManager.CreateTransaction())
        {
            var enumerable = await dict.CreateEnumerableAsync(tx);
            using (var enumerator = enumerable.GetAsyncEnumerator())
            {
                while (await enumerator.MoveNextAsync(cancellationToken))
                {
                    total += enumerator.Current.Value;
                }
            }
        }

        return total;
    }
}

internal sealed class ServiceEventSource
{
    public static readonly ServiceEventSource Current = new ServiceEventSource();
    public void ServiceMessage(ServiceContext context, string message, params object[] args)
    {
        Console.WriteLine($"[CapacityService] {context.ServiceName} - {string.Format(message, args)}");
    }
}
