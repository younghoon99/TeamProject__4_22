using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Kinnly;
public class NPCInventory : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] Kinnly.PlayerInventory playerInventory;
    [SerializeField] Npc npc;
    [SerializeField] Image slotImage;
    [SerializeField] GameObject inventoryItemPrefab;

    private Item currentItem;
    private GameObject currentItemObject;

    Color32 defaultColor = new Color32(255, 255, 255, 255);
    Color32 selectedColor = new Color32(255, 161, 161, 128);

    void Start()
    {
        if (slotImage == null)
        {
            slotImage = GetComponent<Image>();
        }

        if (playerInventory == null)
        {
            playerInventory = FindObjectOfType<PlayerInventory>();
        }

        if (npc == null)
        {
            npc = GetComponentInParent<Npc>();
        }
    }

    public void AddItem(Item item)
    {
        if (item != null)
        {
            if (currentItem != null)
            {
                RemoveItem();
            }
            else if (npc != null)
            {
                npc.SetTask(Npc.NpcTask.None);
            }

            currentItem = item;
            SpawnInventoryItem(item, 1);
            SetNpcTaskBasedOnItem(item);
        }
    }

    private void SpawnInventoryItem(Item item, int amount)
    {
        if (currentItemObject != null)
        {
            Destroy(currentItemObject);
        }

        GameObject go = Instantiate(inventoryItemPrefab, transform);
        InventoryItem inventoryItem = go.GetComponent<InventoryItem>();

        if (inventoryItem != null)
        {
            inventoryItem.SetItem(item, amount);
            inventoryItem.IsDragging = false;
            inventoryItem.Amount = amount;
            go.GetComponent<Image>().raycastTarget = false;
            currentItemObject = go;
        }
        else
        {
            Destroy(go);
        }
    }

    private void SetNpcTaskBasedOnItem(Item item)
    {
        if (npc == null) return;

        if (item.isSword)
        {
            npc.SetTask(Npc.NpcTask.Combat);
        }
        else if (item.isAxe)
        {
            npc.SetTask(Npc.NpcTask.Woodcutting);
        }
        else if (item.isPickaxe)
        {
            npc.SetTask(Npc.NpcTask.Mining);
        }
    }

    public Item GetCurrentItem()
    {
        return currentItem;
    }

    public void RemoveItem()
    {
        if (currentItemObject != null)
        {
            Destroy(currentItemObject);
            currentItemObject = null;
        }

        currentItem = null;

        if (npc != null)
        {
            npc.SetTask(Npc.NpcTask.None);
        }
    }

    public void MoveItemToPlayerInventory()
    {
        if (currentItem == null || playerInventory == null) return;
        npc.animator.SetTrigger("CancelMining");

        playerInventory.AddItem(currentItem, 1);
        RemoveItem();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (playerInventory != null)
        {
            playerInventory.CurrentlyHoveredInventorySlot = this.gameObject;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (playerInventory != null)
        {
            playerInventory.CurrentlyHoveredInventorySlot = null;
        }
    }
}
