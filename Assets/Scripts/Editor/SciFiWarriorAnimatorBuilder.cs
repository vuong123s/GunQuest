#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class SciFiWarriorAnimatorBuilder
{
    private static readonly string[] ControllerPaths =
    {
        "Assets/DL/SciFiWarriorPBRHPPolyart/Animators/SciFiWarrior.controller",
        "Assets/DL/SciFiWarriorPBRHPPolyart/Animators/SciFiWarrior 1.controller"
    };
    private const string AnimationsRoot = "Assets/DL/SciFiWarriorPBRHPPolyart/Animations";

    [MenuItem("GunQuest/Rebuild Player Animator (Smooth)", false, 20)]
    public static void RebuildFromMenu()
    {
        BuildControllers();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog(
            "Animator Updated",
            "SciFiWarrior.controller va SciFiWarrior 1.controller da duoc tao lai voi Blend Tree va transitions muot.\n\n" +
            "Hay nhan Play de kiem tra.",
            "OK");
    }

    [InitializeOnLoadMethod]
    private static void AutoBuildOnLoad()
    {
        EditorApplication.delayCall += () =>
        {
            foreach (string controllerPath in ControllerPaths)
            {
                var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
                if (controller == null || controller.parameters.Length == 0)
                {
                    BuildController(controllerPath);
                }
            }

            AssetDatabase.SaveAssets();
        };
    }

    public static void BuildControllers()
    {
        foreach (string controllerPath in ControllerPaths)
        {
            BuildController(controllerPath);
        }
    }

    public static void BuildController(string controllerPath)
    {
        AnimationClip idle = LoadClip("Idle_Guard_AR");
        AnimationClip walkFront = LoadClip("WalkFront_Shoot_AR");
        AnimationClip walkBack = LoadClip("WalkBack_Shoot_AR");
        AnimationClip walkLeft = LoadClip("WalkLeft_Shoot_AR");
        AnimationClip walkRight = LoadClip("WalkRight_Shoot_AR");
        AnimationClip run = LoadClip("Run_guard_AR");
        AnimationClip crouch = LoadClip("Idle_Ducking_AR");
        AnimationClip jump = LoadClip("Jump");

        if (idle == null || walkFront == null || run == null || jump == null)
        {
            Debug.LogError("[SciFiWarriorAnimatorBuilder] Khong tim thay animation clip. Kiem tra duong dan: " + AnimationsRoot);
            return;
        }

        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        // A controller whose Base Layer has no state machine is corrupt.  It cannot
        // be repaired in-place because AnimatorController.layers is read-only.
        if (controller != null && !HasValidBaseLayer(controller))
        {
            AssetDatabase.DeleteAsset(controllerPath);
            controller = null;
        }

        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        }
        else
        {
            ClearController(controller, controllerPath);
        }

        AddParameter(controller, "MoveX", AnimatorControllerParameterType.Float);
        AddParameter(controller, "MoveY", AnimatorControllerParameterType.Float);
        AddParameter(controller, "Speed", AnimatorControllerParameterType.Float);
        AddParameter(controller, "IsMoving", AnimatorControllerParameterType.Bool);
        AddParameter(controller, "IsGrounded", AnimatorControllerParameterType.Bool);
        AddParameter(controller, "IsCrouching", AnimatorControllerParameterType.Bool);
        AddParameter(controller, "IsSprinting", AnimatorControllerParameterType.Bool);
        AddParameter(controller, "Jump", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine root = controller.layers[0].stateMachine;

        BlendTree locomotionTree = CreateLocomotionBlendTree(controller, idle, walkFront, walkBack, walkLeft, walkRight);
        AnimatorState locomotion = root.AddState("Locomotion", new Vector3(300f, 0f, 0f));
        locomotion.motion = locomotionTree;
        locomotion.speedParameterActive = true;
        locomotion.speedParameter = "Speed";
        root.defaultState = locomotion;

        AnimatorState runState = root.AddState("Run", new Vector3(300f, 120f, 0f));
        runState.motion = run;

        AnimatorState crouchState = root.AddState("Crouch", new Vector3(300f, -120f, 0f));
        crouchState.motion = crouch;

        AnimatorState jumpState = root.AddState("Jump", new Vector3(540f, 0f, 0f));
        jumpState.motion = jump;

        // Keep every animation supplied by the character pack available to the
        // player.  Gameplay scripts can request any of these states through
        // PlayerMotor.PlayAnimation("ClipName").
        AddAdditionalAnimationStates(root, locomotion, new[]
        {
            "Idle_Guard_AR", "WalkFront_Shoot_AR", "WalkBack_Shoot_AR",
            "WalkLeft_Shoot_AR", "WalkRight_Shoot_AR", "Run_guard_AR",
            "Idle_Ducking_AR", "Jump"
        });

        AddTransition(locomotion, runState, 0.12f, false,
            Condition(AnimatorConditionMode.If, "IsSprinting"),
            Condition(AnimatorConditionMode.If, "IsMoving"),
            Condition(AnimatorConditionMode.IfNot, "IsCrouching"));

        AddTransition(runState, locomotion, 0.12f, false,
            Condition(AnimatorConditionMode.IfNot, "IsSprinting"));

        AddTransition(runState, locomotion, 0.12f, false,
            Condition(AnimatorConditionMode.IfNot, "IsMoving"));

        AddTransition(locomotion, crouchState, 0.15f, false,
            Condition(AnimatorConditionMode.If, "IsCrouching"));

        AddTransition(crouchState, locomotion, 0.15f, false,
            Condition(AnimatorConditionMode.IfNot, "IsCrouching"));

        AddTransition(runState, crouchState, 0.12f, false,
            Condition(AnimatorConditionMode.If, "IsCrouching"));

        AddTransition(crouchState, runState, 0.12f, false,
            Condition(AnimatorConditionMode.IfNot, "IsCrouching"),
            Condition(AnimatorConditionMode.If, "IsSprinting"),
            Condition(AnimatorConditionMode.If, "IsMoving"));

        AnimatorStateTransition anyToJump = root.AddAnyStateTransition(jumpState);
        ConfigureTransition(anyToJump, 0.08f, false, Condition(AnimatorConditionMode.If, "Jump"));

        AddTransition(jumpState, locomotion, 0.15f, true,
            Condition(AnimatorConditionMode.If, "IsGrounded"));

        EditorUtility.SetDirty(controller);
        Debug.Log("[SciFiWarriorAnimatorBuilder] Da tao lai " + controllerPath);
    }

    private static void ClearController(AnimatorController controller, string controllerPath)
    {
        Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(controllerPath);
        foreach (Object subAsset in subAssets)
        {
            if (subAsset != controller && subAsset != null)
            {
                Object.DestroyImmediate(subAsset, true);
            }
        }

        for (int i = controller.parameters.Length - 1; i >= 0; i--)
        {
            controller.RemoveParameter(i);
        }

        AnimatorStateMachine root = controller.layers[0].stateMachine;
        ChildAnimatorState[] states = root.states;
        for (int i = states.Length - 1; i >= 0; i--)
        {
            root.RemoveState(states[i].state);
        }

        AnimatorStateTransition[] anyTransitions = root.anyStateTransitions;
        for (int i = anyTransitions.Length - 1; i >= 0; i--)
        {
            root.RemoveAnyStateTransition(anyTransitions[i]);
        }

        root.entryTransitions = new AnimatorTransition[0];
        root.defaultState = null;
    }

    private static bool HasValidBaseLayer(AnimatorController controller)
    {
        return controller.layers != null && controller.layers.Length > 0 &&
               controller.layers[0].stateMachine != null;
    }

    private static void AddAdditionalAnimationStates(
        AnimatorStateMachine root,
        AnimatorState locomotion,
        string[] alreadyUsed)
    {
        var usedNames = new System.Collections.Generic.HashSet<string>(alreadyUsed,
            System.StringComparer.OrdinalIgnoreCase);
        string[] guids = AssetDatabase.FindAssets("t:AnimationClip", new[] { AnimationsRoot });
        int row = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null || usedNames.Contains(clip.name))
            {
                continue;
            }

            AnimatorState state = root.AddState(clip.name, new Vector3(760f, row++ * 75f, 0f));
            state.motion = clip;

            // Action clips return to normal locomotion after playing once.
            AddTransition(state, locomotion, 0.12f, true);
        }
    }

    private static BlendTree CreateLocomotionBlendTree(
        AnimatorController controller,
        AnimationClip idle,
        AnimationClip walkFront,
        AnimationClip walkBack,
        AnimationClip walkLeft,
        AnimationClip walkRight)
    {
        BlendTree tree = new BlendTree
        {
            name = "Locomotion",
            blendType = BlendTreeType.SimpleDirectional2D,
            blendParameter = "MoveX",
            blendParameterY = "MoveY",
            useAutomaticThresholds = false
        };

        tree.AddChild(idle, new Vector2(0f, 0f));
        tree.AddChild(walkFront, new Vector2(0f, 1f));

        if (walkBack != null)
        {
            tree.AddChild(walkBack, new Vector2(0f, -1f));
        }

        if (walkLeft != null)
        {
            tree.AddChild(walkLeft, new Vector2(-1f, 0f));
        }

        if (walkRight != null)
        {
            tree.AddChild(walkRight, new Vector2(1f, 0f));
        }

        AssetDatabase.AddObjectToAsset(tree, controller);
        return tree;
    }

    private static AnimationClip LoadClip(string clipName)
    {
        string[] guids = AssetDatabase.FindAssets("t:AnimationClip", new[] { AnimationsRoot });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip != null && string.Equals(clip.name, clipName, System.StringComparison.OrdinalIgnoreCase))
            {
                return clip;
            }
        }

        return null;
    }

    private static void AddParameter(AnimatorController controller, string name, AnimatorControllerParameterType type)
    {
        controller.AddParameter(name, type);
    }

    private static AnimatorCondition Condition(AnimatorConditionMode mode, string parameter)
    {
        return new AnimatorCondition
        {
            mode = mode,
            parameter = parameter,
            threshold = 0f
        };
    }

    private static void AddTransition(
        AnimatorState from,
        AnimatorState to,
        float duration,
        bool hasExitTime,
        params AnimatorCondition[] conditions)
    {
        AnimatorStateTransition transition = from.AddTransition(to);
        ConfigureTransition(transition, duration, hasExitTime, conditions);
    }

    private static void ConfigureTransition(
        AnimatorStateTransition transition,
        float duration,
        bool hasExitTime,
        params AnimatorCondition[] conditions)
    {
        transition.hasExitTime = hasExitTime;
        transition.exitTime = hasExitTime ? 0.75f : 0f;
        transition.duration = duration;
        transition.hasFixedDuration = true;
        transition.offset = 0f;
        transition.interruptionSource = TransitionInterruptionSource.None;
        transition.orderedInterruption = true;
        transition.canTransitionToSelf = false;

        transition.conditions = conditions;
    }
}
#endif
