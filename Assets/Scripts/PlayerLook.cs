using UnityEngine;
public class PlayerLook : MonoBehaviour
{
    public Camera cam;
    [Min(0.01f)] public float xSensitivity = 0.12f;
    [Min(0.01f)] public float ySensitivity = 0.12f;
    private float pitch;
    private float yaw;
    public float Pitch => pitch;
    public float Yaw => yaw;
    private void Awake()
    {
        if (cam == null) cam = Camera.main;
        yaw = transform.eulerAngles.y;
    }
    public void ProcessLook(Vector2 input)
    {
        if (!ContractGame.ControlsAllowed) return;
        // Mouse delta is already per-frame; do not multiply by deltaTime.
        // Migrate old third-person sensitivity values on existing prefabs.
        float sx = xSensitivity > 5f ? 0.12f : xSensitivity;
        float sy = ySensitivity > 5f ? 0.12f : ySensitivity;
        yaw += input.x * sx;
        pitch = Mathf.Clamp(pitch - input.y * sy, -85f, 85f);
        transform.rotation = Quaternion.Euler(0, yaw, 0);
    }
}
