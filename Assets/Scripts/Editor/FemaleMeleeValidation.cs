#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;
using Path = System.IO.Path;

// Disposable Play Mode checks: no scene saves, contract rewards or progress writes.
[InitializeOnLoad]
public static class FemaleMeleeValidation
{
    const string PendingKey = "GunQuest.FemaleMeleeValidation.Pending";
    const string ProgressKey = "GunQuest.ContractProgress.v1";
    const float DummyHealth = 1000f;
    const double Timeout = 45;
    static readonly HashSet<string> DisabledTypes = new HashSet<string>
    { "EnemyAI", "Enemy", "StateMachine", "MonsterSpawner", "InputManager", "NavMeshAgent" };
    static readonly List<KeyValuePair<Behaviour, bool>> behaviours = new List<KeyValuePair<Behaviour, bool>>();
    static readonly List<KeyValuePair<Collider, bool>> colliders = new List<KeyValuePair<Collider, bool>>();
    static StringBuilder report;
    static ContractGame game;
    static PlayerWeapons weapons;
    static PlayerMelee melee;
    static InputManager input;
    static PlayerLook look;
    static Camera camera;
    static Animator motion;
    static Transform pose;
    static EnemyHealth dummy;
    static GameObject blocker;
    static Mouse mouse;
    static bool mouseEnabled, active, background, progressExisted, pauseSettled;
    static string savedProgress;
    static int step, failures, credits, contracts;
    static double started, due;
    static float attackStarted, expectedHealth, pausedClock, waitUntil;
    static Vector3 pausedPosition;
    static Quaternion pausedRotation;

    static FemaleMeleeValidation()
    {
        EditorApplication.playModeStateChanged += OnPlayMode;
        AssemblyReloadEvents.beforeAssemblyReload += () =>
        {
            if (!active) return;
            Check(false, "Validation interrupted by script reload."); Finish(false);
        };
        if (SessionState.GetBool(PendingKey, false) && EditorApplication.isPlaying)
            EditorApplication.delayCall += Begin;
    }

    [MenuItem("GunQuest/Female/6 - Validate Bat and Shovel in Play Mode")]
    public static void Run()
    {
        if (active) throw new InvalidOperationException("Melee validation is already running.");
        if (EditorApplication.isPlaying)
            throw new InvalidOperationException("Start melee validation from Edit Mode so the test is disposable.");
        SessionState.SetBool(PendingKey, true);
        EditorApplication.isPlaying = true;
    }

