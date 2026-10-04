using UnityEngine;

public class Keypad : Interactable
{
    [SerializeField] private GameObject targetDoor;

    private void Awake() => promptMessage = "Press E to Use Keypad";

    protected override void Interact()
    {
        if (targetDoor == null) return;
        Door door = targetDoor.GetComponent<Door>();
        if (door != null) door.BaseInteract();
        else if (targetDoor.TryGetComponent<Animator>(out Animator animator)) animator.SetBool("IsOpen", true);
        else targetDoor.SetActive(!targetDoor.activeSelf);
    }
}
