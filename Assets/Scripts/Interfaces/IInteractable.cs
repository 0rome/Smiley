using UnityEngine;

public interface IInteractable
{
    string InteractableName { get; }
    void OnLookEnter();
    void OnLookExit();
    void Interact();
}
