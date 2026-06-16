using System.Runtime.Serialization;

namespace Shared.Models;

/// <summary>
/// Represents a unit of work that requires scheduling and execution in the cluster.
/// </summary>
[DataContract]
public class Workload
{
    /// <summary>
    /// The unique identifier for this workload.
    /// </summary>
    [DataMember]
    public string WorkloadId { get; set; } = string.Empty;

    /// <summary>
    /// The current state of the workload in the orchestration lifecycle.
    /// </summary>
    [DataMember]
    public WorkloadState State { get; set; }

    /// <summary>
    /// The amount of capacity required by this workload to run.
    /// </summary>
    [DataMember]
    public int RequiredCapacityUnits { get; set; }

    /// <summary>
    /// The ID of the node assigned to execute this workload, if any.
    /// </summary>
    [DataMember]
    public string AssignedNodeId { get; set; } = string.Empty;

    /// <summary>
    /// The timestamp when the workload was originally submitted.
    /// </summary>
    [DataMember]
    public DateTime CreatedUtc { get; set; }

    /// <summary>
    /// The timestamp of the last state transition or update.
    /// </summary>
    [DataMember]
    public DateTime UpdatedUtc { get; set; }

    /// <summary>
    /// The number of attempts made to schedule this workload.
    /// </summary>
    [DataMember]
    public int Attempts { get; set; }
}
