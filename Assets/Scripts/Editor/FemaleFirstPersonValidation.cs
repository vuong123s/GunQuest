#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

// Editor-only, disposable Play Mode verification. No rewards or save writes.
[InitializeOnLoad]
public static class FemaleFirstPersonValidation
{
    const string PendingKey = "GunQuest.FemaleFPSValidation.Pending";
    const string ProgressKey = "GunQuest.ContractProgress.v1";
    const double Timeout = 45;
    static readonly HashSet<string> DisabledTypes = new HashSet<string>
    { "EnemyAI", "Enemy", "StateMachine", "MonsterSpawner", "InputManager", "NavMeshAgent" };
    static readonly List<KeyValuePair<Behaviour, bool>> behaviours = new List<KeyValuePair<Behaviour, bool>>();
    static readonly List<KeyValuePair<Collider, bool>> colliders = new List<KeyValuePair<Collider, bool>>();
    static StringBuilder report;
    static ContractGame game;
    static PlayerWeapons weapons;
    static InputManager input;
    static PlayerLook look;
    static Camera camera;
    static Animator fps;
    static Mouse mouse;
    static bool mouseEnabled, active;
    static double started, due;
    static int step, failures, credits, contracts, capacity, reserve, cancelledAmmo, cancelledReserve;
    static float reloadStarted, pausedGameTime, pausedNormalized;
    static string savedProgress;

    static FemaleFirstPersonValidation()
    {
        EditorApplication.playModeStateChanged += OnPlayMode;
        AssemblyReloadEvents.beforeAssemblyReload += () =>
        {
            if (!active) return;
            Check(false, "Validation interrupted by script reload.");
            Finish(false);
        };
        if (SessionState.GetBool(PendingKey, false) && EditorApplication.isPlaying)
            EditorApplication.delayCall += Begin;
    }

    [MenuItem("GunQuest/Female/4 - Validate First Person in Play Mode")]
    public static void Run()
    {
        if (active) throw new InvalidOperationException("Female FPS validation is already running.");
        SessionState.SetBool(PendingKey, true);
        if (EditorApplication.isPlaying) EditorApplication.delayCall += Begin;
        else EditorApplication.isPlaying = true;
    }

