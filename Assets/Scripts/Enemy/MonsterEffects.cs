using System.Collections;
using UnityEngine;

public class MonsterEffects : MonoBehaviour
{
    [SerializeField] private Color effectColor = new Color(1f, 0.25f, 0.12f, 1f);
    [SerializeField] private Material laserMaterial;
    [SerializeField] private float laserWidth = 0.2f;
    [SerializeField] private float coreLaserWidth = 0.085f;
    [SerializeField] private float hitFlashDuration = 0.08f;

    private Renderer[] renderers;
    private Color[] baseColors;
    private Transform muzzle;
    private ParticleSystem attackBurst, hitBurst, deathBurst, laserMuzzleParticles, laserImpactParticles;
    private Light attackLight, laserImpactLight;
    private LineRenderer outerLaser, coreLaser;
    private AudioSource audioSource;
    private AudioClip attackClip, hitClip, deathClip;
    private Coroutine flashRoutine, lightRoutine;

    private void Awake()
    {
        CacheRenderers();
        muzzle = FindMuzzle();
        EnsureParticles();
        EnsureAudio();
    }

    public void BeginLaser(Vector3 origin, Vector3 hitPoint, Vector3 hitNormal, bool hitSomething)
    {
        EnsureLaser();
        UpdateLaser(origin, hitPoint, hitNormal, hitSomething);
        outerLaser.enabled = coreLaser.enabled = true;
        if (attackLight != null) attackLight.enabled = true;
        attackBurst?.Emit(16);
        laserMuzzleParticles?.Play();
        PlaySound(attackClip, 0.55f);
    }

    public void UpdateLaser(Vector3 origin, Vector3 hitPoint, Vector3 hitNormal, bool hitSomething)
    {
        if (outerLaser == null || coreLaser == null) return;
        float pulse = 1f + Mathf.Sin(Time.time * 42f) * 0.18f;
        outerLaser.widthMultiplier = laserWidth * pulse;
        coreLaser.widthMultiplier = coreLaserWidth * pulse;
        outerLaser.SetPosition(0, origin); outerLaser.SetPosition(1, hitPoint);
        coreLaser.SetPosition(0, origin); coreLaser.SetPosition(1, hitPoint);

        if (attackLight != null)
        {
            attackLight.transform.position = origin;
            attackLight.enabled = true;
            attackLight.intensity = 4f * pulse;
        }
        if (laserMuzzleParticles != null)
        {
            laserMuzzleParticles.transform.SetPositionAndRotation(origin, Quaternion.LookRotation((hitPoint - origin).normalized));
            if (!laserMuzzleParticles.isPlaying) laserMuzzleParticles.Play();
        }
        if (laserImpactParticles != null)
        {
            Vector3 normal = hitNormal.sqrMagnitude > 0.001f ? hitNormal : Vector3.up;
            laserImpactParticles.transform.SetPositionAndRotation(hitPoint + normal * 0.03f, Quaternion.LookRotation(normal));
            if (hitSomething && !laserImpactParticles.isPlaying) laserImpactParticles.Play();
            else if (!hitSomething && laserImpactParticles.isPlaying) laserImpactParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
        if (laserImpactLight != null)
        {
            laserImpactLight.transform.position = hitPoint + hitNormal * 0.08f;
            laserImpactLight.enabled = hitSomething;
            laserImpactLight.intensity = 3.2f * pulse;
        }
    }

    public void EndLaser()
    {
        if (outerLaser != null) outerLaser.enabled = false;
        if (coreLaser != null) coreLaser.enabled = false;
        if (attackLight != null) attackLight.enabled = false;
        if (laserImpactLight != null) laserImpactLight.enabled = false;
        laserMuzzleParticles?.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        laserImpactParticles?.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    public void PlayHit(Vector3 hitPoint)
    {
        if (hitBurst != null)
        {
            hitBurst.transform.position = hitPoint;
            hitBurst.Emit(16);
        }
        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashRenderers());
        PlaySound(hitClip, 0.45f);
    }

    public void PlayDeath()
    {
        EndLaser();
        if (deathBurst != null)
        {
            deathBurst.transform.position = transform.position + Vector3.up * 1.1f;
            deathBurst.Emit(36);
        }
        PlaySound(deathClip, 0.65f);
    }

    private void CacheRenderers()
    {
        renderers = GetComponentsInChildren<Renderer>();
        baseColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            Material mat = renderers[i].material;
            baseColors[i] = mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : (mat.HasProperty("_Color") ? mat.color : Color.white);
        }
    }

    private Transform FindMuzzle()
    {
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
            if (child.name == "MonsterMuzzle") return child;
        GameObject m = new GameObject("MonsterMuzzle");
        m.transform.SetParent(transform, false);
        m.transform.localPosition = new Vector3(0f, 1.2f, 0.9f);
        return m.transform;
    }

    private void EnsureParticles()
    {
        if (attackBurst == null)
        {
            attackBurst = CreateParticles("AttackVFX", muzzle, effectColor, 0.16f, 3.5f, 0.12f);
            GameObject lightObj = new GameObject("AttackLight");
            lightObj.transform.SetParent(muzzle, false);
            attackLight = lightObj.AddComponent<Light>();
            attackLight.type = LightType.Point;
            attackLight.range = 4.5f;
            attackLight.intensity = 3.2f;
            attackLight.color = effectColor;
            attackLight.enabled = false;
        }
        if (hitBurst == null) hitBurst = CreateParticles("HitVFX", transform, new Color(1f, 0.7f, 0.2f), 0.2f, 4f, 0.12f);
        if (deathBurst == null) deathBurst = CreateParticles("DeathVFX", transform, effectColor, 0.45f, 5.5f, 0.22f);
    }

