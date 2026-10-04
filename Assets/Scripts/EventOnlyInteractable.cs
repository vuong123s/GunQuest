using UnityEngine;

public class EventOnlyInteractable : Interactable
{
    private void Reset()
    {
        useEvents = true;
    }
}
