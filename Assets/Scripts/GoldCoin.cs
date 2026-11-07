using UnityEngine;

public class GoldCoin : MonoBehaviour
{
    private enum CoinState { Bursting, Landed, Attracting, Collected }

    [SerializeField] private int value = 10;
    [SerializeField] private float gravity = 15f;
    [SerializeField] private float magnetRange = 5.0f;
    [SerializeField] private float autoAttractDelay = 1.8f;
    [SerializeField] private float collectionDistance = 0.85f;
    [SerializeField] private float maxLifetime = 60f;
    [SerializeField] private float baseScale = 2.6f;

    private CoinState state = CoinState.Bursting;
    private Vector3 velocity, groundPos;
    private float spawnTime, landedTime, attractSpeed = 8f;
    private int bounceCount;
    private Transform playerTransform;
    private Camera mainCam;
    private SpriteRenderer spriteRenderer;

    private static Material spriteMaterial, particleMaterial;
    private static AudioClip coinPickupSound;
    private static Sprite coinSprite;

    public static void SpawnBurst(Vector3 position, int count, int valuePerCoin)
    {
        for (int i = 0; i < count; i++)
        {
            float angle = (i / (float)count) * Mathf.PI * 2f + Random.Range(-0.35f, 0.35f);
            float hSpeed = Random.Range(2.0f, 4.0f);
            Vector3 vel = new Vector3(Mathf.Cos(angle) * hSpeed, Random.Range(4.5f, 7.2f), Mathf.Sin(angle) * hSpeed);
            Vector3 spawnPos = position + Vector3.up * 0.4f + new Vector3(Mathf.Cos(angle) * 0.15f, 0, Mathf.Sin(angle) * 0.15f);
            CreateCoin(spawnPos, vel, valuePerCoin);
        }
    }

    public static GoldCoin CreateCoin(Vector3 position, Vector3 initialVelocity, int coinValue)
    {
        GameObject coinObj = new GameObject("GoldCoin");
        coinObj.transform.position = position;
        var sr = coinObj.AddComponent<SpriteRenderer>();
        sr.sprite = coinSprite ??= Resources.Load<Sprite>("PixelUI/coin");
        sr.material = GetSpriteMaterial();
        sr.sortingOrder = 5;

        GoldCoin coin = coinObj.AddComponent<GoldCoin>();
        coin.value = coinValue;
        coin.velocity = initialVelocity;
        coin.spawnTime = Time.time;
        coin.spriteRenderer = sr;
        return coin;
    }

    private void Start()
    {
        mainCam = Camera.main;
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        FindPlayer();
    }

    private void Update()
    {
        if (Time.time - spawnTime > maxLifetime) { Destroy(gameObject); return; }
        if (playerTransform == null) FindPlayer();

        if (state == CoinState.Bursting) UpdateBursting();
        else if (state == CoinState.Landed) UpdateLanded();
        else if (state == CoinState.Attracting) UpdateAttracting();
    }

    private void LateUpdate()
    {
        if (mainCam == null) mainCam = Camera.main;
        if (mainCam != null && spriteRenderer != null)
        {
            transform.rotation = mainCam.transform.rotation;
            transform.localScale = new Vector3(Mathf.Cos((Time.time - spawnTime) * 8f) * baseScale, baseScale, 1f);
        }
    }

    private void UpdateBursting()
    {
        velocity.y -= gravity * Time.deltaTime;
        transform.position += velocity * Time.deltaTime;

        if (Physics.Raycast(transform.position + Vector3.up * 0.2f, Vector3.down, out RaycastHit hit, 0.35f, ~0, QueryTriggerInteraction.Ignore)
            && hit.collider != null && !hit.collider.isTrigger && !hit.transform.IsChildOf(transform))
        {
            transform.position = hit.point + Vector3.up * 0.15f;
            if (velocity.y < -1.6f && bounceCount < 2)
            {
                velocity.y = -velocity.y * 0.42f;
                velocity.x *= 0.6f; velocity.z *= 0.6f;
                bounceCount++;
            }
            else
            {
                state = CoinState.Landed;
                landedTime = Time.time;
                groundPos = transform.position;
            }
        }
        else if (transform.position.y < -15f) Destroy(gameObject);
    }

