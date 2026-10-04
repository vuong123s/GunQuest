using UnityEngine;

public class PlayerMelee : MonoBehaviour
{
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackRadius = 0.65f;
    [SerializeField] private float damage = 35f;
    [SerializeField] private float attackCooldown = 0.55f;
    [SerializeField] private LayerMask targetMask = ~0;

    private float nextAttackTime;

    public void Configure(float attackDamage, float cooldown, float range)
    {
        damage = attackDamage;
        attackCooldown = cooldown;
        attackRange = range;
    }

    public bool Attack()
    {
        if (Time.time < nextAttackTime) return false;
        nextAttackTime = Time.time + attackCooldown;
        Vector3 center = transform.position + Vector3.up + transform.forward * attackRange;
        foreach (Collider hit in Physics.OverlapSphere(center, attackRadius, targetMask, QueryTriggerInteraction.Ignore))
        {
            EnemyHealth enemy = hit.GetComponentInParent<EnemyHealth>();
            if (enemy != null) { enemy.TakeDamage(damage); break; }
        }
        return true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + Vector3.up + transform.forward * attackRange, attackRadius);
    }
}
