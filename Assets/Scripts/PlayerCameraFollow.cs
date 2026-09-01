using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(10000)]
public class PlayerCameraFollow : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Vector3 pivotOffset = new Vector3(0f, 1.45f, 0f);
    [SerializeField] private float defaultDistance = 4.6f;
    [SerializeField] private float minDistance = 1.15f;
    [SerializeField] private float maxDistance = 7f;
    [SerializeField] private float zoomSpeed = 0.015f;
    [SerializeField] private float followSharpness = 18f;
    [SerializeField] private float collisionRadius = 0.22f;
    [SerializeField] private float collisionPadding = 0.08f;
    [SerializeField] private float obstructionLift = 0.75f;
    [SerializeField] private LayerMask collisionMask = ~0;

    private PlayerLook playerLook;
    private float currentDistance;

    public Camera TargetCamera => targetCamera;

    private void Awake()
    {
        playerLook = GetComponent<PlayerLook>();
        ResolveCamera();
        currentDistance = Mathf.Clamp(defaultDistance, minDistance, maxDistance);

        if (targetCamera != null && targetCamera.transform.IsChildOf(transform))
        {
            targetCamera.transform.SetParent(null, true);
        }
    }

    private void LateUpdate()
    {
        ResolveCamera();
        if (targetCamera == null)
        {
            return;
        }

        UpdateZoom();

        float yaw = playerLook != null ? playerLook.Yaw : transform.eulerAngles.y;
        float pitch = playerLook != null ? playerLook.Pitch : 12f;
        Quaternion orbitRotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 pivot = transform.position + pivotOffset;
        Vector3 desiredPosition = pivot + orbitRotation * Vector3.back * currentDistance;
        Vector3 cameraPosition = ResolveCollision(pivot, desiredPosition);
        float blend = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);

        targetCamera.transform.SetPositionAndRotation(
            Vector3.Lerp(targetCamera.transform.position, cameraPosition, blend),
            Quaternion.Slerp(targetCamera.transform.rotation, orbitRotation, blend));
    }

    private void UpdateZoom()
    {
        float zoomInput = 0f;
        if (Mouse.current != null)
        {
            zoomInput += Mouse.current.scroll.ReadValue().y;
        }

        if (Touchscreen.current != null)
        {
            var touches = Touchscreen.current.touches;
            if (touches.Count >= 2 && touches[0].press.isPressed && touches[1].press.isPressed)
            {
                Vector2 current = touches[0].position.ReadValue() - touches[1].position.ReadValue();
                Vector2 previous = current - (touches[0].delta.ReadValue() - touches[1].delta.ReadValue());
                zoomInput += current.magnitude - previous.magnitude;
            }
        }

        currentDistance = Mathf.Clamp(currentDistance - zoomInput * zoomSpeed, minDistance, maxDistance);
    }

    private Vector3 ResolveCollision(Vector3 pivot, Vector3 desiredPosition)
    {
        Vector3 castDirection = desiredPosition - pivot;
        float castDistance = castDirection.magnitude;
        if (castDistance <= 0.001f)
        {
            return desiredPosition;
        }

        RaycastHit[] hits = Physics.SphereCastAll(
            pivot,
            collisionRadius,
            castDirection / castDistance,
            castDistance,
            collisionMask,
            QueryTriggerInteraction.Ignore);

        float closestDistance = castDistance;
        foreach (RaycastHit hit in hits)
        {
            if (hit.transform == transform || hit.transform.IsChildOf(transform))
            {
                continue;
            }

            closestDistance = Mathf.Min(closestDistance, hit.distance);
        }

        if (closestDistance >= castDistance)
        {
            return desiredPosition;
        }

        float safeDistance = Mathf.Max(minDistance, closestDistance - collisionPadding);
        float blockedAmount = 1f - safeDistance / castDistance;
        Vector3 raisedPivot = pivot + Vector3.up * obstructionLift * blockedAmount;
        return raisedPivot + castDirection.normalized * safeDistance;
    }

    private void ResolveCamera()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera == null)
        {
            return;
        }

        foreach (MonoBehaviour behaviour in targetCamera.GetComponents<MonoBehaviour>())
        {
            if (behaviour == null)
            {
                continue;
            }

            string namespaceName = behaviour.GetType().Namespace;
            if (!string.IsNullOrEmpty(namespaceName) && namespaceName.Contains("Cinemachine"))
            {
                behaviour.enabled = false;
            }
        }
    }
}
