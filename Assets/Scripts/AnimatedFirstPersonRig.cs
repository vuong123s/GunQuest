using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

// Camera-local Female skin, evaluated by its own event-free FPS Animator.
// Gameplay owns ammo, damage and reload duration; this rig only presents them.
public sealed class AnimatedFirstPersonRig : IDisposable
{
    private readonly GameObject root;
    private readonly Animator animator;
    private readonly PlayerWeapons weapons;
    private readonly Transform cameraTransform;
    private readonly CharacterController controller;
    private readonly Animator bodyAnimator;
    private readonly Transform[] upper = new Transform[2], lower = new Transform[2], hand = new Transform[2];
    private readonly Vector3[] upperRest = new Vector3[2], readyHand = new Vector3[2];
    private readonly Quaternion[] readyRotation = new Quaternion[2];
    private readonly Dictionary<Transform, Transform> copies = new Dictionary<Transform, Transform>();
    private Transform model, muzzle;
    private Quaternion gripRotation, wristCorrection;
    private Vector3 supportGrip;
    private readonly Vector3[] animatedDelta = new Vector3[2];
    private readonly Quaternion[] animatedRotation = new Quaternion[2];
    private readonly Animator meleeAnimator;
    private readonly Transform meleePose;
    private readonly PlayerMelee melee;
    private string meleeState = "Idle";
    private MeleeAttackTiming meleeTiming;
    private Vector3 recoil, recoilVelocity, sway, swayVelocity;
    private Vector3 previousAngles;
    private float stepPhase, movement, sprint, aim, equipDrop, landing, landingVelocity;
    private float wallPullback, wallPullbackVelocity;
    private bool wasReloading, wasGrounded;
    private float reloadBlend;
    public Transform Muzzle => muzzle;
    public GameObject Root => root;

    public AnimatedFirstPersonRig(Animator source, PlayerWeapons weapons, Camera camera)
    {
        this.weapons = weapons;
        melee = weapons.GetComponent<PlayerMelee>();
        bodyAnimator = source;
        cameraTransform = camera.transform;
        controller = weapons.GetComponent<CharacterController>();
        root = Object.Instantiate(source.gameObject);
        root.name = "FirstPersonAnimatedArms";
        root.transform.SetParent(cameraTransform, false);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one * 1.15f;
        Map(source.transform, root.transform);
        animator = root.GetComponent<Animator>();
        foreach (var script in root.GetComponentsInChildren<MonoBehaviour>(true)) script.enabled = false;
        foreach (var other in root.GetComponentsInChildren<Animator>(true)) other.enabled = false;
        foreach (var collider in root.GetComponentsInChildren<Collider>(true))
        { collider.enabled = false; Object.Destroy(collider); }
        foreach (var body in root.GetComponentsInChildren<Rigidbody>(true)) Object.Destroy(body);
        foreach (var particles in root.GetComponentsInChildren<ParticleSystem>(true))
        {
            if (particles.name.Contains("Bubble") || particles.name.Contains("Spark") || particles.name.Contains("Fuse")) continue;
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            Object.Destroy(particles);
        }
        foreach (var light in root.GetComponentsInChildren<Light>(true)) { light.enabled = false; Object.Destroy(light); }
        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            renderer.enabled = false;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
        foreach (var skin in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            var mesh = Resources.Load<Mesh>("FemaleFirstPerson" + skin.name);
            if (mesh == null) continue;
            skin.sharedMesh = mesh; skin.enabled = true; skin.updateWhenOffscreen = true;
            skin.localBounds = new Bounds(Vector3.zero, Vector3.one * 6f);
        }
        animator.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>("FemaleFirstPerson");
        if (animator.runtimeAnimatorController == null)
            throw new InvalidOperationException("Build Female first-person meshes and Animator in the GunQuest menu first.");
        animator.applyRootMotion = false;
        animator.keepAnimatorStateOnDisable = true;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        animator.enabled = false;
        animator.Rebind(); animator.Update(0);
        for (int i = 0; i < 2; i++)
        {
            upper[i] = animator.GetBoneTransform(i == 0 ? HumanBodyBones.LeftUpperArm : HumanBodyBones.RightUpperArm);
            lower[i] = animator.GetBoneTransform(i == 0 ? HumanBodyBones.LeftLowerArm : HumanBodyBones.RightLowerArm);
            hand[i] = animator.GetBoneTransform(i == 0 ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand);
            upperRest[i] = upper[i].localPosition;
        }
        var motion = new GameObject("FirstPersonMeleeMotion");
        motion.transform.SetParent(root.transform, false);
        meleePose = new GameObject("Pose").transform;
        meleePose.SetParent(motion.transform, false);
        meleeAnimator = motion.AddComponent<Animator>();
        meleeAnimator.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>("FemaleMelee/MeleeMotion");
        meleeAnimator.applyRootMotion = false;
        meleeAnimator.keepAnimatorStateOnDisable = true;
        meleeAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        meleeAnimator.enabled = false;
        if (meleeAnimator.runtimeAnimatorController != null) { meleeAnimator.Rebind(); meleeAnimator.Update(0); }
        previousAngles = cameraTransform.eulerAngles;
        wasGrounded = controller != null && controller.isGrounded;
        SetWeapon();
        SetLayerRecursively(root, 6);
    }

