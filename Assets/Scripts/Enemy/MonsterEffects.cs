using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterEffects : MonoBehaviour
{
    [Header("VFX Asset References")]
    [SerializeField] private GameObject vfxRoot;
    [SerializeField] private LineRenderer laserGlow;
    [SerializeField] private LineRenderer laserBody;
    [SerializeField] private LineRenderer laserCore;
    [SerializeField] private ParticleSystem eyeCharge;
    [SerializeField] private ParticleSystem muzzleFlash;
    [SerializeField] private Transform impactRoot;
    [SerializeField] private ParticleSystem impactGlow;
    [SerializeField] private ParticleSystem impactSparks;
    [SerializeField] private ParticleSystem hitBurst;
    [SerializeField] private ParticleSystem deathBurst;
    [SerializeField] private Light muzzleLight;
    [SerializeField] private Light impactLight;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip laserLoop;

    [Header("Laser Motion")]
    [SerializeField] private float pulseSpeed = 24f;
    [SerializeField, Range(0f, 0.3f)] private float pulseAmount = 0.1f;
    [SerializeField] private float fadeDuration = 0.16f;
    [SerializeField, Min(0f)] private float impactSurfaceOffset = 0.12f;

    [Header("Hit Feedback")]
    [SerializeField] private Color hitColor = new Color(1f, 0.08f, 0.18f);
    [SerializeField] private Color deathColor = new Color(0.7f, 0.1f, 1f);

    private Renderer[] bodyRenderers;
    private MaterialPropertyBlock bodyGlowBlock;
    private Coroutine hitRoutine;
    private Coroutine fadeRoutine;
    private BeamColors glowColors;
    private BeamColors bodyColors;
    private BeamColors coreColors;
    private float glowWidth;
    private float bodyWidth;
    private float coreWidth;
    private float muzzleLightIntensity;
    private float impactLightIntensity;
    private bool laserActive;
    private bool impactActive;
    private bool ready;

    private void Awake()
    {
        bodyGlowBlock = new MaterialPropertyBlock();
        bodyRenderers = FindBodyRenderers();
        ready = HasRequiredReferences();

        if (!ready)
        {
            Debug.LogError("[MonsterEffects] Monster1 VFX references are missing.", this);
            enabled = false;
            return;
        }

        glowColors = new BeamColors(laserGlow);
        bodyColors = new BeamColors(laserBody);
        coreColors = new BeamColors(laserCore);
        glowWidth = laserGlow.widthMultiplier;
        bodyWidth = laserBody.widthMultiplier;
        coreWidth = laserCore.widthMultiplier;
        muzzleLightIntensity = muzzleLight.intensity;
        impactLightIntensity = impactLight.intensity;
        StopLaserImmediate();
    }

    private void OnDisable()
    {
        if (ready) StopLaserImmediate();
    }

    public void BeginLaser(Vector3 start, Vector3 end, Vector3 hitNormal, bool hitSomething)
    {
        if (!ready) return;

        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }

        laserActive = true;
        SetBeamEnabled(true);
        ApplyBeamAppearance(1f, 1f);

        Quaternion beamRotation = DirectionRotation(start, end);
        PlayAt(eyeCharge, start, beamRotation);
        PlayAt(muzzleFlash, start, beamRotation);

        audioSource.clip = laserLoop;
        audioSource.loop = true;
        if (laserLoop != null) audioSource.Play();

        UpdateLaser(start, end, hitNormal, hitSomething);
    }

    public void UpdateLaser(Vector3 start, Vector3 end, Vector3 hitNormal, bool hitSomething)
    {
        if (!ready || !laserActive) return;

        SetBeam(laserGlow, start, end);
        SetBeam(laserBody, start, end);
        SetBeam(laserCore, start, end);

        Quaternion beamRotation = DirectionRotation(start, end);
        eyeCharge.transform.SetPositionAndRotation(start, beamRotation);
        muzzleFlash.transform.SetPositionAndRotation(start, beamRotation);

        float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
        ApplyBeamAppearance(1f, pulse);
        muzzleLight.transform.position = start;
        muzzleLight.intensity = muzzleLightIntensity * pulse;
        muzzleLight.enabled = true;

        UpdateImpact(end, hitNormal, hitSomething, pulse);
    }

    public void EndLaser()
    {
        if (!ready || !laserActive) return;

        laserActive = false;
        StopParticle(eyeCharge);
        StopParticle(muzzleFlash);
        StopImpact();
        muzzleLight.enabled = false;
        audioSource.Stop();

        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeLaser());
    }

    public void PlayHit(Vector3 worldPoint)
    {
        if (!ready) return;

        PlayAt(hitBurst, worldPoint, Quaternion.identity);
        if (hitRoutine != null) StopCoroutine(hitRoutine);
        hitRoutine = StartCoroutine(FlashBody());
    }

    public void PlayDeath()
    {
        if (!ready) return;

        EndLaser();
        PlayAt(deathBurst, transform.position + Vector3.up * 1.35f, Quaternion.identity);
        SetBodyGlow(deathColor * 3f);
    }

    private void UpdateImpact(Vector3 position, Vector3 normal, bool hitSomething, float pulse)
    {
        if (!hitSomething)
        {
            StopImpact();
            return;
        }

        Quaternion rotation = normal.sqrMagnitude > 0.001f
            ? Quaternion.LookRotation(normal)
            : Quaternion.identity;
        impactRoot.SetPositionAndRotation(position + normal.normalized * impactSurfaceOffset, rotation);

        if (!impactActive)
        {
            impactActive = true;
            impactGlow.Clear(true);
            impactSparks.Clear(true);
            impactGlow.Play(true);
            impactSparks.Play(true);
        }

        impactLight.enabled = true;
        impactLight.intensity = impactLightIntensity * pulse;
    }

    private void StopImpact()
    {
        if (!impactActive && !impactLight.enabled) return;

        impactActive = false;
        StopParticle(impactGlow);
        StopParticle(impactSparks);
        impactLight.enabled = false;
    }

    private IEnumerator FadeLaser()
    {
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / Mathf.Max(fadeDuration, 0.01f));
            float alpha = 1f - progress;
            ApplyBeamAppearance(alpha, Mathf.Lerp(1f, 0.2f, progress));
            yield return null;
        }

        SetBeamEnabled(false);
        ApplyBeamAppearance(1f, 1f);
        fadeRoutine = null;
    }

    private IEnumerator FlashBody()
    {
        SetBodyGlow(hitColor * 3f);
        yield return new WaitForSeconds(0.09f);
        ClearBodyGlow();
        hitRoutine = null;
    }

    private void StopLaserImmediate()
    {
        laserActive = false;
        impactActive = false;

        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }

        SetBeamEnabled(false);
        ApplyBeamAppearance(1f, 1f);
        StopParticle(eyeCharge);
        StopParticle(muzzleFlash);
        StopParticle(impactGlow);
        StopParticle(impactSparks);
        muzzleLight.enabled = false;
        impactLight.enabled = false;
        audioSource.Stop();
    }

    private void ApplyBeamAppearance(float alpha, float widthScale)
    {
        laserGlow.widthMultiplier = glowWidth * widthScale;
        laserBody.widthMultiplier = bodyWidth * widthScale;
        laserCore.widthMultiplier = coreWidth * widthScale;
        glowColors.Apply(laserGlow, alpha * 0.65f);
        bodyColors.Apply(laserBody, alpha * 0.9f);
        coreColors.Apply(laserCore, alpha);
    }

    private void SetBeamEnabled(bool value)
    {
        laserGlow.enabled = value;
        laserBody.enabled = value;
        laserCore.enabled = value;
    }

    private Renderer[] FindBodyRenderers()
    {
        List<Renderer> renderers = new List<Renderer>();
        foreach (Renderer candidate in GetComponentsInChildren<Renderer>(true))
        {
            if (candidate is not MeshRenderer && candidate is not SkinnedMeshRenderer) continue;
            if (vfxRoot != null && candidate.transform.IsChildOf(vfxRoot.transform)) continue;
            renderers.Add(candidate);
        }

        return renderers.ToArray();
    }

    private bool HasRequiredReferences()
    {
        return vfxRoot != null &&
               laserGlow != null &&
               laserBody != null &&
               laserCore != null &&
               eyeCharge != null &&
               muzzleFlash != null &&
               impactRoot != null &&
               impactGlow != null &&
               impactSparks != null &&
               hitBurst != null &&
               deathBurst != null &&
               muzzleLight != null &&
               impactLight != null &&
               audioSource != null;
    }

    private void SetBodyGlow(Color color)
    {
        foreach (Renderer bodyRenderer in bodyRenderers)
        {
            if (bodyRenderer == null) continue;
            bodyRenderer.GetPropertyBlock(bodyGlowBlock);
            bodyGlowBlock.SetColor("_EmissionColor", color);
            bodyRenderer.SetPropertyBlock(bodyGlowBlock);
            bodyGlowBlock.Clear();
        }
    }

    private void ClearBodyGlow()
    {
        foreach (Renderer bodyRenderer in bodyRenderers)
        {
            if (bodyRenderer != null) bodyRenderer.SetPropertyBlock(null);
        }
    }

    private static void PlayAt(
        ParticleSystem particles,
        Vector3 position,
        Quaternion rotation)
    {
        particles.transform.SetPositionAndRotation(position, rotation);
        particles.Clear(true);
        particles.Play(true);
    }

    private static void StopParticle(ParticleSystem particles)
    {
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private static void SetBeam(LineRenderer beam, Vector3 start, Vector3 end)
    {
        beam.SetPosition(0, start);
        beam.SetPosition(1, end);
    }

    private static Quaternion DirectionRotation(Vector3 start, Vector3 end)
    {
        Vector3 direction = end - start;
        return direction.sqrMagnitude > 0.001f
            ? Quaternion.LookRotation(direction)
            : Quaternion.identity;
    }

    private readonly struct BeamColors
    {
        private readonly Color start;
        private readonly Color end;

        public BeamColors(LineRenderer beam)
        {
            start = beam.startColor;
            end = beam.endColor;
        }

        public void Apply(LineRenderer beam, float alpha)
        {
            beam.startColor = WithAlpha(start, start.a * alpha);
            beam.endColor = WithAlpha(end, end.a * alpha);
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }
}
