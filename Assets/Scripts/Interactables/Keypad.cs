using UnityEngine;

public class Keypad : Interactable
{
    [SerializeField]
    private GameObject targetDoor;
    private bool isOpen;

    void Awake()
    {
        promptMessage = "Press E to Use Keypad";
    }

    protected override void Interact()
    {
        isOpen = !isOpen;
        if (targetDoor != null)
        {
            if (targetDoor.TryGetComponent<Door>(out Door doorComponent))
            {
                doorComponent.BaseInteract();
            }
            else if (targetDoor.TryGetComponent<Animator>(out Animator animator))
            {
                animator.SetBool("IsOpen", isOpen);
            }
            else
            {
                targetDoor.SetActive(!targetDoor.activeSelf);
            }
        }
    }
}
