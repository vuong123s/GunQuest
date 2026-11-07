using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
    [SerializeField] private float distance = 3f;
    [SerializeField] private LayerMask mask = ~0;

    private Camera cam;
    private Interactable current;

    private void Awake() => cam = GetComponentInChildren<Camera>() ?? Camera.main;

    private void Update()
    {
        current = null;
        if (cam != null && Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, distance, mask))
            current = hit.collider.GetComponentInParent<Interactable>();
    }

    public void ProcessInteract() => current?.BaseInteract();
}
