using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
    [SerializeField] private float distance = 3f;
    [SerializeField] private LayerMask mask = ~0;

    private Camera cam;
    private PlayerUI playerUI;
    private Interactable current;

    private void Awake()
    {
        cam = GetComponentInChildren<Camera>() ?? Camera.main;
        playerUI = GetComponentInChildren<PlayerUI>() ?? Object.FindFirstObjectByType<PlayerUI>();
    }

    private void Update()
    {
        current = null;
        if (cam != null && Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, distance, mask))
            current = hit.collider.GetComponentInParent<Interactable>();
        playerUI?.UpdateText(current != null ? current.promptMessage : string.Empty);
    }

    public void ProcessInteract() => current?.BaseInteract();
}
