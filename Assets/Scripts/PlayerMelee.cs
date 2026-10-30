using UnityEngine;

public class PlayerMelee : MonoBehaviour
{
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackRadius = 0.65f;
    [SerializeField] private float damage = 35f;
    [SerializeField] private float attackCooldown = 0.55f;
    [SerializeField] private LayerMask targetMask = ~0;
    private float nextAttackTime;
    private float attackStartedAt;
    private float strikeDamage, strikeRange;
    private bool impactApplied;
    private MeleeAttackTiming timing;
    private PlayerHealth health;
    public bool IsAttacking { get; private set; }
    public float AttackElapsed => IsAttacking ? Mathf.Max(0f, Time.time - attackStartedAt) : 0f;

    private void Awake() => health = GetComponent<PlayerHealth>();

    public void Configure(float attackDamage, float cooldown, float range)
    {
        damage = attackDamage; attackCooldown = cooldown; attackRange = range;
    }

    public bool Attack() => Attack(MeleeAttackTiming.Quick);

    public bool Attack(MeleeAttackTiming attackTiming)
    {
        if (!isActiveAndEnabled || !ContractGame.ControlsAllowed || (health != null && health.IsDead) ||
            IsAttacking || Time.time < nextAttackTime) return false;
        timing = attackTiming;
        attackStartedAt = Time.time;
        strikeDamage = damage; strikeRange = attackRange;
        nextAttackTime = attackStartedAt + Mathf.Max(attackCooldown, timing.Duration);
        impactApplied = false; IsAttacking = true;
        return true;
    }

    private void Update()
    {
        if (!IsAttacking) return;
        if (health != null && health.IsDead) { CancelAttack(); return; }
        if (!ContractGame.ControlsAllowed)
        {
            // A pause keeps the pose and pending strike; a finished contract cancels it.
            if (ContractGame.Instance == null || ContractGame.Instance.State != ContractGame.Phase.Paused) CancelAttack();
            return;
        }
        if (Time.timeScale <= 0f) return;
        float elapsed = AttackElapsed;
        if (!impactApplied && elapsed >= timing.ImpactTime)
        {
            impactApplied = true;
            ApplyImpact();
        }
        if (elapsed >= timing.Duration) IsAttacking = false;
    }

    private void ApplyImpact()
    {
        var camera = Camera.main;
        Vector3 origin = camera != null ? camera.transform.position : transform.position + Vector3.up;
        Vector3 direction = camera != null ? camera.transform.forward : transform.forward;
        var hits = Physics.SphereCastAll(origin, attackRadius * 0.4f, direction, strikeRange, targetMask, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var hit in hits)
        {
            if (hit.transform == transform || hit.transform.IsChildOf(transform)) continue;
            // The nearest wall or body blocks the swing; cannot damage through terrain/cover.
            var enemy = hit.collider.GetComponentInParent<EnemyHealth>();
            if (enemy != null) enemy.TakeDamage(strikeDamage, hit.point);
            break;
        }
    }

    public void CancelAttack()
    {
        IsAttacking = false; impactApplied = false;
    }

    private void OnDisable() => CancelAttack();
}
