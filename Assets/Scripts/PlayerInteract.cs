using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
    private Camera cam;
    [SerializeField]
    private float distance = 3f;
    [SerializeField]
    private LayerMask mask = ~0; // Default to all layers unless configured
    private PlayerUI playerUI;
    private Interactable currentInteractable;

    void Awake()
    {
        cam = GetComponentInChildren<Camera>();
        if (cam == null && Camera.main != null)
        {
            cam = Camera.main;
        }

        playerUI = GetComponent<PlayerUI>();
        if (playerUI == null)
        {
            playerUI = GetComponentInChildren<PlayerUI>();
        }

        if (playerUI == null)
        {
            playerUI = Object.FindFirstObjectByType<PlayerUI>();
        }
    }

    void Update()
    {
        if (cam == null)
        {
            return;
        }

        // Raycast from the center of camera viewport
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit hitInfo;

        if (Physics.Raycast(ray, out hitInfo, distance, mask))
        {
            Interactable interactable = hitInfo.collider.GetComponent<Interactable>();
            if (interactable != null)
            {
                currentInteractable = interactable;
                if (playerUI != null)
                {
                    playerUI.UpdateText(interactable.promptMessage);
                }
                return;
            }
        }

        // Nothing interactable in front
        currentInteractable = null;
        if (playerUI != null)
        {
            playerUI.UpdateText(string.Empty);
        }
    }

    public void ProcessInteract()
    {
        if (currentInteractable != null)
        {
            currentInteractable.BaseInteract();
        }
    }
}