    private void UpdateLanded()
    {
        transform.position = groundPos + Vector3.up * (Mathf.Sin((Time.time - landedTime) * 4f) * 0.04f);
        if (playerTransform == null) return;
        float dist = Vector3.Distance(transform.position, playerTransform.position);
        if (dist <= collectionDistance) { Collect(); return; }
        if (dist <= magnetRange || (Time.time - landedTime > autoAttractDelay && dist <= 16f))
        {
            state = CoinState.Attracting;
            attractSpeed = 8f;
        }
    }

    private void UpdateAttracting()
    {
        if (playerTransform == null) { Collect(); return; }
        Vector3 target = playerTransform.position + Vector3.up;
        attractSpeed = Mathf.MoveTowards(attractSpeed, 22f, Time.deltaTime * 32f);
        transform.position = Vector3.MoveTowards(transform.position, target, attractSpeed * Time.deltaTime);
        if (Vector3.Distance(transform.position, target) <= collectionDistance) Collect();
    }

    private void Collect()
    {
        if (state == CoinState.Collected) return;
        state = CoinState.Collected;
        ContractGame.Instance?.AddCredits(value);
        PlayCoinSound(transform.position);
        SpawnCollectSparkle(transform.position);
        Destroy(gameObject);
    }

    private void FindPlayer()
    {
        var motor = Object.FindFirstObjectByType<PlayerMotor>();
        playerTransform = motor != null ? motor.transform : GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    private static Material GetSpriteMaterial()
    {
        if (spriteMaterial != null) return spriteMaterial;
        Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        return spriteMaterial = new Material(shader) { name = "PixelCoinSpriteMat" };
    }

    private static Material GetParticleMaterial()
    {
        if (particleMaterial != null) return particleMaterial;
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default");
        return particleMaterial = new Material(shader) { name = "CoinParticleMat" };
    }

    private static void PlayCoinSound(Vector3 position)
    {
        if (coinPickupSound == null)
        {
            const int sampleRate = 44100;
            const float duration = 0.18f;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)sampleRate;
                float freq = t < 0.05f ? 987.77f : 1318.51f;
                float env = t < 0.05f ? Mathf.Exp(-16f * t) : Mathf.Exp(-20f * (t - 0.05f));
                samples[i] = (Mathf.Sin(2f * Mathf.PI * freq * t) * 0.75f + Mathf.Sin(4f * Mathf.PI * freq * t) * 0.25f) * env * 0.7f;
            }
            coinPickupSound = AudioClip.Create("CoinPickup", sampleCount, 1, sampleRate, false);
            coinPickupSound.SetData(samples, 0);
        }
        float vol = GameSettings.Instance != null ? GameSettings.Instance.sfxVolume : 0.85f;
        AudioSource.PlayClipAtPoint(coinPickupSound, position, vol);
    }

    private static void SpawnCollectSparkle(Vector3 position)
    {
        GameObject vfx = new GameObject("CoinSparkle");
        vfx.transform.position = position;
        ParticleSystem ps = vfx.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 0.28f; main.startSpeed = 2.8f; main.startSize = 0.1f;
        main.startColor = new Color(1f, 0.88f, 0.2f, 1f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.playOnAwake = false; main.loop = false;

        var emission = ps.emission;
        emission.enabled = true;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 10) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.08f;

        var renderer = vfx.GetComponent<ParticleSystemRenderer>();
        if (renderer != null) renderer.material = GetParticleMaterial();

        ps.Play();
        Destroy(vfx, 0.35f);
    }
}
