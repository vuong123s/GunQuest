#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

public static class EnemyAnimatorAndPrefabBuilder
{
    public const string AnimOutputFolder = "Assets/Animations/Enemy";
    public const string BulletPrefabPath = "Assets/Prefabs/Bullet.prefab";

    public class EnemyConfig
    {
        public string prefabName;
        public string sourceController;
        public string idleNormal;
        public string idleBattle;
        public string walkFWD;
        public string walkBWD;
        public string walkLeft;
        public string walkRight;
        public string run;
        public string senseStart;
        public string senseLoop;
        public string[] attacks;
        public string getHit;
        public string dizzy;
        public string die;
        public string taunt;
        public string victory;

        public float colRadius;
        public float colHeight;
        public float colCenterY;
        public float agentRadius;
        public float agentHeight;
        public float agentSpeed;
        public float maxHealth;
        public float barHeight;
        public float detectionRange;
        public float shootRange;
        public float preferredRange;
        public float projectileDamage;
        public float fireRate;
        public float projectileSpeed;
        public string muzzleBone;
    }

    private static readonly EnemyConfig[] AllConfigs = new[]
    {
        new EnemyConfig
        {
            prefabName = "BeholderPBRDefault",
            sourceController = "Assets/DL/RPGMonsterPartnersPBRPolyart/Animators/Beholder.controller",
            idleNormal = "IdleNormal", idleBattle = "IdleBattle",
            walkFWD = "WalkFWD", walkBWD = "WalkBWD", walkLeft = "WalkLeft", walkRight = "WalkRight", run = "Run",
            senseStart = "SenseSomethingST", senseLoop = "SenseSomethingRPT",
            attacks = new[] { "Attack01", "Attack02ST", "Attack03" },
            getHit = "GetHit", dizzy = "Dizzy", die = "Die", taunt = "Taunting", victory = "Victory",
            colRadius = 0.75f, colHeight = 2.4f, colCenterY = 1.20f,
            agentRadius = 0.85f, agentHeight = 2.4f, agentSpeed = 3.5f,
            maxHealth = 220f, barHeight = 2.8f,
            detectionRange = 24f, shootRange = 14f, preferredRange = 8f,
            projectileDamage = 20f, fireRate = 1.2f, projectileSpeed = 60f,
            muzzleBone = "EyeBallCTRL"
        },
        new EnemyConfig
        {
            prefabName = "BlueSlime",
            sourceController = "Assets/DL/RPGMonsterCouplePBRPolyart/Animation/BlueSlime/BlueSlime.controller",
            idleNormal = "BlueSlime_IdleNormal", idleBattle = "BlueSlime_IdleBattle",
            walkFWD = "BlueSlime_WalkFWD", walkBWD = "BlueSlime_WalkBWD", walkLeft = "BlueSlime_WalkLFT", walkRight = "BlueSlime_WalkRGT", run = "BlueSlime_RunFWD",
            senseStart = "BlueSlime_SenseSomethingStart", senseLoop = "BlueSlime_SenseSomethingRoutine",
            attacks = new[] { "BlueSlime_Attack01", "BlueSlime_Attack02" },
            getHit = "BlueSlime_GetHit", dizzy = "BlueSlime_Dizzy", die = "BlueSlime_Die", taunt = "BlueSlime_Taunting", victory = "BlueSlime_Victory",
            colRadius = 0.50f, colHeight = 0.95f, colCenterY = 0.475f,
            agentRadius = 0.60f, agentHeight = 0.95f, agentSpeed = 3.2f,
            maxHealth = 75f, barHeight = 1.3f,
            detectionRange = 20f, shootRange = 12f, preferredRange = 6f,
            projectileDamage = 12f, fireRate = 1.3f, projectileSpeed = 50f,
            muzzleBone = "EyeA01"
        },
        new EnemyConfig
        {
            prefabName = "CactusPBR",
            sourceController = "Assets/DL/RPGMonsterBuddiesPBRPA/Animation/Cactus/Cactus.controller",
            idleNormal = "Cactus_IdleNormal", idleBattle = "Cactus_IdleBattle",
            walkFWD = "Cactus_WalkFWD", walkBWD = "Cactus_WalkBWD", walkLeft = "Cactus_WalkLFT", walkRight = "Cactus_WalkRGT", run = "Cactus_RunFWD",
            senseStart = "Cactus_SenseSomethingStart", senseLoop = "Cactus_SenseSomethingMaint",
            attacks = new[] { "Cactus_Attack01", "Cactus_Attack02" },
            getHit = "Cactus_GetHit", dizzy = "Cactus_Dizzy", die = "Cactus_Die", taunt = "Cactus_Taunting", victory = "Cactus_Victory",
            colRadius = 0.55f, colHeight = 1.35f, colCenterY = 0.675f,
            agentRadius = 0.65f, agentHeight = 1.35f, agentSpeed = 3.4f,
            maxHealth = 110f, barHeight = 1.7f,
            detectionRange = 22f, shootRange = 13f, preferredRange = 7f,
            projectileDamage = 16f, fireRate = 1.2f, projectileSpeed = 55f,
            muzzleBone = "cactus_mouth"
        },
        new EnemyConfig
        {
            prefabName = "ChestMonsterPBRDefault",
            sourceController = "Assets/DL/RPGMonsterPartnersPBRPolyart/Animators/ChestMonster.controller",
            idleNormal = "IdleNormal", idleBattle = "IdleBattle",
            walkFWD = "WalkFWD", walkBWD = "WalkBWD", walkLeft = "WalkLeft", walkRight = "WalkRight", run = "Run",
            senseStart = "SenseSomethingST", senseLoop = "SenseSomethingRPT",
            attacks = new[] { "Attack01", "Attack02" },
            getHit = "GetHit", dizzy = "Dizzy", die = "Die", taunt = "Taunting", victory = "Victory",
            colRadius = 0.55f, colHeight = 1.35f, colCenterY = 0.675f,
            agentRadius = 0.65f, agentHeight = 1.35f, agentSpeed = 3.6f,
            maxHealth = 140f, barHeight = 1.7f,
            detectionRange = 20f, shootRange = 11f, preferredRange = 5f,
            projectileDamage = 22f, fireRate = 1.1f, projectileSpeed = 60f,
            muzzleBone = "Head"
        },
        new EnemyConfig
        {
            prefabName = "MushroomAngryPBR",
            sourceController = "Assets/DL/RPGMonsterBuddiesPBRPA/Animation/Mushroom/MushroomAngry.controller",
            idleNormal = "Mushroom_IdleNormalAngry", idleBattle = "Mushroom_IdleBattleAngry",
            walkFWD = "Mushroom_walkFWDAngry", walkBWD = "Mushroom_walkBWDAngry", walkLeft = "Mushroom_walkLFTAngry", walkRight = "Mushroom_walkRGTAngry", run = "Mushroom_runFWDAngry",
            senseStart = "Mushroom_SenseSomethingStartAngry", senseLoop = "Mushroom_SenseSomethingMaintainAngry",
            attacks = new[] { "Mushroom_Attack01Angry", "Mushroom_Attack02Angry", "Mushroom_Attack03Angry" },
            getHit = "Mushroom_GetHitAngry", dizzy = "Mushroom_DizzyAngry", die = "Mushroom_DieAngry", taunt = "Mushroom_TauntingAngry", victory = "Mushroom_VictoryAngry",
            colRadius = 0.55f, colHeight = 1.45f, colCenterY = 0.725f,
            agentRadius = 0.65f, agentHeight = 1.45f, agentSpeed = 3.5f,
            maxHealth = 100f, barHeight = 1.7f,
            detectionRange = 22f, shootRange = 13f, preferredRange = 7f,
            projectileDamage = 15f, fireRate = 1.1f, projectileSpeed = 55f,
            muzzleBone = "mushroom_head"
        },
        new EnemyConfig
        {
            prefabName = "P01",
            sourceController = "Assets/DL/PartyMonsterDuo/Animator/Default.controller",
            idleNormal = "Idle03", idleBattle = "Idle03",
            walkFWD = "Run01FWD", walkBWD = "Run01BWD", walkLeft = "Run01Left", walkRight = "Run01Right", run = "Run01FWD",
            senseStart = null, senseLoop = null,
            attacks = new[] { "Attack01", "Attack02" },
            getHit = "GetHit", dizzy = "Dizzy", die = "Die01", taunt = "Dance01", victory = "Victory01",
            colRadius = 0.65f, colHeight = 1.75f, colCenterY = 0.875f,
            agentRadius = 0.75f, agentHeight = 1.75f, agentSpeed = 4.0f,
            maxHealth = 160f, barHeight = 2.1f,
            detectionRange = 24f, shootRange = 13f, preferredRange = 6f,
            projectileDamage = 22f, fireRate = 1.0f, projectileSpeed = 60f,
            muzzleBone = "Headparts"
        },
        new EnemyConfig
        {
            prefabName = "P02",
            sourceController = "Assets/DL/PartyMonsterDuo/Animator/Default.controller",
            idleNormal = "Idle03", idleBattle = "Idle03",
            walkFWD = "Run01FWD", walkBWD = "Run01BWD", walkLeft = "Run01Left", walkRight = "Run01Right", run = "Run01FWD",
            senseStart = null, senseLoop = null,
            attacks = new[] { "Attack01", "Attack02" },
            getHit = "GetHit", dizzy = "Dizzy", die = "Die01", taunt = "Dance01", victory = "Victory01",
            colRadius = 0.80f, colHeight = 2.30f, colCenterY = 1.15f,
            agentRadius = 0.90f, agentHeight = 2.30f, agentSpeed = 3.3f,
            maxHealth = 220f, barHeight = 2.7f,
            detectionRange = 25f, shootRange = 14f, preferredRange = 7f,
            projectileDamage = 28f, fireRate = 1.4f, projectileSpeed = 65f,
            muzzleBone = "MouthandNoses"
        },
        new EnemyConfig
        {
            prefabName = "SlimePBR",
            sourceController = "Assets/DL/RPG Monster DUO PBR Polyart/Animators/Slime.controller",
            idleNormal = "IdleNormal", idleBattle = "IdleBattle",
            walkFWD = "WalkFWD", walkBWD = "WalkBWD", walkLeft = "WalkLeft", walkRight = "WalkRight", run = "RunFWD",
            senseStart = "SenseSomethingST", senseLoop = "SenseSomethingRPT",
            attacks = new[] { "Attack01", "Attack02" },
            getHit = "GetHit", dizzy = "Dizzy", die = "Die", taunt = "Taunt", victory = "Victory",
            colRadius = 0.50f, colHeight = 1.10f, colCenterY = 0.55f,
            agentRadius = 0.60f, agentHeight = 1.10f, agentSpeed = 3.3f,
            maxHealth = 85f, barHeight = 1.4f,
            detectionRange = 20f, shootRange = 12f, preferredRange = 6f,
            projectileDamage = 14f, fireRate = 1.2f, projectileSpeed = 52f,
            muzzleBone = "EyeCTRL"
        },
        new EnemyConfig
        {
            prefabName = "StarFish",
            sourceController = "Assets/DL/RPGMonsterCouplePBRPolyart/Animation/StarFish/StarFish.controller",
            idleNormal = "StarFish_IdleNormal", idleBattle = "StarFish_IdleBattle",
            walkFWD = "StarFish_MoveFWD", walkBWD = "StarFish_MoveBWD", walkLeft = "StarFish_MoveLFT", walkRight = "StarFish_MoveRGT", run = "StarFish_MoveFWD",
            senseStart = "StarFish_SenseSomethingStart", senseLoop = "StarFish_SenseSomethingRoutine",
            attacks = new[] { "StarFish_Attack01", "StarFish_Attack02" },
            getHit = "StarFish_GetHit", dizzy = "StarFish_Dizzy", die = "StarFish_Die", taunt = "StarFish_Taunting", victory = "StarFish_Victory",
            colRadius = 0.55f, colHeight = 1.20f, colCenterY = 0.60f,
            agentRadius = 0.65f, agentHeight = 1.20f, agentSpeed = 3.5f,
            maxHealth = 95f, barHeight = 1.5f,
            detectionRange = 22f, shootRange = 13f, preferredRange = 7f,
            projectileDamage = 16f, fireRate = 1.2f, projectileSpeed = 55f,
            muzzleBone = null
        },
        new EnemyConfig
        {
            prefabName = "Swarm08",
            sourceController = "Assets/DL/Monster Minion Survivor PBR Polyart/Animation/Swarm08/Swarm08.controller",
            idleNormal = "Swarm08_Idle", idleBattle = "Swarm08_Idle",
            walkFWD = "Swarm08_MoveFWD", walkBWD = "Swarm08_MoveBWD", walkLeft = "Swarm08_MoveLFT", walkRight = "Swarm08_MoveRGT", run = null,
            senseStart = null, senseLoop = null,
            attacks = new[] { "Swarm08_Attack" },
            getHit = "Swarm08_GetHit", dizzy = null, die = "Swarm08_Die", taunt = null, victory = null,
            colRadius = 0.40f, colHeight = 0.90f, colCenterY = 0.45f,
            agentRadius = 0.50f, agentHeight = 0.90f, agentSpeed = 4.2f,
            maxHealth = 50f, barHeight = 1.2f,
            detectionRange = 22f, shootRange = 10f, preferredRange = 4f,
            projectileDamage = 10f, fireRate = 0.9f, projectileSpeed = 50f,
            muzzleBone = "Head"
        },
        new EnemyConfig
        {
            prefabName = "Swarm09",
            sourceController = "Assets/DL/Monster Minion Survivor PBR Polyart/Animation/Swarm09/Swarm09.controller",
            idleNormal = "Swarm09_Idle", idleBattle = "Swarm09_Idle",
            walkFWD = "Swarm09_MoveFWD", walkBWD = "Swarm09_MoveBWD", walkLeft = "Swarm09_MoveLFT", walkRight = "Swarm09_MoveRGT", run = null,
            senseStart = null, senseLoop = null,
            attacks = new[] { "Swarm09_Attack" },
            getHit = "Swarm09_GetHit", dizzy = null, die = "Swarm09_Die", taunt = null, victory = null,
            colRadius = 0.40f, colHeight = 0.95f, colCenterY = 0.475f,
            agentRadius = 0.50f, agentHeight = 0.95f, agentSpeed = 4.4f,
            maxHealth = 55f, barHeight = 1.3f,
            detectionRange = 22f, shootRange = 10f, preferredRange = 4f,
            projectileDamage = 11f, fireRate = 0.9f, projectileSpeed = 50f,
            muzzleBone = "Head"
        },
        new EnemyConfig
        {
            prefabName = "TurtleShellPBR",
            sourceController = "Assets/DL/RPG Monster DUO PBR Polyart/Animators/TurtleShell.controller",
            idleNormal = "IdleNormal", idleBattle = "IdleBattle",
            walkFWD = "WalkFWD", walkBWD = "WalkBWD", walkLeft = "WalkLeft", walkRight = "WalkRight", run = "RunFWD",
            senseStart = "SenseSomethingST", senseLoop = "SenseSomethingRPT",
            attacks = new[] { "Attack01", "Attack02" },
            getHit = "GetHit", dizzy = "Dizzy", die = "Die", taunt = "Taunt", victory = "Victory",
            colRadius = 0.55f, colHeight = 1.10f, colCenterY = 0.55f,
            agentRadius = 0.65f, agentHeight = 1.10f, agentSpeed = 2.8f,
            maxHealth = 160f, barHeight = 1.5f,
            detectionRange = 20f, shootRange = 11f, preferredRange = 6f,
            projectileDamage = 18f, fireRate = 1.3f, projectileSpeed = 50f,
            muzzleBone = "Head"
        }
    };

