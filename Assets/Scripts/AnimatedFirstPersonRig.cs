using UnityEngine;
using UnityEngine.Rendering;

[DefaultExecutionOrder(200)]
public class AnimatedFirstPersonRig : MonoBehaviour
{
    private const string SleeveResourcePath = "FemaleFirstPersonSleeve";
    private const string Body01ClipResourcePath = "FemaleFirstPersonBody01";
    private const string Body02ClipResourcePath = "FemaleFirstPersonBody02";
    private const string MeleeControllerResourcePath = "FemaleMelee/MeleeMotion";

    private static readonly int MeleeBatTrigger = Animator.StringToHash("SwingBat");
    private static readonly int MeleeQuickTrigger = Animator.StringToHash("SwingQuick");
    private static readonly int MeleeActiveBool = Animator.StringToHash("Active");

    private static readonly Vector3[] ClippedHoldBones =
    {
        new Vector3(0.01f, 0.01f, 0.01f), // root
        new Vector3(0.01f, 0.01f, 0.01f), // pelvis
        new Vector3(0.01f, 0.01f, 0.01f), // spine_01
        new Vector3(0.01f, 0.01f, 0.01f), // spine_02
        new Vector3(0.01f, 0.01f, 0.01f), // spine_03
        new Vector3(0.01f, 0.01f, 0.01f), // thigh_l
        new Vector3(0.01f, 0.01f, 0.01f), // calf_l
        new Vector3(0.01f, 0.01f, 0.01f), // foot_l
        new Vector3(0.01f, 0.01f, 0.01f), // ball_l
        new Vector3(0.01f, 0.01f, 0.01f), // thigh_r
        new Vector3(0.01f, 0.01f, 0.01f), // calf_r
        new Vector3(0.01f, 0.01f, 0.01f), // foot_r
        new Vector3(0.01f, 0.01f, 0.01f), // ball_r
        new Vector3(0.01f, 0.01f, 0.01f), // clavicle_l
        new Vector3(0.55f, 1f, 1f),       // upperarm_l
        Vector3.one,                      // lowerarm_l
        Vector3.one,                      // hand_l
        new Vector3(0.01f, 0.01f, 0.01f), // clavicle_r
        new Vector3(0.55f, 1f, 1f),       // upperarm_r
        Vector3.one,                      // lowerarm_r
        Vector3.one,                      // hand_r
        new Vector3(0.01f, 0.01f, 0.01f), // neck_01
        new Vector3(0.01f, 0.01f, 0.01f), // head
    };

    private Transform sourceRoot;
    private Transform combatRig;
    private Transform meleeMotionDriver;
    private Animator animator;
    private Animator meleeMotionAnimator;
    private CharacterController controller;
    private PlayerWeapons weapons;
    private Transform clavicleL, upperArmL, lowerArmL, handL;
    private Transform clavicleR, upperArmR, lowerArmR, handR;
    private Transform weaponRoot, pistol, rifle, bat, potion, grenade;
    private Transform[] allWeapons;
    private Transform[] body02Bones;
    private float bobTime;
    private float kick;
    private bool bound;

    public void Initialize(Transform player, Transform visualRoot, int viewModelLayer)
    {
        controller = player != null ? player.GetComponent<CharacterController>() : null;
        BindWeapons(player != null ? player.GetComponent<PlayerWeapons>() : null);
        if (visualRoot == null) return;

        if (sourceRoot != visualRoot || combatRig == null)
        {
            if (combatRig != null) Destroy(combatRig.gameObject);
            sourceRoot = visualRoot;
            CreateViewRig(visualRoot, viewModelLayer);
        }

        SetLayerRecursive(visualRoot, 0);
        SetRenderersEnabled(visualRoot, false);
        Animator sourceAnimator = visualRoot.GetComponent<Animator>();
        if (sourceAnimator != null) sourceAnimator.enabled = false;
        PlayerMotor motor = player != null ? player.GetComponent<PlayerMotor>() : null;
        if (motor != null) motor.GetComponent<FemaleAnimationDriver>()?.Configure(animator);
    }

    public void Kick(float amount = 1f) => kick = Mathf.Clamp01(kick + amount);

