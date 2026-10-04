using UnityEngine;

public class PatrolState : BaseState
{
    private int waypointIndex = 0;

    public override void Enter()
    {
        SetDestination();
    }

    public override void Perform()
    {
        PatrolCycle();

        if (enemy.CanSeePlayer())
        {
            stateMachine.ChangeState(new AttackState());
        }
    }

    public override void Exit()
    {
    }

    public void PatrolCycle()
    {
        if (enemy.path == null || enemy.path.waypoints.Count == 0 || !enemy.Agent.isOnNavMesh || enemy.Agent.pathPending || enemy.Agent.remainingDistance >= 0.8f) return;
        waypointIndex = (waypointIndex + 1) % enemy.path.waypoints.Count;
        SetDestination();
    }

    private void SetDestination()
    {
        if (enemy.path == null || enemy.path.waypoints.Count == 0 || !enemy.Agent.isOnNavMesh) return;
        Transform waypoint = enemy.path.waypoints[waypointIndex];
        if (waypoint != null) enemy.Agent.SetDestination(waypoint.position);
    }
}
