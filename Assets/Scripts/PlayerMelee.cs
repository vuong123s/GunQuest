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
        damage = attackDamage; attackCooldown = cooldown; attackRange = range;
    }
    public bool Attack()
    {
        var health = GetComponent<PlayerHealth>();
        if (!ContractGame.ControlsAllowed || (health != null && health.IsDead) || Time.time < nextAttackTime) return false;
        nextAttackTime = Time.time + attackCooldown;
        var camera = Camera.main;
        Vector3 origin = camera != null ? camera.transform.position : transform.position + Vector3.up;
        Vector3 direction = camera != null ? camera.transform.forward : transform.forward;
        var hits = Physics.SphereCastAll(origin, attackRadius * 0.4f, direction, attackRange, targetMask, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var hit in hits)
        {
            if (hit.transform == transform || hit.transform.IsChildOf(transform)) continue;
            // The nearest wall or body blocks the swing; cannot damage through terrain/cover.
            var enemy = hit.collider.GetComponentInParent<EnemyHealth>();
            if (enemy != null) enemy.TakeDamage(damage, hit.point);
            break;
        }
        return true;
    }
}