    [MenuItem("GunQuest/Enemies/Rebuild All Enemy Animators and Prefabs", false, 30)]
    public static void RebuildAll()
    {
        EnsureFolder(AnimOutputFolder);
        int successCount = 0;

        foreach (var cfg in AllConfigs)
        {
            try
            {
                var controller = BuildAnimatorForConfig(cfg);
                if (controller != null)
                {
                    ConfigurePrefab(cfg, controller);
                    successCount++;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EnemySetup] Failed building {cfg.prefabName}: {ex.Message}\n{ex.StackTrace}");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Also update legacy Monster2..5 prefabs in Assets/Prefabs to use the new controllers
        UpdateLegacyMonsterPrefabs();

        Debug.Log($"[EnemySetup] Successfully processed {successCount}/{AllConfigs.Length} enemies!");
    }

    private static void UpdateLegacyMonsterPrefabs()
    {
        var legacyMap = new Dictionary<string, string>
        {
            { "Assets/Prefabs/Monster2.prefab", "P01" },
            { "Assets/Prefabs/Monster3.prefab", "P02" },
            { "Assets/Prefabs/Monster4.prefab", "SlimePBR" },
            { "Assets/Prefabs/Monster5.prefab", "TurtleShellPBR" }
        };

        foreach (var kvp in legacyMap)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(kvp.Key) == null) continue;
            var cfg = AllConfigs.FirstOrDefault(c => c.prefabName == kvp.Value);
            if (cfg == null) continue;

            var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>($"{AnimOutputFolder}/{cfg.prefabName}.controller");
            if (ctrl == null) continue;

            GameObject root = PrefabUtility.LoadPrefabContents(kvp.Key);
            try
            {
                var anim = root.GetComponentInChildren<Animator>(true);
                if (anim != null)
                {
                    anim.runtimeAnimatorController = ctrl;
                    anim.applyRootMotion = false;
                }
                var agent = root.GetComponent<NavMeshAgent>();
                if (agent == null) agent = root.AddComponent<NavMeshAgent>();
                agent.radius = cfg.agentRadius;
                agent.height = cfg.agentHeight;
                agent.speed = cfg.agentSpeed;
                agent.updateRotation = false;

                var col = root.GetComponent<CapsuleCollider>();
                if (col == null) col = root.AddComponent<CapsuleCollider>();
                col.center = Vector3.up * cfg.colCenterY;
                col.radius = cfg.colRadius;
                col.height = cfg.colHeight;

                var hp = root.GetComponent<EnemyHealth>();
                if (hp == null) hp = root.AddComponent<EnemyHealth>();

                var ai = root.GetComponent<EnemyAI>();
                if (ai == null) ai = root.AddComponent<EnemyAI>();

                PrefabUtility.SaveAsPrefabAsset(root, kvp.Key);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }

    [MenuItem("GunQuest/Enemies/Populate Autumn Valley With All Enemies", false, 31)]
    public static void PopulateAutumnValley()
    {
        var activeScene = EditorSceneManager.GetActiveScene();
        if (activeScene.path != AutumnValleyMapBuilder.ScenePath)
        {
            EditorSceneManager.OpenScene(AutumnValleyMapBuilder.ScenePath, OpenSceneMode.Single);
        }

        var enemyGroupObj = GameObject.Find("EnemyGroup");
        if (enemyGroupObj == null)
        {
            enemyGroupObj = new GameObject("EnemyGroup");
        }
        Transform enemyGroup = enemyGroupObj.transform;

        // Keep boss Monster1 if present
        var boss = GameObject.Find("Monster1");

        // Remove old generic ValleyGuard slimes
        var oldGuards = new List<GameObject>();
        for (int i = 0; i < enemyGroup.childCount; i++)
        {
            var child = enemyGroup.GetChild(i).gameObject;
            if (child != boss && child.name.StartsWith("Monster4_ValleyGuard"))
            {
                oldGuards.Add(child);
            }
        }
        foreach (var og in oldGuards)
        {
            Object.DestroyImmediate(og);
        }

        // Remove any previously added populated enemies to allow idempotent re-population
        var oldPopulated = new List<GameObject>();
        for (int i = 0; i < enemyGroup.childCount; i++)
        {
            var child = enemyGroup.GetChild(i).gameObject;
            if (child != boss && child.name.StartsWith("Enemy_"))
            {
                oldPopulated.Add(child);
            }
        }
        foreach (var op in oldPopulated)
        {
            Object.DestroyImmediate(op);
        }

        // Tiered Level Design (Option 1): 7 strategic enemies + 1 Boss = 8 total enemies
        var placements = new[]
        {
            // --- Chặng 1: Khởi động (Lối vào bờ sông phía Nam - 2 quái) ---
            new { prefab = "BlueSlime", x = -8f, z = -34f, name = "Enemy_BlueSlime_1" },
            new { prefab = "Swarm08", x = 6f, z = -32f, name = "Enemy_Swarm08_1" },

            // --- Chặng 2: Thử thách giữa thung lũng (Trại & Ngã rẽ - 3 quái) ---
            new { prefab = "P01", x = -10f, z = 2f, name = "Enemy_P01_WestTrail" },
            new { prefab = "ChestMonsterPBRDefault", x = 14f, z = -8f, name = "Enemy_Chest_EastRuins" },
            new { prefab = "CactusPBR", x = -3f, z = 20f, name = "Enemy_Cactus_Junction" },

            // --- Chặng 3: Đỉnh điểm (Cổng đền thờ & Hộ vệ Boss - 2 hộ vệ tinh anh) ---
            new { prefab = "TurtleShellPBR", x = 0f, z = 32f, name = "Enemy_Turtle_InnerGate" },
            new { prefab = "BeholderPBRDefault", x = 10f, z = 35f, name = "Enemy_Beholder_NorthFlank" }
        };

        var terrain = Terrain.activeTerrain;
        foreach (var p in placements)
        {
            var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefabs/Enemy/{p.prefab}.prefab");
            if (prefabAsset == null)
            {
                Debug.LogWarning($"[EnemyPopulate] Prefab Assets/Prefabs/Enemy/{p.prefab}.prefab not found.");
                continue;
            }

            Vector3 spawnPos = FindGuaranteedClearSpot(terrain, new Vector3(p.x, 3f, p.z), 5f);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset, enemyGroup);
            instance.name = p.name;
            instance.transform.position = spawnPos;
            instance.transform.rotation = Quaternion.Euler(0, 180f, 0);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log($"[EnemyPopulate] Autumn Valley populated with {placements.Length} varied animated enemies in verified clear locations!");
    }

    [MenuItem("GunQuest/Enemies/Fix Map Colliders and Floating Objects", false, 32)]
    public static void FixEnvironmentAndColliders()
    {
        var activeScene = EditorSceneManager.GetActiveScene();
        if (activeScene.path != AutumnValleyMapBuilder.ScenePath)
        {
            EditorSceneManager.OpenScene(AutumnValleyMapBuilder.ScenePath, OpenSceneMode.Single);
        }

        var terrain = Terrain.activeTerrain;
        if (terrain == null)
        {
            Debug.LogError("[FixEnvironment] No active Terrain found!");
            return;
        }
        var tData = terrain.terrainData;

        // 1. Fix all rocks/stones
        var allStones = GameObject.FindObjectsByType<Transform>(FindObjectsSortMode.None)
            .Where(t => t.name.ToLower().Contains("stone") && t.GetComponent<Terrain>() == null).ToList();

        int rocksFixed = 0;
        int rocksRemoved = 0;
        foreach (var s in allStones)
        {
            // Remove steep un-climbable boundary rocks in vegetation that float on cliffs (>28 deg)
            float normX = (s.position.x - terrain.transform.position.x) / tData.size.x;
            float normZ = (s.position.z - terrain.transform.position.z) / tData.size.z;
            float steepness = tData.GetSteepness(normX, normZ);

            if (s.parent != null && s.parent.name == "AutumnForest" && steepness > 28f)
            {
                Object.DestroyImmediate(s.gameObject);
                rocksRemoved++;
                continue;
            }

            // Replace coarse BoxCollider with exact MeshCollider
            var boxCols = s.GetComponentsInChildren<BoxCollider>(true);
            foreach (var bc in boxCols) Object.DestroyImmediate(bc);

            var mf = s.GetComponentInChildren<MeshFilter>(true);
            if (mf != null && mf.sharedMesh != null)
            {
                var mc = s.GetComponent<MeshCollider>();
                if (mc == null) mc = s.gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
            }

            // Ground the rock: sink it into the slope so the lowest part is buried, eliminating any gaps underneath
            float h = terrain.SampleHeight(s.position);
            float scale = s.lossyScale.x;
            float sink = Mathf.Tan(steepness * Mathf.Deg2Rad) * (2.6f * scale) + 0.45f;
            s.position = new Vector3(s.position.x, h - sink, s.position.z);
            rocksFixed++;
        }

        // 2. Fix all trees in AutumnForest
        var veg = GameObject.Find("AutumnForest");
        int treesRemoved = 0;
        int treesGrounded = 0;
        if (veg != null)
        {
            var trees = new List<GameObject>();
            for (int i = 0; i < veg.transform.childCount; i++)
            {
                var child = veg.transform.GetChild(i).gameObject;
                if (child.name.ToLower().Contains("tree")) trees.Add(child);
            }

            foreach (var tree in trees)
            {
                float normX = (tree.transform.position.x - terrain.transform.position.x) / tData.size.x;
                float normZ = (tree.transform.position.z - terrain.transform.position.z) / tData.size.z;
                float steepness = tData.GetSteepness(normX, normZ);

                // Remove trees placed on steep mountain cliffs (>17 deg) where players cannot climb and roots hover
                if (steepness > 17f)
                {
                    Object.DestroyImmediate(tree);
                    treesRemoved++;
                    continue;
                }

                // Remove disabled box collider
                foreach (var bc in tree.GetComponentsInChildren<BoxCollider>(true)) Object.DestroyImmediate(bc);

                // Ensure proper trunk capsule collider
                var cap = tree.GetComponent<CapsuleCollider>();
                if (cap == null) cap = tree.AddComponent<CapsuleCollider>();
                cap.radius = 0.45f;
                cap.height = 4.5f;
                cap.center = Vector3.up * 2.25f;

                // Sink tree trunk 0.45m into the ground so roots never hang in air
                float h = terrain.SampleHeight(tree.transform.position);
                tree.transform.position = new Vector3(tree.transform.position.x, h - 0.45f, tree.transform.position.z);
                treesGrounded++;
            }
        }

        // 3. Fix Camp & Structure Props
        var post = GameObject.Find("information post");
        if (post != null)
        {
            var bc = post.GetComponent<BoxCollider>();
            if (bc != null) { bc.size = new Vector3(0.35f, 2.2f, 0.25f); bc.center = new Vector3(0, 1.1f, 0); }
        }

        var chests = GameObject.FindObjectsByType<Transform>(FindObjectsSortMode.None)
            .Where(t => t.name == "chest" && t.GetComponent<Terrain>() == null).ToList();
        foreach (var ch in chests)
        {
            var bc = ch.GetComponent<BoxCollider>();
            if (bc != null) { bc.size = new Vector3(1.2f, 0.85f, 0.85f); bc.center = new Vector3(0, 0.42f, 0); }
        }

        // 4. Re-bake NavMesh on AutumnValley_Map
        var surface = Object.FindFirstObjectByType<Unity.AI.Navigation.NavMeshSurface>();
        if (surface != null)
        {
            Physics.SyncTransforms();
            surface.BuildNavMesh();
            Debug.Log("[FixEnvironment] NavMesh re-baked successfully with precise mesh colliders!");
        }

        // 5. Rebuild enemy prefabs with snug colliders
        RebuildAll();

        // 6. Re-populate enemies at verified clear locations
        PopulateAutumnValley();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        Debug.Log($"[FixEnvironment] Complete! Rocks fixed: {rocksFixed}, Steep floating rocks removed: {rocksRemoved}, Trees grounded: {treesGrounded}, Mountain-cliff trees removed: {treesRemoved}.");
    }

    private static Vector3 FindGuaranteedClearSpot(Terrain terrain, Vector3 target, float searchRadius)
    {
        for (float r = 0; r <= searchRadius; r += 0.8f)
        {
            for (int angle = 0; angle < 360; angle += 30)
            {
                float rad = angle * Mathf.Deg2Rad;
                float x = target.x + Mathf.Cos(rad) * r;
                float z = target.z + Mathf.Sin(rad) * r;
                float y = terrain.SampleHeight(new Vector3(x, 0, z));
                Vector3 testPos = new Vector3(x, y, z);

                if (!NavMesh.SamplePosition(testPos, out var hit, 1.8f, NavMesh.AllAreas)) continue;

                // Check clear radius of 1.8m for any non-terrain colliders
                var cols = Physics.OverlapSphere(hit.position + Vector3.up * 0.8f, 1.8f);
                bool hasObstacle = cols.Any(c => c.GetComponent<Terrain>() == null && !c.name.StartsWith("Enemy_"));
                if (!hasObstacle) return hit.position;
            }
        }
        return target;
    }

    private static AnimatorController BuildAnimatorForConfig(EnemyConfig cfg)
    {
        var source = AssetDatabase.LoadAssetAtPath<AnimatorController>(cfg.sourceController);
        if (source == null)
        {
            Debug.LogError($"[EnemySetup] Source controller not found: {cfg.sourceController}");
            return null;
        }

        var clipMap = new Dictionary<string, AnimationClip>(StringComparer.OrdinalIgnoreCase);
        foreach (var clip in source.animationClips)
        {
            if (clip != null && !clipMap.ContainsKey(clip.name))
            {
                clipMap[clip.name] = clip;
            }
        }

        string outputPath = $"{AnimOutputFolder}/{cfg.prefabName}.controller";
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(outputPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(outputPath);
        }
        else
        {
            // Clear existing layers and parameters
            while (controller.layers.Length > 0)
            {
                controller.RemoveLayer(0);
            }
            while (controller.parameters.Length > 0)
            {
                controller.RemoveParameter(0);
            }
            controller.AddLayer("Base Layer");
        }

        // Add standard parameters for EnemyAI
        AddParameters(controller);

        var root = controller.layers[0].stateMachine;
        root.name = "Base Layer";

        // Get clips
        AnimationClip clipIdleNormal = GetClip(clipMap, cfg.idleNormal);
        AnimationClip clipIdleBattle = GetClip(clipMap, cfg.idleBattle) ?? clipIdleNormal;

        AnimationClip clipWalkFWD = GetClip(clipMap, cfg.walkFWD);
        AnimationClip clipWalkBWD = GetClip(clipMap, cfg.walkBWD) ?? clipWalkFWD;
        AnimationClip clipWalkLeft = GetClip(clipMap, cfg.walkLeft) ?? clipWalkFWD;
        AnimationClip clipWalkRight = GetClip(clipMap, cfg.walkRight) ?? clipWalkFWD;
        AnimationClip clipRun = GetClip(clipMap, cfg.run);

        AnimationClip clipSenseStart = GetClip(clipMap, cfg.senseStart);
        AnimationClip clipSenseLoop = GetClip(clipMap, cfg.senseLoop);

        AnimationClip clipHit = GetClip(clipMap, cfg.getHit);
        AnimationClip clipDizzy = GetClip(clipMap, cfg.dizzy);
        AnimationClip clipDie = GetClip(clipMap, cfg.die);
        AnimationClip clipTaunt = GetClip(clipMap, cfg.taunt);
        AnimationClip clipVictory = GetClip(clipMap, cfg.victory);

        // 1. Idle states
        AnimatorState stateIdleNormal = root.AddState("Idle Normal", new Vector3(200, 50, 0));
        stateIdleNormal.motion = clipIdleNormal;

        AnimatorState stateIdleBattle;
        if (clipIdleBattle != clipIdleNormal)
        {
            stateIdleBattle = root.AddState("Idle Battle", new Vector3(550, 50, 0));
            stateIdleBattle.motion = clipIdleBattle;
        }
        else
        {
            stateIdleBattle = stateIdleNormal;
        }

        root.defaultState = stateIdleNormal;

        // 2. Alert / Sense transitions
        if (clipSenseStart != null && clipSenseLoop != null)
        {
            AnimatorState stateSenseStart = root.AddState("Sense Start", new Vector3(350, -50, 0));
            stateSenseStart.motion = clipSenseStart;
            AnimatorState stateSenseLoop = root.AddState("Sense Loop", new Vector3(550, -50, 0));
            stateSenseLoop.motion = clipSenseLoop;

            // Idle Normal -> Sense Start
            var tSense1 = stateIdleNormal.AddTransition(stateSenseStart);
            tSense1.hasExitTime = false;
            tSense1.duration = 0.1f;
            tSense1.AddCondition(AnimatorConditionMode.If, 0, "IsAlert");

            // Sense Start -> Sense Loop
            var tSense2 = stateSenseStart.AddTransition(stateSenseLoop);
            tSense2.hasExitTime = true;
            tSense2.exitTime = 0.85f;
            tSense2.duration = 0.08f;

            // Sense Loop -> Idle Battle
            var tSense3 = stateSenseLoop.AddTransition(stateIdleBattle);
            tSense3.hasExitTime = true;
            tSense3.exitTime = 0.35f;
            tSense3.duration = 0.1f;

            // Idle Battle -> Idle Normal
            var tBattleToNormal = stateIdleBattle.AddTransition(stateIdleNormal);
            tBattleToNormal.hasExitTime = false;
            tBattleToNormal.duration = 0.15f;
            tBattleToNormal.AddCondition(AnimatorConditionMode.IfNot, 0, "IsAlert");
        }
        else if (stateIdleBattle != stateIdleNormal)
        {
            var tAlertOn = stateIdleNormal.AddTransition(stateIdleBattle);
            tAlertOn.hasExitTime = false;
            tAlertOn.duration = 0.12f;
            tAlertOn.AddCondition(AnimatorConditionMode.If, 0, "IsAlert");

            var tAlertOff = stateIdleBattle.AddTransition(stateIdleNormal);
            tAlertOff.hasExitTime = false;
            tAlertOff.duration = 0.12f;
            tAlertOff.AddCondition(AnimatorConditionMode.IfNot, 0, "IsAlert");
        }

        // 3. Directional Movement Blend Tree
        BlendTree moveTree = new BlendTree
        {
            name = $"{cfg.prefabName} Movement Tree",
            blendType = BlendTreeType.SimpleDirectional2D,
            blendParameter = "MoveX",
            blendParameterY = "MoveY",
            useAutomaticThresholds = false
        };
        moveTree.AddChild(clipIdleBattle, Vector2.zero);
        if (clipWalkFWD != null) moveTree.AddChild(clipWalkFWD, Vector2.up);
        if (clipWalkBWD != null) moveTree.AddChild(clipWalkBWD, Vector2.down);
        if (clipWalkLeft != null) moveTree.AddChild(clipWalkLeft, Vector2.left);
        if (clipWalkRight != null) moveTree.AddChild(clipWalkRight, Vector2.right);

        AssetDatabase.AddObjectToAsset(moveTree, controller);

        AnimatorState stateMove = root.AddState("Directional Movement", new Vector3(550, 180, 0));
        stateMove.motion = moveTree;

        // Idle Battle <-> Move
        var tToMove = stateIdleBattle.AddTransition(stateMove);
        tToMove.hasExitTime = false;
        tToMove.duration = 0.12f;
        tToMove.AddCondition(AnimatorConditionMode.If, 0, "IsMoving");

        var tFromMove = stateMove.AddTransition(stateIdleBattle);
        tFromMove.hasExitTime = false;
        tFromMove.duration = 0.12f;
        tFromMove.AddCondition(AnimatorConditionMode.IfNot, 0, "IsMoving");

        // If IdleNormal is distinct, also allow moving directly from IdleNormal
        if (stateIdleBattle != stateIdleNormal)
        {
            var tNormalToMove = stateIdleNormal.AddTransition(stateMove);
            tNormalToMove.hasExitTime = false;
            tNormalToMove.duration = 0.12f;
            tNormalToMove.AddCondition(AnimatorConditionMode.If, 0, "IsMoving");
        }

        // Run state (if exists)
        if (clipRun != null && clipRun != clipWalkFWD)
        {
            AnimatorState stateRun = root.AddState("Run", new Vector3(800, 180, 0));
            stateRun.motion = clipRun;

            var tMoveToRun = stateMove.AddTransition(stateRun);
            tMoveToRun.hasExitTime = false;
            tMoveToRun.duration = 0.12f;
            tMoveToRun.AddCondition(AnimatorConditionMode.Greater, 0.72f, "Speed");

            var tRunToMove = stateRun.AddTransition(stateMove);
            tRunToMove.hasExitTime = false;
            tRunToMove.duration = 0.12f;
            tRunToMove.AddCondition(AnimatorConditionMode.Less, 0.65f, "Speed");

            var tRunToIdle = stateRun.AddTransition(stateIdleBattle);
            tRunToIdle.hasExitTime = false;
            tRunToIdle.duration = 0.12f;
            tRunToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsMoving");
        }

        // 4. Attacks (AnyState transitions)
        var attackStates = new List<AnimatorState>();
        for (int i = 0; i < cfg.attacks.Length; i++)
        {
            var clipAtk = GetClip(clipMap, cfg.attacks[i]);
            if (clipAtk == null) continue;

            AnimatorState stateAtk = root.AddState($"Attack {i + 1:D2}", new Vector3(550 + i * 220, 320, 0));
            stateAtk.motion = clipAtk;
            attackStates.Add(stateAtk);

            // AnyState -> Attack state
            var tAtk = root.AddAnyStateTransition(stateAtk);
            tAtk.hasExitTime = false;
            tAtk.duration = 0.05f;
            tAtk.canTransitionToSelf = false;
            tAtk.AddCondition(AnimatorConditionMode.If, 0, "Attack");

            if (cfg.attacks.Length > 1)
            {
                tAtk.AddCondition(AnimatorConditionMode.Equals, i, "AttackIndex");

                // If only 2 attacks, map index 2 to Attack 01 as well so every random index fires an attack!
                if (cfg.attacks.Length == 2 && i == 0)
                {
                    var tAtkFallback = root.AddAnyStateTransition(stateAtk);
                    tAtkFallback.hasExitTime = false;
                    tAtkFallback.duration = 0.05f;
                    tAtkFallback.canTransitionToSelf = false;
                    tAtkFallback.AddCondition(AnimatorConditionMode.If, 0, "Attack");
                    tAtkFallback.AddCondition(AnimatorConditionMode.Equals, 2, "AttackIndex");
                }
            }

            // Attack -> Idle Battle
            var tAtkEnd = stateAtk.AddTransition(stateIdleBattle);
            tAtkEnd.hasExitTime = true;
            tAtkEnd.exitTime = 0.85f;
            tAtkEnd.duration = 0.1f;
        }

        // 5. Get Hit & Dizzy
        if (clipHit != null)
        {
            AnimatorState stateHit = root.AddState("Get Hit", new Vector3(550, 440, 0));
            stateHit.motion = clipHit;

            var tHit = root.AddAnyStateTransition(stateHit);
            tHit.hasExitTime = false;
            tHit.duration = 0.05f;
            tHit.canTransitionToSelf = false;
            tHit.AddCondition(AnimatorConditionMode.If, 0, "Hit");

            var tHitEnd = stateHit.AddTransition(stateIdleBattle);
            tHitEnd.hasExitTime = true;
            tHitEnd.exitTime = 0.85f;
            tHitEnd.duration = 0.1f;
        }

        if (clipDizzy != null)
        {
            AnimatorState stateDizzy = root.AddState("Dizzy", new Vector3(800, 440, 0));
            stateDizzy.motion = clipDizzy;

            var tDizzy = root.AddAnyStateTransition(stateDizzy);
            tDizzy.hasExitTime = false;
            tDizzy.duration = 0.05f;
            tDizzy.canTransitionToSelf = false;
            tDizzy.AddCondition(AnimatorConditionMode.If, 0, "Dizzy");

            var tDizzyEnd = stateDizzy.AddTransition(stateIdleBattle);
            tDizzyEnd.hasExitTime = true;
            tDizzyEnd.exitTime = 0.8f;
            tDizzyEnd.duration = 0.1f;
        }

        // 6. Taunt & Victory
        if (clipTaunt != null)
        {
            AnimatorState stateTaunt = root.AddState("Taunt", new Vector3(550, 560, 0));
            stateTaunt.motion = clipTaunt;

            var tTaunt = root.AddAnyStateTransition(stateTaunt);
            tTaunt.hasExitTime = false;
            tTaunt.duration = 0.08f;
            tTaunt.canTransitionToSelf = false;
            tTaunt.AddCondition(AnimatorConditionMode.If, 0, "Taunt");

            var tTauntEnd = stateTaunt.AddTransition(stateIdleBattle);
            tTauntEnd.hasExitTime = true;
            tTauntEnd.exitTime = 0.9f;
            tTauntEnd.duration = 0.1f;
        }

        if (clipVictory != null)
        {
            AnimatorState stateVictory = root.AddState("Victory", new Vector3(800, 560, 0));
            stateVictory.motion = clipVictory;

            var tVictory = root.AddAnyStateTransition(stateVictory);
            tVictory.hasExitTime = false;
            tVictory.duration = 0.08f;
            tVictory.canTransitionToSelf = false;
            tVictory.AddCondition(AnimatorConditionMode.If, 0, "Victory");

            var tVictoryEnd = stateVictory.AddTransition(stateIdleBattle);
            tVictoryEnd.hasExitTime = true;
            tVictoryEnd.exitTime = 0.9f;
            tVictoryEnd.duration = 0.1f;
        }

        // 7. Die (No exit transitions)
        if (clipDie != null)
        {
            AnimatorState stateDie = root.AddState("Die", new Vector3(1050, 560, 0));
            stateDie.motion = clipDie;

            var tDie = root.AddAnyStateTransition(stateDie);
            tDie.hasExitTime = false;
            tDie.duration = 0.05f;
            tDie.canTransitionToSelf = false;
            tDie.AddCondition(AnimatorConditionMode.If, 0, "Die");
        }

        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static void ConfigurePrefab(EnemyConfig cfg, AnimatorController controller)
    {
        string prefabPath = $"Assets/Prefabs/Enemy/{cfg.prefabName}.prefab";
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
        if (prefabRoot == null)
        {
            Debug.LogError($"[EnemySetup] Cannot load prefab: {prefabPath}");
            return;
        }

        try
        {
            // 1. Animator
            var animator = prefabRoot.GetComponentInChildren<Animator>(true);
            if (animator == null)
            {
                animator = prefabRoot.AddComponent<Animator>();
            }
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;

            // 2. NavMeshAgent
            var agent = prefabRoot.GetComponent<NavMeshAgent>();
            if (agent == null) agent = prefabRoot.AddComponent<NavMeshAgent>();
            agent.radius = cfg.agentRadius;
            agent.height = cfg.agentHeight;
            agent.speed = cfg.agentSpeed;
            agent.acceleration = 14f;
            agent.angularSpeed = 720f;
            agent.stoppingDistance = 0f;
            agent.updateRotation = false;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;

            // 3. CapsuleCollider
            var col = prefabRoot.GetComponent<CapsuleCollider>();
            if (col == null) col = prefabRoot.AddComponent<CapsuleCollider>();
            col.center = Vector3.up * cfg.colCenterY;
            col.radius = cfg.colRadius;
            col.height = cfg.colHeight;
            col.isTrigger = false;

            // 4. EnemyHealth
            var health = prefabRoot.GetComponent<EnemyHealth>();
            if (health == null) health = prefabRoot.AddComponent<EnemyHealth>();
            var soHealth = new SerializedObject(health);
            soHealth.FindProperty("maxHealth").floatValue = cfg.maxHealth;
            soHealth.FindProperty("barHeight").floatValue = cfg.barHeight;
            soHealth.FindProperty("deathDelay").floatValue = 2.5f;
            soHealth.ApplyModifiedPropertiesWithoutUndo();

            // 5. MonsterMuzzle & Eye
            Transform muzzleTrans = FindChildRecursive(prefabRoot.transform, "MonsterMuzzle");
            if (muzzleTrans == null)
            {
                var muzzleObj = new GameObject("MonsterMuzzle");
                muzzleTrans = muzzleObj.transform;
            }

            Transform boneTarget = !string.IsNullOrEmpty(cfg.muzzleBone)
                ? FindChildRecursive(prefabRoot.transform, cfg.muzzleBone)
                : null;

            if (boneTarget != null)
            {
                muzzleTrans.SetParent(boneTarget, false);
                muzzleTrans.localPosition = Vector3.forward * 0.2f;
                muzzleTrans.localRotation = Quaternion.identity;
            }
            else
            {
                muzzleTrans.SetParent(prefabRoot.transform, false);
                muzzleTrans.localPosition = Vector3.up * cfg.colCenterY + Vector3.forward * 0.5f;
                muzzleTrans.localRotation = Quaternion.identity;
            }

            // 6. EnemyAI
            var ai = prefabRoot.GetComponent<EnemyAI>();
            if (ai == null) ai = prefabRoot.AddComponent<EnemyAI>();
            var soAI = new SerializedObject(ai);
            soAI.FindProperty("detectionRange").floatValue = cfg.detectionRange;
            soAI.FindProperty("shootRange").floatValue = cfg.shootRange;
            soAI.FindProperty("preferredRange").floatValue = cfg.preferredRange;
            soAI.FindProperty("moveSpeed").floatValue = cfg.agentSpeed;
            soAI.FindProperty("turnSpeed").floatValue = 10f;
            soAI.FindProperty("fireRate").floatValue = cfg.fireRate;
            soAI.FindProperty("attackWindup").floatValue = 0.25f;
            soAI.FindProperty("projectileOffset").floatValue = cfg.colCenterY;
            soAI.FindProperty("projectileSpeed").floatValue = cfg.projectileSpeed;
            soAI.FindProperty("projectileDamage").floatValue = cfg.projectileDamage;
            soAI.FindProperty("projectileLifeTime").floatValue = 3.0f;
            soAI.FindProperty("gunBarrel").objectReferenceValue = muzzleTrans;

            var bulletAsset = AssetDatabase.LoadAssetAtPath<GameObject>(BulletPrefabPath);
            soAI.FindProperty("bulletPrefab").objectReferenceValue = bulletAsset;

            if (boneTarget != null)
            {
                soAI.FindProperty("eye").objectReferenceValue = boneTarget;
            }

            soAI.ApplyModifiedPropertiesWithoutUndo();

            // Save Prefab
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static void AddParameters(AnimatorController controller)
    {
        controller.AddParameter("MoveX", AnimatorControllerParameterType.Float);
        controller.AddParameter("MoveY", AnimatorControllerParameterType.Float);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsAlert", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsLaserActive", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("AttackIndex", AnimatorControllerParameterType.Int);
        controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Dizzy", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Taunt", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Victory", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("GunQuestCustomEnemyV1", AnimatorControllerParameterType.Bool);
    }

    private static AnimationClip GetClip(Dictionary<string, AnimationClip> map, string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        return map.TryGetValue(name, out var clip) ? clip : null;
    }

    private static Transform FindChildRecursive(Transform parent, string name)
    {
        if (parent.name.Equals(name, StringComparison.OrdinalIgnoreCase)) return parent;
        for (int i = 0; i < parent.childCount; i++)
        {
            var result = FindChildRecursive(parent.GetChild(i), name);
            if (result != null) return result;
        }
        return null;
    }

    private static void EnsureFolder(string folderPath)
    {
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            string parent = System.IO.Path.GetDirectoryName(folderPath).Replace('\\', '/');
            string folderName = System.IO.Path.GetFileName(folderPath);
            if (!AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
#endif
