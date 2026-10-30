#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

public static class FemaleFirstPersonBuilder
{
    const string Folder = "Assets/Resources/";
    const string ControllerPath = Folder + "FemaleFirstPerson.controller";
    const string ClipFolder = "Assets/Animations/Female/Clips/";
    const float MinimumArmWeight = 0.999f;
    const float ShoulderInset = 0.85f;

    sealed class ArmRegion
    {
        public bool[] bones;
        public Vector3 shoulder, direction;
        public float inset;
    }

    [MenuItem("GunQuest/Female/3 - Build First Person Meshes")]
    public static void Build()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
        Directory.CreateDirectory(Folder);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/DefaultFemalePBR.prefab");
        if (prefab == null) throw new InvalidOperationException("Female source prefab is missing.");
        var clips = LoadClips();
        var generated = new List<KeyValuePair<string, Mesh>>();
        try
        {
            foreach (var skin in prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (skin.name != "Body01" && skin.name != "Body02") continue;
                generated.Add(new KeyValuePair<string, Mesh>("FemaleFirstPerson" + skin.name, ExtractArms(skin)));
            }
            if (generated.Count != 2) throw new InvalidOperationException("Expected both Female Body01 and Body02 meshes.");
            // Validate both variants before replacing any generated resource.
            foreach (var output in generated) SaveMesh(output.Value, output.Key);
            BuildAnimator(clips);
            AssetDatabase.SaveAssets();
        }
        finally
        {
            foreach (var output in generated)
                if (output.Value != null && !EditorUtility.IsPersistent(output.Value)) Object.DestroyImmediate(output.Value);
        }
        Debug.Log("Female FPS skinned arms and independent Animator saved. Source meshes and clips are unchanged.");
    }

    static Mesh ExtractArms(SkinnedMeshRenderer skin)
    {
        var original = skin.sharedMesh;
        if (original == null || !original.isReadable)
            throw new InvalidOperationException(skin.name + " requires a readable source mesh.");
        var weights = original.boneWeights;
        var vertices = original.vertices;
        if (weights.Length != vertices.Length || original.bindposes.Length != skin.bones.Length)
            throw new InvalidOperationException(skin.name + " has an incomplete skin binding.");
        var left = Region(skin, "l");
        var right = Region(skin, "r");
        var arm = new int[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            arm[i] = -1;
            float leftWeight = Weight(weights[i], left.bones);
            float rightWeight = Weight(weights[i], right.bones);
            if (leftWeight >= MinimumArmWeight && leftWeight > rightWeight && InArm(vertices[i], left)) arm[i] = 0;
            else if (rightWeight >= MinimumArmWeight && rightWeight > leftWeight && InArm(vertices[i], right)) arm[i] = 1;
        }
        var counts = new int[2];
        var mesh = Object.Instantiate(original);
        try
        {
            for (int sub = 0; sub < original.subMeshCount; sub++)
            {
                if (original.GetTopology(sub) != MeshTopology.Triangles)
                    throw new InvalidOperationException(skin.name + " contains a non-triangle submesh.");
                var triangles = original.GetTriangles(sub);
                var selected = new List<int>();
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    int side = arm[triangles[i]];
                    if (side >= 0 && arm[triangles[i + 1]] == side && arm[triangles[i + 2]] == side)
                    {
                        selected.Add(triangles[i]);
                        selected.Add(triangles[i + 1]);
                        selected.Add(triangles[i + 2]);
                        counts[side]++;
                    }
                }
                mesh.SetTriangles(selected, sub);
            }
            if (counts[0] == 0 || counts[1] == 0)
                throw new InvalidOperationException(skin.name + " extraction did not retain both arms.");
            // After excluding chest-weighted garment vertices, remove tiny residual
            // weights: camera-space shoulder placement must not leave chest/clavicle
            // influences pulling the cropped sleeve back to the hidden torso.
            // Elbow, wrist and finger blends remain within the same arm.
            for (int i = 0; i < weights.Length; i++)
                if (arm[i] >= 0) weights[i] = ArmOnly(weights[i], arm[i] == 0 ? left.bones : right.bones);
            mesh.boneWeights = weights;
            // Original skinning across elbow/wrist
            // seams, bind poses, normals, UVs and material slots remain intact.
            Debug.Log(skin.name + " skinned arms: " + counts[0] + " left / " + counts[1] + " right triangles.");
            return mesh;
        }
        catch
        {
            Object.DestroyImmediate(mesh);
            throw;
        }
    }

    static ArmRegion Region(SkinnedMeshRenderer skin, string side)
    {
        int upper = Bone(skin, "upperarm_" + side), lower = Bone(skin, "lowerarm_" + side);
        int hand = Bone(skin, "hand_" + side);
        var upperBone = skin.bones[upper];
        if (!skin.bones[lower].IsChildOf(upperBone) || !skin.bones[hand].IsChildOf(skin.bones[lower]))
            throw new InvalidOperationException(skin.name + " has an unexpected " + side + " arm hierarchy.");
        var bindposes = skin.sharedMesh.bindposes;
        Vector3 shoulder = bindposes[upper].inverse.MultiplyPoint3x4(Vector3.zero);
        Vector3 elbow = bindposes[lower].inverse.MultiplyPoint3x4(Vector3.zero);
        Vector3 axis = elbow - shoulder;
        if (axis.sqrMagnitude < 0.000001f) throw new InvalidOperationException(skin.name + " arm bind pose has no length.");
        var region = new ArmRegion
        {
            bones = new bool[skin.bones.Length], shoulder = shoulder,
            direction = axis.normalized, inset = axis.magnitude * ShoulderInset
        };
        // All upperarm descendants include lowerarm, hand, every finger and any
        // twist/helper bones. Elbows and wrists are never separately cropped.
        for (int i = 0; i < skin.bones.Length; i++)
            region.bones[i] = skin.bones[i] != null && (skin.bones[i] == upperBone || skin.bones[i].IsChildOf(upperBone));
        return region;
    }

    static int Bone(SkinnedMeshRenderer skin, string name)
    {
        for (int i = 0; i < skin.bones.Length; i++)
            if (skin.bones[i] != null && string.Equals(skin.bones[i].name, name, StringComparison.OrdinalIgnoreCase)) return i;
        throw new InvalidOperationException(skin.name + " is missing bone " + name + ".");
    }

    static BoneWeight ArmOnly(BoneWeight w, bool[] bones)
    {
        if (!bones[w.boneIndex0]) w.weight0 = 0;
        if (!bones[w.boneIndex1]) w.weight1 = 0;
        if (!bones[w.boneIndex2]) w.weight2 = 0;
        if (!bones[w.boneIndex3]) w.weight3 = 0;
        float sum = w.weight0 + w.weight1 + w.weight2 + w.weight3;
        w.weight0 /= sum; w.weight1 /= sum; w.weight2 /= sum; w.weight3 /= sum;
        return w;
    }
    static bool InArm(Vector3 vertex, ArmRegion arm)
    {
        // A cut near the elbow excludes the original oversized shoulder armor and
        // chest flaps. The original forearm/elbow skin remains; the open end belongs below the frame.
        return Vector3.Dot(vertex - arm.shoulder, arm.direction) >= arm.inset;
    }

    static float Weight(BoneWeight weight, bool[] bones)
    {
        return (bones[weight.boneIndex0] ? weight.weight0 : 0f) +
            (bones[weight.boneIndex1] ? weight.weight1 : 0f) +
            (bones[weight.boneIndex2] ? weight.weight2 : 0f) +
            (bones[weight.boneIndex3] ? weight.weight3 : 0f);
    }

    static Dictionary<string, AnimationClip> LoadClips()
    {
        var clips = new Dictionary<string, AnimationClip>();
        foreach (string suffix in new[] { "HG01", "AR" })
            foreach (string action in new[] { "IdleBattle01", "Reloading", "ShootSingleshot" })
            {
                string name = action + "_" + suffix;
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipFolder + name + ".anim");
                if (clip == null || !clip.humanMotion || clip.length <= 0f)
                    throw new InvalidOperationException("Missing or invalid Humanoid FPS source clip: " + name);
                clips.Add(name, clip);
            }
        return clips;
    }

    static void BuildAnimator(Dictionary<string, AnimationClip> clips)
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.layers = Array.Empty<AnimatorControllerLayer>();
        controller.parameters = Array.Empty<AnimatorControllerParameter>();
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(ControllerPath))
            if (asset != controller) Object.DestroyImmediate(asset, true);
        controller.AddParameter("IsRifle", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsMeleeWeapon", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Reload", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Shoot", AnimatorControllerParameterType.Trigger);
        controller.AddParameter(new AnimatorControllerParameter
        { name = "ReloadRate", type = AnimatorControllerParameterType.Float, defaultFloat = 1f });
        var root = new AnimatorStateMachine { name = "FPS Arms" };
        AssetDatabase.AddObjectToAsset(root, controller);
        controller.AddLayer(new AnimatorControllerLayer { name = "Base Layer", stateMachine = root, defaultWeight = 1f });
        var ready = new AnimatorState[2];
        for (int i = 0; i < 2; i++)
        {
            bool rifle = i == 1;
            string stance = rifle ? "AR" : "HG", suffix = rifle ? "AR" : "HG01";
            float y = i * 240;
            ready[i] = State(root, controller, "Ready" + stance, clips["IdleBattle01_" + suffix], true, 220, y);
            var shot = State(root, controller, "Shoot" + stance, clips["ShootSingleshot_" + suffix], false, 500, y);
            var reload = State(root, controller, "Reload" + stance, clips["Reloading_" + suffix], false, 780, y);
            float duration = rifle ? 2f : 1.4f;
            reload.speed = clips["Reloading_" + suffix].length / duration;
            // ReloadRate is a multiplier: 1 preserves the gameplay reload duration.
            reload.speedParameter = "ReloadRate"; reload.speedParameterActive = true;
            Action(root, shot, "Shoot", rifle, true);
            Action(root, reload, "Reload", rifle, false);
            Return(shot, ready[i]);
            Return(reload, ready[i]);
        }
        root.defaultState = ready[0];
        Bool(Transition(ready[0], ready[1]), "IsRifle", true);
        Bool(Transition(ready[1], ready[0]), "IsRifle", false);
        EditorUtility.SetDirty(controller);
    }

    static AnimatorState State(AnimatorStateMachine root, AnimatorController controller, string name, AnimationClip source, bool loop, float x, float y)
    {
        // Embedded FPS copies allow independent loop/event settings. No gameplay
        // events are replayed, and source/TPS animation assets remain untouched.
        var copy = Object.Instantiate(source);
        copy.name = "FPS_" + source.name;
        AnimationUtility.SetAnimationEvents(copy, Array.Empty<AnimationEvent>());
        var settings = AnimationUtility.GetAnimationClipSettings(copy);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(copy, settings);
        AssetDatabase.AddObjectToAsset(copy, controller);
        var state = root.AddState(name, new Vector3(x, y));
        state.motion = copy; state.iKOnFeet = false;
        return state;
    }

    static AnimatorStateTransition Transition(AnimatorState from, AnimatorState to)
    {
        var transition = from.AddTransition(to);
        transition.hasExitTime = false; transition.hasFixedDuration = true;
        transition.duration = 0.1f; transition.canTransitionToSelf = false;
        return transition;
    }

    static void Action(AnimatorStateMachine root, AnimatorState state, string trigger, bool rifle, bool repeat)
    {
        var transition = root.AddAnyStateTransition(state);
        transition.hasExitTime = false; transition.hasFixedDuration = true;
        transition.duration = 0.1f; transition.canTransitionToSelf = repeat;
        transition.AddCondition(AnimatorConditionMode.If, 0f, trigger);
        Bool(transition, "IsRifle", rifle); Bool(transition, "IsMeleeWeapon", false);
    }

    static void Return(AnimatorState from, AnimatorState ready)
    {
        var transition = Transition(from, ready);
        transition.hasExitTime = true; transition.exitTime = 1f;
    }

    static void Bool(AnimatorStateTransition transition, string name, bool value)
    {
        transition.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, name);
    }

    static void SaveMesh(Mesh mesh, string name)
    {
        mesh.name = name;
        string path = Folder + name + ".asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing == null) AssetDatabase.CreateAsset(mesh, path);
        else
        {
            EditorUtility.CopySerialized(mesh, existing);
            EditorUtility.SetDirty(existing);
            Object.DestroyImmediate(mesh);
        }
    }
}
#endif
