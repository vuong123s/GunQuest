#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.IO;

public class TutorialMapBuilder : EditorWindow
{
    [MenuItem("GunQuest/Build YouTube Tutorial Map", false, 1)]
    public static void ShowWindow()
    {
        GetWindow<TutorialMapBuilder>("Tutorial Map Builder");
    }

    [MenuItem("GunQuest/1-Click Generate Entire Tutorial Map & Setup", false, 2)]
    public static void GenerateAllOneClick()
    {
        TutorialMapBuilder builder = CreateInstance<TutorialMapBuilder>();
        builder.BuildCompleteTutorialMap();
    }

    private void OnGUI()
    {
        GUILayout.Label("YouTube Tutorial Map & Scene Generator", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox("Tạo toàn bộ bản đồ, chướng ngại vật, nhà thử nghiệm cửa/keypad, khối đổi màu, vùng thử nghiệm máu, Prefab đạn, Kẻ địch AI (FSM) và giao diện HUD Canvas giống video hướng dẫn.", MessageType.Info);
        EditorGUILayout.Space();

        if (GUILayout.Button("1. Tạo toàn bộ Map, Vật phẩm & Kẻ địch AI", GUILayout.Height(40)))
        {
            BuildCompleteTutorialMap();
        }

        EditorGUILayout.Space();
        if (GUILayout.Button("2. Tạo Canvas UI & HUD Máu / Tương tác", GUILayout.Height(30)))
        {
            SetupPlayerHUD();
        }

        if (GUILayout.Button("3. Tạo Prefab Viên Đạn (Bullet)", GUILayout.Height(30)))
        {
            CreateBulletPrefab();
        }

        if (GUILayout.Button("4. Tạo Lộ trình tuần tra (Patrol Path)", GUILayout.Height(30)))
        {
            CreatePatrolPath();
        }
    }

    public void BuildCompleteTutorialMap()
    {
        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Build Tutorial Map");
        int group = Undo.GetCurrentGroup();

        // 1. Materials
        Material floorMat = GetOrCreateMaterial("Tutorial_Floor", new Color(0.22f, 0.24f, 0.28f));
        Material wallMat = GetOrCreateMaterial("Tutorial_Wall", new Color(0.45f, 0.48f, 0.52f));
        Material obstacleMat = GetOrCreateMaterial("Tutorial_Obstacle", new Color(0.18f, 0.35f, 0.55f));
        Material buildingMat = GetOrCreateMaterial("Tutorial_Building", new Color(0.35f, 0.37f, 0.42f));
        Material doorMat = GetOrCreateMaterial("Tutorial_Door", new Color(0.6f, 0.35f, 0.15f));
        Material keypadMat = GetOrCreateMaterial("Tutorial_Keypad", new Color(0.15f, 0.15f, 0.15f));
        Material hazardMat = GetOrCreateMaterial("Tutorial_Hazard", new Color(0.85f, 0.15f, 0.15f));
        Material healMat = GetOrCreateMaterial("Tutorial_Heal", new Color(0.15f, 0.85f, 0.35f));
        Material cubeMat = GetOrCreateMaterial("Tutorial_Cube", new Color(0.2f, 0.7f, 0.9f));
        Material enemyMat = GetOrCreateMaterial("Tutorial_Enemy", new Color(0.8f, 0.2f, 0.2f));

        // Root Map
        GameObject mapRoot = GameObject.Find("Tutorial_Map");
        if (mapRoot != null)
        {
            DestroyImmediate(mapRoot);
        }
        mapRoot = new GameObject("Tutorial_Map");
        Undo.RegisterCreatedObjectUndo(mapRoot, "Create Map Root");

        // 2. Floor & Boundaries
        GameObject floor = CreateBlock("Floor", mapRoot.transform, new Vector3(0, -0.5f, 0), new Vector3(60, 1, 60), floorMat, true);
        GameObject northWall = CreateBlock("Wall_North", mapRoot.transform, new Vector3(0, 2.5f, 30), new Vector3(60, 5, 1), wallMat, true);
        GameObject southWall = CreateBlock("Wall_South", mapRoot.transform, new Vector3(0, 2.5f, -30), new Vector3(60, 5, 1), wallMat, true);
        GameObject eastWall = CreateBlock("Wall_East", mapRoot.transform, new Vector3(30, 2.5f, 0), new Vector3(1, 5, 60), wallMat, true);
        GameObject westWall = CreateBlock("Wall_West", mapRoot.transform, new Vector3(-30, 2.5f, 0), new Vector3(1, 5, 60), wallMat, true);

        // 3. Test Building with Doorway, Door, and Keypad
        GameObject buildingRoot = new GameObject("Testing_Building");
        buildingRoot.transform.parent = mapRoot.transform;
        buildingRoot.transform.position = new Vector3(-15, 0, 15);

        // Building Walls (Room 12x10, height 4)
        CreateBlock("B_Wall_Back", buildingRoot.transform, new Vector3(0, 2, 5), new Vector3(12, 4, 0.5f), buildingMat, true);
        CreateBlock("B_Wall_Left", buildingRoot.transform, new Vector3(-6, 2, 0), new Vector3(0.5f, 4, 10), buildingMat, true);
        CreateBlock("B_Wall_Right", buildingRoot.transform, new Vector3(6, 2, 0), new Vector3(0.5f, 4, 10), buildingMat, true);
        CreateBlock("B_Wall_Front_Left", buildingRoot.transform, new Vector3(-4, 2, -5), new Vector3(4, 4, 0.5f), buildingMat, true);
        CreateBlock("B_Wall_Front_Right", buildingRoot.transform, new Vector3(4, 2, -5), new Vector3(4, 4, 0.5f), buildingMat, true);
        CreateBlock("B_Ceiling", buildingRoot.transform, new Vector3(0, 4.25f, 0), new Vector3(12.5f, 0.5f, 10.5f), buildingMat, true);
        CreateBlock("B_Door_Lintel", buildingRoot.transform, new Vector3(0, 3.25f, -5), new Vector3(4, 1.5f, 0.5f), buildingMat, true);

        // Door
        GameObject doorObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        doorObj.name = "Door";
        doorObj.transform.parent = buildingRoot.transform;
        doorObj.transform.position = new Vector3(0, 1.25f, -5);
        doorObj.transform.localScale = new Vector3(2f, 2.5f, 0.2f);
        doorObj.GetComponent<MeshRenderer>().material = doorMat;
        Door doorComp = doorObj.AddComponent<Door>();

        // Keypad
        GameObject keypadObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        keypadObj.name = "Keypad";
        keypadObj.transform.parent = buildingRoot.transform;
        keypadObj.transform.position = new Vector3(1.5f, 1.3f, -5.2f);
        keypadObj.transform.localScale = new Vector3(0.4f, 0.5f, 0.2f);
        keypadObj.GetComponent<MeshRenderer>().material = keypadMat;
        Keypad keypadComp = keypadObj.AddComponent<Keypad>();
        // Set Keypad targetDoor
        SerializedObject keypadSO = new SerializedObject(keypadComp);
        keypadSO.FindProperty("targetDoor").objectReferenceValue = doorObj;
        keypadSO.ApplyModifiedProperties();

        // 4. Interactable Station (Table with Color Change Cube & Event Light Button)
        GameObject stationRoot = new GameObject("Interaction_Station");
        stationRoot.transform.parent = mapRoot.transform;
        stationRoot.transform.position = new Vector3(-15, 0, -10);

        CreateBlock("Table", stationRoot.transform, new Vector3(0, 0.5f, 0), new Vector3(4, 1, 2), buildingMat, true);

        // Color change cube
        GameObject cubeObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cubeObj.name = "ColorChangeCube";
        cubeObj.transform.parent = stationRoot.transform;
        cubeObj.transform.position = new Vector3(-1f, 1.5f, 0);
        cubeObj.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
        cubeObj.GetComponent<MeshRenderer>().material = cubeMat;
        cubeObj.AddComponent<ColorChangeCube>();

        // Event Only Interactable (Light Switch)
        GameObject lightButton = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        lightButton.name = "Event_LightButton";
        lightButton.transform.parent = stationRoot.transform;
        lightButton.transform.position = new Vector3(1f, 1.5f, 0);
        lightButton.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
        lightButton.GetComponent<MeshRenderer>().material = GetOrCreateMaterial("Tutorial_YellowGlow", Color.yellow);
        EventOnlyInteractable eventInteract = lightButton.AddComponent<EventOnlyInteractable>();
        eventInteract.promptMessage = "Press E to Toggle Station Lamp";

        // Light object
        GameObject stationLight = new GameObject("Station_Lamp");
        stationLight.transform.parent = stationRoot.transform;
        stationLight.transform.position = new Vector3(0, 3.5f, 0);
        Light lightComp = stationLight.AddComponent<Light>();
        lightComp.type = LightType.Point;
        lightComp.color = Color.yellow;
        lightComp.range = 8f;
        lightComp.intensity = 2f;

        // 5. Health & Damage Test Zones
        GameObject hazardZone = GameObject.CreatePrimitive(PrimitiveType.Cube);
        hazardZone.name = "HazardZone_DamageArea";
        hazardZone.transform.parent = mapRoot.transform;
        hazardZone.transform.position = new Vector3(0, 0.1f, -15);
        hazardZone.transform.localScale = new Vector3(6, 0.2f, 6);
        hazardZone.GetComponent<MeshRenderer>().material = hazardMat;
        BoxCollider hzCollider = hazardZone.GetComponent<BoxCollider>();
        hzCollider.isTrigger = true;
        DamageTest dmgTest = hazardZone.AddComponent<DamageTest>();

        GameObject healZone = GameObject.CreatePrimitive(PrimitiveType.Cube);
        healZone.name = "HealingZone_Area";
        healZone.transform.parent = mapRoot.transform;
        healZone.transform.position = new Vector3(0, 0.1f, 15);
        healZone.transform.localScale = new Vector3(6, 0.2f, 6);
        healZone.GetComponent<MeshRenderer>().material = healMat;
        BoxCollider healCollider = healZone.GetComponent<BoxCollider>();
        healCollider.isTrigger = true;

        // 6. Obstacle & Cover Course (For AI Line of Sight & Search State)
        GameObject obstaclesRoot = new GameObject("Obstacles_And_Cover");
        obstaclesRoot.transform.parent = mapRoot.transform;

        // Central Cover Pillars
        CreateBlock("Pillar_1", obstaclesRoot.transform, new Vector3(8, 2, 8), new Vector3(3, 4, 3), obstacleMat, true);
        CreateBlock("Pillar_2", obstaclesRoot.transform, new Vector3(18, 2, 8), new Vector3(3, 4, 3), obstacleMat, true);
        CreateBlock("Pillar_3", obstaclesRoot.transform, new Vector3(8, 2, -8), new Vector3(3, 4, 3), obstacleMat, true);
        CreateBlock("Pillar_4", obstaclesRoot.transform, new Vector3(18, 2, -8), new Vector3(3, 4, 3), obstacleMat, true);

        // Cover Wall Barriers
        CreateBlock("CoverWall_A", obstaclesRoot.transform, new Vector3(13, 1.25f, 0), new Vector3(6, 2.5f, 0.8f), obstacleMat, true);
        CreateBlock("CoverWall_B", obstaclesRoot.transform, new Vector3(22, 1.25f, 0), new Vector3(0.8f, 2.5f, 8f), obstacleMat, true);

        // Elevated Walkway Platform & Ramp
        GameObject platform = CreateBlock("Elevated_Platform", obstaclesRoot.transform, new Vector3(15, 1.5f, 20), new Vector3(14, 3, 8), obstacleMat, true);
        GameObject ramp = CreateBlock("Ramp_Up", obstaclesRoot.transform, new Vector3(5, 0.75f, 20), new Vector3(6, 0.5f, 4), obstacleMat, true);
        ramp.transform.rotation = Quaternion.Euler(0, 0, -25f);

        // 7. Bullet Prefab
        GameObject bulletPrefab = CreateBulletPrefab();

        // 8. Patrol Path
        GameObject pathObj = CreatePatrolPath();
        pathObj.transform.parent = mapRoot.transform;

        // 9. Enemy AI
        GameObject enemyObj = GameObject.Find("Enemy_AI");
        if (enemyObj != null)
        {
            DestroyImmediate(enemyObj);
        }
        enemyObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        enemyObj.name = "Enemy_AI";
        enemyObj.transform.parent = mapRoot.transform;
        enemyObj.transform.position = new Vector3(8, 1f, 15);
        enemyObj.GetComponent<MeshRenderer>().material = enemyMat;
        NavMeshAgent agent = enemyObj.AddComponent<NavMeshAgent>();
        agent.speed = 4f;
        agent.stoppingDistance = 1.5f;

        // Gun Barrel
        GameObject gunBarrel = new GameObject("GunBarrel");
        gunBarrel.transform.parent = enemyObj.transform;
        gunBarrel.transform.localPosition = new Vector3(0.3f, 0.3f, 0.6f);

        Enemy enemyScript = enemyObj.AddComponent<Enemy>();
        enemyScript.path = pathObj.GetComponent<Path>();
        enemyScript.gunBarrel = gunBarrel.transform;
        enemyScript.bulletPrefab = bulletPrefab;
        enemyScript.fireRate = 1.2f;
        enemyScript.sightDistance = 22f;
        enemyScript.fieldOfView = 90f;

        // 10. Player Setup & HUD
        SetupPlayerHUD();

        // Ensure Player is placed properly in scene
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            PlayerMotor pm = Object.FindFirstObjectByType<PlayerMotor>();
            if (pm != null) player = pm.gameObject;
        }

        if (player != null)
        {
            player.transform.position = new Vector3(-8, 1f, -5);
            // Ensure PlayerInteract is attached
            if (player.GetComponent<PlayerInteract>() == null)
            {
                player.AddComponent<PlayerInteract>();
            }
        }

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Tutorial Map, Interaction Stations, Enemy AI & Player HUD generated successfully!");
    }

