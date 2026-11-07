using UnityEngine;
using UnityEngine.Rendering.Universal;

[DefaultExecutionOrder(20)]
public class PlayerCameraFollow : MonoBehaviour
{
    private const int ViewModelLayer = 30;

    [SerializeField] private Transform target;
    [SerializeField] private Vector3 headOffset = new Vector3(0f, 1.58f, 0.08f);
    [SerializeField] private Vector3 shoulderOffset = Vector3.zero;
    [SerializeField] private Vector3 aimOffset = new Vector3(0f, 1.56f, 0.1f);
    [SerializeField] private float aimFov = 58f;
    [SerializeField] private float fovSharpness = 12f;

    private Camera targetCamera;
    private Camera weaponCamera;
    private PlayerLook look;
    private AnimatedFirstPersonRig femaleRig;
    private bool aiming;
    private float baseFov = 60f;

    public Camera TargetCamera => targetCamera;
    public Camera WeaponCamera => weaponCamera;

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        look = target != null ? target.GetComponent<PlayerLook>() : null;
        SetupCharacterVisuals();
        SnapToTarget();
    }

    public void SetAiming(bool value) => aiming = value;

    public void Kick(float amount = 1f)
    {
        if (femaleRig != null) femaleRig.Kick(amount);
    }

    private void Awake()
    {
        targetCamera = GetComponent<Camera>();
        if (targetCamera != null) baseFov = targetCamera.fieldOfView;
        if (target == null)
        {
            PlayerMotor motor = Object.FindFirstObjectByType<PlayerMotor>();
            if (motor != null) target = motor.transform;
        }

        look = target != null ? target.GetComponent<PlayerLook>() : null;
        SetupCharacterVisuals();
        SnapToTarget();
    }

    private void LateUpdate()
    {
        if (target == null) return;
        Quaternion rotation = GetViewRotation();
        Vector3 offset = headOffset.sqrMagnitude > 0.001f ? headOffset : shoulderOffset;
        if (aiming) offset = aimOffset;
        transform.SetPositionAndRotation(target.position + rotation * offset, rotation);
        UpdateFov();
    }

    private void SnapToTarget()
    {
        if (target == null) return;
        Quaternion rotation = GetViewRotation();
        Vector3 offset = headOffset.sqrMagnitude > 0.001f ? headOffset : shoulderOffset;
        transform.SetPositionAndRotation(target.position + rotation * offset, rotation);
    }

    private Quaternion GetViewRotation()
    {
        if (look == null && target != null) look = target.GetComponent<PlayerLook>();
        return look != null ? Quaternion.Euler(look.Pitch, look.Yaw, 0f) : Quaternion.Euler(0f, target.eulerAngles.y, 0f);
    }

    private void SetupCharacterVisuals()
    {
        if (target == null) return;
        Transform femaleVisual = target.Find("DefaultFemale");
        if (femaleVisual == null) return;

        femaleRig = GetComponent<AnimatedFirstPersonRig>();
        if (femaleRig == null) femaleRig = gameObject.AddComponent<AnimatedFirstPersonRig>();
        EnsureWeaponOverlayCamera();
        femaleRig.Initialize(target, femaleVisual, ViewModelLayer);
    }

    private void EnsureWeaponOverlayCamera()
    {
        if (targetCamera == null) targetCamera = GetComponent<Camera>();
        if (targetCamera == null) return;

        targetCamera.nearClipPlane = 0.04f;
        targetCamera.cullingMask &= ~(1 << ViewModelLayer);

        if (weaponCamera == null)
        {
            Transform existing = transform.Find("WeaponOverlayCamera");
            GameObject camObj = existing != null ? existing.gameObject : new GameObject("WeaponOverlayCamera");
            camObj.transform.SetParent(transform, false);
            weaponCamera = camObj.GetComponent<Camera>();
            if (weaponCamera == null) weaponCamera = camObj.AddComponent<Camera>();
        }

        weaponCamera.clearFlags = CameraClearFlags.Depth;
        weaponCamera.cullingMask = 1 << ViewModelLayer;
        weaponCamera.nearClipPlane = 0.01f;
        weaponCamera.farClipPlane = 4f;
        weaponCamera.fieldOfView = targetCamera.fieldOfView;
        weaponCamera.depth = targetCamera.depth + 1f;

        var baseData = targetCamera.GetUniversalAdditionalCameraData();
        var overlayData = weaponCamera.GetUniversalAdditionalCameraData();
        if (baseData != null && overlayData != null)
        {
            overlayData.renderType = CameraRenderType.Overlay;
            if (!baseData.cameraStack.Contains(weaponCamera)) baseData.cameraStack.Add(weaponCamera);
        }
    }

    private void UpdateFov()
    {
        if (targetCamera == null) return;
        float targetFov = aiming ? aimFov : baseFov;
        targetCamera.fieldOfView = Mathf.Lerp(targetCamera.fieldOfView, targetFov, fovSharpness * Time.deltaTime);
        if (weaponCamera != null) weaponCamera.fieldOfView = targetCamera.fieldOfView;
    }
}
