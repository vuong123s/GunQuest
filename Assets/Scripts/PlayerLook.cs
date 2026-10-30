using UnityEngine;

public class PlayerLook : MonoBehaviour
{
    public Camera cam;
    [Min(0.01f)] public float xSensitivity = 0.12f;
    [Min(0.01f)] public float ySensitivity = 0.12f;
    public bool invertY = false;

    private float pitch;
    private float yaw;
    public float Pitch => pitch;
    public float Yaw => yaw;

    private void Awake()
    {
        if (cam == null) cam = Camera.main;
        yaw = transform.eulerAngles.y;

        if (GameSettings.Instance != null)
        {
            xSensitivity = GameSettings.Instance.mouseSensitivity;
            ySensitivity = GameSettings.Instance.mouseSensitivity;
            invertY = GameSettings.Instance.invertY;
        }
    }

    public void ProcessLook(Vector2 input)
    {
        if (!ContractGame.ControlsAllowed) return;
        // Mouse delta is already per-frame; do not multiply by deltaTime.
        float sx = xSensitivity > 5f ? 0.12f : xSensitivity;
        float sy = ySensitivity > 5f ? 0.12f : ySensitivity;
        yaw += input.x * sx;
        float lookY = invertY ? (input.y * sy) : (-input.y * sy);
        pitch = Mathf.Clamp(pitch + lookY, -85f, 85f);
        transform.rotation = Quaternion.Euler(0, yaw, 0);
    }

    public void ApplyRecoil(Vector2 recoil)
    {
        pitch = Mathf.Clamp(pitch + recoil.y, -85f, 85f);
        yaw += recoil.x;
        transform.rotation = Quaternion.Euler(0, yaw, 0);
    }
}
