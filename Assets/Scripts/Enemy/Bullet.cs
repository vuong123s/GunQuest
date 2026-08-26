using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField]
    private float speed = 35f;
    [SerializeField]
    private float damage = 15f;
    [SerializeField]
    private float lifeTime = 5f;

    private Transform owner;

    void Awake()
    {
        Collider bulletCollider = GetComponent<Collider>();
        if (bulletCollider != null)
        {
            bulletCollider.isTrigger = true;
        }

        Rigidbody body = GetComponent<Rigidbody>();
        if (body == null)
        {
            body = gameObject.AddComponent<Rigidbody>();
        }

        body.useGravity = false;
        body.isKinematic = true;
    }

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        transform.position += transform.forward * speed * Time.deltaTime;
    }

    void OnTriggerEnter(Collider other)
    {
        if (owner != null && (other.transform == owner || other.transform.IsChildOf(owner)))
        {
            return;
        }

        if (other.TryGetComponent<PlayerHealth>(out PlayerHealth health))
        {
            health.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        Enemy enemy = other.GetComponentInParent<Enemy>();
        if (enemy != null)
        {
            Destroy(enemy.gameObject);
            Destroy(gameObject);
            return;
        }

        // Don't collide with other bullets or triggers unless environment
        if (!other.isTrigger)
        {
            Destroy(gameObject);
        }
    }

    public void SetOwner(Transform newOwner)
    {
        owner = newOwner;
    }
}
