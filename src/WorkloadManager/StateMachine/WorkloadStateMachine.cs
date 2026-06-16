using Shared.Models;

namespace WorkloadManager.StateMachine;

public static class WorkloadStateMachine
{
    public const int MaxRetryAttempts = 5;

    public static (Workload nextState, bool requeue) Advance(Workload current, CapacityGrant grant)
    {
        var next = new Workload
        {
            WorkloadId = current.WorkloadId,
            State = current.State,
            RequiredCapacityUnits = current.RequiredCapacityUnits,
            AssignedNodeId = current.AssignedNodeId,
            CreatedUtc = current.CreatedUtc,
            UpdatedUtc = DateTime.UtcNow,
            Attempts = current.Attempts + 1
        };

        if (current.State == WorkloadState.Submitted)
        {
            if (grant.Granted)
            {
                next.State = WorkloadState.Scheduled;
                next.AssignedNodeId = grant.NodeId;
                return (next, true); // Requeue to move to Running
            }
            else
            {
                if (next.Attempts >= MaxRetryAttempts)
                {
                    next.State = WorkloadState.Failed;
                    return (next, false);
                }
                return (next, true); // Requeue to try again
            }
        }

        if (current.State == WorkloadState.Scheduled)
        {
            next.State = WorkloadState.Running;
            return (next, true); // Requeue to simulate running/completion
        }

        if (current.State == WorkloadState.Running)
        {
            // Simulate completion after it runs
            next.State = WorkloadState.Completed;
            return (next, false);
        }

        return (current, false);
    }
}