    public static GameObject CreateBlock(string name, Transform parent, Vector3 position, Vector3 scale, Material mat, bool isStatic)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.parent = parent;
        cube.transform.position = position;
        cube.transform.localScale = scale;
        if (mat != null)
        {
            cube.GetComponent<MeshRenderer>().material = mat;
        }
        if (isStatic)
        {
            GameObjectUtility.SetStaticEditorFlags(cube, StaticEditorFlags.NavigationStatic | StaticEditorFlags.ContributeGI);
        }
        return cube;
    }

    public static GameObject CreateBulletPrefab()
    {
        string prefabsPath = "Assets/Prefabs";
        if (!AssetDatabase.IsValidFolder(prefabsPath))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }

        string bulletPrefabPath = prefabsPath + "/Bullet.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(bulletPrefabPath);
        if (existing != null)
        {
            return existing;
        }

        GameObject bulletGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bulletGO.name = "Bullet";
        bulletGO.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
        Material bulletMat = GetOrCreateMaterial("Bullet_Material", new Color(1f, 0.8f, 0.1f));
        bulletGO.GetComponent<MeshRenderer>().material = bulletMat;

        SphereCollider sc = bulletGO.GetComponent<SphereCollider>();
        sc.isTrigger = true;

        bulletGO.AddComponent<Bullet>();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(bulletGO, bulletPrefabPath);
        DestroyImmediate(bulletGO);
        AssetDatabase.SaveAssets();
        return prefab;
    }

    public static GameObject CreatePatrolPath()
    {
        GameObject existing = GameObject.Find("Enemy_Patrol_Path");
        if (existing != null)
        {
            return existing;
        }

        GameObject pathObj = new GameObject("Enemy_Patrol_Path");
        Path pathScript = pathObj.AddComponent<Path>();

        Vector3[] waypointsPositions = new Vector3[]
        {
            new Vector3(8, 0, 15),
            new Vector3(22, 0, 15),
            new Vector3(22, 0, -15),
            new Vector3(8, 0, -15),
            new Vector3(2, 0, 0),
            new Vector3(13, 0, 5)
        };

        for (int i = 0; i < waypointsPositions.Length; i++)
        {
            GameObject wp = new GameObject($"Waypoint_{i + 1}");
            wp.transform.parent = pathObj.transform;
            wp.transform.position = waypointsPositions[i];
            pathScript.waypoints.Add(wp.transform);
        }

        return pathObj;
    }

    public static void SetupPlayerHUD()
    {
        // Find or create Canvas
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        GameObject canvasObj;
        if (canvas == null)
        {
            canvasObj = new GameObject("Player_HUD_Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        else
        {
            canvasObj = canvas.gameObject;
        }

        // Ensure EventSystem exists
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // 1. Crosshair Dot
        Transform crosshair = canvasObj.transform.Find("Crosshair");
        if (crosshair == null)
        {
            GameObject chObj = new GameObject("Crosshair");
            chObj.transform.parent = canvasObj.transform;
            RectTransform rt = chObj.AddComponent<RectTransform>();
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(6, 6);
            Image img = chObj.AddComponent<Image>();
            img.color = Color.white;
            img.raycastTarget = false;
        }

        // 2. Prompt Text (Center of screen)
        Transform promptTr = canvasObj.transform.Find("PromptText");
        TextMeshProUGUI promptTMP = null;
        if (promptTr == null)
        {
            GameObject pObj = new GameObject("PromptText");
            pObj.transform.parent = canvasObj.transform;
            RectTransform rt = pObj.AddComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0, -60);
            rt.sizeDelta = new Vector2(800, 80);
            promptTMP = pObj.AddComponent<TextMeshProUGUI>();
            promptTMP.alignment = TextAlignmentOptions.Center;
            promptTMP.fontSize = 28;
            promptTMP.color = Color.white;
            promptTMP.text = "";
            promptTMP.raycastTarget = false;
        }
        else
        {
            promptTMP = promptTr.GetComponent<TextMeshProUGUI>();
        }

        // 3. Health Bar UI (Bottom Left)
        Transform healthContainerTr = canvasObj.transform.Find("HealthBar_Container");
        Image frontBar = null;
        Image backBar = null;

        if (healthContainerTr == null)
        {
            GameObject hcObj = new GameObject("HealthBar_Container");
            hcObj.transform.parent = canvasObj.transform;
            RectTransform hcRt = hcObj.AddComponent<RectTransform>();
            hcRt.anchorMin = new Vector2(0, 0);
            hcRt.anchorMax = new Vector2(0, 0);
            hcRt.pivot = new Vector2(0, 0);
            hcRt.anchoredPosition = new Vector2(50, 50);
            hcRt.sizeDelta = new Vector2(300, 30);
            Image bg = hcObj.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

            // Back Bar (Red / Chip-away)
            GameObject bbObj = new GameObject("BackHealthBar");
            bbObj.transform.parent = hcObj.transform;
            RectTransform bbRt = bbObj.AddComponent<RectTransform>();
            bbRt.anchorMin = Vector2.zero;
            bbRt.anchorMax = Vector2.one;
            bbRt.sizeDelta = Vector2.zero;
            backBar = bbObj.AddComponent<Image>();
            backBar.color = Color.red;
            backBar.type = Image.Type.Filled;
            backBar.fillMethod = Image.FillMethod.Horizontal;
            backBar.fillAmount = 1f;

            // Front Bar (Green / Current)
            GameObject fbObj = new GameObject("FrontHealthBar");
            fbObj.transform.parent = hcObj.transform;
            RectTransform fbRt = fbObj.AddComponent<RectTransform>();
            fbRt.anchorMin = Vector2.zero;
            fbRt.anchorMax = Vector2.one;
            fbRt.sizeDelta = Vector2.zero;
            frontBar = fbObj.AddComponent<Image>();
            frontBar.color = new Color(0.2f, 0.85f, 0.3f);
            frontBar.type = Image.Type.Filled;
            frontBar.fillMethod = Image.FillMethod.Horizontal;
            frontBar.fillAmount = 1f;
        }
        else
        {
            frontBar = healthContainerTr.Find("FrontHealthBar")?.GetComponent<Image>();
            backBar = healthContainerTr.Find("BackHealthBar")?.GetComponent<Image>();
        }

        // 4. Damage Screen Overlay (Full screen red flash)
        Transform overlayTr = canvasObj.transform.Find("DamageOverlay");
        Image overlayImg = null;
        if (overlayTr == null)
        {
            GameObject oObj = new GameObject("DamageOverlay");
            oObj.transform.parent = canvasObj.transform;
            RectTransform oRt = oObj.AddComponent<RectTransform>();
            oRt.anchorMin = Vector2.zero;
            oRt.anchorMax = Vector2.one;
            oRt.sizeDelta = Vector2.zero;
            overlayImg = oObj.AddComponent<Image>();
            overlayImg.color = new Color(1f, 0f, 0f, 0f);
            overlayImg.raycastTarget = false;
        }
        else
        {
            overlayImg = overlayTr.GetComponent<Image>();
        }

        // Wire references to Player
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            PlayerMotor pm = Object.FindFirstObjectByType<PlayerMotor>();
            if (pm != null) player = pm.gameObject;
        }

        if (player != null)
        {
            PlayerUI playerUI = player.GetComponent<PlayerUI>();
            if (playerUI == null) playerUI = player.AddComponent<PlayerUI>();
            
            SerializedObject puiSO = new SerializedObject(playerUI);
            puiSO.FindProperty("promptTextTMP").objectReferenceValue = promptTMP;
            puiSO.ApplyModifiedProperties();

            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth == null) playerHealth = player.AddComponent<PlayerHealth>();
            playerHealth.frontHealthBar = frontBar;
            playerHealth.backHealthBar = backBar;
            playerHealth.overlay = overlayImg;

            if (player.GetComponent<PlayerInteract>() == null)
            {
                player.AddComponent<PlayerInteract>();
            }
        }
    }

    public static Material GetOrCreateMaterial(string name, Color color)
    {
        string path = "Assets/Settings/" + name + ".mat";
        if (!AssetDatabase.IsValidFolder("Assets/Settings"))
        {
            AssetDatabase.CreateFolder("Assets", "Settings");
        }

        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            mat = new Material(shader);
            mat.color = color;
            AssetDatabase.CreateAsset(mat, path);
        }
        return mat;
    }
}
#endif
