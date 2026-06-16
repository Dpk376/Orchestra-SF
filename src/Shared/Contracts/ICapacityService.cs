using Microsoft.ServiceFabric.Services.Remoting;
using Shared.Models;

namespace Shared.Contracts;

/// <summary>
/// Provides cluster-wide capacity tracking and reservation capabilities.
/// </summary>
public interface ICapacityService : IService
{
    /// <summary>
    /// Attempts to reserve capacity units for a workload.
    /// </summary>
    /// <param name="requiredUnits">The number of units required.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A grant containing the reservation status and the assigned node.</returns>
    Task<CapacityGrant> ReserveAsync(int requiredUnits, CancellationToken cancellationToken);

    /// <summary>
    /// Releases previously reserved capacity back to a node.
    /// </summary>
    /// <param name="nodeId">The identifier of the node.</param>
    /// <param name="units">The number of units to release.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    Task ReleaseAsync(string nodeId, int units, CancellationToken cancellationToken);

    /// <summary>
    /// Calculates the total available units across all nodes.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The total available units.</returns>
    Task<int> AvailableUnitsAsync(CancellationToken cancellationToken);
}
