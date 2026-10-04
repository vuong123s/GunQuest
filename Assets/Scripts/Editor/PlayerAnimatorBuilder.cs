#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class PlayerAnimatorBuilder
{
    private const string SourcePath = "Assets/DL/TinyBirdDuo/Animator&Mask/DefaultAnimator.controller";
    private const string MaskPath = "Assets/DL/TinyBirdDuo/Animator&Mask/RootMotion&AnimBlending/AvatarMaskForAnimBlending.mask";
    private const string OutputFolder = "Assets/Animations";
    private const string OutputPath = OutputFolder + "/Player.controller";
    private const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";
    private const string VersionParameter = "GunQuestPlayerAnimatorV1";

    [MenuItem("GunQuest/Rebuild Player Animator", false, 20)]
    public static void Build()
    {
        AnimatorController source = AssetDatabase.LoadAssetAtPath<AnimatorController>(SourcePath);
        if (source == null)
        {
            Debug.LogError("[PlayerAnimator] Khong tim thay DefaultAnimator: " + SourcePath);
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
        BuildBaseLayer(controller, clips);
        BuildUpperBodyLayer(controller, clips);
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssignToPlayer(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[PlayerAnimator] Created " + OutputPath + " from all DefaultAnimator clips.");
    }

    [InitializeOnLoadMethod]
    private static void BuildWhenNeeded()
    {
        EditorApplication.delayCall += () =>
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(OutputPath);
            if (!HasVersion(controller)) Build();
        };
    }

    private static void BuildBaseLayer(
        AnimatorController controller,
        IReadOnlyDictionary<string, AnimationClip> clips)
    {
        AnimatorStateMachine root = controller.layers[0].stateMachine;
        root.name = "Base Layer";

        AnimatorState locomotion = AddState(root, "Locomotion",
            CreateDirectionalTree(controller, "Walk", clips, false), 300f, 20f);
        AnimatorState run = AddState(root, "Run",
            CreateDirectionalTree(controller, "Run", clips, true), 540f, 20f);
        AnimatorState lookAround = AddState(root, "Look Around", clips["LookAroundOneWeapon"], 300f, 180f);
        AnimatorState shoulderIdle = AddState(root, "Shoulder Idle", clips["IdleOneWeaponShoulder"], 540f, 180f);
        AnimatorState jump = AddState(root, "Jump", clips["JumpFullOneWeapon_IP"], 780f, 20f);
        AnimatorState hit = AddState(root, "Hit", clips["GetHitOneWeapon"], 780f, 140f);
        AnimatorState dizzy = AddState(root, "Dizzy", clips["DizzyOneWeapon"], 780f, 260f);
        AnimatorState die = AddState(root, "Die", clips["DieOneWeapon"], 1020f, 140f);
        root.defaultState = locomotion;

        AddTransition(locomotion, run, 0.1f, false, 0f,
            If("IsMoving"), If("IsSprinting"));
        AddTransition(run, locomotion, 0.1f, false, 0f, IfNot("IsSprinting"));
        AddTransition(run, locomotion, 0.1f, false, 0f, IfNot("IsMoving"));

        AddTransition(locomotion, lookAround, 0.15f, true, 4f,
            IfNot("IsMoving"), IfNot("IsFiring"));
        AddTransition(lookAround, shoulderIdle, 0.12f, true, 0.92f,
            IfNot("IsMoving"), IfNot("IsFiring"));
        AddTransition(shoulderIdle, locomotion, 0.12f, true, 0.92f);
        AddTransition(lookAround, locomotion, 0.08f, false, 0f, If("IsMoving"));
        AddTransition(shoulderIdle, locomotion, 0.08f, false, 0f, If("IsMoving"));

        AddAnyTransition(root, jump, "Jump", 0.06f);
        AddTransition(jump, locomotion, 0.1f, true, 0.8f, If("IsGrounded"));

        AddDashTransitions(root, locomotion, clips);
        AddAnyTransition(root, hit, "Hit", 0.05f);
        AddTransition(hit, locomotion, 0.1f, true, 0.9f);
        AddAnyTransition(root, dizzy, "Dizzy", 0.08f);
        AddTransition(dizzy, locomotion, 0.12f, true, 0.9f);
        AddAnyTransition(root, die, "Die", 0.08f);
    }

    private static void BuildUpperBodyLayer(
        AnimatorController controller,
        IReadOnlyDictionary<string, AnimationClip> clips)
    {
        AnimatorStateMachine root = new AnimatorStateMachine { name = "Upper Body" };
        AssetDatabase.AddObjectToAsset(root, controller);
        controller.AddLayer(new AnimatorControllerLayer
        {
            name = "Upper Body",
            stateMachine = root,
            avatarMask = AssetDatabase.LoadAssetAtPath<AvatarMask>(MaskPath),
            blendingMode = AnimatorLayerBlendingMode.Override,
            defaultWeight = 1f,
            syncedLayerIndex = -1
        });

        AnimatorState empty = AddState(root, "Empty", null, 300f, 20f);
        AnimatorState fire = AddState(root, "Fire", clips["ShootSingleshotOneWeapon"], 540f, 20f);
        AnimatorState melee = AddState(root, "Melee", clips["MeleeVerticalOneWeapon"], 540f, 160f);
        root.defaultState = empty;

        AddTransition(empty, fire, 0.04f, false, 0f, If("IsFiring"));
        AddTransition(fire, empty, 0.06f, false, 0f, IfNot("IsFiring"));
        AddAnyTransition(root, melee, "Melee", 0.05f);
        AddTransition(melee, empty, 0.08f, true, 0.9f);
    }

    private static BlendTree CreateDirectionalTree(
        AnimatorController controller,
        string name,
        IReadOnlyDictionary<string, AnimationClip> clips,
        bool running)
    {
        string prefix = running ? "Run" : "Walk";
        BlendTree tree = new BlendTree
        {
            name = name + " Directional",
            blendType = BlendTreeType.SimpleDirectional2D,
            blendParameter = "MoveX",
            blendParameterY = "MoveY",
            useAutomaticThresholds = false
        };

        if (!running) tree.AddChild(clips["IdleOneWeapon"], Vector2.zero);
        tree.AddChild(clips[prefix + "FWDOneWeapon_IP"], Vector2.up);
        tree.AddChild(clips[prefix + "BWDOneWeapon_IP"], Vector2.down);
        tree.AddChild(clips[prefix + "LFTOneWeapon_IP"], Vector2.left);
        tree.AddChild(clips[prefix + "RGTOneWeapon_IP"], Vector2.right);
        AssetDatabase.AddObjectToAsset(tree, controller);
        return tree;
    }

    private static void AddDashTransitions(
        AnimatorStateMachine root,
        AnimatorState locomotion,
        IReadOnlyDictionary<string, AnimationClip> clips)
    {
        AnimatorState forward = AddState(root, "Dash Forward", clips["DashFWDOneWeapon_IP"], 1020f, 20f);
        AnimatorState back = AddState(root, "Dash Back", clips["DashBWDOneWeapon_IP"], 1020f, 260f);
        AnimatorState left = AddState(root, "Dash Left", clips["DashLFTOneWeapon_IP"], 1020f, 380f);
        AnimatorState right = AddState(root, "Dash Right", clips["DashRGTOneWeapon_IP"], 1020f, 500f);

        AddAnyTransition(root, forward, "Dash", 0.04f, Greater("MoveY", 0.25f));
        AddAnyTransition(root, back, "Dash", 0.04f, Less("MoveY", -0.25f));
        AddAnyTransition(root, left, "Dash", 0.04f, Less("MoveX", -0.25f));
        AddAnyTransition(root, right, "Dash", 0.04f, Greater("MoveX", 0.25f));

        AddTransition(forward, locomotion, 0.08f, true, 0.9f);
        AddTransition(back, locomotion, 0.08f, true, 0.9f);
        AddTransition(left, locomotion, 0.08f, true, 0.9f);
        AddTransition(right, locomotion, 0.08f, true, 0.9f);
    }

    private static Dictionary<string, AnimationClip> ReadClips(AnimatorController source)
    {
        Dictionary<string, AnimationClip> clips = new Dictionary<string, AnimationClip>(StringComparer.OrdinalIgnoreCase);
        foreach (AnimatorControllerLayer layer in source.layers) ReadStateMachine(layer.stateMachine, clips);
        Debug.Log("[PlayerAnimator] Read " + clips.Count + " clips from DefaultAnimator.");
        return clips;
    }

    private static void ReadStateMachine(
        AnimatorStateMachine machine,
        IDictionary<string, AnimationClip> clips)
    {
        foreach (ChildAnimatorState child in machine.states)
        {
            if (child.state.motion is AnimationClip clip) clips[child.state.name] = clip;
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
            "IdleOneWeapon", "IdleOneWeaponShoulder", "LookAroundOneWeapon",
            "WalkFWDOneWeapon_IP", "WalkBWDOneWeapon_IP", "WalkLFTOneWeapon_IP", "WalkRGTOneWeapon_IP",
            "RunFWDOneWeapon_IP", "RunBWDOneWeapon_IP", "RunLFTOneWeapon_IP", "RunRGTOneWeapon_IP",
            "DashFWDOneWeapon_IP", "DashBWDOneWeapon_IP", "DashLFTOneWeapon_IP", "DashRGTOneWeapon_IP",
            "JumpFullOneWeapon_IP", "ShootSingleshotOneWeapon", "MeleeVerticalOneWeapon",
            "GetHitOneWeapon", "DizzyOneWeapon", "DieOneWeapon"
        };

        foreach (string name in names)
        {
            if (clips.ContainsKey(name)) continue;
            Debug.LogError("[PlayerAnimator] DefaultAnimator thieu state: " + name);
            return false;
        }

        return true;
    }

    private static void AddParameters(AnimatorController controller)
    {
        controller.AddParameter("MoveX", AnimatorControllerParameterType.Float);
        controller.AddParameter("MoveY", AnimatorControllerParameterType.Float);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsCrouching", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsSprinting", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsAiming", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsFiring", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Jump", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Dash", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Melee", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Dizzy", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Reload", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);
        controller.AddParameter(VersionParameter, AnimatorControllerParameterType.Bool);
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

    private static void AssignToPlayer(RuntimeAnimatorController controller)
    {
        if (!System.IO.File.Exists(PlayerPrefabPath)) return;
        GameObject player = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
        try
        {
            Animator animator = player.GetComponentInChildren<Animator>(true);
            if (animator == null)
            {
                Debug.LogError("[PlayerAnimator] Player prefab khong co Animator.");
                return;
            }

            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            PrefabUtility.SaveAsPrefabAsset(player, PlayerPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(player);
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
