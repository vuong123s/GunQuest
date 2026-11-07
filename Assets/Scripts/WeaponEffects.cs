using System.Collections;
using UnityEngine;

public class WeaponEffects : MonoBehaviour
{
    [SerializeField] private float tracerDuration = 0.045f;
    [SerializeField] private float flashDuration = 0.04f;
    [SerializeField] private float recoilPitch = 0.85f;
    [SerializeField] private float recoilYaw = 0.3f;

    private LineRenderer tracer;
    private Light flashLight;
    private AudioSource audioSource;
    private AudioClip shotClip;
    private PlayerLook playerLook;
    private PlayerCameraFollow cameraFollow;
    private Coroutine effectCoroutine;
    private static GameObject urpMuzzlePrefab;
    private static GameObject urpImpactPrefab;

    public void Initialize(Transform muzzle)
    {
        playerLook = GetComponent<PlayerLook>();
        if (Camera.main != null) cameraFollow = Camera.main.GetComponent<PlayerCameraFollow>();
        EnsureTracer();
        EnsureAudio();
        if (urpMuzzlePrefab == null) urpMuzzlePrefab = Resources.Load<GameObject>("VFX/vfx_MuzzleFlash_01");
        if (urpImpactPrefab == null) urpImpactPrefab = Resources.Load<GameObject>("VFX/vfx_Impact_01");
        if (muzzle != null) EnsureMuzzleEffects(muzzle);
    }

    public void Play(Transform muzzle, Vector3 hitPoint, Vector3 hitNormal, bool hitSomething)
    {
        if (muzzle != null && (flashLight == null || flashLight.transform.parent != muzzle))
            EnsureMuzzleEffects(muzzle);

        Vector3 startPoint = muzzle != null ? muzzle.position : transform.position + Vector3.up * 1.25f;
        if (muzzle != null && urpMuzzlePrefab != null)
        {
            GameObject mf = Instantiate(urpMuzzlePrefab, muzzle.position, muzzle.rotation, muzzle);
            mf.transform.localScale = Vector3.one * 0.45f;
            Destroy(mf, 0.5f);
        }

        if (hitSomething && urpImpactPrefab != null)
        {
            Quaternion rot = Quaternion.LookRotation(hitNormal.sqrMagnitude > 0.01f ? hitNormal : Vector3.up);
            GameObject imp = Instantiate(urpImpactPrefab, hitPoint + hitNormal * 0.02f, rot);
            imp.transform.localScale = Vector3.one * 0.65f;
            Destroy(imp, 1.5f);
        }

        if (audioSource != null && shotClip != null)
        {
            audioSource.pitch = Random.Range(0.95f, 1.05f);
            float sfxVol = GameSettings.Instance != null ? GameSettings.Instance.sfxVolume : 0.8f;
            audioSource.PlayOneShot(shotClip, 0.55f * sfxVol);
        }

        playerLook?.ApplyRecoil(new Vector2(Random.Range(-recoilYaw, recoilYaw), -recoilPitch));
        cameraFollow?.Kick(1f);

        if (effectCoroutine != null) StopCoroutine(effectCoroutine);
        effectCoroutine = StartCoroutine(ShowShot(startPoint, hitPoint));
    }

    private IEnumerator ShowShot(Vector3 startPoint, Vector3 endPoint)
    {
        if (tracer != null)
        {
            tracer.SetPosition(0, startPoint);
            tracer.SetPosition(1, endPoint);
            tracer.enabled = true;
        }
        if (flashLight != null) flashLight.enabled = true;
        yield return new WaitForSeconds(Mathf.Max(tracerDuration, flashDuration));
        if (tracer != null) tracer.enabled = false;
        if (flashLight != null) flashLight.enabled = false;
    }

    private void EnsureTracer()
    {
        if (tracer != null) return;
        GameObject tracerObject = new GameObject("BulletTracer");
        tracerObject.transform.SetParent(transform, false);
        tracer = tracerObject.AddComponent<LineRenderer>();
        tracer.positionCount = 2;
        tracer.startWidth = 0.03f;
        tracer.endWidth = 0.01f;
        tracer.useWorldSpace = true;
        tracer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        tracer.receiveShadows = false;
        tracer.material = new Material(Shader.Find("Sprites/Default"));
        tracer.startColor = new Color(1f, 0.9f, 0.45f, 0.95f);
        tracer.endColor = new Color(1f, 0.45f, 0.1f, 0.15f);
        tracer.enabled = false;
    }

    private void EnsureMuzzleEffects(Transform muzzle)
    {
        Transform existingLight = muzzle.Find("MuzzleLight");
        if (existingLight != null)
        {
            flashLight = existingLight.GetComponent<Light>();
        }
        else
        {
            GameObject lightObject = new GameObject("MuzzleLight");
            lightObject.transform.SetParent(muzzle, false);
            flashLight = lightObject.AddComponent<Light>();
            flashLight.type = LightType.Point;
            flashLight.range = 4f;
            flashLight.intensity = 3.5f;
            flashLight.color = new Color(1f, 0.72f, 0.3f);
            flashLight.enabled = false;
        }
    }

    private void EnsureAudio()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0.2f;
        }
        if (shotClip == null) shotClip = CreateShotClip();
    }

    private static AudioClip CreateShotClip()
    {
        const int sampleRate = 44100;
        const float duration = 0.09f;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];
        System.Random random = new System.Random(17);

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleRate;
            float envelope = Mathf.Exp(-38f * t);
            float pop = Mathf.Sin(2f * Mathf.PI * 155f * t);
            float noise = (float)(random.NextDouble() * 2.0 - 1.0);
            samples[i] = (noise * 0.7f + pop * 0.45f) * envelope;
        }

        AudioClip clip = AudioClip.Create("GunShot", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
