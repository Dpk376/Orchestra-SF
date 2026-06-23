using Microsoft.ServiceFabric.Services.Remoting;
using Shared.Models;

namespace Shared.Contracts;

/// <summary>
/// Manages the lifecycle and state transitions of workloads in the reliable services platform.
/// </summary>
public interface IWorkloadManager : IService
{
    /// <summary>
    /// Submits a new workload to the orchestration platform.
    /// </summary>
    /// <param name="requiredCapacityUnits">The amount of capacity required to schedule this workload.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A unique identifier for the submitted workload.</returns>
    Task SubmitAsync(string workloadId, int requiredCapacityUnits, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves the current state of a workload.
    /// </summary>
    /// <param name="workloadId">The unique identifier of the workload.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The workload state, or null if it was not found.</returns>
    Task<Workload?> GetAsync(string workloadId, CancellationToken cancellationToken);

    /// <summary>
    /// Cancels a workload if it is not already in a terminal state.
    /// </summary>
    /// <param name="workloadId">The unique identifier of the workload.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>True if successfully cancelled, false if not found or already in a terminal state.</returns>
    Task<bool> CancelAsync(string workloadId, CancellationToken cancellationToken);
}
