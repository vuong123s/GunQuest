using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ItemPrefabBuilder
{
    private const string Folder = "Assets/Prefabs/Items";

    [MenuItem("GunQuest/Items/Build All Item Prefabs", false, 50)]
    public static void BuildAll()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs")) AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Prefabs", "Items");

        BuildHealthPotionModel();
        BuildGrenadeModel();
        BuildThrownGrenadePrefab();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[ItemPrefabBuilder] High-quality Health Potion and Grenade prefabs created successfully!");
    }

    public static Mesh CreateLatheMesh(Vector2[] profile, int radialSegments, bool invertNormals = false)
    {
        Mesh mesh = new Mesh();
        int numPoints = profile.Length;
        int vertexCount = numPoints * (radialSegments + 1);
        Vector3[] vertices = new Vector3[vertexCount];
        Vector3[] normals = new Vector3[vertexCount];
        Vector2[] uvs = new Vector2[vertexCount];
        int triCount = (numPoints - 1) * radialSegments * 6;
        int[] triangles = new int[triCount];

        int vIndex = 0;
        for (int i = 0; i < numPoints; i++)
        {
            float y = profile[i].y;
            float r = profile[i].x;
            float v = (float)i / (numPoints - 1);

            for (int j = 0; j <= radialSegments; j++)
            {
                float u = (float)j / radialSegments;
                float angle = u * Mathf.PI * 2f;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);

                vertices[vIndex] = new Vector3(cos * r, y, sin * r);
                normals[vIndex] = new Vector3(cos, 0.1f, sin).normalized * (invertNormals ? -1f : 1f);
                uvs[vIndex] = new Vector2(u, v);
                vIndex++;
            }
        }

        int tIndex = 0;
        int stride = radialSegments + 1;
        for (int i = 0; i < numPoints - 1; i++)
        {
            for (int j = 0; j < radialSegments; j++)
            {
                int current = i * stride + j;
                int next = current + stride;

                if (!invertNormals)
                {
                    triangles[tIndex++] = current;
                    triangles[tIndex++] = next;
                    triangles[tIndex++] = current + 1;

                    triangles[tIndex++] = current + 1;
                    triangles[tIndex++] = next;
                    triangles[tIndex++] = next + 1;
                }
                else
                {
                    triangles[tIndex++] = current;
                    triangles[tIndex++] = current + 1;
                    triangles[tIndex++] = next;

                    triangles[tIndex++] = current + 1;
                    triangles[tIndex++] = next + 1;
                    triangles[tIndex++] = next;
                }
            }
        }

        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    public static GameObject BuildHealthPotionModel()
    {
        string path = $"{Folder}/HealthPotionModel.prefab";
        GameObject root = new GameObject("HealthPotionModel");

        Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
        if (litShader == null) litShader = Shader.Find("Standard");

        // 1. Crystal Glass Material (URP Transparent ruby crystal glass)
        Material glassMat = AssetDatabase.LoadAssetAtPath<Material>($"{Folder}/PotionGlassMat.mat");
        if (glassMat == null)
        {
            glassMat = new Material(litShader) { name = "PotionGlassMat" };
            AssetDatabase.CreateAsset(glassMat, $"{Folder}/PotionGlassMat.mat");
        }
        glassMat.shader = litShader;
        glassMat.SetFloat("_Surface", 1f); // Transparent
        glassMat.SetFloat("_Blend", 0f);   // Alpha
        glassMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        glassMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        glassMat.SetInt("_ZWrite", 0);
        glassMat.SetFloat("_Cull", 0f);    // Double-sided
        glassMat.SetFloat("_BlendModePreserveSpecular", 1f);
        glassMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        glassMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        glassMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        Color rubyGlass = new Color(1.0f, 0.18f, 0.28f, 0.40f);
        glassMat.color = rubyGlass;
        if (glassMat.HasProperty("_BaseColor")) glassMat.SetColor("_BaseColor", rubyGlass);
        glassMat.SetFloat("_Metallic", 0.08f);
        glassMat.SetFloat("_Smoothness", 0.98f);
        EditorUtility.SetDirty(glassMat);

        // 2. Glowing Ruby Elixir Material (Opaque vibrant red with emission)
        Material liquidMat = AssetDatabase.LoadAssetAtPath<Material>($"{Folder}/PotionLiquidMat.mat");
        if (liquidMat == null)
        {
            liquidMat = new Material(litShader) { name = "PotionLiquidMat" };
            AssetDatabase.CreateAsset(liquidMat, $"{Folder}/PotionLiquidMat.mat");
        }
        liquidMat.shader = litShader;
        Color richRed = new Color(0.98f, 0.04f, 0.14f, 1f);
        liquidMat.color = richRed;
        if (liquidMat.HasProperty("_BaseColor")) liquidMat.SetColor("_BaseColor", richRed);
        liquidMat.SetFloat("_Metallic", 0.04f);
        liquidMat.SetFloat("_Smoothness", 0.92f);
        liquidMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
        if (liquidMat.HasProperty("_EmissionColor"))
        {
            liquidMat.EnableKeyword("_EMISSION");
            liquidMat.SetColor("_EmissionColor", new Color(1.0f, 0.06f, 0.18f) * 2.5f);
        }
        EditorUtility.SetDirty(liquidMat);

        // 3. Cork Material
        Material corkMat = AssetDatabase.LoadAssetAtPath<Material>($"{Folder}/PotionCorkMat.mat");
        if (corkMat == null)
        {
            corkMat = new Material(litShader) { name = "PotionCorkMat" };
            AssetDatabase.CreateAsset(corkMat, $"{Folder}/PotionCorkMat.mat");
        }
        corkMat.shader = litShader;
        corkMat.color = new Color(0.58f, 0.38f, 0.22f);
        corkMat.SetFloat("_Smoothness", 0.15f);
        EditorUtility.SetDirty(corkMat);

        // 4. Gold Brass Material
        Material goldMat = AssetDatabase.LoadAssetAtPath<Material>($"{Folder}/PotionGoldMat.mat");
        if (goldMat == null)
        {
            goldMat = new Material(litShader) { name = "PotionGoldMat" };
            AssetDatabase.CreateAsset(goldMat, $"{Folder}/PotionGoldMat.mat");
        }
        goldMat.shader = litShader;
        goldMat.color = new Color(0.95f, 0.78f, 0.26f);
        goldMat.SetFloat("_Metallic", 0.92f);
        goldMat.SetFloat("_Smoothness", 0.85f);
        EditorUtility.SetDirty(goldMat);

        // --- Glass Flask Lathe Mesh ---
        Vector2[] flaskProfile = new Vector2[]
        {
            new Vector2(0.005f, 0.000f),
            new Vector2(0.038f, 0.003f),
            new Vector2(0.056f, 0.020f),
            new Vector2(0.072f, 0.048f),
            new Vector2(0.068f, 0.076f),
            new Vector2(0.045f, 0.105f),
            new Vector2(0.026f, 0.125f),
            new Vector2(0.025f, 0.150f),
            new Vector2(0.035f, 0.158f),
            new Vector2(0.033f, 0.165f),
            new Vector2(0.023f, 0.163f),
            new Vector2(0.022f, 0.135f)
        };
        Mesh flaskMesh = CreateLatheMesh(flaskProfile, 28);
        AssetDatabase.CreateAsset(flaskMesh, $"{Folder}/FlaskMesh.asset");

        GameObject flaskObj = new GameObject("GlassFlask");
        flaskObj.transform.SetParent(root.transform, false);
        flaskObj.AddComponent<MeshFilter>().sharedMesh = flaskMesh;
        flaskObj.AddComponent<MeshRenderer>().sharedMaterial = glassMat;

        // --- Liquid Lathe Mesh ---
        Vector2[] liquidProfile = new Vector2[]
        {
            new Vector2(0.005f, 0.005f),
            new Vector2(0.035f, 0.008f),
            new Vector2(0.052f, 0.022f),
            new Vector2(0.068f, 0.048f),
            new Vector2(0.064f, 0.076f),
            new Vector2(0.042f, 0.105f),
            new Vector2(0.023f, 0.125f),
            new Vector2(0.002f, 0.124f)
        };
        Mesh liquidMesh = CreateLatheMesh(liquidProfile, 28);
        AssetDatabase.CreateAsset(liquidMesh, $"{Folder}/LiquidMesh.asset");

        GameObject liquidObj = new GameObject("RubyLiquid");
        liquidObj.transform.SetParent(root.transform, false);
        liquidObj.AddComponent<MeshFilter>().sharedMesh = liquidMesh;
        liquidObj.AddComponent<MeshRenderer>().sharedMaterial = liquidMat;

        // --- Gold Collar Ring ---
        GameObject collar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        collar.name = "GoldCollar";
        collar.transform.SetParent(root.transform, false);
        collar.transform.localPosition = new Vector3(0, 0.138f, 0);
        collar.transform.localScale = new Vector3(0.056f, 0.010f, 0.056f);
        collar.GetComponent<Renderer>().sharedMaterial = goldMat;
        Object.DestroyImmediate(collar.GetComponent<Collider>());

        // --- Gold Pendant Medallion ---
        GameObject pendant = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pendant.name = "GoldPendant";
        pendant.transform.SetParent(collar.transform, false);
        pendant.transform.localPosition = new Vector3(0, -0.6f, 0.55f);
        pendant.transform.localRotation = Quaternion.Euler(0, 0, 45f); // Diamond shape
        pendant.transform.localScale = new Vector3(0.25f, 0.25f, 0.12f);
        pendant.GetComponent<Renderer>().sharedMaterial = goldMat;
        Object.DestroyImmediate(pendant.GetComponent<Collider>());

        // --- Natural Cork Stopper ---
        GameObject cork = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cork.name = "CorkStopper";
        cork.transform.SetParent(root.transform, false);
        cork.transform.localPosition = new Vector3(0, 0.174f, 0);
        cork.transform.localScale = new Vector3(0.046f, 0.022f, 0.046f);
        cork.GetComponent<Renderer>().sharedMaterial = corkMat;
        Object.DestroyImmediate(cork.GetComponent<Collider>());

        // --- Subtle Sparkling Bubble Particles inside liquid ---
        GameObject bubbleObj = new GameObject("Bubbles");
        bubbleObj.transform.SetParent(root.transform, false);
        bubbleObj.transform.localPosition = new Vector3(0, 0.04f, 0);
        var ps = bubbleObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 0.9f;
        main.startSpeed = 0.06f;
        main.startSize = 0.010f;
        main.startColor = new Color(1f, 0.2f, 0.3f, 0.95f);
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.playOnAwake = true;
        main.loop = true;

        var emission = ps.emission;
        emission.rateOverTime = 12f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.045f;

        Shader partShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (partShader == null) partShader = Shader.Find("Sprites/Default");
        Material bubbleMat = AssetDatabase.LoadAssetAtPath<Material>($"{Folder}/PotionBubbleMat.mat");
        if (bubbleMat == null)
        {
            bubbleMat = new Material(partShader) { name = "PotionBubbleMat" };
            AssetDatabase.CreateAsset(bubbleMat, $"{Folder}/PotionBubbleMat.mat");
        }
        bubbleMat.shader = partShader;
        bubbleMat.color = new Color(1f, 0.25f, 0.35f, 1f);
        EditorUtility.SetDirty(bubbleMat);
        bubbleObj.GetComponent<ParticleSystemRenderer>().sharedMaterial = bubbleMat;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    public static GameObject BuildGrenadeModel()
    {
        string path = $"{Folder}/GrenadeModel.prefab";
        GameObject root = new GameObject("GrenadeModel");

        Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
        if (litShader == null) litShader = Shader.Find("Standard");

        // 1. Cast Iron Material (Dark hammered metal)
        Material ironMat = new Material(litShader) { name = "GrenadeIronMat" };
        ironMat.color = new Color(0.18f, 0.20f, 0.22f);
        ironMat.SetFloat("_Metallic", 0.88f);
        ironMat.SetFloat("_Smoothness", 0.45f);
        AssetDatabase.CreateAsset(ironMat, $"{Folder}/GrenadeIronMat.mat");

        // 2. Antique Dark Brass Material
        Material brassMat = new Material(litShader) { name = "GrenadeBrassMat" };
        brassMat.color = new Color(0.55f, 0.45f, 0.28f);
        brassMat.SetFloat("_Metallic", 0.92f);
        brassMat.SetFloat("_Smoothness", 0.70f);
        AssetDatabase.CreateAsset(brassMat, $"{Folder}/GrenadeBrassMat.mat");

        // 3. Braided Hemp Fuse Rope Material
        Material ropeMat = new Material(litShader) { name = "GrenadeRopeMat" };
        ropeMat.color = new Color(0.78f, 0.62f, 0.38f);
        ropeMat.SetFloat("_Smoothness", 0.12f);
        AssetDatabase.CreateAsset(ropeMat, $"{Folder}/GrenadeRopeMat.mat");

        // --- Cast Iron Bomb Sphere ---
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        body.name = "BombSphere";
        body.transform.SetParent(root.transform, false);
        body.transform.localPosition = new Vector3(0, 0.075f, 0);
        body.transform.localScale = new Vector3(0.14f, 0.14f, 0.14f);
        body.GetComponent<Renderer>().sharedMaterial = ironMat;
        Object.DestroyImmediate(body.GetComponent<Collider>());

        // --- Brass Reinforcing Equator Band ---
        GameObject band = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        band.name = "ReinforceBand";
        band.transform.SetParent(root.transform, false);
        band.transform.localPosition = new Vector3(0, 0.075f, 0);
        band.transform.localScale = new Vector3(0.144f, 0.016f, 0.144f);
        band.GetComponent<Renderer>().sharedMaterial = brassMat;
        Object.DestroyImmediate(band.GetComponent<Collider>());

        // 6 Brass Studs around the band
        for (int i = 0; i < 6; i++)
        {
            float angle = (i / 6f) * Mathf.PI * 2f;
            float rx = Mathf.Cos(angle) * 0.073f;
            float rz = Mathf.Sin(angle) * 0.073f;
            GameObject stud = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            stud.name = "Stud_" + i;
            stud.transform.SetParent(root.transform, false);
            stud.transform.localPosition = new Vector3(rx, 0.075f, rz);
            stud.transform.localScale = new Vector3(0.016f, 0.016f, 0.016f);
            stud.GetComponent<Renderer>().sharedMaterial = brassMat;
            Object.DestroyImmediate(stud.GetComponent<Collider>());
        }

        // --- Top Brass Collar Nozzle ---
        GameObject nozzle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        nozzle.name = "FuseNozzle";
        nozzle.transform.SetParent(root.transform, false);
        nozzle.transform.localPosition = new Vector3(0, 0.146f, 0);
        nozzle.transform.localScale = new Vector3(0.052f, 0.026f, 0.052f);
        nozzle.GetComponent<Renderer>().sharedMaterial = brassMat;
        Object.DestroyImmediate(nozzle.GetComponent<Collider>());

        // --- Braided Fuse Rope ---
        GameObject fuse = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        fuse.name = "FuseRope";
        fuse.transform.SetParent(root.transform, false);
        fuse.transform.localPosition = new Vector3(0.008f, 0.178f, 0);
        fuse.transform.localRotation = Quaternion.Euler(0, 0, -18f); // Curved tilt
        fuse.transform.localScale = new Vector3(0.018f, 0.038f, 0.018f);
        fuse.GetComponent<Renderer>().sharedMaterial = ropeMat;
        Object.DestroyImmediate(fuse.GetComponent<Collider>());

        // --- Active Live Sizzling Spark ParticleSystem ---
        GameObject sparkObj = new GameObject("FuseSizzlingSparks");
        sparkObj.transform.SetParent(root.transform, false);
        sparkObj.transform.localPosition = new Vector3(0.018f, 0.205f, 0);

        var ps = sparkObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 0.25f;
        main.startSpeed = 0.85f;
        main.startSize = 0.022f;
        main.startColor = new Color(1f, 0.8f, 0.15f, 1f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.playOnAwake = true;
        main.loop = true;

        var emission = ps.emission;
        emission.rateOverTime = 25f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 35f;
        shape.radius = 0.01f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(new Color(1f, 0.95f, 0.4f), 0f), new GradientColorKey(new Color(1f, 0.2f, 0.02f), 0.7f), new GradientColorKey(new Color(0.2f, 0.2f, 0.2f), 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) }
        );
        colorOverLifetime.color = grad;

        Shader partShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (partShader == null) partShader = Shader.Find("Sprites/Default");
        var sparkMat = new Material(partShader) { name = "BombSparkMat" };
        sparkMat.color = new Color(1f, 0.85f, 0.2f, 1f);
        sparkObj.GetComponent<ParticleSystemRenderer>().sharedMaterial = sparkMat;
        AssetDatabase.CreateAsset(sparkMat, $"{Folder}/BombSparkMat.mat");

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    public static GameObject BuildThrownGrenadePrefab()
    {
        string path = $"{Folder}/ThrownGrenade.prefab";
        GameObject root = new GameObject("ThrownGrenade");

        var col = root.AddComponent<SphereCollider>();
        col.radius = 0.075f;

        // Bouncy physics material
        var pMat = new PhysicsMaterial("GrenadeBounce")
        {
            bounciness = 0.42f,
            frictionCombine = PhysicsMaterialCombine.Average,
            bounceCombine = PhysicsMaterialCombine.Average,
            dynamicFriction = 0.5f,
            staticFriction = 0.6f
        };
        col.material = pMat;
        AssetDatabase.CreateAsset(pMat, $"{Folder}/GrenadeBounce.physicMaterial");

        var rb = root.AddComponent<Rigidbody>();
        rb.mass = 0.45f;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        var proj = root.AddComponent<GrenadeProjectile>();

        // Load explosion VFX
        var vfx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/VFX/vfx_Explosion_01.prefab");
        if (vfx == null) vfx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DL/GabrielAguiarProductions/1/FreeQuickEffectsVol1/Prefabs/vfx_Explosion_01.prefab");

        var so = new SerializedObject(proj);
        so.FindProperty("explosionVfxPrefab").objectReferenceValue = vfx;
        so.FindProperty("fuseTime").floatValue = 1.8f;
        so.FindProperty("explosionRadius").floatValue = 6.5f;
        so.FindProperty("explosionDamage").floatValue = 120f;
        so.ApplyModifiedProperties();

        // Visual Child
        var modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{Folder}/GrenadeModel.prefab");
        if (modelPrefab != null)
        {
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab, root.transform);
            visual.name = "Visual";
            visual.transform.localPosition = new Vector3(0, -0.075f, 0); // Center the sphere
        }

        // Trail spark
        var trailObj = new GameObject("FuseTrail");
        trailObj.transform.SetParent(root.transform, false);
        trailObj.transform.localPosition = new Vector3(0, 0.12f, 0);
        var trail = trailObj.AddComponent<TrailRenderer>();
        trail.time = 0.28f;
        trail.startWidth = 0.04f;
        trail.endWidth = 0.005f;
        Shader s = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (s == null) s = Shader.Find("Sprites/Default");
        var tMat = new Material(s) { name = "FuseTrailMat" };
        tMat.color = new Color(1f, 0.8f, 0.2f, 1f);
        trail.material = tMat;
        trail.startColor = new Color(1f, 0.85f, 0.2f, 1f);
        trail.endColor = new Color(1f, 0.2f, 0.05f, 0f);
        AssetDatabase.CreateAsset(tMat, $"{Folder}/FuseTrailMat.mat");

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    [MenuItem("GunQuest/Items/Setup Player 6 Slots Inventory", false, 51)]
    public static void SetupActivePlayerInventory()
    {
        var player = Object.FindFirstObjectByType<PlayerWeapons>();
        if (player == null)
        {
            Debug.LogError("No PlayerWeapons found in the active scene!");
            return;
        }

        SetupPlayerInventory(player.gameObject);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[ItemPrefabBuilder] Active player inventory successfully expanded to 6 slots!");
    }

    public static void SetupPlayerInventory(GameObject playerObj)
    {
        var weapons = playerObj.GetComponent<PlayerWeapons>();
        if (weapons == null) return;

        var socket = playerObj.transform.Find("FemaleVisual/root/pelvis/spine_01/spine_02/spine_03/clavicle_r/upperarm_r/lowerarm_r/hand_r/FemaleWeaponSocket");
        if (socket == null)
        {
            Debug.LogError("FemaleWeaponSocket not found on player!");
            return;
        }

        var potionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{Folder}/HealthPotionModel.prefab");
        var grenadePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{Folder}/GrenadeModel.prefab");
        var thrownPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{Folder}/ThrownGrenade.prefab");
        var healVfxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/VFX/vfx_Heal_01.prefab");
        if (healVfxPrefab == null) healVfxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DL/GabrielAguiarProductions/1/FreeQuickEffectsVol1/Prefabs/vfx_Heal_01.prefab");

        var existingPotion = socket.Find("HealthPotionModel");
        if (existingPotion != null) Object.DestroyImmediate(existingPotion.gameObject);
        var potionInst = (GameObject)PrefabUtility.InstantiatePrefab(potionPrefab, socket);
        potionInst.name = "HealthPotionModel";
        potionInst.transform.localPosition = new Vector3(0.04f, -0.04f, 0.015f);
        potionInst.transform.localRotation = Quaternion.Euler(5f, 80f, 85f);
        potionInst.transform.localScale = Vector3.one * 1.05f;
        potionInst.SetActive(false);

        var existingGrenade = socket.Find("GrenadeModel");
        if (existingGrenade != null) Object.DestroyImmediate(existingGrenade.gameObject);
        var grenadeInst = (GameObject)PrefabUtility.InstantiatePrefab(grenadePrefab, socket);
        grenadeInst.name = "GrenadeModel";
        grenadeInst.transform.localPosition = new Vector3(0.04f, -0.035f, 0.015f);
        grenadeInst.transform.localRotation = Quaternion.Euler(0f, 85f, 90f);
        grenadeInst.transform.localScale = Vector3.one * 1.1f;
        grenadeInst.SetActive(false);

        var so = new SerializedObject(weapons);
        var slotsProp = so.FindProperty("slots");
        slotsProp.arraySize = 6;

        // Slot 4: Bình Máu
        var s4 = slotsProp.GetArrayElementAtIndex(4);
        s4.FindPropertyRelative("label").stringValue = "Bình Máu";
        s4.FindPropertyRelative("model").objectReferenceValue = potionInst;
        s4.FindPropertyRelative("muzzle").objectReferenceValue = null;
        s4.FindPropertyRelative("melee").boolValue = false;
        s4.FindPropertyRelative("automatic").boolValue = false;
        s4.FindPropertyRelative("kind").enumValueIndex = (int)SlotKind.Consumable;
        s4.FindPropertyRelative("count").intValue = 3;
        s4.FindPropertyRelative("maxCount").intValue = 5;
        s4.FindPropertyRelative("damage").floatValue = 50f;
        s4.FindPropertyRelative("cooldown").floatValue = 1.0f;
        s4.FindPropertyRelative("range").floatValue = 0f;
        s4.FindPropertyRelative("itemPrefab").objectReferenceValue = healVfxPrefab;

        // Slot 5: Lựu Đạn
        var s5 = slotsProp.GetArrayElementAtIndex(5);
        s5.FindPropertyRelative("label").stringValue = "Lựu Đạn";
        s5.FindPropertyRelative("model").objectReferenceValue = grenadeInst;
        s5.FindPropertyRelative("muzzle").objectReferenceValue = null;
        s5.FindPropertyRelative("melee").boolValue = false;
        s5.FindPropertyRelative("automatic").boolValue = false;
        s5.FindPropertyRelative("kind").enumValueIndex = (int)SlotKind.Throwable;
        s5.FindPropertyRelative("count").intValue = 3;
        s5.FindPropertyRelative("maxCount").intValue = 5;
        s5.FindPropertyRelative("damage").floatValue = 120f;
        s5.FindPropertyRelative("cooldown").floatValue = 1.2f;
        s5.FindPropertyRelative("range").floatValue = 6.5f;
        s5.FindPropertyRelative("itemPrefab").objectReferenceValue = thrownPrefab;

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(weapons);
    }
}
