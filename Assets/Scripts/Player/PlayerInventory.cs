using UnityEngine;
using System;
using Zenject;

public class PlayerInventory : MonoBehaviour
{
    [Header("Item settings")]
    [SerializeField] private int maxItemSlots = 3;
    [SerializeField] private ItemBase[] itemSlots;
    [SerializeField] private ItemBase itemInHand;
    [SerializeField] private Transform itemInHandTransform;

    [Inject] private ScreenClueManager screenClueManager;

    private SpringJoint handJoint;

    public ItemBase[] ItemSlots => itemSlots;
    public ItemBase ItemInHand => itemInHand;

    // Событие: инвентарь изменился (подбор, выбор, выброс)
    public event Action OnInventoryChanged;

    private void Awake()
    {
        if (itemSlots == null || itemSlots.Length != maxItemSlots)
            itemSlots = new ItemBase[maxItemSlots];

        handJoint = itemInHandTransform.GetComponent<SpringJoint>();
        if (handJoint == null)
            handJoint = itemInHandTransform.gameObject.AddComponent<SpringJoint>();
    }

    private void Update() => ItemInput();

    private void ItemInput()
    {
        for (int i = 0; i < itemSlots.Length && i < 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                ChooseItem(i);
        }

        if (Input.GetKeyDown(KeyCode.G))
            DropItem();
    }

    /// <summary>
    /// Выбрасывает предмет из руки и поднимает в руку следующий по порядку предмет из слотов, если он есть.
    /// </summary>
    private void DropItem()
    {
        if (itemInHand == null)
        {
            Debug.Log("Нечего выбрасывать — рука пуста");
            return;
        }

        var dropped = itemInHand;

        // Отцепляем от джоинта
        DetachFromJoint(dropped);

        // Возвращаем предмету физику и отпускаем в мир
        dropped.transform.SetParent(null);
        dropped.gameObject.SetActive(true);

        itemInHand = null;

        // Ищем следующий по порядку предмет в слотах
        for (int i = 0; i < itemSlots.Length; i++)
        {
            if (itemSlots[i] == null) continue;

            var next = itemSlots[i];
            itemSlots[i] = null;

            itemInHand = next;
            itemInHand.gameObject.SetActive(true);
            AttachToHand(itemInHand);
            OnInventoryChanged?.Invoke();
            break;
        }
    }

    private void ChooseItem(int itemIndex)
    {
        if (itemIndex < 0 || itemIndex >= itemSlots.Length) return;

        var selected = itemSlots[itemIndex];
        if (selected == null)
        {
            Debug.Log("Нет предмета: " + itemIndex);
            return;
        }

        // Прячем все остальные предметы в слотах
        foreach (var item in itemSlots)
            if (item != null && item != selected)
                item.gameObject.SetActive(false);

        var previousInHand = itemInHand;

        // Отцепляем текущий предмет от джоинта, прежде чем менять
        if (previousInHand != null)
            DetachFromJoint(previousInHand);

        itemSlots[itemIndex] = null;
        itemInHand = selected;
        itemInHand.gameObject.SetActive(true);
        AttachToHand(itemInHand);

        if (previousInHand != null)
        {
            previousInHand.gameObject.SetActive(false);
            previousInHand.transform.SetParent(null);
            itemSlots[itemIndex] = previousInHand;
        }

        OnInventoryChanged?.Invoke();
    }

    public bool PickItem(ItemBase newItem)
    {
        if (newItem == itemInHand)
            return false;

        foreach (var item in itemSlots)
        {
            if (item == newItem)
                return false;
        }

        if (itemInHand == null)
        {
            itemInHand = newItem;
            AttachToHand(newItem);
            newItem.gameObject.SetActive(true);
            return true;
        }

        for (int i = 0; i < itemSlots.Length; i++)
        {
            if (itemSlots[i] != null) continue;

            // Отцепляем текущий предмет от джоинта и убираем в слот
            DetachFromJoint(itemInHand);
            itemInHand.gameObject.SetActive(false);
            itemInHand.transform.SetParent(null);
            itemSlots[i] = itemInHand;

            itemInHand = newItem;
            AttachToHand(newItem);
            itemInHand.gameObject.SetActive(true);
            OnInventoryChanged?.Invoke();
            return true;
        }

        screenClueManager.ShowClue("You can't carry any more");
        return false;
    }

    /// <summary>
    /// Привязывает предмет к руке через FixedJoint.
    /// </summary>
    private void AttachToHand(ItemBase item)
    {
        // Выставляем позицию/поворот относительно руки
        item.transform.SetParent(itemInHandTransform);
        item.transform.localPosition = Vector3.zero;
        item.transform.localRotation = Quaternion.identity;

        var itemBody = item.GetComponent<Rigidbody>();
        if (itemBody == null)
        {
            Debug.LogWarning($"[PlayerInventory] У предмета '{item.name}' нет Rigidbody — джоинт не сработает.");
            return;
        }

        // Привязываем Rigidbody предмета к джоинту руки
        handJoint.connectedBody = itemBody;
        handJoint.autoConfigureConnectedAnchor = true;
        handJoint.connectedMassScale = 1f;
        handJoint.massScale = 1f;

        OnInventoryChanged?.Invoke();
    }

    /// <summary>
    /// Отцепляет предмет от джоинта.
    /// </summary>
    private void DetachFromJoint(ItemBase item)
    {
        if (handJoint != null && handJoint.connectedBody == item.GetComponent<Rigidbody>())
            handJoint.connectedBody = null;

        // Возвращаем предмету нормальную физику
        var itemBody = item.GetComponent<Rigidbody>();
        if (itemBody != null)
        {
            itemBody.isKinematic = false;
            itemBody.useGravity = true;
        }
    }
}