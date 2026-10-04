#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.AI;

public static class Monster1AnimatorBuilder
{
    private const string SourcePath = "Assets/DL/RPGMonsterPartnersPBRPolyart/Animators/Beholder.controller";
    private const string OutputFolder = "Assets/Animations";
    private const string OutputPath = OutputFolder + "/Monster1.controller";
    private const string MonsterPrefabPath = "Assets/Prefabs/Monster1.prefab";
    private const string BulletPrefabPath = "Assets/Prefabs/Bullet.prefab";
    private const string VersionParameter = "GunQuestMonster1AnimatorV2";

    [MenuItem("GunQuest/Rebuild Monster1 Animator", false, 21)]
    public static void Build()
    {
        AnimatorController source = AssetDatabase.LoadAssetAtPath<AnimatorController>(SourcePath);
        if (source == null)
        {
            Debug.LogError("[Monster1Animator] Cannot find Beholder controller: " + SourcePath);
            return;
        }

        Dictionary<string, AnimationClip> clips = ReadClips(source);
        if (!HasRequiredClips(clips)) return;

        EnsureFolder();
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(OutputPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(OutputPath);
        }
        else
        {
            ResetController(controller);
        }

        AddParameters(controller);
        BuildLayer(controller, clips);
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        ConfigureMonster(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Monster1Animator] Created " + OutputPath + " with all 18 Beholder clips.");
    }

    [InitializeOnLoadMethod]
    private static void BuildWhenNeeded()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(OutputPath);
            if (!HasVersion(controller))
            {
                Build();
                return;
            }

