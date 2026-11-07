using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum EnemyAttackType
{
    Melee,
    Ranged
}

[RequireComponent(typeof(EnemyHealth))]
public class EnemyAI : MonoBehaviour
{
    [Header("Attack Mode")]
    [SerializeField] private EnemyAttackType attackType = EnemyAttackType.Ranged;
    [SerializeField] private float meleeDamage = 20f;
    [SerializeField] private float meleeRange = 2.0f;
    [SerializeField] private float meleeCooldown = 1.1f;
    [SerializeField] private float meleeWindup = 0.22f;

    [Header("Movement")]
    [SerializeField] private float detectionRange = 18f;
    [SerializeField] private float shootRange = 12f;
    [SerializeField] private float preferredRange = 8f;
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float turnSpeed = 10f;

    [Header("Ranged Attack")]
    [SerializeField] private float fireRate = 1.2f;
    [SerializeField] private float attackWindup = 0.25f;
    [SerializeField] private float projectileOffset = 1.2f;
    [SerializeField] private float projectileSpeed = 65f;
    [SerializeField] private float projectileDamage = 15f;
    [SerializeField] private float projectileLifeTime = 1.6f;
    [SerializeField] private Transform gunBarrel;
    [SerializeField] private GameObject bulletPrefab;

    [Header("Eye Laser")]
    [SerializeField] private Transform eye;
    [SerializeField] private float laserDuration = 1.2f;
    [SerializeField] private float laserRange = 20f;
    [SerializeField] private float laserDamageInterval = 0.2f;
    [SerializeField] private float eyeTrackingSpeed = 10f;
    [SerializeField] private float eyeSurfaceOffset = 0.35f;

    [Header("Legacy Animation States")]
    [SerializeField] private string idleState = "Idle";
    [SerializeField] private string walkState = "Walk";
    [SerializeField] private string attackState = "Attack";
    [SerializeField] private string hitState = "GetHit";
    [SerializeField] private string dieState = "Die";

    private static readonly int MoveX = Animator.StringToHash("MoveX");
    private static readonly int MoveY = Animator.StringToHash("MoveY");
    private static readonly int Speed = Animator.StringToHash("Speed");
    private static readonly int IsMoving = Animator.StringToHash("IsMoving");
    private static readonly int IsAlert = Animator.StringToHash("IsAlert");
    private static readonly int IsLaserActive = Animator.StringToHash("IsLaserActive");
    private static readonly int Attack = Animator.StringToHash("Attack");
    private static readonly int AttackIndex = Animator.StringToHash("AttackIndex");
    private static readonly int Hit = Animator.StringToHash("Hit");
    private static readonly int Dizzy = Animator.StringToHash("Dizzy");
    private static readonly int Die = Animator.StringToHash("Die");

    private readonly HashSet<int> animatorParameters = new HashSet<int>();
    private readonly RaycastHit[] laserHits = new RaycastHit[16];
    private Transform player;
    private NavMeshAgent agent;
    private Animator animator;
    private MonsterEffects effects;
    private float nextAttackTime, pendingShotTime, nextStrafeTime, nextPlayerSearchTime, alertLockUntil, laserEndTime, nextLaserDamageTime;
    private int strafeDirection = 1, hitCount;
    private bool alert, shotPending, laserActive, dead;
    private Vector3 eyeDirection;
    private string currentLegacyState;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>() ?? gameObject.AddComponent<NavMeshAgent>();
        agent.speed = moveSpeed;
        agent.stoppingDistance = attackType == EnemyAttackType.Melee ? Mathf.Max(0.8f, meleeRange * 0.6f) : 0f;
        agent.updateRotation = false;
        agent.radius = 0.85f;
        agent.height = 2.7f;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;

        animator = GetComponentInChildren<Animator>();
        effects = GetComponent<MonsterEffects>();
        if (eye == null) eye = FindChild("EyeBallCTRL") ?? FindChild("EyeCTRL");
        if (gunBarrel == null) gunBarrel = FindChild("MonsterMuzzle");
        eyeDirection = transform.forward;
        if (animator != null)
            foreach (AnimatorControllerParameter parameter in animator.parameters)
                animatorParameters.Add(parameter.nameHash);