    private void Map(Transform original, Transform copy)
    {
        copies.Add(original, copy);
        for (int i = 0; i < original.childCount; i++) Map(original.GetChild(i), copy.GetChild(i));
    }

    public void SetWeapon()
    {
        if (weapons.Current == null) return;
        foreach (var pair in copies)
        {
            if (pair.Key == bodyAnimator.transform || pair.Key == null || pair.Value == null) continue;
            pair.Value.gameObject.SetActive(pair.Key.gameObject.activeSelf);
            foreach (var renderer in pair.Value.GetComponents<MeshRenderer>()) renderer.enabled = false;
        }
        foreach (var ps in root.GetComponentsInChildren<ParticleSystem>(true))
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        copies.TryGetValue(weapons.Current.model.transform, out model);
        if (model != null)
        {
            foreach (var renderer in model.GetComponentsInChildren<MeshRenderer>(true)) renderer.enabled = true;
            foreach (var ps in model.GetComponentsInChildren<ParticleSystem>(true))
            {
                ps.gameObject.SetActive(true);
                ps.Play();
            }
            model.localScale = Vector3.one * (weapons.Current.melee ? .65f : weapons.Current.automatic ? .9f : 1f);
        }
        muzzle = null;
        if (weapons.Current.muzzle != null) copies.TryGetValue(weapons.Current.muzzle, out muzzle);
        if (muzzle == null) muzzle = model;
        root.SetActive(true);
        bool rifle = weapons.Current.automatic;
        animator.ResetTrigger("Reload"); animator.ResetTrigger("Shoot");
        animator.SetBool("IsRifle", rifle);
        bool isItem = weapons.Current != null && (weapons.Current.kind == SlotKind.Consumable || weapons.Current.kind == SlotKind.Throwable);
        animator.SetBool("IsMeleeWeapon", weapons.Current.melee || isItem);
        animator.SetFloat("ReloadRate", 1f);
        for (int i = 0; i < 2; i++) upper[i].localPosition = upperRest[i];
        animator.Play(rifle ? "ReadyAR" : "ReadyHG", 0, 0);
        animator.Update(0);
        for (int i = 0; i < 2; i++)
        {
            readyHand[i] = root.transform.InverseTransformPoint(hand[i].position);
            readyRotation[i] = Quaternion.Inverse(root.transform.rotation) * hand[i].rotation;
        }
        // Calibrate in this stance; subsequent wrist/finger motion remains animated.
        gripRotation = Quaternion.Inverse(readyRotation[1]);
        Transform nativeGrip = null;
        string reference = rifle ? "AssaultRiflePBR" : "PistolPBR";
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == reference) { nativeGrip = t; break; }
        if (nativeGrip != null && model != null && !weapons.Current.melee && !isItem)
        {
            model.localPosition = hand[1].InverseTransformPoint(nativeGrip.position);
            gripRotation = Quaternion.Inverse(hand[1].rotation) * nativeGrip.rotation;
        }
        wristCorrection = Quaternion.Inverse(gripRotation) * Quaternion.Inverse(readyRotation[1]);
        supportGrip = Quaternion.Inverse(readyRotation[1]) * (readyHand[0] - readyHand[1]);
        if (model != null && !weapons.Current.melee && !isItem) model.localRotation = gripRotation;
        meleeState = "Idle";
        SampleMelee(0f);
        meleeAnimator.transform.localPosition = Vector3.zero;
        if (model != null && weapons.Current.melee)
        {
            model.SetParent(hand[1], false);
            bool shovel = weapons.CurrentMeleeTiming.Duration == MeleeAttackTiming.Shovel.Duration;
            model.localPosition = shovel ? new Vector3(0.06f, -0.06f, 0.00f) : new Vector3(0.06f, -0.04f, 0.00f);
            var baseGripRot = Quaternion.Euler(0.15f, 89.79f, 89.79f);
            gripRotation = baseGripRot;
            model.localRotation = gripRotation;
            model.localScale = Vector3.one * (shovel ? .55f : .60f);
        }
        else if (model != null && isItem)
        {
            model.SetParent(hand[1], false);
            if (weapons.Current.kind == SlotKind.Consumable)
            {
                // Health Potion: held cleanly in palm upright, tilted slightly inward
                model.localPosition = new Vector3(0.04f, -0.04f, 0.015f);
                gripRotation = Quaternion.Euler(5f, 80f, 85f);
                model.localRotation = gripRotation;
                model.localScale = Vector3.one * 1.05f;
            }
            else
            {
                // Bomb / Grenade: held in palm
                model.localPosition = new Vector3(0.04f, -0.035f, 0.015f);
                gripRotation = Quaternion.Euler(0f, 85f, 90f);
                model.localRotation = gripRotation;
                model.localScale = Vector3.one * 1.1f;
            }
        }
        SetLayerRecursively(root, 6);
        wasReloading = false; reloadBlend = 0; equipDrop = 1f;
        recoil = recoilVelocity = Vector3.zero;
    }

    public void PlayAttack()
    {
        if (weapons.Current == null) return;
        bool isItem = weapons.Current.kind == SlotKind.Consumable || weapons.Current.kind == SlotKind.Throwable;
        if (isItem)
        {
            if (weapons.Current.kind == SlotKind.Consumable)
            {
                recoil += new Vector3(2.4f, 0.5f, 0.04f);
                BeginMelee("BatSwing", MeleeAttackTiming.Quick);
            }
            else
            {
                recoil += new Vector3(3.2f, -0.8f, 0.06f);
                BeginMelee("BatSwing", MeleeAttackTiming.ForWeapon(false));
            }
        }
        else if (!weapons.Current.melee)
        {
            animator.SetTrigger("Shoot");
            recoil += new Vector3(weapons.Current.automatic ? 1.1f : 2.6f, .12f, weapons.Current.automatic ? .015f : .035f);
            recoil.x = Mathf.Min(recoil.x, 6f); recoil.z = Mathf.Min(recoil.z, .075f);
        }
        else
        {
            bool shovel = weapons.CurrentMeleeTiming.Duration == MeleeAttackTiming.Shovel.Duration;
            recoil += shovel ? new Vector3(1.4f, 0.4f, 0.03f) : new Vector3(0.6f, -1.2f, 0.02f);
            BeginMelee(shovel ? "ShovelSwing" : "BatSwing", weapons.CurrentMeleeTiming);
        }
    }

    public void PlayQuickMelee()
    {
        recoil += new Vector3(0.8f, -0.6f, 0.02f);
        BeginMelee("QuickSwing", MeleeAttackTiming.Quick);
    }

    private void BeginMelee(string state, MeleeAttackTiming timing)
    {
        meleeState = state; meleeTiming = timing;
        SampleMelee(0f);
    }

    private void SampleMelee(float normalizedTime)
    {
        if (meleeAnimator == null || meleeAnimator.runtimeAnimatorController == null) return;
        meleeAnimator.Play(meleeState, 0, Mathf.Clamp01(normalizedTime));
        meleeAnimator.Update(0f);
    }

    private void UpdateMeleeMotion()
    {
        if (melee == null || !melee.IsAttacking) meleeState = "Idle";
        SampleMelee(meleeState == "Idle" ? 0f : melee.AttackElapsed / meleeTiming.Duration);
    }

    private void PoseMeleeHands()
    {
        if (model == null) return;
        // The weapon is animated independently; both genuine Female hands follow
        // grip points on its handle, with elbow hints below the camera.
        Quaternion rightRotation = model.rotation * readyRotation[1];
        Quaternion leftRotation = model.rotation * readyRotation[0];
        Vector3 right = root.transform.InverseTransformPoint(model.TransformPoint(new Vector3(0f, -.06f, 0f)));
        Vector3 left = root.transform.InverseTransformPoint(model.TransformPoint(new Vector3(0f, .15f, 0f)));
        Solve(1, new Vector3(.34f, -.48f, .12f), right, rightRotation, new Vector3(.65f, -1.2f, .2f));
        Solve(0, new Vector3(-.06f, -.45f, .48f), left, leftRotation, new Vector3(-.25f, -1.5f, .12f));
    }
    public void UpdatePose(Vector3 framingAdjustment, bool aiming)
    {
        if (root == null || animator == null || weapons.Current == null) return;
        bool playing = ContractGame.ControlsAllowed;
        root.SetActive(playing);
        if (!playing) { previousAngles = cameraTransform.eulerAngles; return; }
        float dt = Mathf.Min(Time.deltaTime, .05f);
        if (dt <= 0) return;
        bool reloading = weapons.IsReloading;
        if (reloading && !wasReloading) { animator.ResetTrigger("Shoot"); animator.SetTrigger("Reload"); }
        if (!reloading && wasReloading)
        {
            animator.ResetTrigger("Reload");
            animator.CrossFadeInFixedTime(weapons.Current.automatic ? "ReadyAR" : "ReadyHG", .12f);
        }
        wasReloading = reloading;
        for (int i = 0; i < 2; i++) upper[i].localPosition = upperRest[i];
        animator.Update(Time.deltaTime);
        reloadBlend = Mathf.MoveTowards(reloadBlend, reloading ? 1f : 0f, dt * 12f);
        float speed = controller != null ? Vector3.ProjectOnPlane(controller.velocity, Vector3.up).magnitude : 0;
        movement = Mathf.Lerp(movement, Mathf.Clamp01(speed / 5f), 1f - Mathf.Exp(-10f * dt));
        bool running = bodyAnimator != null && bodyAnimator.GetBool("IsSprinting") && speed > 1f && !reloading;
        sprint = Mathf.Lerp(sprint, running ? 1f : 0f, 1f - Mathf.Exp(-9f * dt));
        aim = Mathf.Lerp(aim, aiming && !reloading && !running && !weapons.Current.melee ? 1f : 0f, 1f - Mathf.Exp(-14f * dt));
        equipDrop = Mathf.MoveTowards(equipDrop, 0, dt * 5f);
        recoil = Vector3.SmoothDamp(recoil, Vector3.zero, ref recoilVelocity, .085f, Mathf.Infinity, dt);
        Vector3 angles = cameraTransform.eulerAngles;
        Vector3 mouse = new Vector3(Mathf.DeltaAngle(previousAngles.x, angles.x), Mathf.DeltaAngle(previousAngles.y, angles.y), 0);
        previousAngles = angles;
        Vector3 swayTarget = new Vector3(Mathf.Clamp(mouse.x / Mathf.Max(Time.deltaTime, .001f) * -.009f, -2f, 2f), Mathf.Clamp(mouse.y / Mathf.Max(Time.deltaTime, .001f) * -.009f, -2f, 2f), Mathf.Clamp(mouse.y / Mathf.Max(Time.deltaTime, .001f) * -.004f, -1f, 1f));
        sway = Vector3.SmoothDamp(sway, swayTarget, ref swayVelocity, .07f, Mathf.Infinity, dt);
        bool grounded = controller != null && controller.isGrounded;
        if (grounded && !wasGrounded) landing = -.035f;
        wasGrounded = grounded;
        landing = Mathf.SmoothDamp(landing, 0, ref landingVelocity, .12f, Mathf.Infinity, dt);
        stepPhase += dt * (running ? 12f : 8f) * movement;
        Vector3 bob = new Vector3(Mathf.Sin(stepPhase) * .005f, Mathf.Cos(stepPhase * 2f) * .005f, 0) * movement * (1f - aim * .8f);
        bob.y += Mathf.Sin(Time.time * 1.7f) * .0015f + landing - equipDrop * .20f - sprint * .08f;
        bob.z -= recoil.z;
        float targetPullback = 0f;
        int worldMask = ~((1 << 6) | (1 << 2));
        if (Physics.SphereCast(cameraTransform.position, 0.16f, cameraTransform.forward, out RaycastHit hit, 0.85f, worldMask, QueryTriggerInteraction.Ignore))
        {
            targetPullback = Mathf.Clamp01((0.85f - hit.distance) / 0.85f);
        }
        wallPullback = Mathf.SmoothDamp(wallPullback, targetPullback, ref wallPullbackVelocity, 0.08f, Mathf.Infinity, dt);
        Vector3 wallOffset = new Vector3(0f, -wallPullback * 0.10f, -wallPullback * 0.26f);
        Vector3 wallAngles = new Vector3(wallPullback * 14f, -wallPullback * 4f, 0f);
        // Existing saved offset remains a small trim, relative to its legacy default.
        root.transform.localPosition = new Vector3(0, -.08f, .15f) + bob + framingAdjustment - new Vector3(-.1f, -.8f, .55f) + wallOffset;
        root.transform.localRotation = Quaternion.Euler(sway + new Vector3(-recoil.x + sprint * 13f + equipDrop * 12f, recoil.y, -sprint * 8f) + wallAngles);
        UpdateMeleeMotion();
        if (meleeState != "Idle" && meleePose != null)
        {
            root.transform.localPosition += meleePose.localPosition;
            root.transform.localRotation *= meleePose.localRotation;
        }
        bool isItem = weapons.Current != null && (weapons.Current.kind == SlotKind.Consumable || weapons.Current.kind == SlotKind.Throwable);
        for (int i = 0; i < 2; i++)
        {
            upper[i].localPosition = upperRest[i];
            animatedDelta[i] = root.transform.InverseTransformPoint(hand[i].position) - readyHand[i];
            animatedRotation[i] = (weapons.Current.melee || isItem) ? hand[i].rotation : root.transform.rotation * wristCorrection * Quaternion.Inverse(root.transform.rotation) * hand[i].rotation;
        }
        Vector3 wrist = Vector3.Lerp(new Vector3(.23f, -.23f, .51f), new Vector3(.015f, -.15f, .55f), aim);
        if (weapons.Current.automatic) wrist += new Vector3(.015f, -.015f, -.035f);
        if (weapons.Current.melee) wrist = new Vector3(.24f, -.26f, .48f);
        if (isItem) wrist = new Vector3(.24f, -.24f, .44f);
        Solve(1, Vector3.Lerp(new Vector3(.36f, -.44f, .12f), new Vector3(.22f, -.34f, .19f), aim), wrist + animatedDelta[1] * 1.15f, animatedRotation[1], new Vector3(.65f, -1f, .25f));
        if (model != null) model.localRotation = gripRotation;
        // A stance-specific support target follows the gun. The reload clip takes
        // over continuously as the support hand leaves the grip.
        Vector3 support = root.transform.InverseTransformPoint(hand[1].TransformPoint(supportGrip));
        if (weapons.Current.automatic && model != null)
            support = root.transform.InverseTransformPoint(model.TransformPoint(new Vector3(-.07f, -.035f, .16f)));
        Vector3 animatedLeft = new Vector3(-.08f, -.24f, .55f) + animatedDelta[0] * 1.15f;
        if (weapons.Current.melee || isItem) animatedLeft = new Vector3(-.24f, -.50f, .28f);
        Vector3 leftGoal = Vector3.Lerp(support, animatedLeft, (weapons.Current.melee || isItem) ? 1f : reloadBlend);
        Solve(0, new Vector3(-.10f, -.45f, .48f), leftGoal, animatedRotation[0], new Vector3(-.10f, -1.6f, 0f));
    }

    private void Solve(int side, Vector3 shoulder, Vector3 wrist, Quaternion wristRotation, Vector3 hint)
    {
        var a = upper[side]; var b = lower[side]; var c = hand[side];
        a.position = root.transform.TransformPoint(shoulder);
        Vector3 goal = root.transform.TransformPoint(wrist);
        Vector3 pole = root.transform.TransformPoint(hint);
        float first = Vector3.Distance(a.position, b.position), second = Vector3.Distance(b.position, c.position);
        Vector3 toGoal = goal - a.position;
        float distance = Mathf.Clamp(toGoal.magnitude, Mathf.Abs(first-second)+.001f, (first+second)*.985f);
        Vector3 direction = toGoal.sqrMagnitude > .000001f ? toGoal.normalized : root.transform.forward;
        goal = a.position + direction * distance;
        Vector3 bend = Vector3.ProjectOnPlane(pole-a.position, direction).normalized;
        if (bend.sqrMagnitude < .001f) bend = Vector3.ProjectOnPlane(-root.transform.up, direction).normalized;
        float along = (first*first + distance*distance - second*second)/(2f*distance);
        Vector3 elbow = a.position + direction*along + bend*Mathf.Sqrt(Mathf.Max(0,first*first-along*along));
        a.rotation = Quaternion.FromToRotation(b.position-a.position, elbow-a.position) * a.rotation;
        b.rotation = Quaternion.FromToRotation(c.position-b.position, goal-b.position) * b.rotation;
        c.rotation = wristRotation;
    }

    public static void SetLayerRecursively(GameObject obj, int layer)
    {
        if (obj == null) return;
        obj.layer = layer;
        var t = obj.transform;
        for (int i = 0; i < t.childCount; i++)
            SetLayerRecursively(t.GetChild(i).gameObject, layer);
    }

    public void Dispose()
    {
        if (root != null) Object.Destroy(root);
    }
}