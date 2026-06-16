using Shared.Models;
using WorkloadManager.StateMachine;
using Xunit;

namespace Platform.Tests;

public class StateMachineTests
{
    [Fact]
    public void Advance_Submitted_Granted_MovesToScheduled()
    {
        var workload = new Workload { State = WorkloadState.Submitted, Attempts = 0 };
        var grant = new CapacityGrant { Granted = true, NodeId = "node-1" };

        var (next, requeue) = WorkloadStateMachine.Advance(workload, grant);

        Assert.Equal(WorkloadState.Scheduled, next.State);
        Assert.Equal("node-1", next.AssignedNodeId);
        Assert.True(requeue);
        Assert.Equal(1, next.Attempts);
    }

    [Fact]
    public void Advance_Submitted_Denied_Retries()
    {
        var workload = new Workload { State = WorkloadState.Submitted, Attempts = 0 };
        var grant = new CapacityGrant { Granted = false };

        var (next, requeue) = WorkloadStateMachine.Advance(workload, grant);

        Assert.Equal(WorkloadState.Submitted, next.State);
        Assert.True(requeue);
        Assert.Equal(1, next.Attempts);
    }

    [Fact]
    public void Advance_Submitted_Denied_MaxAttempts_Fails()
    {
        var workload = new Workload { State = WorkloadState.Submitted, Attempts = 4 };
        var grant = new CapacityGrant { Granted = false };

        var (next, requeue) = WorkloadStateMachine.Advance(workload, grant);

        Assert.Equal(WorkloadState.Failed, next.State);
        Assert.False(requeue);
        Assert.Equal(5, next.Attempts);
    }

    [Fact]
    public void Advance_Scheduled_MovesToRunning()
    {
        var workload = new Workload { State = WorkloadState.Scheduled, AssignedNodeId = "node-1" };
        var grant = new CapacityGrant { Granted = false };

        var (next, requeue) = WorkloadStateMachine.Advance(workload, grant);

        Assert.Equal(WorkloadState.Running, next.State);
        Assert.True(requeue);
    }

    [Fact]
    public void Advance_Running_MovesToCompleted()
    {
        var workload = new Workload { State = WorkloadState.Running, AssignedNodeId = "node-1" };
        var grant = new CapacityGrant { Granted = false };

        var (next, requeue) = WorkloadStateMachine.Advance(workload, grant);

        Assert.Equal(WorkloadState.Completed, next.State);
        Assert.False(requeue);
    }
}
