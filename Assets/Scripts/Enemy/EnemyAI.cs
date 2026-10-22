using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(EnemyHealth))]
public class EnemyAI : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float detectionRange = 18f;
    [SerializeField] private float shootRange = 12f;
    [SerializeField] private float preferredRange = 8f;
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float turnSpeed = 10f;

    [Header("Attack")]
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
    private static readonly int Taunt = Animator.StringToHash("Taunt");
    private static readonly int Victory = Animator.StringToHash("Victory");

    private readonly HashSet<int> animatorParameters = new HashSet<int>();
    private readonly RaycastHit[] laserHits = new RaycastHit[16];
    private Transform player;
    private NavMeshAgent agent;
    private Animator animator;
    private MonsterEffects effects;
    private float nextAttackTime;
    private float pendingShotTime;
    private float nextStrafeTime;
    private float nextPlayerSearchTime;
    private float alertLockUntil;
    private float laserEndTime;
    private float nextLaserDamageTime;
    private int strafeDirection = 1;
    private int hitCount;
    private bool alert;
    private bool shotPending;
    private bool laserActive;
    private bool dead;
    private Vector3 eyeDirection;
    private string currentLegacyState;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null) agent = gameObject.AddComponent<NavMeshAgent>();
        agent.speed = moveSpeed;
        agent.stoppingDistance = 0f;
        agent.updateRotation = false;

        animator = GetComponentInChildren<Animator>();
        effects = GetComponent<MonsterEffects>();
        if (eye == null) eye = FindChild("EyeBallCTRL");
        if (eye == null) eye = FindChild("EyeCTRL");
        if (gunBarrel == null) gunBarrel = FindChild("MonsterMuzzle");
        eyeDirection = transform.forward;
        CacheAnimatorParameters();

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
        if (distance > detectionRange)
        {
            LeaveCombat();
            return;
        }

        EnterCombat();
        FacePlayer();
        UpdateEyeAim();

        if (laserActive)
        {
            UpdateLaser();
            StopMoving();
            SetMovement(Vector2.zero, 0f);
            return;
        }

        FirePendingShot();
        if (laserActive)
        {
            StopMoving();
            SetMovement(Vector2.zero, 0f);
            return;
        }

        if (Time.time < alertLockUntil)
        {
            StopMoving();
            SetMovement(Vector2.zero, 0f);
            return;
        }

        UpdateCombatMovement(distance);
        if (distance <= shootRange && Time.time >= nextAttackTime) BeginAttack();
    }

    public void OnDamaged(float damage)
    {
        if (dead || damage <= 0f) return;
        EnterCombat();
        FacePlayer();
        hitCount++;

        bool played = hitCount % 4 == 0 ? SetTrigger(Dizzy) : SetTrigger(Hit);
        if (!played) PlayLegacy(hitState);
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

    public void PlayTaunt()
    {
        if (!dead) SetTrigger(Taunt);
    }

    public void PlayVictory()
    {
        if (!dead) SetTrigger(Victory);
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

    private void Move(Vector3 direction, float normalizedSpeed)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f)
        {
            StopMoving();
            SetMovement(Vector2.zero, 0f);
            return;
        }

        direction.Normalize();
        float targetSpeed = moveSpeed * normalizedSpeed;
        if (agent.enabled && agent.isOnNavMesh)
        {
            agent.speed = targetSpeed;
            agent.isStopped = false;
            agent.SetDestination(transform.position + direction * 3f);
        }
        else
        {
            transform.position += direction * targetSpeed * Time.deltaTime;
        }

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
        if (direction.sqrMagnitude < 0.001f) return;
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
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
        {
            gunBarrel.SetPositionAndRotation(
                eyePosition + eyeDirection * eyeSurfaceOffset,
                Quaternion.LookRotation(eyeDirection));
        }
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
        pendingShotTime = Time.time + attackWindup + attack * 0.04f;
        nextAttackTime = Time.time + Mathf.Max(fireRate, attackWindup + 0.1f);
    }

    private void FirePendingShot()
    {
        if (!shotPending || Time.time < pendingShotTime) return;
        shotPending = false;

        Transform muzzle = gunBarrel != null ? gunBarrel : transform;
        Vector3 position = gunBarrel != null
            ? muzzle.position
            : transform.position + Vector3.up * projectileOffset + transform.forward;
        Vector3 direction = (player.position + Vector3.up - position).normalized;

        if (effects != null)
        {
            StartLaser();
            return;
        }

        GameObject projectile = bulletPrefab != null
            ? Instantiate(bulletPrefab, position, Quaternion.LookRotation(direction))
            : CreateProjectile(position, direction);

        Bullet bullet = projectile.GetComponent<Bullet>();
        if (bullet == null) bullet = projectile.AddComponent<Bullet>();
        bullet.Configure(projectileSpeed, projectileDamage, projectileLifeTime);
        bullet.SetOwner(transform);
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
        if (player == null || Time.time >= laserEndTime)
        {
            StopLaser();
            return;
        }

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
        Vector3 origin = gunBarrel != null
            ? gunBarrel.position
            : (eye != null ? eye.position : transform.position + Vector3.up * projectileOffset);
        Vector3 direction = gunBarrel != null ? gunBarrel.forward : eyeDirection;
        if (direction.sqrMagnitude < 0.001f) direction = transform.forward;
        direction.Normalize();

        float maxDistance = Mathf.Max(laserRange, 0.5f);
        Vector3 endPoint = origin + direction * maxDistance;
        Vector3 hitNormal = -direction;
        Collider closestCollider = null;
        float closestDistance = maxDistance;

        int count = Physics.RaycastNonAlloc(
            origin,
            direction,
            laserHits,
            maxDistance,
            ~0,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = laserHits[i];
            if (hit.transform == transform || hit.transform.IsChildOf(transform) || hit.distance >= closestDistance) continue;
            closestDistance = hit.distance;
            closestCollider = hit.collider;
            endPoint = hit.point;
            hitNormal = hit.normal;
        }

        if (closestCollider != null)
        {
            PlayerHealth health = closestCollider.GetComponentInParent<PlayerHealth>();
            if (health != null && Time.time >= nextLaserDamageTime)
            {
                health.TakeDamage(projectileDamage);
                nextLaserDamageTime = Time.time + Mathf.Max(laserDamageInterval, 0.05f);
            }
        }

        if (firstFrame)
        {
            effects.BeginLaser(origin, endPoint, hitNormal, closestCollider != null);
        }
        else
        {
            effects.UpdateLaser(origin, endPoint, hitNormal, closestCollider != null);
        }
    }

    private Transform FindChild(string childName)
    {
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (child.name == childName) return child;
        }

        return null;
    }

    private static GameObject CreateProjectile(Vector3 position, Vector3 direction)
    {
        GameObject projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        projectile.name = "EnemyBullet";
        projectile.transform.SetPositionAndRotation(position, Quaternion.LookRotation(direction));
        projectile.transform.localScale = Vector3.one * 0.22f;
        projectile.GetComponent<Collider>().isTrigger = true;
        projectile.AddComponent<Bullet>();
        return projectile;
    }

    private void CacheAnimatorParameters()
    {
        if (animator == null) return;
        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            animatorParameters.Add(parameter.nameHash);
        }
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
        {
            animator.SetFloat(parameter, value, 0.12f, Time.deltaTime);
        }
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