    static void OnPlayMode(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(PendingKey, false))
            EditorApplication.delayCall += Begin;
        if (state == PlayModeStateChange.ExitingPlayMode && active)
        { Check(false, "Validation interrupted before completion."); Finish(false); }
    }

    static void Begin()
    {
        if (active || !EditorApplication.isPlaying || !SessionState.GetBool(PendingKey, false)) return;
        SessionState.SetBool(PendingKey, false);
        active = true; step = failures = 0; waitUntil = 0; pauseSettled = false; behaviours.Clear(); colliders.Clear();
        report = new StringBuilder("Female Bat / Shovel deterministic Play Mode validation\n");
        report.AppendLine(DateTime.UtcNow.ToString("O") + " | Unity " + Application.unityVersion);
        progressExisted = PlayerPrefs.HasKey(ProgressKey); savedProgress = PlayerPrefs.GetString(ProgressKey, "");
        mouse = Mouse.current; mouseEnabled = mouse != null && mouse.enabled;
        if (mouseEnabled) InputSystem.DisableDevice(mouse);
        background = Application.runInBackground; Application.runInBackground = true;
        started = EditorApplication.timeSinceStartup; due = started + .5;
        EditorApplication.update += Tick;
    }

    static void Tick()
    {
        if (!active) return;
        if (!EditorApplication.isPlaying) { Finish(false); return; }
        if (EditorApplication.timeSinceStartup - started > Timeout)
        { Check(false, "Exceeded 45-second deadline at step " + step); Finish(true); return; }
        if (EditorApplication.timeSinceStartup < due || Time.time < waitUntil) return;
        try
        {
            switch (step)
            {
                case 0:
                    Setup(); Equip(2); Capture("FemaleMelee_BatIdle.png"); NewDummy(1.2f); Next(.5); break;
                case 1:
                    StartAttack("Bat");
                    Check(!weapons.Attack(), "Bat rejects another attack during wind-up.");
                    waitUntil = attackStarted + .08f; Next(0); break;
                case 2:
                    Check(dummy.NormalizedHealth == 1f, "Bat deals no damage during early wind-up.");
                    Check(MotionState("BatSwing"), "Bat plays its own BatSwing animation.");
                    Capture("FemaleMelee_BatWindup.png");
                    WaitForAttack(MeleeAttackTiming.Bat.ImpactTime); break;
                case 3:
                    VerifyDamage("Bat impact"); Capture("FemaleMelee_BatImpact.png");
                    WaitForAttack(MeleeAttackTiming.Bat.ImpactTime + .12f); break;
                case 4:
                    VerifyDamage("Bat follow-through remains a single hit");
                    Capture("FemaleMelee_BatFollowThrough.png");
                    WaitForAttack(MeleeAttackTiming.Bat.Duration + .1f); break;
                case 5:
                    Check(!weapons.IsMeleeAttacking, "Bat releases attack lock after recovery.");
                    StartAttack("Bat second swing");
                    WaitForAttack(MeleeAttackTiming.Bat.Duration + .1f); break;
                case 6:
                    VerifyDamage("Bat can hit again after recovery");
                    NewDummy(1.2f); MakeWall(); StartAttack("Bat blocked swing");
                    WaitForAttack(MeleeAttackTiming.Bat.Duration + .1f); break;
                case 7:
                    Check(dummy.NormalizedHealth == 1f, "A wall blocks Bat damage.");
                    Object.DestroyImmediate(blocker); NewDummy(weapons.Current.range + 2f);
                    StartAttack("Bat out-of-range swing");
                    WaitForAttack(MeleeAttackTiming.Bat.Duration + .1f); break;
                case 8:
                    Check(dummy.NormalizedHealth == 1f, "Bat cannot hit an out-of-range target.");
                    NewDummy(1.2f); StartAttack("Bat switch cancellation");
                    Equip(0);
                    Check(!weapons.IsMeleeAttacking, "Switching to Pistol cancels the pending Bat attack.");
                    WaitForAttack(MeleeAttackTiming.Bat.Duration + .1f); break;
                case 9:
                    Check(dummy.NormalizedHealth == 1f, "Cancelled Bat attack cannot deal delayed damage.");
                    Equip(3); Capture("FemaleMelee_ShovelIdle.png"); NewDummy(1.2f); Next(.5); break;
                case 10:
                    StartAttack("Shovel"); Check(!weapons.Attack(), "Shovel rejects overlapping attacks.");
                    waitUntil = attackStarted + .12f; Next(0); break;
                case 11:
                    Check(dummy.NormalizedHealth == 1f, "Shovel deals no damage during early wind-up.");
                    Check(MotionState("ShovelSwing"), "Shovel plays its own ShovelSwing animation.");
                    Capture("FemaleMelee_ShovelWindup.png");
                    WaitForAttack(MeleeAttackTiming.Shovel.ImpactTime); break;
                case 12:
                    VerifyDamage("Shovel impact"); Capture("FemaleMelee_ShovelImpact.png");
                    WaitForAttack(MeleeAttackTiming.Shovel.ImpactTime + .15f); break;
                case 13:
                    VerifyDamage("Shovel follow-through remains a single hit");
                    Capture("FemaleMelee_ShovelFollowThrough.png");
                    WaitForAttack(MeleeAttackTiming.Shovel.Duration + .1f); break;
                case 14:
                    Check(!weapons.IsMeleeAttacking, "Shovel releases attack lock after recovery.");
                    NewDummy(1.2f); StartAttack("Shovel paused swing"); game.Pause();
                    pausedClock = Time.time; pausedPosition = pose.localPosition;
                    pausedRotation = pose.localRotation; Next(.3); break;
                case 15:
                    if (!pauseSettled)
                    {
                        pauseSettled = true; pausedPosition = pose.localPosition; pausedRotation = pose.localRotation;
                        due = EditorApplication.timeSinceStartup + .2; break;
                    }
                    Check(game.State == ContractGame.Phase.Paused && Time.timeScale == 0f, "Pause freezes the swing.");
                    Check(Mathf.Abs(Time.time - pausedClock) < .001f, "Paused melee does not advance game time.");
                    Check(dummy.NormalizedHealth == 1f, "Paused wind-up cannot deliver damage.");
                    Check(Vector3.Distance(pose.localPosition, pausedPosition) < .001f &&
                        Quaternion.Angle(pose.localRotation, pausedRotation) < .1f,
                        "Weapon motion does not advance while paused.");
                    game.Resume(); input.enabled = false;
                    WaitForAttack(MeleeAttackTiming.Shovel.Duration + .1f); break;
                case 16:
                    VerifyDamage("Resumed Shovel swing delivers exactly one hit");
                    NewDummy(1.2f); MakeWall(); StartAttack("Shovel blocked swing");
                    WaitForAttack(MeleeAttackTiming.Shovel.Duration + .1f); break;
                case 17:
                    Check(dummy.NormalizedHealth == 1f, "A wall blocks Shovel damage.");
                    Object.DestroyImmediate(blocker); NewDummy(weapons.Current.range + 2f);
                    StartAttack("Shovel out-of-range swing");
                    WaitForAttack(MeleeAttackTiming.Shovel.Duration + .1f); break;
                case 18:
                    Check(dummy.NormalizedHealth == 1f, "Shovel cannot hit an out-of-range target.");
                    NewDummy(1.2f); StartAttack("Shovel switch cancellation"); Equip(0);
                    WaitForAttack(MeleeAttackTiming.Shovel.Duration + .1f); break;
                case 19:
                    Check(dummy.NormalizedHealth == 1f && !weapons.IsMeleeAttacking,
                        "Switching to Pistol cancels pending Shovel damage.");
                    NewDummy(1.2f); attackStarted = Time.time;
                    Check(weapons.QuickMelee(), "Pistol accepts the dedicated quick melee action.");
                    Check(dummy.NormalizedHealth == 1f, "Quick melee also waits until contact.");
                    Check(!weapons.Attack(), "Firearm cannot fire during quick melee.");
                    WaitForAttack(MeleeAttackTiming.Quick.Duration + .1f); break;
                case 20:
                    Check(Mathf.Abs(dummy.NormalizedHealth - (1f - 35f * game.Progress.DamageMultiplier / DummyHealth)) < .0001f,
                        "Quick melee damages once using its own timing.");
                    Equip(3); Next(.5); break;
                case 21:
                    NewDummy(1.2f); StartAttack("Shovel death cancellation");
                    weapons.GetComponent<PlayerHealth>().TakeDamage(float.MaxValue); Next(.35); break;
                case 22:
                    Check(dummy.NormalizedHealth == 1f && !weapons.IsMeleeAttacking,
                        "Player death cancels a pending swing without damage.");
                    Check(game.Progress.credits == credits && game.Progress.completedContracts == contracts,
                        "No rewards or contract completion were granted.");
                    Finish(true); break;
            }
        }
        catch (Exception exception)
        { Check(false, "Exception at step " + step + ": " + exception); Finish(true); }
    }

    static void Setup()
    {
        game = ContractGame.Instance; weapons = Object.FindFirstObjectByType<PlayerWeapons>();
        Require(game != null && weapons != null, "Active scene requires ContractGame and PlayerWeapons.");
        input = weapons.GetComponent<InputManager>(); look = weapons.GetComponent<PlayerLook>();
        melee = weapons.GetComponent<PlayerMelee>(); camera = Camera.main;
        Require(input != null && look != null && melee != null && camera != null, "Player input, look, melee and camera are present.");
        foreach (var behaviour in Object.FindObjectsByType<Behaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!DisabledTypes.Contains(behaviour.GetType().Name)) continue;
            behaviours.Add(new KeyValuePair<Behaviour, bool>(behaviour, behaviour.enabled)); behaviour.enabled = false;
        }
        // Input is disabled, so gravity ProcessMove never runs. Disable all scene geometry only
        // for these disposable physics probes; the isolated target and wall are added below.
        foreach (var collider in Object.FindObjectsByType<Collider>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (collider.transform == weapons.transform || collider.transform.IsChildOf(weapons.transform)) continue;
            colliders.Add(new KeyValuePair<Collider, bool>(collider, collider.enabled)); collider.enabled = false;
        }
        credits = game.Progress.credits; contracts = game.Progress.completedContracts;
        Require(game.State == ContractGame.Phase.Board, "Validation starts from a fresh contract board.");
        game.AcceptContract(); Require(game.IsPlaying, "Configured contract starts successfully.");
        input.enabled = false;
        float sensitivity = look.ySensitivity > 5f ? .12f : Mathf.Max(.01f, look.ySensitivity);
        look.ProcessLook(new Vector2(0, look.Pitch / sensitivity));
        foreach (var animator in camera.GetComponentsInChildren<Animator>(true))
            if (animator.name == "FirstPersonMeleeMotion") motion = animator;
        Require(motion != null && motion.runtimeAnimatorController != null, "Dedicated melee motion Animator exists.");
        pose = motion.transform.Find("Pose"); Require(pose != null, "Melee motion contains the animated Pose transform.");
        bool eventFree = true;
        foreach (var clip in motion.runtimeAnimatorController.animationClips)
            eventFree &= AnimationUtility.GetAnimationEvents(clip).Length == 0;
        Check(eventFree, "Melee animation clips contain no gameplay animation events.");
        Check(motion.GetComponentsInChildren<Collider>(true).Length == 0, "First-person melee models have no colliders.");
    }

    static void NewDummy(float distance)
    {
        if (dummy != null) Object.DestroyImmediate(dummy.gameObject);
        var target = new GameObject("MeleeValidationTarget"); target.SetActive(false);
        target.transform.position = camera.transform.position + camera.transform.forward * distance - Vector3.up * .5f;
        var capsule = target.AddComponent<CapsuleCollider>(); capsule.center = Vector3.up * .5f;
        capsule.height = 1f; capsule.radius = .28f;
        // A second collider on the same enemy must not cause duplicate damage.
        var sphere = target.AddComponent<SphereCollider>(); sphere.center = Vector3.up * .5f; sphere.radius = .26f;
        dummy = target.AddComponent<EnemyHealth>();
        var serialized = new SerializedObject(dummy); serialized.FindProperty("maxHealth").floatValue = DummyHealth;
        serialized.ApplyModifiedPropertiesWithoutUndo(); target.SetActive(true);
        foreach (var canvas in target.GetComponentsInChildren<Canvas>()) canvas.enabled = false;
        Physics.SyncTransforms();
    }

    static void MakeWall()
    {
        blocker = new GameObject("MeleeValidationWall");
        blocker.transform.position = camera.transform.position + camera.transform.forward * .55f;
        blocker.transform.rotation = camera.transform.rotation;
        blocker.AddComponent<BoxCollider>().size = new Vector3(1.2f, 2f, .12f); Physics.SyncTransforms();
    }

    static void StartAttack(string label)
    {
        float health = dummy.NormalizedHealth;
        expectedHealth = health - weapons.Current.damage * game.Progress.DamageMultiplier / DummyHealth;
        attackStarted = Time.time;
        Check(weapons.Attack(), label + " accepted.");
        Check(weapons.IsMeleeAttacking && melee.IsAttacking, label + " holds the gameplay attack lock.");
        Check(Mathf.Abs(dummy.NormalizedHealth - health) < .0001f, label + " does not apply immediate damage.");
    }

    static void VerifyDamage(string label)
    {
        Check(Mathf.Abs(dummy.NormalizedHealth - expectedHealth) < .0001f,
            label + " | expected " + expectedHealth.ToString("F4") + ", got " + dummy.NormalizedHealth.ToString("F4"));
    }

    static void Equip(int index)
    {
        if (weapons.SelectedIndex != index) Require(weapons.Equip(index), "Can equip slot " + index + ".");
        input.enabled = false;
    }

    static bool MotionState(string name)
    {
        return motion.GetCurrentAnimatorStateInfo(0).IsName(name) ||
            (motion.IsInTransition(0) && motion.GetNextAnimatorStateInfo(0).IsName(name));
    }

    static void WaitForAttack(float time)
    {
        Next(0); waitUntil = attackStarted + time;
    }

    static void Next(double delay) { step++; waitUntil = 0; due = EditorApplication.timeSinceStartup + delay; }
    static void Require(bool condition, string message)
    { Check(condition, message); if (!condition) throw new InvalidOperationException(message); }
    static void Check(bool condition, string message)
    { if (!condition) failures++; report?.AppendLine((condition ? "PASS " : "FAIL ") + message); }

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
            report.AppendLine("CAPTURE " + file + " | " + weapons.Current.label + " | attack elapsed " + (Time.time - attackStarted).ToString("F3"));
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
            Check(PlayerPrefs.HasKey(ProgressKey) == progressExisted && PlayerPrefs.GetString(ProgressKey, "") == savedProgress,
                "Saved contract progress was not changed.");
            report.AppendLine("RESULT " + (failures == 0 ? "PASS" : "FAIL") + " | " + failures + " failed checks | " +
                (EditorApplication.timeSinceStartup - started).ToString("F2") + " wall seconds");
            File.WriteAllText(System.IO.Path.Combine(OutputFolder(), "MeleeValidation.txt"), report.ToString());
        }
        catch (Exception exception) { Debug.LogException(exception); }
        finally
        {
            if (dummy != null) Object.DestroyImmediate(dummy.gameObject);
            if (blocker != null) Object.DestroyImmediate(blocker);
            foreach (var item in behaviours) if (item.Key != null) item.Key.enabled = item.Value;
            foreach (var item in colliders) if (item.Key != null) item.Key.enabled = item.Value;
            if (mouse != null && mouse.added && mouseEnabled) InputSystem.EnableDevice(mouse);
            Application.runInBackground = background; Time.timeScale = 1;
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            if (stopPlay && EditorApplication.isPlaying) EditorApplication.isPlaying = false;
        }
        Debug.Log("Female melee validation finished with " + failures + " failed checks. See Documentation/MeleeValidation.txt.");
    }
}
#endif
