using UnityEngine;

public class AttackState : BaseState
{
    private float lostSightSince;
    private float nextMoveTime;
    private float nextShotTime;

    public override void Enter()
    {
        lostSightSince = 0f;
        nextMoveTime = 0f;
        nextShotTime = 0f;
    }

    public override void Perform()
    {
        if (enemy.Player == null)
        {
            stateMachine.ChangeState(new PatrolState());
            return;
        }

        if (!enemy.CanSeePlayer())
        {
            if (lostSightSince == 0f) lostSightSince = Time.time;
            if (Time.time - lostSightSince > 4f) stateMachine.ChangeState(new SearchState());
            return;
        }

        lostSightSince = 0f;
        enemy.LastKnownPosition = enemy.Player.transform.position;
        FacePlayer();

        if (Time.time >= nextShotTime)
        {
            Shoot();
            nextShotTime = Time.time + enemy.fireRate;
        }

        if (enemy.Agent.isOnNavMesh && Time.time >= nextMoveTime)
        {
            enemy.Agent.SetDestination(enemy.Player.transform.position);
            nextMoveTime = Time.time + Random.Range(2f, 4f);
        }
    }

    public override void Exit() { }

    private void FacePlayer()
    {
        Vector3 direction = enemy.Player.transform.position - enemy.transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.001f)
            enemy.transform.rotation = Quaternion.Slerp(enemy.transform.rotation, Quaternion.LookRotation(direction), 5f * Time.deltaTime);
    }

    private void Shoot()
    {
        if (enemy.bulletPrefab == null) return;
        Transform muzzle = enemy.gunBarrel != null ? enemy.gunBarrel : enemy.transform;
        Vector3 direction = (enemy.Player.transform.position + Vector3.up - muzzle.position).normalized;
        GameObject bullet = Object.Instantiate(enemy.bulletPrefab, muzzle.position, Quaternion.LookRotation(direction));
        bullet.GetComponent<Bullet>()?.SetOwner(enemy.transform);
    }
}
