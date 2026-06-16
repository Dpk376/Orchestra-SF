namespace Shared.Models;

/// <summary>
/// Defines the lifecycle stages of a workload in the orchestration platform.
/// </summary>
public enum WorkloadState
{
    /// <summary>
    /// The workload has been submitted but has not yet acquired capacity.
    /// </summary>
    Submitted,

    /// <summary>
    /// Capacity has been reserved, and the workload is scheduled on a node.
    /// </summary>
    Scheduled,

    /// <summary>
    /// The workload is currently executing on the assigned node.
    /// </summary>
    Running,

    /// <summary>
    /// The workload completed successfully and released its capacity.
    /// </summary>
    Completed,

    /// <summary>
    /// The workload failed to schedule or execute after maximum retries.
    /// </summary>
    Failed
}
