using System.Runtime.Serialization;

namespace Shared.Models;

/// <summary>
/// Represents the outcome of a capacity reservation request.
/// </summary>
[DataContract]
public class CapacityGrant
{
    /// <summary>
    /// True if the required capacity was successfully reserved.
    /// </summary>
    [DataMember]
    public bool Granted { get; set; }

    /// <summary>
    /// The ID of the node where the capacity was reserved.
    /// </summary>
    [DataMember]
    public string NodeId { get; set; } = string.Empty;
}
