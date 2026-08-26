using UnityEngine;
using UnityEngine.Events;

public abstract class Interactable : MonoBehaviour
{
    // Toggle whether this interactable uses UnityEvents configured in Inspector
    public bool useEvents;

    // Message displayed to player when looking at the object (e.g. "Press E to open door")
    [SerializeField]
    public string promptMessage = "Interact";

    [SerializeField]
    public UnityEvent onInteract;

    // Template method called by PlayerInteract
    public void BaseInteract()
    {
        if (useEvents)
        {
            if (onInteract != null)
            {
                onInteract.Invoke();
            }
        }
        Interact();
    }

    protected virtual void Interact()
    {
        // To be overridden by subclasses
    }
}