    static void OnPlayMode(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(PendingKey, false))
            EditorApplication.delayCall += Begin;
        if (state == PlayModeStateChange.ExitingPlayMode && active)
        {
            Check(false, "Validation interrupted before completion.");
            Finish(false);
        }
    }

    static void Begin()
    {
        if (active || !EditorApplication.isPlaying || !SessionState.GetBool(PendingKey, false)) return;
        SessionState.SetBool(PendingKey, false);
        active = true; step = failures = 0; behaviours.Clear(); colliders.Clear();
        report = new StringBuilder("Female FPS deterministic Play Mode validation\n");
        report.AppendLine(DateTime.UtcNow.ToString("O") + " | Unity " + Application.unityVersion);
        savedProgress = PlayerPrefs.GetString(ProgressKey, "");
        mouse = Mouse.current; mouseEnabled = mouse != null && mouse.enabled;
        if (mouseEnabled) InputSystem.DisableDevice(mouse);
        Application.runInBackground = true;
        started = EditorApplication.timeSinceStartup; due = started + .5;
        EditorApplication.update += Tick;
    }

    static void Tick()
    {
        if (!active) return;
        if (!EditorApplication.isPlaying) { Finish(false); return; }
        if (EditorApplication.timeSinceStartup - started > Timeout)
        { Check(false, "Exceeded 45-second wall-clock deadline at step " + step); Finish(true); return; }
        if (EditorApplication.timeSinceStartup < due) return;
        try
        {
            switch (step)
            {
                case 0:
                    Setup(); Next(1); break;
                case 1:
                    Check(weapons.SelectedIndex == 0, "Pistol selected for initial view.");
                    Check(!weapons.Reload(), "Full pistol magazine rejects reload.");
                    Capture("FemaleFPS_Pistol.png");
                    capacity = weapons.Capacity(0); reserve = weapons.Reserve;
                    Pitch(-75); Next(.1); break;
                case 2:
                    Check(weapons.Attack(), "Pistol fires once.");
                    Check(weapons.Ammo == capacity - 1, "Pistol shot consumes exactly one round.");
                    Pitch(0); StartReload(); Next(.55); break;
                case 3:
                    Check(weapons.IsReloading, "Pistol remains reloading before 1.4 seconds.");
                    Check(State("ReloadHG"), "Independent FPS Animator enters ReloadHG.");
                    pausedGameTime = Time.time; pausedNormalized = fps.GetCurrentAnimatorStateInfo(0).normalizedTime;
                    game.Pause(); Next(.35); break;
                case 4:
                    Check(game.State == ContractGame.Phase.Paused && Time.timeScale == 0, "Pause freezes gameplay.");
                    Check(Mathf.Abs(Time.time - pausedGameTime) < .02f, "Game clock does not advance while paused.");
                    Check(Mathf.Abs(fps.GetCurrentAnimatorStateInfo(0).normalizedTime - pausedNormalized) < .02f,
                        "FPS reload normalized time does not advance while paused.");
                    Check(weapons.IsReloading && weapons.Ammo == capacity - 1, "Pause preserves pending reload and ammo.");
                    game.Resume(); input.enabled = false; Next(0); break;
                case 5:
                    if (weapons.IsReloading) break;
                    VerifyReload(1.4f, "Pistol");
                    Equip(1); Next(.7); break;
                case 6:
                    Check(State("ReadyAR"), "Rifle idle uses ReadyAR.");
                    Capture("FemaleFPS_Rifle.png");
                    capacity = weapons.Capacity(1); reserve = weapons.Reserve;
                    Pitch(-75); Next(.1); break;
                case 7:
                    Check(weapons.Attack(), "Rifle fires once.");
                    Pitch(0); StartReload(); Next(.85); break;
                case 8:
                    Check(weapons.IsReloading && State("ReloadAR"), "Rifle uses ReloadAR during its 2-second reload.");
                    Capture("FemaleFPS_Reload.png"); Next(0); break;
                case 9:
                    if (weapons.IsReloading) break;
                    VerifyReload(2f, "Rifle"); Pitch(-75); Next(.2); break;
                case 10:
                    Check(weapons.Attack(), "Rifle fires before cancellation test.");
                    Pitch(0); cancelledAmmo = weapons.Ammo; cancelledReserve = weapons.Reserve;
                    StartReload(); Next(.35); break;
                case 11:
                    Equip(0);
                    Check(!weapons.IsReloading && State("ReadyHG"), "Switching to pistol cancels reload and restores ReadyHG.");
                    Next(2.1); break;
                case 12:
                    Equip(1);
                    Check(weapons.Ammo == cancelledAmmo && weapons.Reserve == cancelledReserve,
                        "Cancelled rifle reload does not transfer ammo after its old deadline.");
                    Equip(2); Next(.4); break;
                case 13:
                    Capture("FemaleFPS_Bat.png");
                    Check(weapons.Current.melee && weapons.Attack(), "Bat plays melee attack without a magazine.");
                    Next(.65); break;
                case 14:
                    Equip(3); Next(.4); break;
                case 15:
                    Capture("FemaleFPS_Shovel.png");
                    Check(weapons.Current.melee && weapons.Attack(), "Shovel plays melee attack without a magazine.");
                    Next(.4); break;
                case 16:
                    Equip(0); Pitch(75); Next(.35); break;
                case 17:
                    Capture("FemaleFPS_PitchDown.png"); Pitch(-75); Next(.35); break;
                case 18:
                    Capture("FemaleFPS_PitchUp.png"); Pitch(0); Next(.1); break;
                case 19:
                    Check(game.Progress.credits == credits && game.Progress.completedContracts == contracts,
                        "No rewards or contract completion were granted.");
                    Check(PlayerPrefs.GetString(ProgressKey, "") == savedProgress, "Saved progress was not modified.");
                    Finish(true); break;
            }
        }
        catch (Exception exception)
        {
            Check(false, "Exception at step " + step + ": " + exception);
            Finish(true);
        }
    }

    static void Setup()
    {
        game = ContractGame.Instance;
        weapons = Object.FindFirstObjectByType<PlayerWeapons>();
        Require(game != null && weapons != null, "Active scene requires ContractGame and PlayerWeapons.");
        input = weapons.GetComponent<InputManager>(); look = weapons.GetComponent<PlayerLook>(); camera = Camera.main;
        Require(input != null && look != null && camera != null, "Player input/look and MainCamera are required.");
        foreach (var behaviour in Object.FindObjectsByType<Behaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!DisabledTypes.Contains(behaviour.GetType().Name)) continue;
            behaviours.Add(new KeyValuePair<Behaviour, bool>(behaviour, behaviour.enabled)); behaviour.enabled = false;
        }
        // Prevent even test shots or swings from damaging targets or changing mission phase.
        foreach (var enemy in Object.FindObjectsByType<EnemyHealth>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            foreach (var collider in enemy.GetComponentsInChildren<Collider>(true))
            {
                colliders.Add(new KeyValuePair<Collider, bool>(collider, collider.enabled)); collider.enabled = false;
            }
        credits = game.Progress.credits; contracts = game.Progress.completedContracts;
        if (game.State == ContractGame.Phase.Board) game.AcceptContract();
        else if (game.State == ContractGame.Phase.Paused) game.Resume();
        Require(game.IsPlaying, "Validation starts from a configured board or active contract.");
        input.enabled = false; weapons.ApplyProgress(game.Progress);
        if (weapons.SelectedIndex != 0) Require(weapons.Equip(0), "Pistol slot cannot be selected.");
        var rig = camera.transform.Find("FirstPersonAnimatedArms");
        Require(rig != null, "Independent FPS render rig is missing."); fps = rig.GetComponent<Animator>();
        Require(fps != null && fps.runtimeAnimatorController != null, "Independent FPS Animator is missing.");
        Check(fps.avatar != null && fps.avatar.isValid && fps.avatar.isHuman, "FPS avatar is a valid Humanoid.");
        Check(!fps.enabled, "FPS Animator uses manual evaluation rather than automatic updates.");
        Check(fps.runtimeAnimatorController.name == "FemaleFirstPerson", "FPS Animator has an independent controller.");
        bool eventFree = true;
        foreach (var clip in fps.runtimeAnimatorController.animationClips)
            eventFree &= AnimationUtility.GetAnimationEvents(clip).Length == 0;
        Check(eventFree, "FPS clips contain no gameplay animation events.");
        Check(rig.GetComponentsInChildren<Collider>(true).Length == 0, "FPS rig contains no colliders.");
        bool noTubes = true;
        foreach (var child in rig.GetComponentsInChildren<Transform>(true))
            noTubes &= child.name != "LeftViewSleeve" && child.name != "RightViewSleeve";
        Check(noTubes, "FPS rig contains no procedural sleeve tubes.");
        int visibleArms = 0;
        foreach (var skin in rig.GetComponentsInChildren<SkinnedMeshRenderer>())
            if (skin.enabled && skin.sharedMesh != null && skin.sharedMesh.name.StartsWith("FemaleFirstPersonBody") &&
                skin.sharedMesh.triangles.Length > 0) visibleArms++;
        Require(visibleArms == 1, "Exactly one selected Female arm variant has visible generated geometry.");
        Pitch(0);
    }

    static void StartReload()
    {
        reloadStarted = Time.time;
        Check(weapons.Reload(), "Gameplay accepts reload for the partially empty magazine.");
    }

    static void VerifyReload(float expected, string label)
    {
        float elapsed = Time.time - reloadStarted;
        Check(Mathf.Abs(elapsed - expected) <= .2f,
            label + " reload completes in " + elapsed.ToString("F3") + " game seconds (expected " + expected + ").");
        Check(weapons.Ammo == capacity && weapons.Reserve == reserve - 1,
            label + " reload fills magazine and consumes one reserve round.");
    }

    static void Equip(int slot)
    {
        if (weapons.SelectedIndex != slot) Require(weapons.Equip(slot), "Cannot equip slot " + slot);
        input.enabled = false;
    }

    static void Pitch(float value)
    {
        float sensitivity = look.ySensitivity > 5f ? .12f : Mathf.Max(.01f, look.ySensitivity);
        look.ProcessLook(new Vector2(0, -(value - look.Pitch) / sensitivity));
    }

    static bool State(string name)
    {
        return fps.GetCurrentAnimatorStateInfo(0).IsName(name) ||
            (fps.IsInTransition(0) && fps.GetNextAnimatorStateInfo(0).IsName(name));
    }

    static void Next(double delay)
    {
        step++; due = EditorApplication.timeSinceStartup + delay;
    }

    static void Require(bool condition, string message)
    {
        Check(condition, message);
        if (!condition) throw new InvalidOperationException(message);
    }

    static void Check(bool condition, string message)
    {
        if (!condition) failures++;
        report?.AppendLine((condition ? "PASS " : "FAIL ") + message);
    }

    sealed class CanvasState
    {
        public Canvas canvas;
        public RenderMode mode;
        public Camera camera;
        public float distance;
    }

    static void Capture(string file)
    {
        var states = new List<CanvasState>();
        RenderTexture previousTarget = camera.targetTexture, previousActive = RenderTexture.active;
        float previousAspect = camera.aspect;
        var target = RenderTexture.GetTemporary(1280, 720, 24, RenderTextureFormat.ARGB32);
        Texture2D texture = null;
        try
        {
            camera.targetTexture = target; camera.aspect = 1280f / 720f;
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (!canvas.enabled || !canvas.gameObject.activeInHierarchy || canvas.renderMode != RenderMode.ScreenSpaceOverlay) continue;
                states.Add(new CanvasState { canvas = canvas, mode = canvas.renderMode, camera = canvas.worldCamera, distance = canvas.planeDistance });
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = .051f;
            }
            Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = target;
            texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); texture.Apply();
            File.WriteAllBytes(System.IO.Path.Combine(OutputFolder(), file), texture.EncodeToPNG());
            report.AppendLine("CAPTURE " + file + " | " + weapons.Current.label + " | " + fps.GetCurrentAnimatorStateInfo(0).normalizedTime.ToString("F3"));
        }
        finally
        {
            foreach (var state in states)
                if (state.canvas != null)
                { state.canvas.renderMode = state.mode; state.canvas.worldCamera = state.camera; state.canvas.planeDistance = state.distance; }
            camera.targetTexture = previousTarget; camera.aspect = previousAspect;
            RenderTexture.active = previousActive;
            if (texture != null) Object.DestroyImmediate(texture);
            RenderTexture.ReleaseTemporary(target); Canvas.ForceUpdateCanvases();
        }
    }

    static string OutputFolder()
    {
        string folder = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..", "Documentation"));
        Directory.CreateDirectory(folder); return folder;
    }

    static void Finish(bool stopPlay)
    {
        if (!active) return;
        active = false; EditorApplication.update -= Tick; SessionState.SetBool(PendingKey, false);
        try
        {
            Check(PlayerPrefs.GetString(ProgressKey, "") == savedProgress, "Final cleanup confirms no saved progress changes.");
            report.AppendLine("RESULT " + (failures == 0 ? "PASS" : "FAIL") + " | " + failures + " failed checks | " +
                (EditorApplication.timeSinceStartup - started).ToString("F2") + " wall seconds");
            File.WriteAllText(System.IO.Path.Combine(OutputFolder(), "FemaleFPSValidation.txt"), report.ToString());
        }
        catch (Exception exception) { Debug.LogException(exception); }
        finally
        {
            foreach (var item in behaviours) if (item.Key != null) item.Key.enabled = item.Value;
            foreach (var item in colliders) if (item.Key != null) item.Key.enabled = item.Value;
            if (mouse != null && mouse.added && mouseEnabled) InputSystem.EnableDevice(mouse);
            Application.runInBackground = false; Time.timeScale = 1;
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            if (stopPlay && EditorApplication.isPlaying) EditorApplication.isPlaying = false;
        }
        Debug.Log("Female FPS validation finished with " + failures + " failed checks. See Documentation/FemaleFPSValidation.txt.");
    }
}
#endif
