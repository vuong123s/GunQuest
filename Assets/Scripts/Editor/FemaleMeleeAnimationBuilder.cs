#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Editable, event-free weapon motion clips. The runtime samples this Generic
/// Animator before positioning the Female hands, so the original skinned arms
/// follow the same weapon motion instead of replaying a second hit event.
/// </summary>
public static class FemaleMeleeAnimationBuilder
{
    const string Folder = "Assets/Resources/FemaleMelee";
    const string ControllerPath = Folder + "/MeleeMotion.controller";

    readonly struct PoseKey
    {
        public readonly float Time;
        public readonly Vector3 Position, Rotation;
        public PoseKey(float time, Vector3 position, Vector3 rotation)
        { Time = time; Position = position; Rotation = rotation; }
    }

    [MenuItem("GunQuest/Female/5 - Build Melee Animations")]
    public static void Build()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before building melee animations.");
        EnsureFolder(Folder);
        var bat = MeleeAttackTiming.Bat;
        var shovel = MeleeAttackTiming.Shovel;
        var quick = MeleeAttackTiming.Quick;
        var idle = SaveClip("Idle", new[] { Rest(0f), Rest(1f) }, true);
        var batClip = SaveClip("BatSwing", new[]
        {
            Rest(0f),
            Pose(bat.ImpactTime * .45f, .06f, .08f, -.08f, -25f, 12f, -15f),
            Pose(bat.ImpactTime, -.08f, -.06f, .16f, 32f, -8f, 12f),
            Pose(bat.ImpactTime + .12f, -.14f, -.12f, .08f, 42f, -14f, 18f),
            Pose(bat.Duration * .80f, -.04f, -.04f, -.01f, 10f, -4f, 4f),
            Rest(bat.Duration)
        }, false);
        var shovelClip = SaveClip("ShovelSwing", new[]
        {
            Rest(0f),
            Pose(shovel.ImpactTime * .45f, .06f, .10f, -.10f, -30f, 14f, -18f),
            Pose(shovel.ImpactTime, -.08f, -.08f, .18f, 38f, -10f, 15f),
            Pose(shovel.ImpactTime + .15f, -.16f, -.15f, .10f, 50f, -18f, 22f),
            Pose(shovel.Duration * .80f, -.04f, -.05f, -.01f, 12f, -5f, 5f),
            Rest(shovel.Duration)
        }, false);
        var quickClip = SaveClip("QuickSwing", new[]
        {
            Rest(0f),
            Pose(quick.ImpactTime * .4f, .01f, .08f, -.05f, -15f, 3f, -2f),
            Pose(quick.ImpactTime, -.02f, -.10f, .10f, 25f, -3f, 4f),
            Pose(quick.ImpactTime + .055f, -.03f, -.12f, .06f, 32f, -4f, 5f),
            Rest(quick.Duration)
        }, false);
        SaveController(idle, batClip, shovelClip, quickClip);
        AssetDatabase.SaveAssets();
        Debug.Log("Female melee motion clips saved: BatSwing, ShovelSwing, QuickSwing. Edit these clips in the Animation window; damage timing stays in MeleeAttackTiming.");
    }

    static PoseKey Rest(float time) => new PoseKey(time, Vector3.zero, Vector3.zero);
    static PoseKey Pose(float time, float x, float y, float z, float rx, float ry, float rz)
        => new PoseKey(time, new Vector3(x, y, z), new Vector3(rx, ry, rz));

    static AnimationClip SaveClip(string name, PoseKey[] poses, bool loop)
    {
        var clip = new AnimationClip { name = name, frameRate = 60f, legacy = false, wrapMode = loop ? WrapMode.Loop : WrapMode.ClampForever };
        for (int axis = 0; axis < 3; axis++)
        {
            string suffix = axis == 0 ? ".x" : axis == 1 ? ".y" : ".z";
            AnimationUtility.SetEditorCurve(clip,
                EditorCurveBinding.FloatCurve("Pose", typeof(Transform), "m_LocalPosition" + suffix), Curve(poses, axis, false));
            AnimationUtility.SetEditorCurve(clip,
                EditorCurveBinding.FloatCurve("Pose", typeof(Transform), "localEulerAnglesRaw" + suffix), Curve(poses, axis, true));
        }
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        settings.loopBlend = false;
        settings.keepOriginalPositionXZ = true;
        settings.keepOriginalPositionY = true;
        settings.keepOriginalOrientation = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        AnimationUtility.SetAnimationEvents(clip, Array.Empty<AnimationEvent>());
        string path = Folder + "/" + name + ".anim";
        var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (existing == null) AssetDatabase.CreateAsset(clip, path);
        else
        {
            EditorUtility.CopySerialized(clip, existing);
            EditorUtility.SetDirty(existing);
            Object.DestroyImmediate(clip);
            clip = existing;
        }
        return clip;
    }

    static AnimationCurve Curve(PoseKey[] poses, int axis, bool rotation)
    {
        var keys = new Keyframe[poses.Length];
        for (int i = 0; i < poses.Length; i++)
            keys[i] = new Keyframe(poses[i].Time, rotation ? poses[i].Rotation[axis] : poses[i].Position[axis]);
        var curve = new AnimationCurve(keys) { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever };
        // Clamped tangents keep the weapon inside the authored sweep and smooth
        // the recovery. Endpoints have zero velocity so entering/leaving Idle
        // never snaps the hands even when the Animator is manually evaluated.
        for (int i = 0; i < curve.length; i++)
        {
            AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
            AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
        }
        for (int i = 0; i < curve.length; i += curve.length - 1)
        {
            AnimationUtility.SetKeyBroken(curve, i, true);
            AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Free);
            AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Free);
            var key = curve[i]; key.inTangent = key.outTangent = 0f;
            curve.MoveKey(i, key);
        }
        return curve;
    }

    static void SaveController(AnimationClip idle, AnimationClip bat, AnimationClip shovel, AnimationClip quick)
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.layers = Array.Empty<AnimatorControllerLayer>();
        controller.parameters = Array.Empty<AnimatorControllerParameter>();
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(ControllerPath))
            if (asset != controller) Object.DestroyImmediate(asset, true);
        var machine = new AnimatorStateMachine { name = "Weapon Motion" };
        AssetDatabase.AddObjectToAsset(machine, controller);
        controller.AddLayer(new AnimatorControllerLayer { name = "Base Layer", stateMachine = machine, defaultWeight = 1f });
        var idleState = State(machine, "Idle", idle, 220f, 0f);
        machine.defaultState = idleState;
        ReturnToIdle(State(machine, "BatSwing", bat, 480f, -100f), idleState);
        ReturnToIdle(State(machine, "ShovelSwing", shovel, 480f, 40f), idleState);
        ReturnToIdle(State(machine, "QuickSwing", quick, 480f, 180f), idleState);
        EditorUtility.SetDirty(controller);
    }

    static AnimatorState State(AnimatorStateMachine machine, string name, AnimationClip clip, float x, float y)
    {
        var state = machine.AddState(name, new Vector3(x, y));
        state.motion = clip;
        state.iKOnFeet = false;
        state.writeDefaultValues = false;
        return state;
    }

    static void ReturnToIdle(AnimatorState action, AnimatorState idle)
    {
        var transition = action.AddTransition(idle);
        transition.hasExitTime = true;
        transition.exitTime = 1f;
        transition.hasFixedDuration = true;
        transition.duration = .08f;
        transition.canTransitionToSelf = false;
    }

    static void EnsureFolder(string folder)
    {
        string[] parts = folder.Split('/');
        string parent = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = parent + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(parent, parts[i]);
            parent = next;
        }
    }
}
#endif