    private void CreateViewRig(Transform visualRoot, int viewModelLayer)
    {
        GameObject clone = Instantiate(visualRoot.gameObject, transform);
        clone.name = "FemaleCombatRig";
        combatRig = clone.transform;
        combatRig.localPosition = new Vector3(0.10f, -1.30f, -0.02f);
        combatRig.localRotation = Quaternion.Euler(4f, 0f, 0f);
        combatRig.localScale = Vector3.one;

        animator = clone.GetComponent<Animator>();
        if (animator != null)
        {
            RuntimeAnimatorController fpController = Resources.Load<RuntimeAnimatorController>("FemaleFirstPerson");
            if (fpController != null) animator.runtimeAnimatorController = fpController;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.enabled = true;
        }

        ConfigureViewRenderers(combatRig);
        ApplyClippedArmMeshes(combatRig);
        SetLayerRecursive(combatRig, viewModelLayer);
        BindBones();
        SetupMeleeMotionAnimator();
        BuildHandCaps(combatRig, viewModelLayer);
    }

    private static void ConfigureViewRenderers(Transform root)
    {
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            string name = renderer.name;
            bool isArmOrWeapon = name == "Body01" || name == "Body02" || IsWeaponTransform(renderer.transform);
            renderer.enabled = isArmOrWeapon;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            if (renderer is SkinnedMeshRenderer skinned)
            {
                skinned.updateWhenOffscreen = true;
                skinned.localBounds = new Bounds(Vector3.zero, Vector3.one * 4f);
            }
        }
    }

    private void ApplyClippedArmMeshes(Transform root)
    {
        Mesh body01Mesh = Resources.Load<Mesh>(Body01ClipResourcePath);
        Mesh body02Mesh = Resources.Load<Mesh>(Body02ClipResourcePath);
        Material sleeveMaterial = Resources.Load<Material>(SleeveResourcePath);
        SkinnedMeshRenderer body01 = root.Find("Body01")?.GetComponent<SkinnedMeshRenderer>();
        SkinnedMeshRenderer body02 = root.Find("Body02")?.GetComponent<SkinnedMeshRenderer>();
        body02Bones = body02 != null ? body02.bones : null;

        if (body01 != null && body01Mesh != null) body01.sharedMesh = body01Mesh;
        if (body02 != null && body02Mesh != null)
        {
            body02.sharedMesh = body02Mesh;
            if (sleeveMaterial != null) body02.sharedMaterial = sleeveMaterial;
        }
        else if (body01 != null)
        {
            body01.enabled = false;
        }
    }

    private static bool IsWeaponTransform(Transform item)
    {
        for (Transform current = item; current != null; current = current.parent)
            if (current.name == "FirstPersonWeapons" || current.name == "Weapon_R") return true;
        return false;
    }

    private void BindBones()
    {
        clavicleL = FindBone("clavicle_l"); upperArmL = FindBone("upperarm_l");
        lowerArmL = FindBone("lowerarm_l"); handL = FindBone("hand_l");
        clavicleR = FindBone("clavicle_r"); upperArmR = FindBone("upperarm_r");
        lowerArmR = FindBone("lowerarm_r"); handR = FindBone("hand_r");
        weaponRoot = FindBone("FirstPersonWeapons") ?? FindBone("Weapon_R");
        pistol = FindWeapon("PistolView", "Pistol");
        rifle = FindWeapon("RifleView", "AssaultRifle");
        bat = FindWeapon("BatView", "BaseBallBat");
        potion = FindWeapon("PotionView", "HealthPotion");
        grenade = FindWeapon("GrenadeView", "FragGrenade");
        allWeapons = new[] { pistol, rifle, bat, potion, grenade };
        bound = clavicleL != null && upperArmL != null && lowerArmL != null && handL != null &&
                clavicleR != null && upperArmR != null && lowerArmR != null && handR != null;
    }

    private void SetupMeleeMotionAnimator()
    {
        if (combatRig == null) return;
        Transform existing = combatRig.Find("MeleeMotionDriver");
        GameObject driverObj = existing != null ? existing.gameObject : new GameObject("MeleeMotionDriver");
        meleeMotionDriver = driverObj.transform;
        meleeMotionDriver.SetParent(combatRig, false);
        meleeMotionDriver.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        meleeMotionDriver.localScale = Vector3.one;

        meleeMotionAnimator = driverObj.GetComponent<Animator>();
        if (meleeMotionAnimator == null) meleeMotionAnimator = driverObj.AddComponent<Animator>();
        meleeMotionAnimator.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>(MeleeControllerResourcePath);
        meleeMotionAnimator.applyRootMotion = false;
        meleeMotionAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
    }

    private void BindWeapons(PlayerWeapons targetWeapons)
    {
        if (weapons == targetWeapons) return;
        UnbindWeapons();
        weapons = targetWeapons;
        if (weapons == null) return;
        weapons.Attacked += OnWeaponAttacked;
        weapons.QuickMeleeStarted += OnQuickMeleeStarted;
    }

    private void UnbindWeapons()
    {
        if (weapons == null) return;
        weapons.Attacked -= OnWeaponAttacked;
        weapons.QuickMeleeStarted -= OnQuickMeleeStarted;
        weapons = null;
    }

    private void OnDestroy() => UnbindWeapons();

    private void OnWeaponAttacked()
    {
        if (weapons != null && weapons.Current != null && weapons.Current.melee)
            TriggerMeleeMotion(MeleeBatTrigger);
    }

    private void OnQuickMeleeStarted() => TriggerMeleeMotion(MeleeQuickTrigger);

    private void TriggerMeleeMotion(int triggerHash)
    {
        if (meleeMotionAnimator == null || meleeMotionAnimator.runtimeAnimatorController == null) return;
        meleeMotionAnimator.ResetTrigger(MeleeBatTrigger);
        meleeMotionAnimator.ResetTrigger(MeleeQuickTrigger);
        meleeMotionAnimator.SetBool(MeleeActiveBool, true);
        meleeMotionAnimator.SetTrigger(triggerHash);
    }

    private void LateUpdate()
    {
        if (!bound || combatRig == null) return;
        SyncActiveWeaponModel();

        Vector3 velocity = controller != null ? controller.velocity : Vector3.zero;
        velocity.y = 0f;
        float speed = velocity.magnitude;
        if (speed > 0.1f) bobTime += Time.deltaTime * Mathf.Lerp(6f, 11f, Mathf.Clamp01(speed / 8f));

        float bobX = Mathf.Sin(bobTime) * 0.012f * Mathf.Clamp01(speed / 5f);
        float bobY = Mathf.Abs(Mathf.Cos(bobTime)) * 0.010f * Mathf.Clamp01(speed / 5f);
        kick = Mathf.MoveTowards(kick, 0f, 10f * Time.deltaTime);

        bool meleeEquipped = weapons != null && weapons.Current != null && weapons.Current.melee;
        bool meleeSwinging = weapons != null && weapons.IsMeleeAttacking;
        bool meleeMode = meleeEquipped || meleeSwinging;
        if (meleeMotionAnimator != null && meleeMotionAnimator.runtimeAnimatorController != null)
            meleeMotionAnimator.SetBool(MeleeActiveBool, meleeSwinging);

        Vector3 motionPos = meleeMotionDriver != null && meleeSwinging ? meleeMotionDriver.localPosition : Vector3.zero;
        Quaternion motionRot = meleeMotionDriver != null && meleeSwinging ? meleeMotionDriver.localRotation : Quaternion.identity;

        Vector3 basePos = meleeMode ? new Vector3(0.08f, -1.33f, 0.02f) : new Vector3(0.10f, -1.30f, -0.02f);
        Quaternion baseRot = meleeMode ? Quaternion.Euler(7f, -6f, 2f) : Quaternion.Euler(4f, 0f, 0f);
        combatRig.localPosition = basePos + new Vector3(bobX, -bobY, -kick * 0.05f) + motionPos;
        combatRig.localRotation = Quaternion.Euler(-kick * 7f, 0f, 0f) * baseRot * motionRot;

        HideBodyWaitForArmsOnly();
        PoseEquippedWeapon();
    }

    private void HideBodyWaitForArmsOnly()
    {
        if (body02Bones == null) return;
        int count = Mathf.Min(body02Bones.Length, ClippedHoldBones.Length);
        for (int i = 0; i < count; i++)
            if (body02Bones[i] != null) body02Bones[i].localScale = ClippedHoldBones[i];
    }

    private void SyncActiveWeaponModel()
    {
        if (weapons == null || weapons.Current == null) return;
        Transform active = GetActiveWeaponView();
        if (active == null) return;
        foreach (Transform candidate in allWeapons)
            if (candidate != null) candidate.gameObject.SetActive(candidate == active);
    }

    private Transform GetActiveWeaponView()
    {
        string label = weapons != null && weapons.Current != null && weapons.Current.label != null
            ? weapons.Current.label.ToLowerInvariant()
            : string.Empty;
        var kind = weapons != null ? weapons.CurrentKind : SlotKind.Gun;
        if (kind == SlotKind.Consumable || label.Contains("bình") || label.Contains("máu") || label.Contains("potion")) return potion;
        if (kind == SlotKind.Throwable || label.Contains("lựu") || label.Contains("bom") || label.Contains("grenade")) return grenade;
        if (weapons != null && weapons.Current != null && weapons.Current.melee) return bat;
        if (weapons != null && weapons.Current != null && weapons.Current.automatic) return rifle != null ? rifle : pistol;
        return pistol != null ? pistol : rifle;
    }

    private void PoseEquippedWeapon()
    {
        if (weaponRoot != null)
        {
            weaponRoot.localPosition = new Vector3(-0.02f, 0.04f, 0.02f);
            weaponRoot.localRotation = Quaternion.identity;
            weaponRoot.localScale = Vector3.one;
        }
        if (pistol != null && pistol.gameObject.activeSelf)
        {
            pistol.localPosition = new Vector3(0.02f, 0.03f, 0.07f);
            pistol.localRotation = Quaternion.Euler(0f, 92f, 90f);
            pistol.localScale = Vector3.one * 0.88f;
        }
        if (rifle != null && rifle.gameObject.activeSelf)
        {
            rifle.localPosition = new Vector3(0.03f, 0.02f, 0.14f);
            rifle.localRotation = Quaternion.Euler(0f, 92f, 88f);
            rifle.localScale = Vector3.one * 0.72f;
        }
        if (bat != null && bat.gameObject.activeSelf)
        {
            bat.localPosition = new Vector3(0.03f, 0.05f, 0.06f);
            bat.localRotation = Quaternion.Euler(-74f, 96f, 88f);
            bat.localScale = Vector3.one * 0.78f;
        }
        if (potion != null && potion.gameObject.activeSelf)
        {
            potion.localPosition = new Vector3(0.02f, 0.04f, 0.06f);
            potion.localRotation = Quaternion.Euler(10f, 180f, 80f);
            potion.localScale = Vector3.one * 0.95f;
        }
        if (grenade != null && grenade.gameObject.activeSelf)
        {
            grenade.localPosition = new Vector3(0.02f, 0.04f, 0.06f);
            grenade.localRotation = Quaternion.Euler(10f, 180f, 80f);
            grenade.localScale = Vector3.one * 1.05f;
        }
    }

    private static void BuildHandCaps(Transform rigRoot, int layer)
    {
        Material sleeveMaterial = Resources.Load<Material>(SleeveResourcePath);
        if (sleeveMaterial == null) return;
        foreach (string boneName in new[] { "lowerarm_l", "lowerarm_r" })
        {
            Transform bone = FindBoneIn(rigRoot, boneName);
            if (bone == null || bone.Find("SleeveCuff") != null) continue;
            GameObject cuff = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cuff.name = "SleeveCuff";
            Destroy(cuff.GetComponent<Collider>());
            cuff.layer = layer;
            cuff.transform.SetParent(bone, false);
            cuff.transform.localPosition = new Vector3(0.18f, 0f, 0f);
            cuff.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            cuff.transform.localScale = new Vector3(0.065f, 0.03f, 0.065f);
            Renderer r = cuff.GetComponent<Renderer>();
            r.sharedMaterial = sleeveMaterial;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
        }
    }

    private Transform FindBone(string boneName) => FindBoneIn(combatRig, boneName);

    private Transform FindWeapon(string primary, string fallback)
    {
        if (weaponRoot != null)
        {
            Transform direct = weaponRoot.Find(primary) ?? weaponRoot.Find(fallback);
            if (direct != null) return direct;
        }
        return FindBone(primary) ?? FindBone(fallback);
    }

    private static Transform FindBoneIn(Transform root, string boneName)
    {
        if (root == null) return null;
        foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
            if (item.name == boneName) return item;
        return null;
    }

    private static void SetRenderersEnabled(Transform root, bool enabled)
    {
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            renderer.enabled = enabled;
    }

    private static void SetLayerRecursive(Transform root, int layer)
    {
        foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
            item.gameObject.layer = layer;
    }
}