            ConfigureMonster(controller);
        };
    }

    private static void BuildLayer(
        AnimatorController controller,
        IReadOnlyDictionary<string, AnimationClip> clips)
    {
        AnimatorStateMachine root = controller.layers[0].stateMachine;
        root.name = "Base Layer";

        AnimatorState idleNormal = AddState(root, "Idle Normal", clips["IdleNormal"], 260f, 30f);
        AnimatorState senseStart = AddState(root, "Sense Start", clips["SenseSomethingST"], 500f, -80f);
        AnimatorState senseLoop = AddState(root, "Sense", clips["SenseSomethingRPT"], 720f, -80f);
        AnimatorState idleBattle = AddState(root, "Idle Battle", clips["IdleBattle"], 940f, 30f);
        AnimatorState move = AddState(root, "Directional Movement", CreateMoveTree(controller, clips), 940f, 160f);
        AnimatorState run = AddState(root, "Run", clips["Run"], 1180f, 160f);
        AnimatorState attack01 = AddState(root, "Attack 01", clips["Attack01"], 940f, 310f);
        AnimatorState attack02Start = AddState(root, "Attack 02 Start", clips["Attack02ST"], 1180f, 310f);
        AnimatorState attack02Repeat = AddState(root, "Attack 02 Repeat", clips["Attack02RPT"], 1410f, 310f);
        AnimatorState attack03 = AddState(root, "Attack 03", clips["Attack03"], 940f, 430f);
        AnimatorState hit = AddState(root, "Get Hit", clips["GetHit"], 1180f, 430f);
        AnimatorState dizzy = AddState(root, "Dizzy", clips["Dizzy"], 1410f, 430f);
        AnimatorState taunt = AddState(root, "Taunt", clips["Taunting"], 940f, 550f);
        AnimatorState victory = AddState(root, "Victory", clips["Victory"], 1180f, 550f);
        AnimatorState die = AddState(root, "Die", clips["Die"], 1410f, 550f);
        root.defaultState = idleNormal;

        AddTransition(idleNormal, senseStart, 0.1f, false, 0f, If("IsAlert"));
        AddTransition(senseStart, senseLoop, 0.08f, true, 0.88f);
        AddTransition(senseLoop, idleBattle, 0.1f, true, 0.28f);
        AddTransition(idleBattle, idleNormal, 0.15f, false, 0f, IfNot("IsAlert"));
        AddTransition(idleBattle, move, 0.12f, false, 0f, If("IsMoving"));
        AddTransition(move, idleBattle, 0.12f, false, 0f, IfNot("IsMoving"));
        AddTransition(move, run, 0.12f, false, 0f, Greater("Speed", 0.72f));
        AddTransition(run, move, 0.12f, false, 0f, Less("Speed", 0.65f));
        AddTransition(run, idleBattle, 0.12f, false, 0f, IfNot("IsMoving"));

        AddAnyTransition(root, attack01, "Attack", 0.05f, Equals("AttackIndex", 0f));
        AddAnyTransition(root, attack02Start, "Attack", 0.05f, Equals("AttackIndex", 1f));
        AddAnyTransition(root, attack03, "Attack", 0.05f, Equals("AttackIndex", 2f));
        AddTransition(attack01, attack02Repeat, 0.07f, true, 0.8f, If("IsLaserActive"));
        AddTransition(attack01, idleBattle, 0.09f, true, 0.88f, IfNot("IsLaserActive"));
        AddTransition(attack02Start, attack02Repeat, 0.05f, true, 0.9f);
        AddTransition(attack02Repeat, idleBattle, 0.09f, false, 0f, IfNot("IsLaserActive"));
        AddTransition(attack03, attack02Repeat, 0.07f, true, 0.8f, If("IsLaserActive"));
        AddTransition(attack03, idleBattle, 0.09f, true, 0.88f, IfNot("IsLaserActive"));

        AddAnyTransition(root, hit, "Hit", 0.05f);
        AddTransition(hit, idleBattle, 0.08f, true, 0.88f);
        AddAnyTransition(root, dizzy, "Dizzy", 0.06f);
        AddTransition(dizzy, idleBattle, 0.1f, true, 0.75f);
        AddAnyTransition(root, taunt, "Taunt", 0.08f);
        AddTransition(taunt, idleBattle, 0.1f, true, 0.9f);
        AddAnyTransition(root, victory, "Victory", 0.08f);
        AddTransition(victory, idleBattle, 0.1f, true, 0.9f);
        AddAnyTransition(root, die, "Die", 0.06f);
    }

    private static BlendTree CreateMoveTree(
        AnimatorController controller,
        IReadOnlyDictionary<string, AnimationClip> clips)
    {
        BlendTree tree = new BlendTree
        {
            name = "Beholder Directional Movement",
            blendType = BlendTreeType.SimpleDirectional2D,
            blendParameter = "MoveX",
            blendParameterY = "MoveY",
            useAutomaticThresholds = false
        };

        tree.AddChild(clips["IdleBattle"], Vector2.zero);
        tree.AddChild(clips["WalkFWD"], Vector2.up);
        tree.AddChild(clips["WalkBWD"], Vector2.down);
        tree.AddChild(clips["WalkLeft"], Vector2.left);
        tree.AddChild(clips["WalkRight"], Vector2.right);
        AssetDatabase.AddObjectToAsset(tree, controller);
        return tree;
    }

    private static Dictionary<string, AnimationClip> ReadClips(AnimatorController source)
    {
        Dictionary<string, AnimationClip> clips = new Dictionary<string, AnimationClip>(StringComparer.OrdinalIgnoreCase);
        foreach (AnimatorControllerLayer layer in source.layers) ReadStateMachine(layer.stateMachine, clips);
        Debug.Log("[Monster1Animator] Read " + clips.Count + " unique clips from Beholder.");
        return clips;
    }

    private static void ReadStateMachine(
        AnimatorStateMachine machine,
        IDictionary<string, AnimationClip> clips)
    {
        foreach (ChildAnimatorState child in machine.states)
        {
            if (child.state.motion is AnimationClip clip) clips[clip.name] = clip;
        }

        foreach (ChildAnimatorStateMachine child in machine.stateMachines)
        {
            ReadStateMachine(child.stateMachine, clips);
        }
    }

    private static bool HasRequiredClips(IReadOnlyDictionary<string, AnimationClip> clips)
    {
        string[] names =
        {
            "Attack01", "Attack02ST", "Attack02RPT", "Attack03",
            "Die", "Dizzy", "GetHit", "IdleBattle", "IdleNormal", "Run",
            "SenseSomethingST", "SenseSomethingRPT", "Taunting", "Victory",
            "WalkBWD", "WalkFWD", "WalkLeft", "WalkRight"
        };

        foreach (string name in names)
        {
            if (clips.ContainsKey(name)) continue;
            Debug.LogError("[Monster1Animator] Beholder is missing state: " + name);
            return false;
        }

        return clips.Count == names.Length;
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
        controller.AddParameter(VersionParameter, AnimatorControllerParameterType.Bool);
    }

    private static void ConfigureMonster(RuntimeAnimatorController controller)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(MonsterPrefabPath) == null) return;
        GameObject monster = PrefabUtility.LoadPrefabContents(MonsterPrefabPath);
        try
        {
            Animator animator = monster.GetComponentInChildren<Animator>(true);
            if (animator == null)
            {
                Debug.LogError("[Monster1Animator] Monster1 prefab has no Animator.");
                return;
            }

            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;

            Component health = GetOrAddComponent(monster, "EnemyHealth");
            Component effects = GetOrAddComponent(monster, "MonsterEffects");
            Component ai = GetOrAddComponent(monster, "EnemyAI");

            Transform eye = FindChild(monster.transform, "EyeBallCTRL");
            if (eye == null) eye = FindChild(monster.transform, "EyeCTRL");
            Transform muzzle = FindChild(monster.transform, "MonsterMuzzle");
            if (muzzle == null)
            {
                GameObject muzzleObject = new GameObject("MonsterMuzzle");
                muzzle = muzzleObject.transform;
            }
            muzzle.SetParent(eye != null ? eye : monster.transform, false);
            muzzle.localPosition = Vector3.zero;
            muzzle.localRotation = Quaternion.identity;

            NavMeshAgent agent = monster.GetComponent<NavMeshAgent>();
            if (agent == null) agent = monster.AddComponent<NavMeshAgent>();
            agent.radius = 0.75f;
            agent.height = 2.7f;
            agent.speed = 3.5f;
            agent.acceleration = 14f;
            agent.angularSpeed = 720f;
            agent.stoppingDistance = 0f;
            agent.updateRotation = false;

            CapsuleCollider body = monster.GetComponent<CapsuleCollider>();
            if (body == null) body = monster.AddComponent<CapsuleCollider>();
            body.center = Vector3.up * 1.35f;
            body.height = 2.7f;
            body.radius = 0.8f;

            SetObjectReference(ai, "gunBarrel", muzzle);
            SetObjectReference(ai, "bulletPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(BulletPrefabPath));
            SetObjectReference(ai, "eye", eye);
            SetFloat(ai, "detectionRange", 24f);
            SetFloat(ai, "shootRange", 14f);
            SetFloat(ai, "preferredRange", 9f);
            SetFloat(ai, "moveSpeed", 3.5f);
            SetFloat(ai, "fireRate", 1.35f);
            SetFloat(ai, "laserDuration", 1.2f);
            SetFloat(ai, "laserRange", 20f);
            SetFloat(ai, "laserDamageInterval", 0.2f);
            SetFloat(ai, "eyeTrackingSpeed", 10f);
            SetFloat(ai, "eyeSurfaceOffset", 0.35f);
            SetObjectReference(effects, "muzzle", muzzle);
            SetFloat(health, "maxHealth", 140f);
            SetFloat(health, "barHeight", 3f);
            SetFloat(health, "deathDelay", 2.8f);

            PrefabUtility.SaveAsPrefabAsset(monster, MonsterPrefabPath);
            Debug.Log("[Monster1Animator] Configured Monster1 prefab.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(monster);
        }
    }

    private static Transform FindChild(Transform root, string objectName)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == objectName) return child;
        }

        return null;
    }

    private static Component GetOrAddComponent(GameObject target, string typeName)
    {
        Type type = FindRuntimeType(typeName);
        if (type == null)
        {
            Debug.LogWarning("[Monster1Animator] Cannot find component type: " + typeName);
            return null;
        }

        Component component = target.GetComponent(type);
        return component != null ? component : target.AddComponent(type);
    }

    private static Type FindRuntimeType(string typeName)
    {
        foreach (System.Reflection.Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type type = assembly.GetType(typeName);
            if (type != null) return type;
        }

        return null;
    }

    private static void SetObjectReference(Component component, string propertyName, UnityEngine.Object value)
    {
        if (component == null) return;
        SerializedObject serialized = new SerializedObject(component);
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null) return;
        property.objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetFloat(Component component, string propertyName, float value)
    {
        if (component == null) return;
        SerializedObject serialized = new SerializedObject(component);
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null) return;
        property.floatValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ResetController(AnimatorController controller)
    {
        for (int i = controller.layers.Length - 1; i > 0; i--)
        {
            AnimatorStateMachine layerRoot = controller.layers[i].stateMachine;
            controller.RemoveLayer(i);
            if (layerRoot != null) UnityEngine.Object.DestroyImmediate(layerRoot, true);
        }

        for (int i = controller.parameters.Length - 1; i >= 0; i--) controller.RemoveParameter(i);
        AnimatorStateMachine root = controller.layers[0].stateMachine;
        foreach (ChildAnimatorState state in root.states) root.RemoveState(state.state);
        foreach (AnimatorStateTransition transition in root.anyStateTransitions) root.RemoveAnyStateTransition(transition);
        root.defaultState = null;

        foreach (UnityEngine.Object item in AssetDatabase.LoadAllAssetsAtPath(OutputPath))
        {
            if (item is BlendTree) UnityEngine.Object.DestroyImmediate(item, true);
        }
    }

    private static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder(OutputFolder)) AssetDatabase.CreateFolder("Assets", "Animations");
    }

    private static AnimatorState AddState(
        AnimatorStateMachine root,
        string name,
        Motion motion,
        float x,
        float y)
    {
        AnimatorState state = root.AddState(name, new Vector3(x, y, 0f));
        state.motion = motion;
        return state;
    }

    private static void AddAnyTransition(
        AnimatorStateMachine root,
        AnimatorState target,
        string trigger,
        float duration,
        params AnimatorCondition[] extraConditions)
    {
        AnimatorCondition[] conditions = new AnimatorCondition[extraConditions.Length + 1];
        conditions[0] = If(trigger);
        Array.Copy(extraConditions, 0, conditions, 1, extraConditions.Length);
        Configure(root.AddAnyStateTransition(target), duration, false, 0f, conditions);
    }

    private static void AddTransition(
        AnimatorState from,
        AnimatorState to,
        float duration,
        bool hasExitTime,
        float exitTime,
        params AnimatorCondition[] conditions)
    {
        Configure(from.AddTransition(to), duration, hasExitTime, exitTime, conditions);
    }

    private static void Configure(
        AnimatorStateTransition transition,
        float duration,
        bool hasExitTime,
        float exitTime,
        AnimatorCondition[] conditions)
    {
        transition.duration = duration;
        transition.hasFixedDuration = true;
        transition.hasExitTime = hasExitTime;
        transition.exitTime = exitTime;
        transition.canTransitionToSelf = false;
        transition.interruptionSource = TransitionInterruptionSource.SourceThenDestination;
        transition.conditions = conditions;
    }

    private static AnimatorCondition If(string name) => Condition(AnimatorConditionMode.If, name, 0f);
    private static AnimatorCondition IfNot(string name) => Condition(AnimatorConditionMode.IfNot, name, 0f);
    private static AnimatorCondition Greater(string name, float value) => Condition(AnimatorConditionMode.Greater, name, value);
    private static AnimatorCondition Less(string name, float value) => Condition(AnimatorConditionMode.Less, name, value);
    private static AnimatorCondition Equals(string name, float value) => Condition(AnimatorConditionMode.Equals, name, value);

    private static AnimatorCondition Condition(AnimatorConditionMode mode, string name, float value)
    {
        return new AnimatorCondition { mode = mode, parameter = name, threshold = value };
    }

    private static bool HasVersion(AnimatorController controller)
    {
        if (controller == null) return false;
        foreach (AnimatorControllerParameter parameter in controller.parameters)
        {
            if (parameter.name == VersionParameter) return true;
        }

        return false;
    }
}
#endif
