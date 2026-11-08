using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class WeaponEffects : MonoBehaviour
{
    [Header("Optional assets")]
    [SerializeField] private ParticleSystem muzzleFlash;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip shotSound;

    [Header("Appearance")]
    [SerializeField] private Color flashColor = new Color(1f, 0.55f, 0.12f);
    [SerializeField] private Color tracerColor = new Color(1f, 0.85f, 0.25f);
    [SerializeField] private float tracerWidth = 0.045f;
    [SerializeField] private float effectDuration = 0.07f;

    private static Material effectMaterial;
    private static GameObject muzzleFlashPrefab;
    private static GameObject impactPrefab;

    private Transform muzzle;
    private Light muzzleLight;
    private LineRenderer tracer;
    private ParticleSystem impactEffect;
    private GameObject muzzleVfxInstance;
    private ParticleSystem[] muzzleParticleSystems;
    private Coroutine hideRoutine;

    public void Initialize(Transform muzzlePoint)
    {
        muzzle = muzzlePoint;
        EnsureEffects();
        if (hideRoutine != null) StopCoroutine(hideRoutine);
        hideRoutine = null;
        if (muzzleFlash != null)
        {
            muzzleFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            muzzleFlash.transform.SetParent(muzzle != null ? muzzle : transform, false);
        }
        if (muzzleVfxInstance != null)
        {
            muzzleVfxInstance.transform.SetParent(muzzle != null ? muzzle : transform, false);
            muzzleVfxInstance.SetActive(false);
        }
        muzzleLight.transform.SetParent(muzzle != null ? muzzle : transform, false);
        muzzleLight.enabled = false;
        tracer.enabled = false;
    }

    public void Play(Transform muzzlePoint, Vector3 hitPoint, Vector3 hitNormal, bool hitSomething)
    {
        muzzle = muzzlePoint != null ? muzzlePoint : muzzle;
        EnsureEffects();

        Vector3 start = muzzle != null ? muzzle.position : transform.position + transform.forward;
        Vector3 direction = hitPoint - start;
        if (direction.sqrMagnitude < 0.001f) direction = transform.forward;

        if (muzzleVfxInstance != null)
        {
            muzzleVfxInstance.transform.SetPositionAndRotation(start, Quaternion.LookRotation(direction));
            muzzleVfxInstance.SetActive(true);
            for (int i = 0; i < muzzleParticleSystems.Length; i++)
            {
                if (muzzleParticleSystems[i] != null)
                {
                    muzzleParticleSystems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    muzzleParticleSystems[i].Play(true);
                }
            }
        }
        else if (muzzleFlash != null)
        {
            muzzleFlash.transform.SetPositionAndRotation(start, Quaternion.LookRotation(direction));
            muzzleFlash.Emit(8);
        }

        muzzleLight.transform.position = start;
        muzzleLight.enabled = true;

        tracer.SetPosition(0, start);
        tracer.SetPosition(1, hitPoint);
        tracer.enabled = true;

        if (hitSomething)
        {
            if (impactPrefab != null)
            {
                Quaternion rot = hitNormal.sqrMagnitude > 0.001f ? Quaternion.LookRotation(hitNormal) : Quaternion.identity;
                GameObject imp = Instantiate(impactPrefab, hitPoint, rot);
                imp.transform.localScale = Vector3.one * 0.55f;
                Destroy(imp, 1.2f);
            }
            else if (impactEffect != null)
            {
                impactEffect.transform.SetPositionAndRotation(hitPoint, Quaternion.LookRotation(hitNormal));
                impactEffect.Emit(12);
            }
        }

        audioSource.PlayOneShot(shotSound, GameSettings.Instance != null ? GameSettings.Instance.sfxVolume : 1f);
        if (hideRoutine != null) StopCoroutine(hideRoutine);
        hideRoutine = StartCoroutine(HideFlash());
    }

    private void EnsureEffects()
    {
        if (muzzleFlashPrefab == null) muzzleFlashPrefab = Resources.Load<GameObject>("VFX/vfx_MuzzleFlash_01");
        if (impactPrefab == null) impactPrefab = Resources.Load<GameObject>("VFX/vfx_Impact_01");

        if (muzzleVfxInstance == null && muzzleFlashPrefab != null)
        {
            muzzleVfxInstance = Instantiate(muzzleFlashPrefab, muzzle != null ? muzzle : transform);
            muzzleVfxInstance.name = "vfx_MuzzleFlash_Instance";
            muzzleVfxInstance.transform.localScale = Vector3.one * 0.4f;
            muzzleParticleSystems = muzzleVfxInstance.GetComponentsInChildren<ParticleSystem>(true);
            muzzleVfxInstance.SetActive(false);
        }

        if (muzzleFlash == null) muzzleFlash = CreateParticles("MuzzleFlash", true);
        if (muzzleLight == null) muzzleLight = CreateMuzzleLight();
        if (tracer == null) tracer = CreateTracer();
        if (impactEffect == null) impactEffect = CreateParticles("BulletImpact", false);

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0.75f;
            audioSource.volume = 0.55f;
            audioSource.minDistance = 1f;
            audioSource.maxDistance = 35f;
        }

        if (shotSound == null) shotSound = CreateShotSound();
    }

    private ParticleSystem CreateParticles(string objectName, bool isMuzzle)
    {
        GameObject effectObject = new GameObject(objectName);
        effectObject.transform.SetParent(isMuzzle && muzzle != null ? muzzle : transform, false);
        ParticleSystem particles = effectObject.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = particles.main;
        main.playOnAwake = false;
        main.loop = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = isMuzzle
            ? new ParticleSystem.MinMaxCurve(0.025f, 0.06f)
            : new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
        main.startSpeed = isMuzzle
            ? new ParticleSystem.MinMaxCurve(0.2f, 0.8f)
            : new ParticleSystem.MinMaxCurve(0.8f, 2.2f);
        main.startSize = isMuzzle
            ? new ParticleSystem.MinMaxCurve(0.1f, 0.22f)
            : new ParticleSystem.MinMaxCurve(0.035f, 0.09f);
        main.startColor = new ParticleSystem.MinMaxGradient(Color.white, isMuzzle ? flashColor : tracerColor);
        main.gravityModifier = isMuzzle ? 0f : 0.4f;
        main.maxParticles = 40;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.enabled = false;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = isMuzzle ? ParticleSystemShapeType.Cone : ParticleSystemShapeType.Hemisphere;
        shape.radius = isMuzzle ? 0.01f : 0.035f;
        if (isMuzzle) shape.angle = 16f;

        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.material = GetEffectMaterial();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        return particles;
    }

    private Light CreateMuzzleLight()
    {
        GameObject lightObject = new GameObject("MuzzleLight");
        lightObject.transform.SetParent(muzzle != null ? muzzle : transform, false);
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = flashColor;
        light.intensity = 4f;
        light.range = 4f;
        light.shadows = LightShadows.None;
        light.enabled = false;
        return light;
    }

    private LineRenderer CreateTracer()
    {
        GameObject tracerObject = new GameObject("ShotTracer");
        tracerObject.transform.SetParent(transform, false);
        LineRenderer line = tracerObject.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = 2;
        line.startWidth = tracerWidth;
        line.endWidth = 0.015f;

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(2.5f, 2.2f, 1.4f), 0.0f),  // Blinding white-gold core
                new GradientColorKey(new Color(2.0f, 0.9f, 0.2f), 0.4f),  // Fiery amber trail
                new GradientColorKey(new Color(1.2f, 0.3f, 0.05f), 1.0f)  // Crimson tip
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1.0f, 0.0f),
                new GradientAlphaKey(0.85f, 0.7f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        line.colorGradient = gradient;
        line.material = GetEffectMaterial();
        line.alignment = LineAlignment.View;
        line.shadowCastingMode = ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.enabled = false;
        return line;
    }

    private AudioClip CreateShotSound()
    {
        const int sampleRate = 44100;
        const float duration = 0.09f;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];
        System.Random random = new System.Random(9137);

        for (int i = 0; i < sampleCount; i++)
        {
            float time = i / (float)sampleRate;
            float envelope = Mathf.Exp(-42f * time);
            float noise = (float)(random.NextDouble() * 2.0 - 1.0);
            samples[i] = (noise * 0.72f + Mathf.Sin(time * 620f) * 0.28f) * envelope;
        }

        AudioClip clip = AudioClip.Create("GeneratedGunShot", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static Material GetEffectMaterial()
    {
        if (effectMaterial != null) return effectMaterial;
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        effectMaterial = new Material(shader) { name = "Runtime Weapon Effect" };
        effectMaterial.hideFlags = HideFlags.HideAndDontSave;
        return effectMaterial;
    }

    private IEnumerator HideFlash()
    {
        yield return new WaitForSeconds(effectDuration);
        muzzleLight.enabled = false;
        tracer.enabled = false;
        hideRoutine = null;
    }
}
