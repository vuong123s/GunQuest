using UnityEngine;

public class EventOnlyInteractable : Interactable
{
    void Reset()
    {
        useEvents = true;
    }
}
