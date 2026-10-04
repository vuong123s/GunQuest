using UnityEngine;

public class PlayerShoot : MonoBehaviour
{
    [SerializeField] private Transform firePoint;
    [SerializeField] private Camera aimCamera;
    [SerializeField] private float fireRate = 0.18f;
    [SerializeField] private float damage = 15f;
    [SerializeField] private float aimDistance = 100f;
    [SerializeField] private LayerMask aimMask = ~0;

    private readonly RaycastHit[] hits = new RaycastHit[16];
    private WeaponEffects effects;
    private float nextFireTime;
    private bool canShoot = true;

    public void Configure(Transform muzzle, float shotDamage, float interval, float range, bool ranged)
    {
        firePoint = muzzle;
        damage = shotDamage;
        fireRate = interval;
        aimDistance = range;
        canShoot = ranged;
        effects.Initialize(muzzle);
    }

    private void Awake()
    {
        aimCamera = aimCamera != null ? aimCamera : GetComponentInChildren<Camera>();
        if (aimCamera == null) aimCamera = Camera.main;
        firePoint = firePoint != null ? firePoint : FindFirePoint();
        effects = GetComponent<WeaponEffects>() ?? gameObject.AddComponent<WeaponEffects>();
        effects.Initialize(firePoint);
    }

    public bool Shoot()
    {
        if (!canShoot || Time.time < nextFireTime) return false;
        nextFireTime = Time.time + fireRate;
        Transform origin = firePoint != null ? firePoint : aimCamera != null ? aimCamera.transform : transform;
        Vector3 aimPoint = GetAimPoint(origin, out Vector3 hitNormal, out EnemyHealth enemy, out bool hitSomething);
        if (enemy != null) enemy.TakeDamage(damage, aimPoint);
        effects.Play(origin, aimPoint, hitNormal, hitSomething);
        return true;
    }

    private Vector3 GetAimPoint(Transform origin, out Vector3 hitNormal, out EnemyHealth enemy, out bool hitSomething)
    {
        hitNormal = -origin.forward;
        enemy = null;
        hitSomething = false;
        if (aimCamera == null) return origin.position + origin.forward * aimDistance;

        Ray ray = new Ray(aimCamera.transform.position, aimCamera.transform.forward);
        Vector3 aimPoint = ray.origin + ray.direction * aimDistance;
        float closestDistance = float.MaxValue;
        int count = Physics.RaycastNonAlloc(ray, hits, aimDistance, aimMask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.transform == transform || hit.transform.IsChildOf(transform) || hit.distance >= closestDistance) continue;
            closestDistance = hit.distance;
            aimPoint = hit.point;
            hitNormal = hit.normal;
            enemy = hit.collider.GetComponentInParent<EnemyHealth>();
            hitSomething = true;
        }

        return aimPoint;
    }

    private Transform FindFirePoint()
    {
        Transform weapon = null;
        foreach (Transform item in GetComponentsInChildren<Transform>())
        {
            if (item.name == "FirePoint" || item.name == "Muzzle" || item.name == "GunBarrel") return item;
            string lowerName = item.name.ToLowerInvariant();
            if (weapon == null && (lowerName.Contains("weapon") || lowerName.Contains("gun") || lowerName.Contains("rifle")))
            {
                weapon = item;
            }
        }

        GameObject generated = new GameObject("FirePoint");
        generated.transform.SetParent(weapon != null ? weapon : transform, false);
        generated.transform.localPosition = weapon != null ? Vector3.forward * 0.35f : new Vector3(0.35f, 1.25f, 0.55f);
        return generated.transform;
    }

}
