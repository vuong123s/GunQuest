#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public static class FemalePlayerAnimatorBuilder
{
    const string Folder = "Assets/Animations/Female";
    const string ControllerPath = Folder + "/FemalePlayer.controller";
    const string Source = "Assets/DL/BattleRoyaleDuoPAPBR/Animation";
    const string FemalePrefab = "Assets/Prefabs/Player/DefaultFemalePBR.prefab";
    const string OutputPrefab = "Assets/Prefabs/Player/PlayerFemale.prefab";

    [MenuItem("GunQuest/Female/1 - Build Animator")]
    public static void Build()
    {
        Directory.CreateDirectory(Folder + "/Clips");
        AssetDatabase.Refresh();
        // Validate the entire source set before altering an existing controller.
        var clips = new Dictionary<string, AnimationClip>();
        foreach (string file in Directory.GetFiles(Source, "*.fbx", SearchOption.AllDirectories))
        {
            var clip = AssetDatabase.LoadAllAssetsAtPath(file).OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview__"));
            if (clip == null || !clip.humanMotion) throw new InvalidOperationException("Missing Humanoid clip: " + file);
            string name = System.IO.Path.GetFileNameWithoutExtension(file).Replace("_Anim", "");
            string path = Folder + "/Clips/" + name + ".anim";
            var copy = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (copy == null) { copy = Object.Instantiate(clip); AssetDatabase.CreateAsset(copy, path); }
            else EditorUtility.CopySerialized(clip, copy);
            copy.name = name;
            var settings = AnimationUtility.GetAnimationClipSettings(copy);
            settings.loopTime = name.StartsWith("Idle") || name.StartsWith("Run") ||
                name.StartsWith("JumpAir") || name.StartsWith("VictoryRepeat") || name.StartsWith("ShootAuto") || name.StartsWith("ShootSustain");
            // CharacterController owns world displacement. Root-motion variants are retained in the library.
            settings.keepOriginalOrientation = true;
            settings.keepOriginalPositionXZ = true;
            settings.keepOriginalPositionY = true;
            settings.loopBlendOrientation = true;
            settings.loopBlendPositionXZ = true;
            settings.loopBlendPositionY = true;
            AnimationUtility.SetAnimationClipSettings(copy, settings);
            // Imported pack events can call demo scripts that are absent on our gameplay root.
            AnimationUtility.SetAnimationEvents(copy, Array.Empty<AnimationEvent>());
            clips.Add(name, copy);
            EditorUtility.SetDirty(copy);
        }
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null) { controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath); controller.layers = Array.Empty<AnimatorControllerLayer>(); }
        else
        {
            controller.layers = Array.Empty<AnimatorControllerLayer>();
            controller.parameters = Array.Empty<AnimatorControllerParameter>();
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(ControllerPath))
                if (asset != controller) Object.DestroyImmediate(asset, true);
        }
        foreach (string name in new[]{"MoveX","MoveY","Speed","AimPitch","MoveRate"})
            controller.AddParameter(name, AnimatorControllerParameterType.Float);
        foreach (string name in new[]{"IsMoving","IsGrounded","IsCrouching","IsSprinting","IsAiming","IsFiring","IsRifle","IsDead","IsMeleeWeapon"})
            controller.AddParameter(name, AnimatorControllerParameterType.Bool);
        foreach (string name in new[]{"Jump","Dash","DashBack","DashLeft","DashRight","Roll","RollBack","RollLeft","RollRight","Hit","Hit2","Dizzy","Knockdown","GetUp","PowerUp","Victory","Reload","Shoot","Melee","Die"})
            controller.AddParameter(name, AnimatorControllerParameterType.Trigger);
        var root = new AnimatorStateMachine { name = "Base Layer" };
        AssetDatabase.AddObjectToAsset(root, controller);
        controller.AddLayer(new AnimatorControllerLayer { name = "Base Layer", stateMachine = root, defaultWeight = 1 });
        foreach (bool rifle in new[]{false,true}) BuildStance(controller, root, clips, rifle);
        BuildUpper(controller, clips);
        // Every source clip, including RM variants and aim offsets, stays available for preview.
        var library = root.AddStateMachine("Clip Library - Preview Only", new Vector3(1400, 800));
        int index = 0;
        foreach (var pair in clips)
        {
            var state = library.AddState(pair.Key, new Vector3((index % 8) * 230, (index / 8) * 70));
            state.motion = pair.Value; index++;
        }
        AssetDatabase.SaveAssets();
        Debug.Log("Female controller built: " + clips.Count + " Humanoid clips; HG + AR stances. " + ControllerPath);
    }

    static AnimationClip Clip(Dictionary<string, AnimationClip> clips, string action, bool rifle)
    {
        string suffix = rifle ? "_AR" : "_HG01";
        return clips.First(pair => pair.Key.Equals(action + suffix, StringComparison.OrdinalIgnoreCase)).Value;
    }
    static AnimatorState State(AnimatorStateMachine root, string name, Motion motion, float x, float y, bool fullBody = false)
    {
        var state = root.AddState(name, new Vector3(x,y)); state.motion = motion;
        if (fullBody) state.tag = "FullBody";
        return state;
    }
    static AnimatorStateTransition Link(AnimatorState from, AnimatorState to, bool exit = false, float duration = 0.1f)
    {
        var t = from.AddTransition(to); t.hasExitTime = exit; t.exitTime = 0.9f;
        t.duration = duration; return t;
    }
    static void Bool(AnimatorStateTransition t, string name, bool value) =>
        t.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, name);
    static void Action(AnimatorStateMachine root, AnimatorState target, string trigger, bool rifle)
    {
        var t = root.AddAnyStateTransition(target); t.hasExitTime = false; t.duration = 0.08f; t.canTransitionToSelf = false;
        t.AddCondition(AnimatorConditionMode.If,0,trigger);
        Bool(t,"IsRifle",rifle);
        if (trigger != "Die") Bool(t,"IsDead",false);
    }
    static void Return(AnimatorState from, AnimatorState locomotion) => Link(from, locomotion, true);

    static void BuildStance(AnimatorController c, AnimatorStateMachine root, Dictionary<string,AnimationClip> clips, bool rifle)
    {
        string prefix = rifle ? "AR " : "HG ";
        float y = rifle ? 1000 : 0;
        var tree = new BlendTree { name = prefix + "Directional Movement", blendType = BlendTreeType.FreeformDirectional2D,
            blendParameter = "MoveX", blendParameterY = "MoveY", useAutomaticThresholds = false };
        AssetDatabase.AddObjectToAsset(tree,c);
        tree.AddChild(Clip(clips,"IdleBattle01",rifle),Vector2.zero);
        tree.AddChild(Clip(clips,"RunFWD",rifle),Vector2.up);
        tree.AddChild(Clip(clips,"RunBWD",rifle),Vector2.down);
        tree.AddChild(Clip(clips,"RunLeft",rifle),Vector2.left);
        tree.AddChild(Clip(clips,"RunRight",rifle),Vector2.right);
        var locomotion = State(root,prefix+"Locomotion",tree,300,y);
        locomotion.speedParameter = "MoveRate"; locomotion.speedParameterActive = true; if (!rifle) root.defaultState = locomotion;
        // Stance changes also work after actions finish.


        var start = State(root,prefix+"Jump Start",Clip(clips,"JumpStart",rifle),550,y,true);
        var air = State(root,prefix+"Jump Air",Clip(clips,"JumpAir",rifle),780,y,true);
        var end = State(root,prefix+"Jump End",Clip(clips,"JumpEnd",rifle),1000,y,true);
        Action(root,start,"Jump",rifle); Link(start,air,true,0.05f);
        var land = Link(air,end,false,0.05f); Bool(land,"IsGrounded",true);
        Return(end,locomotion);
        string[] actions = {"DashFWD","DashBWD","DashLeft","DashRight","RollFWD","RollBWD","RollLeft","RollRight",
            "GetHit01","GetHit02","Dizzy","Knockdown","GetUp","PowerUP","VictoryNormal"};
        string[] triggers = {"Dash","DashBack","DashLeft","DashRight","Roll","RollBack","RollLeft","RollRight",
            "Hit","Hit2","Dizzy","Knockdown","GetUp","PowerUp","Victory"};
        for(int i=0;i<actions.Length;i++)
        {
            string action = !rifle && actions[i]=="VictoryNormal" ? "Victory" : actions[i];
            var s = State(root,prefix+action,Clip(clips,action,rifle),550+(i%4)*240,y+150+(i/4)*120,true);
            Action(root,s,triggers[i],rifle);
            if(actions[i]=="Knockdown")
            {
                var up = State(root,prefix+"Recover From Knockdown",Clip(clips,"GetUp",rifle),1500,y+450,true);
                Link(s,up,true); Return(up,locomotion);
            }
            else Return(s,locomotion);
        }
        var die = State(root,prefix+"Die",Clip(clips,"Die",rifle),1500,y,true);
        var death = root.AddAnyStateTransition(die); death.hasExitTime=false;death.duration=0.08f;death.canTransitionToSelf=false;
        death.AddCondition(AnimatorConditionMode.If,0,"Die"); Bool(death,"IsRifle",rifle);
        // Locomotion cross-links created after both stances exist.
        if(rifle)
        {
            var hg=root.states.First(s=>s.state.name=="HG Locomotion").state;
            Bool(Link(hg,locomotion),"IsRifle",true);
            Bool(Link(locomotion,hg),"IsRifle",false);
        }
    }

    static void BuildUpper(AnimatorController c, Dictionary<string,AnimationClip> clips)
    {
        var mask = new AvatarMask {name="Female Upper Body"};
        for(int i=0;i<(int)AvatarMaskBodyPart.LastBodyPart;i++)
            mask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i, i==(int)AvatarMaskBodyPart.Body ||
                i==(int)AvatarMaskBodyPart.Head || i==(int)AvatarMaskBodyPart.LeftArm ||
                i==(int)AvatarMaskBodyPart.RightArm || i==(int)AvatarMaskBodyPart.LeftFingers || i==(int)AvatarMaskBodyPart.RightFingers);
        AssetDatabase.AddObjectToAsset(mask,c);
        var root = new AnimatorStateMachine {name="Upper Body"};
        AssetDatabase.AddObjectToAsset(root,c);
        c.AddLayer(new AnimatorControllerLayer {name="Upper Body",stateMachine=root,avatarMask=mask,defaultWeight=1});
        var empty=State(root,"Ready",null,220,0);root.defaultState=empty;
        foreach(bool rifle in new[]{false,true})
        {
            string prefix=rifle?"AR ":"HG ";float y=rifle?300:100;
            var shot=State(root,prefix+"Shoot",Clip(clips,"ShootSingleshot",rifle),500,y);
            var reload=State(root,prefix+"Reload",Clip(clips,"Reloading",rifle),800,y);
            Action(root,shot,"Shoot",rifle);Return(shot,empty);
            reload.speed = Clip(clips,"Reloading",rifle).length / (rifle ? 2f : 1.4f); Action(root,reload,"Reload",rifle);Return(reload,empty);
        }
    }

    static Quaternion GripRotation(Animator animator, AnimatorController controller, bool rifle)
    {
        var clip = controller.animationClips.First(c => c.name == (rifle ? "IdleBattle01_AR" : "IdleBattle01_HG01"));
        Quaternion grip;
        AnimationMode.StartAnimationMode();
        try
        {
            AnimationMode.BeginSampling();
            AnimationMode.SampleAnimationClip(animator.gameObject, clip, 0);
            AnimationMode.EndSampling();
            grip = Quaternion.Inverse(animator.GetBoneTransform(HumanBodyBones.RightHand).rotation) * animator.transform.rotation;
        }
        finally { AnimationMode.StopAnimationMode(); }
        return grip;
    }
    [MenuItem("GunQuest/Female/2 - Replace Scene Player")]
    public static void ReplaceScenePlayer()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode first.");
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if(controller==null){Build();controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);}
        var player=Object.FindFirstObjectByType<PlayerMotor>();
        if(player==null)throw new InvalidOperationException("No PlayerMotor in the open scene.");
        if(player.GetComponent<FemaleAnimationDriver>()!=null)throw new InvalidOperationException("Female already installed.");
        var scene=player.gameObject.scene;
        EditorSceneManager.SaveScene(scene);
        string backup=System.IO.Path.ChangeExtension(scene.path,null)+"_BeforeFemale_20261017.unity";
        if(!File.Exists(backup)&&!AssetDatabase.CopyAsset(scene.path,backup))throw new IOException("Backup failed.");
        // Preserve the existing gameplay root so mission, health and camera references remain valid.
        if(PrefabUtility.IsPartOfPrefabInstance(player.gameObject))
            PrefabUtility.UnpackPrefabInstance(PrefabUtility.GetOutermostPrefabInstanceRoot(player.gameObject),PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
        var female=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(FemalePrefab),player.transform);
        Undo.RegisterCreatedObjectUndo(female,"Install Female player");
        female.name="FemaleVisual";
        female.transform.localPosition=Vector3.zero;female.transform.localRotation=Quaternion.identity;female.transform.localScale=Vector3.one;
        foreach (var demo in female.GetComponentsInChildren<MeshRenderer>(true)) if (demo.name == "PistolPBR" || demo.name == "AssaultRiflePBR") { demo.gameObject.SetActive(false); PrefabUtility.RecordPrefabInstancePropertyModifications(demo.gameObject); }
        var animator=female.GetComponent<Animator>();
        animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
        // Keep the chosen Body02/Head02 variant; both already selected by the source prefab.
        var weapons=new SerializedObject(player.GetComponent<PlayerWeapons>());
        var slots=weapons.FindProperty("slots");
        var hand=animator.GetBoneTransform(HumanBodyBones.RightHand);
        var socket=new GameObject("FemaleWeaponSocket").transform;socket.SetParent(hand,false);
        socket.localPosition=Vector3.zero;socket.localRotation=Quaternion.identity;
        for(int i=0;i<slots.arraySize;i++)
        {
            var model=slots.GetArrayElementAtIndex(i).FindPropertyRelative("model").objectReferenceValue as GameObject;
            if(model==null)continue;
            // Preserve each weapon's art/model offsets; the new socket is the grip origin.
            model.transform.SetParent(socket,false);
            model.transform.localPosition=Vector3.zero; model.transform.localRotation=GripRotation(animator, controller, i == 1); model.transform.localScale=Vector3.one;
        }
        var legacy=player.GetComponent<Animator>();if(legacy!=null)legacy.enabled=false;
        foreach(Transform child in player.transform)if(child!=female.transform)child.gameObject.SetActive(false);
        var motor=new SerializedObject(player);motor.FindProperty("characterAnimator").objectReferenceValue=animator;motor.ApplyModifiedProperties();
        var driver=player.gameObject.AddComponent<FemaleAnimationDriver>();
        var so=new SerializedObject(driver);so.FindProperty("characterAnimator").objectReferenceValue=animator;so.ApplyModifiedProperties();
        PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
        PrefabUtility.SaveAsPrefabAssetAndConnect(player.gameObject,OutputPrefab,InteractionMode.AutomatedAction);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        Selection.activeGameObject=female;
        Debug.Log("Female installed with retained gameplay root. Backup: "+backup+"; prefab: "+OutputPrefab);
    }
}
#endif
