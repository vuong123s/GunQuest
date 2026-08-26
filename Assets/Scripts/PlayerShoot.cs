using UnityEngine;

public class PlayerShoot : MonoBehaviour
{
    [SerializeField]
    private GameObject bulletPrefab;
    [SerializeField]
    private Transform firePoint;
    [SerializeField]
    private Camera aimCamera;
    [SerializeField]
    private float fireRate = 0.18f;
    [SerializeField]
    private float spawnDistance = 0.8f;
    [SerializeField]
    private float aimDistance = 100f;
    [SerializeField]
    private LayerMask aimMask = ~0;

    private float nextFireTime;

    void Awake()
    {
        if (aimCamera == null)
        {
            aimCamera = GetComponentInChildren<Camera>();
        }

        if (aimCamera == null)
        {
            aimCamera = Camera.main;
        }
    }

    public void Shoot()
    {
        if (Time.time < nextFireTime)
        {
            return;
        }

        nextFireTime = Time.time + fireRate;

        Transform origin = firePoint != null ? firePoint : aimCamera != null ? aimCamera.transform : transform;
        Vector3 shootDirection = GetShootDirection(origin);
        Vector3 spawnPosition = origin.position + shootDirection * spawnDistance;
        GameObject bulletObject = CreateBullet(spawnPosition, Quaternion.LookRotation(shootDirection));

        if (bulletObject.TryGetComponent<Bullet>(out Bullet bullet))
        {
            bullet.SetOwner(transform.root);
        }
    }

    private Vector3 GetShootDirection(Transform origin)
    {
        if (aimCamera == null)
        {
            return origin.forward;
        }

        Ray ray = new Ray(aimCamera.transform.position, aimCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, aimDistance, aimMask, QueryTriggerInteraction.Ignore))
        {
            return (hit.point - origin.position).normalized;
        }

        return aimCamera.transform.forward;
    }

    private GameObject CreateBullet(Vector3 position, Quaternion rotation)
    {
        if (bulletPrefab != null)
        {
            return Instantiate(bulletPrefab, position, rotation);
        }

        GameObject bulletObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bulletObject.name = "PlayerBullet";
        bulletObject.transform.SetPositionAndRotation(position, rotation);
        bulletObject.transform.localScale = Vector3.one * 0.18f;

        Collider bulletCollider = bulletObject.GetComponent<Collider>();
        bulletCollider.isTrigger = true;

        bulletObject.AddComponent<Bullet>();
        return bulletObject;
    }
}
