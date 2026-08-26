using UnityEngine;

public class AttackState : BaseState
{
    private float moveTimer;
    private float losePlayerTimer;
    private float shotTimer;

    public override void Enter()
    {
        shotTimer = 0f;
        losePlayerTimer = 0f;
        moveTimer = 0f;
    }

    public override void Perform()
    {
        if (enemy.Player == null)
        {
            stateMachine.ChangeState(new PatrolState());
            return;
        }

        if (enemy.CanSeePlayer())
        {
            // Reset timers when player is visible
            losePlayerTimer = 0f;
            enemy.LastKnownPosition = enemy.Player.transform.position;

            // Rotate towards player smoothly
            Vector3 direction = (enemy.Player.transform.position - enemy.transform.position).normalized;
            direction.y = 0; // Keep horizontal rotation
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                enemy.transform.rotation = Quaternion.Slerp(enemy.transform.rotation, targetRotation, Time.deltaTime * 5f);
            }

            // Shoot interval
            shotTimer += Time.deltaTime;
            if (shotTimer >= enemy.fireRate)
            {
                Shoot();
                shotTimer = 0f;
            }

            // Reposition / move towards player occasionally
            moveTimer += Time.deltaTime;
            if (moveTimer > Random.Range(2f, 4f))
            {
                if (enemy.Agent.isOnNavMesh)
                {
                    enemy.Agent.SetDestination(enemy.Player.transform.position);
                }
                moveTimer = 0f;
            }
        }
        else
        {
            // Count up when line of sight is broken
            losePlayerTimer += Time.deltaTime;
            if (losePlayerTimer > 4f)
            {
                // Lost sight of player -> Transition to SearchState
                stateMachine.ChangeState(new SearchState());
            }
        }
    }

    public override void Exit()
    {
    }

    public void Shoot()
    {
        if (enemy.bulletPrefab == null)
        {
            return;
        }

        Transform spawnPoint = enemy.gunBarrel != null ? enemy.gunBarrel : enemy.transform;
        Vector3 aimTarget = enemy.Player.transform.position + Vector3.up; // Aim at player chest
        Vector3 shootDirection = (aimTarget - spawnPoint.position).normalized;

        GameObject bulletObj = Object.Instantiate(enemy.bulletPrefab, spawnPoint.position, Quaternion.LookRotation(shootDirection));
        if (bulletObj.TryGetComponent<Bullet>(out Bullet bullet))
        {
            bullet.SetOwner(enemy.transform);
        }
    }
}
