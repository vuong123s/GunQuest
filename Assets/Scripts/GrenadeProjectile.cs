using System.Collections.Generic;
using UnityEngine;

public class GrenadeProjectile : MonoBehaviour
{
    [Header("Explosion Settings")]
    [SerializeField] private float fuseTime = 1.8f;
    [SerializeField] private float explosionRadius = 6.5f;
    [SerializeField] private float explosionDamage = 120f;
    [SerializeField] private GameObject explosionVfxPrefab;

    private float timer;
    private bool detonated;
    private static AudioClip explosionSound;

    public void Initialize(GameObject vfxPrefab, float damage = 120f, float radius = 6.5f)
    {
        if (vfxPrefab != null) explosionVfxPrefab = vfxPrefab;
        explosionDamage = damage;
        explosionRadius = radius;
    }

    private void Awake() => timer = fuseTime;

    private void Update()
    {
        if (detonated) return;
        timer -= Time.deltaTime;
        if (timer <= 0f) Detonate();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (detonated) return;
        var enemy = collision.collider.GetComponentInParent<EnemyHealth>();
        if (enemy != null && !enemy.IsDead) Detonate();
    }

    public void Detonate()
    {
        if (detonated) return;
        detonated = true;
        Vector3 blastPos = transform.position;

        if (explosionVfxPrefab == null) explosionVfxPrefab = Resources.Load<GameObject>("VFX/vfx_Explosion_01");
        if (explosionVfxPrefab != null) Destroy(Instantiate(explosionVfxPrefab, blastPos, Quaternion.identity), 3.5f);

        PlayExplosionSound(blastPos);

        Collider[] hits = Physics.OverlapSphere(blastPos, explosionRadius);
        HashSet<EnemyHealth> damagedEnemies = new HashSet<EnemyHealth>();
        foreach (var hit in hits)
        {
            var enemy = hit.GetComponentInParent<EnemyHealth>();
            if (enemy != null && !damagedEnemies.Contains(enemy) && !enemy.IsDead)
            {
                damagedEnemies.Add(enemy);
                float dist = Vector3.Distance(blastPos, enemy.transform.position);
                float falloff = Mathf.Clamp01(1f - (dist / explosionRadius) * 0.65f);
                enemy.TakeDamage(explosionDamage * falloff);
            }
        }

        var player = Object.FindFirstObjectByType<PlayerHealth>();
        if (player != null && !player.IsDead)
        {
            float playerDist = Vector3.Distance(blastPos, player.transform.position);
            if (playerDist <= explosionRadius * 0.45f) player.TakeDamage(15f);
            if (playerDist <= 16f)
            {
                float intensity = 1f - (playerDist / 16f);
                Object.FindFirstObjectByType<PlayerLook>()?.ApplyRecoil(
                    new Vector2(Random.Range(-1.5f, 1.5f) * intensity * 2.5f, -Random.Range(2.5f, 4.5f) * intensity));
            }
        }

        Destroy(gameObject);
    }

    private static void PlayExplosionSound(Vector3 position)
    {
        if (explosionSound == null) explosionSound = CreateExplosionSound();
        float vol = GameSettings.Instance != null ? GameSettings.Instance.sfxVolume : 1f;
        AudioSource.PlayClipAtPoint(explosionSound, position, vol);
    }

    private static AudioClip CreateExplosionSound()
    {
        const int sampleRate = 44100;
        const float duration = 0.85f;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];
        System.Random random = new System.Random(404);
        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleRate;
            float envelope = Mathf.Exp(-5.5f * t);
            float subBass = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(90f, 32f, t) * t);
            float noise = (float)(random.NextDouble() * 2.0 - 1.0);
            samples[i] = (noise * 0.65f + subBass * 0.55f) * envelope;
        }
        AudioClip clip = AudioClip.Create("GrenadeExplosion", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
