using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private float speed = 35f;
    [SerializeField] private float damage = 15f;
    [SerializeField] private float lifeTime = 5f;

    private Transform owner;

    private void Awake()
    {
        Collider bulletCollider = GetComponent<Collider>();
        if (bulletCollider != null) bulletCollider.isTrigger = true;
        Rigidbody body = GetComponent<Rigidbody>();
        if (body == null) body = gameObject.AddComponent<Rigidbody>();
        body.useGravity = false;
        body.isKinematic = true;
    }

    private void Start() => Destroy(gameObject, lifeTime);
    private void Update() => transform.position += transform.forward * speed * Time.deltaTime;

    private static GameObject hitVfxPrefab;

    private void SpawnHitVfx(Vector3 pos)
    {
        if (hitVfxPrefab == null) hitVfxPrefab = Resources.Load<GameObject>("VFX/vfx_Impact_01");
        if (hitVfxPrefab != null)
        {
            GameObject fx = Instantiate(hitVfxPrefab, pos, Quaternion.identity);
            fx.transform.localScale = Vector3.one * 0.7f;
            Destroy(fx, 1.5f);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (owner != null && (other.transform == owner || other.transform.IsChildOf(owner))) return;
        bool firedByPlayer = owner != null && owner.GetComponent<PlayerMotor>() != null;

        if (!firedByPlayer && other.TryGetComponent<PlayerHealth>(out PlayerHealth player))
        {
            player.TakeDamage(damage);
            SpawnHitVfx(transform.position);
            Destroy(gameObject);
        }
        else if (firedByPlayer && other.GetComponentInParent<EnemyHealth>() is EnemyHealth enemy)
        {
            enemy.TakeDamage(damage, transform.position);
            SpawnHitVfx(transform.position);
            Destroy(gameObject);
        }
        else if (!other.isTrigger)
        {
            SpawnHitVfx(transform.position);
            Destroy(gameObject);
        }
    }

    public void SetOwner(Transform value)
    {
        owner = value;
        Collider bulletCollider = GetComponent<Collider>();
        if (bulletCollider == null || owner == null) return;
        foreach (Collider ownerCollider in owner.GetComponentsInChildren<Collider>()) Physics.IgnoreCollision(bulletCollider, ownerCollider, true);
    }

    public void Configure(float newSpeed, float newDamage, float newLifeTime)
    {
        speed = Mathf.Max(0f, newSpeed);
        damage = Mathf.Max(0f, newDamage);
        lifeTime = Mathf.Max(0.05f, newLifeTime);
    }
}
