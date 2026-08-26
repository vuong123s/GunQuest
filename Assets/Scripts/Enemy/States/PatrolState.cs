using UnityEngine;

public class PatrolState : BaseState
{
    private int waypointIndex = 0;

    public override void Enter()
    {
        if (enemy.path != null && enemy.path.waypoints.Count > 0)
        {
            if (enemy.Agent.isOnNavMesh)
            {
                enemy.Agent.SetDestination(enemy.path.waypoints[waypointIndex].position);
            }
        }
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
        if (enemy.path == null || enemy.path.waypoints.Count == 0)
        {
            return;
        }

        // Check if agent reached current waypoint
        if (enemy.Agent.isOnNavMesh && !enemy.Agent.pathPending && enemy.Agent.remainingDistance < 0.8f)
        {
            waypointIndex = (waypointIndex + 1) % enemy.path.waypoints.Count;
            if (enemy.path.waypoints[waypointIndex] != null)
            {
                enemy.Agent.SetDestination(enemy.path.waypoints[waypointIndex].position);
            }
        }
    }
}
