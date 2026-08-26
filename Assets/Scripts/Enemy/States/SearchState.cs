using UnityEngine;
using UnityEngine.AI;

public class SearchState : BaseState
{
    private float searchTimer;
    private float moveTimer;

    public override void Enter()
    {
        searchTimer = 0f;
        moveTimer = 0f;

        // Move to last known player position
        if (enemy.LastKnownPosition != Vector3.zero)
        {
            if (enemy.Agent.isOnNavMesh)
            {
                enemy.Agent.SetDestination(enemy.LastKnownPosition);
            }
        }
    }

    public override void Perform()
    {
        // If player spotted again, immediately resume attack
        if (enemy.CanSeePlayer())
        {
            stateMachine.ChangeState(new AttackState());
            return;
        }

        searchTimer += Time.deltaTime;

        // When arrived near last known position, search around
        if (enemy.Agent.isOnNavMesh && !enemy.Agent.pathPending && enemy.Agent.remainingDistance < 1f)
        {
            moveTimer += Time.deltaTime;
            if (moveTimer > 2.5f)
            {
                Vector3 randomDirection = Random.insideUnitSphere * 8f;
                randomDirection += enemy.LastKnownPosition;

                if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, 8f, NavMesh.AllAreas))
                {
                    enemy.Agent.SetDestination(hit.position);
                }

                moveTimer = 0f;
            }
        }

        // If search timer exceeds 8 seconds without seeing player, return to patrol
        if (searchTimer > 8f)
        {
            stateMachine.ChangeState(new PatrolState());
        }
    }

    public override void Exit()
    {
    }
}
