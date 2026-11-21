using UnityEngine;
using UnityEngine.UI;
using Zenject;

public abstract class ItemBase : InteractableBase
{
    [Header("Audio")]
    [SerializeField] private AudioSource itemAudioSource;
    [SerializeField] private AudioClip takeItemClip;

    [Header("UI")]
    [SerializeField] private Sprite itemImage;

    [Inject] private PlayerInventory playerInventory;

    public Sprite ItemImage => itemImage;

    public override void Interact()
    {
        playerInventory.PickItem(this);

        itemAudioSource.PlayOneShot(takeItemClip);
    }
    public abstract void UseItem();
}
