using UnityEditor;
using UnityEngine;

public static class Monster1LaserVfxBuilder
{
    private const string Root = "Assets/VFX/Monster1/";
    private const string PrefabPath = Root + "Prefabs/Monster1LaserVFX.prefab";

    [InitializeOnLoadMethod]
    private static void BuildWhenMissing()
    {
        EditorApplication.delayCall += () =>
        {
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "Textures/EndpointWhiteMask.asset") == null)
                Build();
        };
    }

    [MenuItem("GunQuest/Rebuild Monster1 Laser Endpoints", false, 22)]
    public static void Build()
    {
        Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(Root + "Shaders/Monster1Laser.shadergraph");
        Texture2D flareTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(
            "Assets/VFX/beamvfxassets/BeamVFXAssets/FireSpot.png");
        Texture2D glowTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "Textures/Monster1Glow.png");
        if (shader == null || flareTexture == null || glowTexture == null)
        {
            Debug.LogError("[Monster1VFX] Missing endpoint shader or textures.");
            return;
        }

        Texture2D mask = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "Textures/EndpointWhiteMask.asset");
        if (mask == null)
        {
            mask = new Texture2D(1, 1, TextureFormat.RGBA32, false) { name = "EndpointWhiteMask" };
            mask.SetPixel(0, 0, Color.white);
            mask.Apply();
            AssetDatabase.CreateAsset(mask, Root + "Textures/EndpointWhiteMask.asset");
        }
        Material flare = SaveMaterial("LaserEndpointFlare", shader, flareTexture, mask, new Color(6f, 3f, 8f));
        Material glow = SaveMaterial("LaserEndpointGlow", shader, glowTexture, mask, new Color(4f, 2f, 6f));
        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            ParticleSystem muzzle = root.transform.Find("MuzzleFlash").GetComponent<ParticleSystem>();
            ParticleSystem impact = root.transform.Find("ImpactRoot/ImpactGlow").GetComponent<ParticleSystem>();
            ConfigureEndpoint(muzzle, flare, glow);
            ConfigureEndpoint(impact, flare, glow);

            ParticleSystem sparks = root.transform.Find("ImpactRoot/ImpactSparks").GetComponent<ParticleSystem>();
            var main = sparks.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor = Color.white;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.35f);
            var emission = sparks.emission;
            emission.rateOverTime = 65f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 8) });
            sparks.GetComponent<ParticleSystemRenderer>().sharedMaterials = new[] { glow, glow };
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[Monster1VFX] Saved looping flare, halo and rays at both laser endpoints.");
    }

    private static Material SaveMaterial(string name, Shader shader, Texture2D texture, Texture2D mask, Color color)
    {
        string path = Root + "Materials/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }
        material.shader = shader;
        material.SetTexture("_MainTexture", texture);
        material.SetTexture("_Mask", mask);
        material.SetVector("_MainSpeed", Vector4.zero);
        material.SetColor("_Color", color);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void ConfigureEndpoint(ParticleSystem flare, Material flareMaterial, Material glowMaterial)
    {
        ConfigureParticle(flare, flareMaterial, 2.2f, 2.8f, 18f, 0.75f);
        ParticleSystem halo = ChildParticle(flare.transform, "EndpointHalo");
        ConfigureParticle(halo, glowMaterial, 2.8f, 3.2f, 12f, 0.3f);

        ParticleSystem core = ChildParticle(flare.transform, "EndpointCore");
        ConfigureParticle(core, glowMaterial, 0.65f, 0.9f, 18f, 1f);

        ParticleSystem rays = ChildParticle(flare.transform, "EndpointRays");
        ConfigureParticle(rays, glowMaterial, 1f, 1f, 32f, 0.8f);
        var main = rays.main;
        main.startSize3D = true;
        main.startSizeX = new ParticleSystem.MinMaxCurve(0.07f, 0.12f);
        main.startSizeY = new ParticleSystem.MinMaxCurve(2.8f, 3.8f);
        main.startSizeZ = 1f;
        var emission = rays.emission;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 6) });
    }

    private static ParticleSystem ChildParticle(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child != null) return child.GetComponent<ParticleSystem>();
        GameObject gameObject = new GameObject(name);
        gameObject.transform.SetParent(parent, false);
        return gameObject.AddComponent<ParticleSystem>();
    }

    private static void ConfigureParticle(
        ParticleSystem particles, Material material, float minSize, float maxSize, float rate, float alpha)
    {
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = particles.main;
        main.duration = 1f;
        main.loop = true;
        main.playOnAwake = false;
        main.prewarm = false;
        main.startDelay = 0f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.16f, 0.24f);
        main.startSpeed = 0f;
        main.startSize3D = false;
        main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = new Color(1f, 1f, 1f, alpha);
        main.gravityModifier = 0f;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Local;
        main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
        main.maxParticles = 16;

        var shape = particles.shape;
        shape.enabled = false;
        var emission = particles.emission;
        emission.enabled = true;
        emission.rateOverTime = rate;
        emission.rateOverDistance = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });
        var size = particles.sizeOverLifetime;
        size.enabled = false;
        var color = particles.colorOverLifetime;
        color.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.9f, 0.65f, 1f), 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.5f), new GradientAlphaKey(0f, 1f) });
        color.color = gradient;

        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.enabled = true;
        renderer.sharedMaterial = material;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.alignment = ParticleSystemRenderSpace.View;
        renderer.sortingOrder = 26;
        renderer.maxParticleSize = 1f;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }
}
