using UnityEngine;

public class Door : Interactable
{
    [SerializeField] private GameObject doorObject;
    private bool isOpen;

    private void Awake()
    {
        doorObject ??= gameObject;
        UpdatePrompt();
    }

    protected override void Interact()
    {
        isOpen = !isOpen;
        Animator animator = doorObject.GetComponent<Animator>();
        if (animator != null) animator.SetBool("IsOpen", isOpen);
        else doorObject.transform.Rotate(0f, isOpen ? 90f : -90f, 0f);
        UpdatePrompt();
    }

    private void UpdatePrompt() => promptMessage = isOpen ? "Press E to Close Door" : "Press E to Open Door";
}
