using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class PlayerInventoryUI : MonoBehaviour
{
    [SerializeField] private Image[] itemImages;       // слоты 0..N-1

    [Inject] private PlayerInventory playerInventory;

    private void Start()
    {
        playerInventory.OnInventoryChanged += Refresh;
        Refresh();
    }

    private void OnDestroy()
    {
        if (playerInventory != null)
            playerInventory.OnInventoryChanged -= Refresh;
    }

    public void Refresh()
    {
        // Слоты
        var slots = playerInventory.ItemSlots;
        for (int i = 0; i < itemImages.Length; i++)
        {
            if (itemImages[i] == null) continue;

            ItemBase item = (i < slots.Length) ? slots[i] : null;
            SetImage(itemImages[i], item);
        }

        //// Рука
        //if (itemInHandImage != null)
        //    SetImage(itemInHandImage, playerInventory.ItemInHand);
    }

    private void SetImage(Image target, ItemBase item)
    {
        if (item != null && item.ItemImage != null)
        {
            target.sprite = item.ItemImage;
            target.enabled = true;
        }
        else
        {
            target.sprite = null;
            target.enabled = false;
        }
    }
}