        if (GetComponentInChildren<Collider>() == null)
        {
            CapsuleCollider body = gameObject.AddComponent<CapsuleCollider>();
            body.center = Vector3.up * 1.35f;
            body.height = 2.7f;
            body.radius = 0.8f;
        }
    }

    private void Start() => FindPlayer();

    private void Update()
    {
        if (dead || !ContractGame.ControlsAllowed) return;
        if (player == null)
        {
            if (Time.time >= nextPlayerSearchTime) FindPlayer();
            StopMoving();
            SetMovement(Vector2.zero, 0f);
            return;
        }

        float distance = Vector3.Distance(transform.position, player.position);
        if (distance > detectionRange) { LeaveCombat(); return; }

        EnterCombat();
        FacePlayer();
        if (attackType == EnemyAttackType.Ranged) UpdateEyeAim();

        if (laserActive)
        {
            UpdateLaser();
            StopMoving();
            SetMovement(Vector2.zero, 0f);
            return;
        }

        FirePendingShot();
        if (laserActive || Time.time < alertLockUntil)
        {
            StopMoving();
            SetMovement(Vector2.zero, 0f);
            return;
        }

        UpdateCombatMovement(distance);
        float currentAttackRange = attackType == EnemyAttackType.Melee ? meleeRange : shootRange;
        if (distance <= currentAttackRange && Time.time >= nextAttackTime) BeginAttack();
    }

    public void OnDamaged(float damage)
    {
        if (dead || damage <= 0f) return;
        EnterCombat();
        FacePlayer();
        hitCount++;
        if (!(hitCount % 4 == 0 ? SetTrigger(Dizzy) : SetTrigger(Hit))) PlayLegacy(hitState);
    }

    public void DieNow()
    {
        if (dead || !ContractGame.ControlsAllowed) return;
        dead = true;
        shotPending = false;
        StopLaser();
        StopMoving();
        SetMovement(Vector2.zero, 0f);
        if (!SetTrigger(Die)) PlayLegacy(dieState);
        if (agent != null) agent.enabled = false;
        enabled = false;
    }

    private void FindPlayer()
    {
        PlayerMotor motor = Object.FindFirstObjectByType<PlayerMotor>();
        player = motor != null ? motor.transform : GameObject.FindGameObjectWithTag("Player")?.transform;
        nextPlayerSearchTime = Time.time + 1f;
    }

    private void EnterCombat()
    {
        if (alert) return;
        alert = true;
        alertLockUntil = Time.time + 1.5f;
        SetBool(IsAlert, true);
    }

    private void LeaveCombat()
    {
        if (alert)
        {
            alert = false;
            shotPending = false;
            StopLaser();
            SetBool(IsAlert, false);
        }
        StopMoving();
        SetMovement(Vector2.zero, 0f);
        PlayLegacy(idleState);
    }

    private void UpdateCombatMovement(float distance)
    {
        if (attackType == EnemyAttackType.Melee) { UpdateMeleeMovement(distance); return; }

        Vector3 direction;
        float normalizedSpeed;
        if (distance > preferredRange + 1f)
        {
            direction = FlatDirectionToPlayer();
            normalizedSpeed = distance > preferredRange + 5f ? 1f : 0.6f;
        }
        else if (distance < preferredRange - 1.5f)
        {
            direction = -FlatDirectionToPlayer();
            normalizedSpeed = 0.55f;
        }
        else
        {
            if (Time.time >= nextStrafeTime)
            {
                strafeDirection = Random.value < 0.5f ? -1 : 1;
                nextStrafeTime = Time.time + Random.Range(1.4f, 2.8f);
            }
            direction = Vector3.Cross(Vector3.up, FlatDirectionToPlayer()) * strafeDirection;
            normalizedSpeed = 0.5f;
        }
        Move(direction, normalizedSpeed);
    }

    private void UpdateMeleeMovement(float distance)
    {
        if (distance <= meleeRange)
        {
            StopMoving();
            SetMovement(Vector2.zero, 0f);
            return;
        }

        float normalizedSpeed = distance > meleeRange + 4f ? 1f : 0.85f;
        float targetSpeed = moveSpeed * normalizedSpeed;
        if (agent.enabled && agent.isOnNavMesh)
        {
            agent.speed = targetSpeed;
            agent.isStopped = false;
            agent.SetDestination(player.position);
        }
        else
        {
            transform.position += FlatDirectionToPlayer() * targetSpeed * Time.deltaTime;
        }

        Vector3 moveDir = agent.enabled && agent.isOnNavMesh && agent.velocity.sqrMagnitude > 0.01f
            ? agent.velocity.normalized : FlatDirectionToPlayer();
        Vector3 localDirection = transform.InverseTransformDirection(moveDir);
        SetMovement(new Vector2(localDirection.x, localDirection.z), normalizedSpeed);
        PlayLegacy(walkState);
    }

    private void Move(Vector3 direction, float normalizedSpeed)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f) { StopMoving(); SetMovement(Vector2.zero, 0f); return; }
        direction.Normalize();
        float targetSpeed = moveSpeed * normalizedSpeed;
        if (agent.enabled && agent.isOnNavMesh)
        {
            agent.speed = targetSpeed;
            agent.isStopped = false;
            agent.SetDestination(transform.position + direction * 3f);
        }
        else transform.position += direction * targetSpeed * Time.deltaTime;

        Vector3 localDirection = transform.InverseTransformDirection(direction);
        SetMovement(new Vector2(localDirection.x, localDirection.z), normalizedSpeed);
        PlayLegacy(walkState);
    }

    private void StopMoving()
    {
        if (agent != null && agent.enabled && agent.isOnNavMesh) agent.isStopped = true;
    }

    private void FacePlayer()
    {
        Vector3 direction = FlatDirectionToPlayer();
        if (direction.sqrMagnitude >= 0.001f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), turnSpeed * Time.deltaTime);
    }

    private void UpdateEyeAim()
    {
        if (player == null) return;
        Vector3 eyePosition = eye != null ? eye.position : transform.position + Vector3.up * projectileOffset;
        Vector3 desiredDirection = player.position + Vector3.up - eyePosition;
        if (desiredDirection.sqrMagnitude < 0.001f) return;

        float blend = 1f - Mathf.Exp(-eyeTrackingSpeed * Time.deltaTime);
        eyeDirection = Vector3.Slerp(eyeDirection, desiredDirection.normalized, blend).normalized;
        if (gunBarrel != null)
            gunBarrel.SetPositionAndRotation(eyePosition + eyeDirection * eyeSurfaceOffset, Quaternion.LookRotation(eyeDirection));
    }

    private Vector3 FlatDirectionToPlayer()
    {
        if (player == null) return transform.forward;
        Vector3 direction = player.position - transform.position;
        direction.y = 0f;
        return direction.sqrMagnitude > 0.001f ? direction.normalized : transform.forward;
    }

    private void BeginAttack()
    {
        int attack = Random.Range(0, 3);
        SetInt(AttackIndex, attack);
        if (!SetTrigger(Attack)) PlayLegacy(attackState);

        shotPending = true;
        float windup = attackType == EnemyAttackType.Melee ? meleeWindup : attackWindup;
        pendingShotTime = Time.time + windup + attack * 0.04f;
        float rate = attackType == EnemyAttackType.Melee ? meleeCooldown : fireRate;
        nextAttackTime = Time.time + Mathf.Max(rate, windup + 0.1f);
    }

    private void FirePendingShot()
    {
        if (!shotPending || Time.time < pendingShotTime) return;
        shotPending = false;

        if (attackType == EnemyAttackType.Melee) { ExecuteMeleeHit(); return; }
        if (effects != null) { StartLaser(); return; }
        if (bulletPrefab == null) return;

        Vector3 position = gunBarrel != null ? gunBarrel.position : transform.position + Vector3.up * projectileOffset + transform.forward;
        Vector3 direction = (player.position + Vector3.up - position).normalized;
        GameObject projectile = Instantiate(bulletPrefab, position, Quaternion.LookRotation(direction));
        Bullet bullet = projectile.GetComponent<Bullet>() ?? projectile.AddComponent<Bullet>();
        bullet.Configure(projectileSpeed, projectileDamage, projectileLifeTime);
        bullet.SetOwner(transform);
    }

    private void ExecuteMeleeHit()
    {
        if (player == null || Vector3.Distance(transform.position, player.position) > meleeRange + 1.2f) return;
        PlayerHealth health = player.GetComponent<PlayerHealth>();
        if (health == null || health.IsDead) return;
        health.TakeDamage(meleeDamage);

        Vector3 hitPos = (transform.position + player.position) * 0.5f + Vector3.up * 1.2f;
        GameObject impactPrefab = Resources.Load<GameObject>("VFX/vfx_Impact_01");
        if (impactPrefab != null)
        {
            GameObject vfx = Instantiate(impactPrefab, hitPos, Quaternion.LookRotation((player.position - transform.position).normalized));
            vfx.transform.localScale = Vector3.one * 0.75f;
            Destroy(vfx, 1.2f);
        }
    }

    private void StartLaser()
    {
        laserActive = true;
        SetBool(IsLaserActive, true);
        laserEndTime = Time.time + Mathf.Max(laserDuration, 0.05f);
        nextLaserDamageTime = Time.time;
        nextAttackTime = laserEndTime + fireRate;
        TraceLaser(true);
    }

    private void UpdateLaser()
    {
        if (player == null || Time.time >= laserEndTime) { StopLaser(); return; }
        TraceLaser(false);
    }

    private void StopLaser()
    {
        if (!laserActive) return;
        laserActive = false;
        SetBool(IsLaserActive, false);
        effects?.EndLaser();
    }

    private void TraceLaser(bool firstFrame)
    {
        Vector3 origin = gunBarrel != null ? gunBarrel.position : (eye != null ? eye.position : transform.position + Vector3.up * projectileOffset);
        Vector3 direction = gunBarrel != null ? gunBarrel.forward : eyeDirection;
        if (direction.sqrMagnitude < 0.001f) direction = transform.forward;
        direction.Normalize();

        float maxDistance = Mathf.Max(laserRange, 0.5f);
        Vector3 endPoint = origin + direction * maxDistance;
        Vector3 hitNormal = -direction;
        Collider closestCollider = null;
        float closestDistance = maxDistance;

        int count = Physics.RaycastNonAlloc(origin, direction, laserHits, maxDistance, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = laserHits[i];
            if (hit.transform == transform || hit.transform.IsChildOf(transform) || hit.distance >= closestDistance) continue;
            closestDistance = hit.distance;
            closestCollider = hit.collider;
            endPoint = hit.point;
            hitNormal = hit.normal;
        }

        if (closestCollider != null && Time.time >= nextLaserDamageTime)
        {
            PlayerHealth health = closestCollider.GetComponentInParent<PlayerHealth>();
            if (health != null)
            {
                health.TakeDamage(projectileDamage);
                nextLaserDamageTime = Time.time + Mathf.Max(laserDamageInterval, 0.05f);
            }
        }

        if (firstFrame) effects.BeginLaser(origin, endPoint, hitNormal, closestCollider != null);
        else effects.UpdateLaser(origin, endPoint, hitNormal, closestCollider != null);
    }

    private Transform FindChild(string childName)
    {
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
            if (child.name == childName) return child;
        return null;
    }

    private void SetMovement(Vector2 direction, float speed)
    {
        SetFloat(MoveX, direction.x);
        SetFloat(MoveY, direction.y);
        SetFloat(Speed, speed);
        SetBool(IsMoving, speed > 0.05f);
    }

    private void SetFloat(int parameter, float value)
    {
        if (animator != null && animatorParameters.Contains(parameter))
            animator.SetFloat(parameter, value, 0.12f, Time.deltaTime);
    }

    private void SetInt(int parameter, int value)
    {
        if (animator != null && animatorParameters.Contains(parameter)) animator.SetInteger(parameter, value);
    }

    private void SetBool(int parameter, bool value)
    {
        if (animator != null && animatorParameters.Contains(parameter)) animator.SetBool(parameter, value);
    }

    private bool SetTrigger(int parameter)
    {
        if (animator == null || !animatorParameters.Contains(parameter)) return false;
        animator.SetTrigger(parameter);
        return true;
    }

    private void PlayLegacy(string state)
    {
        if (animator == null || animatorParameters.Contains(IsMoving) || currentLegacyState == state) return;
        int stateHash = Animator.StringToHash(state);
        if (!animator.HasState(0, stateHash)) return;
        currentLegacyState = state;
        animator.CrossFade(stateHash, 0.15f);
    }
}
