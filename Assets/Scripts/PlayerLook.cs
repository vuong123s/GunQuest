using UnityEngine;

public class PlayerLook : MonoBehaviour
{
    public Camera cam;
    [Min(0.01f)] public float xSensitivity = 180f;
    [Min(0.01f)] public float ySensitivity = 140f;
    [SerializeField] private float minPitch = -35f;
    [SerializeField] private float maxPitch = 65f;

    private float xRotation = 12f;
    private float yRotation;

    public float Pitch => xRotation;
    public float Yaw => yRotation;

    private void Awake()
    {
        if (cam == null)
        {
            cam = GetComponentInChildren<Camera>();
        }

        if (cam == null)
        {
            cam = Camera.main;
        }

        yRotation = transform.eulerAngles.y;
        if (cam != null)
        {
            float cameraPitch = cam.transform.eulerAngles.x;
            xRotation = Mathf.Clamp(cameraPitch > 180f ? cameraPitch - 360f : cameraPitch, minPitch, maxPitch);
        }
    }

    public void ProcessLook(Vector2 input)
    {
        yRotation += input.x * xSensitivity * Time.deltaTime;
        xRotation = Mathf.Clamp(xRotation - input.y * ySensitivity * Time.deltaTime, minPitch, maxPitch);
    }
}
