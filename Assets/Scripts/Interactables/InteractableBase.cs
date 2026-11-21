using UnityEngine;

public abstract class InteractableBase : MonoBehaviour, IInteractable
{
    private string interactableName;
    public string InteractableName => interactableName;

    private  void Awake()
    {
        interactableName = gameObject.name;
    }

    public virtual void OnLookEnter()
    {
       
    }

    public virtual void OnLookExit()
    {
       
    }

    
    
    public abstract void Interact();
}
