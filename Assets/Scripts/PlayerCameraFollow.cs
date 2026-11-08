using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(10000)]
public class PlayerCameraFollow : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Camera weaponCamera;
    [SerializeField] private Vector3 animatedArmsOffset = new Vector3(-0.1f, -0.8f, 0.55f);
    private AnimatedFirstPersonRig animatedRig;
    private Animator femaleAnimator;
    private PlayerLook look;
    private CharacterController controller;
    private PlayerWeapons weapons;
    private GameObject viewModel;
    private Transform viewMuzzle;
    private float kick;
    public Camera TargetCamera => targetCamera;
    public Camera WeaponCamera => weaponCamera;

    private void Awake()
    {
        look = GetComponent<PlayerLook>(); controller = GetComponent<CharacterController>();
        weapons = GetComponent<PlayerWeapons>();
        var female = GetComponent<FemaleAnimationDriver>();
        femaleAnimator = female != null ? female.CharacterAnimator : null;
        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera == null) return;
        targetCamera.transform.SetParent(null, true);
        targetCamera.nearClipPlane = 0.05f; targetCamera.fieldOfView = GameSettings.Instance != null ? GameSettings.Instance.fieldOfView : 75f;
        targetCamera.cullingMask &= ~(1 << 6);
        SetupWeaponCamera();
        foreach (var behaviour in targetCamera.GetComponents<MonoBehaviour>())
            if (behaviour != null && behaviour.GetType().Namespace != null &&
                behaviour.GetType().Namespace.Contains("Cinemachine")) behaviour.enabled = false;
        // Hide the full character from the eye camera, but retain its world shadow.
        foreach (var renderer in GetComponentsInChildren<Renderer>(true))
            if (renderer is MeshRenderer || renderer is SkinnedMeshRenderer) renderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
        if (weapons != null) { weapons.Changed += RebuildViewModel; weapons.Attacked += OnAttack; weapons.QuickMeleeStarted += OnQuickMelee; }
    }

    private void SetupWeaponCamera()
    {
        if (targetCamera == null) return;
        if (weaponCamera == null)
        {
            var child = targetCamera.transform.Find("WeaponCamera");
            if (child != null) weaponCamera = child.GetComponent<Camera>();
        }
        if (weaponCamera == null)
        {
            var camGo = new GameObject("WeaponCamera");
            camGo.transform.SetParent(targetCamera.transform, false);
            weaponCamera = camGo.AddComponent<Camera>();
        }
        weaponCamera.transform.localPosition = Vector3.zero;
        weaponCamera.transform.localRotation = Quaternion.identity;
        weaponCamera.transform.localScale = Vector3.one;
        weaponCamera.nearClipPlane = 0.01f;
        weaponCamera.farClipPlane = 30f;
        weaponCamera.fieldOfView = targetCamera.fieldOfView;
        weaponCamera.cullingMask = 1 << 6;
        weaponCamera.clearFlags = CameraClearFlags.Depth;

        var mainData = targetCamera.GetUniversalAdditionalCameraData();
        if (mainData != null)
        {
            mainData.renderType = CameraRenderType.Base;
            var weaponData = weaponCamera.GetUniversalAdditionalCameraData();
            if (weaponData != null)
            {
                weaponData.renderType = CameraRenderType.Overlay;
                if (!mainData.cameraStack.Contains(weaponCamera))
                {
                    mainData.cameraStack.Add(weaponCamera);
                }
            }
        }
    }

    private void Start()
    {
        if (targetCamera != null && femaleAnimator != null && weapons != null)
        {
            animatedRig = new AnimatedFirstPersonRig(femaleAnimator, weapons, targetCamera);
            RebuildViewModel();
        }
        else if (viewModel == null) RebuildViewModel();
    }
    private void OnAttack()
    {
        if (animatedRig != null) animatedRig.PlayAttack();
        else kick = weapons.Current != null && weapons.Current.melee ? 0.12f : 0.04f;
    }
    private void OnQuickMelee()
    {
        if (animatedRig != null) animatedRig.PlayQuickMelee();
        else kick = .10f;
    }
    private void RebuildViewModel()
    {
        if (targetCamera == null || weapons == null || weapons.Current == null) return;
        if (femaleAnimator != null)
        {
            if (animatedRig == null) return;
            animatedRig.SetWeapon();
            viewMuzzle = animatedRig.Muzzle;
            ConfigureMuzzle();
            return;
        }
        if (viewModel != null) Destroy(viewModel);
        viewModel = new GameObject("FirstPersonWeapon");
        viewModel.transform.SetParent(targetCamera.transform, false);
        var model = weapons.Current.model;
        foreach (var source in model.GetComponentsInChildren<MeshFilter>(true))
        {
            var renderer = source.GetComponent<MeshRenderer>();
            if (renderer == null) continue;
            var part = new GameObject(source.name);
            part.transform.SetParent(viewModel.transform, false);
            part.transform.localPosition = model.transform.InverseTransformPoint(source.transform.position);
            part.transform.localRotation = Quaternion.Inverse(model.transform.rotation) * source.transform.rotation;
            Vector3 scale = source.transform.lossyScale, rootScale = model.transform.lossyScale;
            part.transform.localScale = new Vector3(scale.x/rootScale.x, scale.y/rootScale.y, scale.z/rootScale.z);
            part.AddComponent<MeshFilter>().sharedMesh = source.sharedMesh;
            var copy = part.AddComponent<MeshRenderer>(); copy.sharedMaterials = renderer.sharedMaterials;
            copy.shadowCastingMode = ShadowCastingMode.Off; copy.receiveShadows = false;
        }
        bool melee = weapons.Current.melee;
        viewModel.transform.localScale = Vector3.one * (melee ? 0.55f : weapons.Current.automatic ? 0.5f : 0.65f);
        AnimatedFirstPersonRig.SetLayerRecursively(viewModel, 6);
        viewMuzzle = new GameObject("FirstPersonMuzzle").transform;
        viewMuzzle.SetParent(viewModel.transform, false);
        // Weapon art faces +Z. Pistol and rifle meshes are 0.78m and 1.64m long.
        viewMuzzle.localPosition = new Vector3(0, 0.08f, weapons.Current.automatic ? 1.05f : 0.46f);
        ConfigureMuzzle();
    }

    private void ConfigureMuzzle()
    {
        var slot = weapons.Current;
        float multiplier = ContractGame.Instance != null ? ContractGame.Instance.Progress.DamageMultiplier : 1f;
        GetComponent<PlayerShoot>().Configure(viewMuzzle, slot.damage * multiplier, slot.cooldown, slot.range, !slot.melee);
    }

    private void LateUpdate()
    {
        if (targetCamera == null) return;
        if (weaponCamera != null)
        {
            weaponCamera.fieldOfView = targetCamera.fieldOfView;
        }
        float height = controller != null ? controller.height : 2f;
        targetCamera.transform.position = transform.position + Vector3.up * Mathf.Max(0.65f, height - 0.18f);
        targetCamera.transform.rotation = Quaternion.Euler(look != null ? look.Pitch : 0, look != null ? look.Yaw : transform.eulerAngles.y, 0);
        if (animatedRig != null)
        {
            animatedRig.UpdatePose(animatedArmsOffset, Mouse.current != null && Mouse.current.rightButton.isPressed);
            return;
        }
        if (viewModel == null) return;
        kick = Mathf.MoveTowards(kick, 0, Time.deltaTime * 0.6f);
        viewModel.transform.localPosition = new Vector3(0.24f, -0.23f, 0.32f - kick);
        viewModel.transform.localRotation = Quaternion.Euler(weapons.IsReloading ? 25 : -kick * 80f, weapons.Current.melee ? -15 : 0, weapons.IsReloading ? -20 : 0);
        viewModel.SetActive(ContractGame.ControlsAllowed);
    }

    private void OnDestroy()
    {
        animatedRig?.Dispose();
        if (weapons != null) { weapons.Changed -= RebuildViewModel; weapons.Attacked -= OnAttack; weapons.QuickMeleeStarted -= OnQuickMelee; }
        if (viewModel != null) Destroy(viewModel);
        if (targetCamera != null && weaponCamera != null)
        {
            var mainData = targetCamera.GetUniversalAdditionalCameraData();
            if (mainData != null && mainData.cameraStack != null)
                mainData.cameraStack.Remove(weaponCamera);
        }
    }
}
