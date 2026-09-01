using UnityEngine;

public class PlayerMelee : MonoBehaviour
{
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackRadius = 0.65f;
    [SerializeField] private float attackCooldown = 0.55f;
    [SerializeField] private LayerMask targetMask = ~0;

    private float nextAttackTime;

    public bool Attack()
    {
        if (Time.time < nextAttackTime)
        {
            return false;
        }

        nextAttackTime = Time.time + attackCooldown;
        Vector3 center = transform.position + Vector3.up + transform.forward * attackRange;
        Collider[] targets = Physics.OverlapSphere(center, attackRadius, targetMask, QueryTriggerInteraction.Ignore);

        foreach (Collider target in targets)
        {
            Enemy enemy = target.GetComponentInParent<Enemy>();
            if (enemy != null)
            {
                Destroy(enemy.gameObject);
                break;
            }
        }

        return true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + Vector3.up + transform.forward * attackRange, attackRadius);
    }
}