    private void EnsureLaser()
    {
        if (outerLaser != null && coreLaser != null) return;
        Material sharedMat = GetLaserMaterial();
        outerLaser = CreateLaserRenderer("EyeLaserOuter", sharedMat, new Color(1f, 0.12f, 0.05f, 0.78f), new Color(1f, 0.35f, 0.08f, 0.55f), laserWidth);
        coreLaser = CreateLaserRenderer("EyeLaserCore", sharedMat, new Color(1f, 0.95f, 0.75f, 0.98f), new Color(1f, 0.65f, 0.25f, 0.92f), coreLaserWidth);
        laserMuzzleParticles = CreateContinuousLaserParticles("EyeLaserMuzzleVFX", muzzle, new Color(1f, 0.42f, 0.12f), 0.12f, 2.2f, 0.12f, 55f);
        laserImpactParticles = CreateContinuousLaserParticles("EyeLaserImpactVFX", transform, new Color(1f, 0.55f, 0.16f), 0.18f, 3.8f, 0.14f, 70f);

        GameObject lightObj = new GameObject("EyeLaserImpactLight");
        lightObj.transform.SetParent(transform, false);
        laserImpactLight = lightObj.AddComponent<Light>();
        laserImpactLight.type = LightType.Point;
        laserImpactLight.range = 4.5f;
        laserImpactLight.intensity = 3.2f;
        laserImpactLight.color = new Color(1f, 0.32f, 0.12f);
        laserImpactLight.enabled = false;
    }

    private LineRenderer CreateLaserRenderer(string name, Material sharedMat, Color startColor, Color endColor, float width)
    {
        GameObject laserObj = new GameObject(name);
        laserObj.transform.SetParent(transform, false);
        LineRenderer lr = laserObj.AddComponent<LineRenderer>();
        lr.positionCount = 2; lr.useWorldSpace = true;
        lr.widthMultiplier = width; lr.numCapVertices = 6; lr.numCornerVertices = 4;
        lr.textureMode = LineTextureMode.Tile;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        lr.material = new Material(sharedMat);
        lr.startColor = startColor; lr.endColor = endColor;
        lr.enabled = false;
        return lr;
    }

    private Material GetLaserMaterial()
    {
        if (laserMaterial != null) return laserMaterial;
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default");
        return laserMaterial = new Material(shader) { name = "RuntimeMonsterLaser" };
    }

    private static ParticleSystem CreateParticles(string name, Transform parent, Color color, float lifetime, float speed, float size)
    {
        GameObject effect = new GameObject(name);
        effect.transform.SetParent(parent, false);
        ParticleSystem ps = effect.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.playOnAwake = false; main.loop = false;
        main.startLifetime = lifetime; main.startSpeed = speed; main.startSize = size;
        main.startColor = color; main.simulationSpace = ParticleSystemSimulationSpace.World;
        var emission = ps.emission; emission.enabled = false;
        var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = 0.08f;
        var renderer = effect.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default"));
        return ps;
    }

    private ParticleSystem CreateContinuousLaserParticles(string name, Transform parent, Color color, float lifetime, float speed, float size, float rate)
    {
        ParticleSystem ps = CreateParticles(name, parent, color, lifetime, speed, size);
        var main = ps.main; main.loop = true;
        var emission = ps.emission; emission.enabled = true; emission.rateOverTime = rate;
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(GetLaserMaterial());
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        return ps;
    }

    private IEnumerator FlashRenderers()
    {
        SetRendererColor(new Color(1f, 0.35f, 0.25f));
        yield return new WaitForSeconds(hitFlashDuration);
        RestoreRendererColors();
    }

    private void SetRendererColor(Color color)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            Material mat = renderers[i].material;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.color = color;
        }
    }

    private void RestoreRendererColors()
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            Material mat = renderers[i].material;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColors[i]);
            if (mat.HasProperty("_Color")) mat.color = baseColors[i];
        }
    }

    private void EnsureAudio()
    {
        audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0.85f;
        audioSource.maxDistance = 24f;
        attackClip = CreateTone("MonsterAttack", 0.16f, 220f, 92f, 26f);
        hitClip = CreateTone("MonsterHit", 0.09f, 340f, 160f, 34f);
        deathClip = CreateTone("MonsterDeath", 0.42f, 180f, 48f, 7f);
    }

    private void PlaySound(AudioClip clip, float volume)
    {
        if (audioSource == null || clip == null) return;
        audioSource.pitch = Random.Range(0.92f, 1.08f);
        float sfxVol = GameSettings.Instance != null ? GameSettings.Instance.sfxVolume : 0.8f;
        audioSource.PlayOneShot(clip, volume * sfxVol);
    }

    private static AudioClip CreateTone(string name, float duration, float startFreq, float endFreq, float decay)
    {
        const int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];
        System.Random random = new System.Random(name.GetHashCode());
        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleRate;
            float frequency = Mathf.Lerp(startFreq, endFreq, t / duration);
            float envelope = Mathf.Exp(-decay * t);
            float wave = Mathf.Sin(2f * Mathf.PI * frequency * t);
            float noise = (float)(random.NextDouble() * 2.0 - 1.0);
            samples[i] = (wave * 0.65f + noise * 0.35f) * envelope;
        }
        AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
