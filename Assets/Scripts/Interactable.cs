using UnityEngine;
using UnityEngine.Events;

public abstract class Interactable : MonoBehaviour
{
    public bool useEvents;
    public string promptMessage = "Interact";
    public UnityEvent onInteract;

    public void BaseInteract()
    {
        if (useEvents) onInteract?.Invoke();
        Interact();
    }

    protected virtual void Interact() { }
}
