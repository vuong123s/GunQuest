using UnityEngine;
using UnityEngine.AI;

public class SearchState : BaseState
{
    private float endsAt;
    private float nextMoveTime;

    public override void Enter()
    {
        endsAt = Time.time + 8f;
        nextMoveTime = 0f;
        if (enemy.Agent.isOnNavMesh) enemy.Agent.SetDestination(enemy.LastKnownPosition);
    }

    public override void Perform()
    {
        if (enemy.CanSeePlayer())
        {
            stateMachine.ChangeState(new AttackState());
            return;
        }

        if (Time.time >= endsAt)
        {
            stateMachine.ChangeState(new PatrolState());
            return;
        }

        if (!enemy.Agent.isOnNavMesh || enemy.Agent.pathPending || enemy.Agent.remainingDistance >= 1f || Time.time < nextMoveTime) return;
        Vector3 position = enemy.LastKnownPosition + Random.insideUnitSphere * 8f;
        if (NavMesh.SamplePosition(position, out NavMeshHit hit, 8f, NavMesh.AllAreas)) enemy.Agent.SetDestination(hit.position);
        nextMoveTime = Time.time + 2.5f;
    }

    public override void Exit() { }
}
