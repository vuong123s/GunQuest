using UnityEngine;

public class PlayerShoot : MonoBehaviour
{
    private const string GeneratedFirePointName = "FirePoint";

    [SerializeField]
    private GameObject bulletPrefab;
    [SerializeField]
    private Transform firePoint;
    [SerializeField]
    private string[] firePointNames = { "FirePoint", "Muzzle", "GunBarrel" };
    [SerializeField]
    private string[] weaponNames = { "Weapon_R", "Weapon", "Gun", "AssaultRifle", "Pistol" };
    [SerializeField]
    private Camera aimCamera;
    [SerializeField]
    private float fireRate = 0.18f;
    [SerializeField]
    private float spawnDistance = 0.45f;
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

        ResolveFirePoint();
    }

    public bool Shoot()
    {
        if (Time.time < nextFireTime)
        {
            return false;
        }

        nextFireTime = Time.time + fireRate;

        if (firePoint == null)
        {
            ResolveFirePoint();
        }

        Transform origin = firePoint != null ? firePoint : aimCamera != null ? aimCamera.transform : transform;
        Vector3 shootDirection = GetShootDirection(origin);
        Vector3 spawnPosition = origin.position + shootDirection * spawnDistance;
        GameObject bulletObject = CreateBullet(spawnPosition, Quaternion.LookRotation(shootDirection));

        if (bulletObject.TryGetComponent<Bullet>(out Bullet bullet))
        {
            bullet.SetOwner(transform.root);
        }

        return true;
    }

    private void ResolveFirePoint()
    {
        firePoint = FindFirstChildByNames(firePointNames);
        if (firePoint != null)
        {
            return;
        }

        Transform weapon = FindFirstChildByNames(weaponNames);
        if (weapon == null)
        {
            return;
        }

        GameObject generatedFirePoint = new GameObject(GeneratedFirePointName);
        generatedFirePoint.transform.SetParent(weapon, false);
        generatedFirePoint.transform.localPosition = Vector3.zero;
        generatedFirePoint.transform.localRotation = Quaternion.identity;
        firePoint = generatedFirePoint.transform;
    }

    private Transform FindFirstChildByNames(string[] names)
    {
        foreach (string childName in names)
        {
            Transform found = FindChildRecursive(transform, childName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private Transform FindChildRecursive(Transform parent, string childName)
    {
        if (parent.name == childName)
        {
            return parent;
        }

        foreach (Transform child in parent)
        {
            Transform found = FindChildRecursive(child, childName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private Vector3 GetShootDirection(Transform origin)
    {
        if (aimCamera == null)
        {
            return origin.forward;
        }

        Ray ray = new Ray(aimCamera.transform.position, aimCamera.transform.forward);
        RaycastHit[] hits = Physics.RaycastAll(ray, aimDistance, aimMask, QueryTriggerInteraction.Ignore);
        float closestDistance = float.PositiveInfinity;
        Vector3 aimPoint = ray.origin + ray.direction * aimDistance;

        foreach (RaycastHit hit in hits)
        {
            if (hit.transform == transform || hit.transform.IsChildOf(transform) || hit.distance >= closestDistance)
            {
                continue;
            }

            closestDistance = hit.distance;
            aimPoint = hit.point;
        }

        Vector3 shootDirection = (aimPoint - origin.position).normalized;
        return shootDirection.sqrMagnitude > 0.001f ? shootDirection : aimCamera.transform.forward;
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
