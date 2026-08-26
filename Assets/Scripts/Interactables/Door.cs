using UnityEngine;

public class Door : Interactable
{
    [SerializeField]
    private GameObject doorObject;
    private bool doorOpen;

    void Awake()
    {
        if (doorObject == null)
        {
            doorObject = gameObject;
        }
        UpdatePrompt();
    }

    protected override void Interact()
    {
        doorOpen = !doorOpen;
        if (doorObject.TryGetComponent<Animator>(out Animator animator))
        {
            animator.SetBool("IsOpen", doorOpen);
        }
        else
        {
            // Simple rotation toggle if no Animator attached
            doorObject.transform.Rotate(0f, doorOpen ? 90f : -90f, 0f);
        }

        UpdatePrompt();
    }

    private void UpdatePrompt()
    {
        promptMessage = doorOpen ? "Press E to Close Door" : "Press E to Open Door";
    }
}
