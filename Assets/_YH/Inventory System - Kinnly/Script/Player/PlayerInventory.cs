using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Kinnly
{
    public class PlayerInventory : MonoBehaviour
    {
        [Header("Core")]
        [SerializeField] GameObject inventoryUI;
        [SerializeField] GameObject toolbarUI;
        [SerializeField] List<GameObject> inventorySlot = new List<GameObject>();
        [SerializeField] List<GameObject> toolbarSlot = new List<GameObject>();

        [Header("Prefabs")]
        [SerializeField] GameObject inventoryItem;
        [SerializeField] GameObject toolbarItem;
        [SerializeField] GameObject itemDrop;

        [Header("Config")]
        public int MaxAmount;

        [HideInInspector] public GameObject CurrentlyHoveredInventorySlot;
        [HideInInspector] public GameObject CurrentlyHoveredToolbarSlot;
        [HideInInspector] public InventoryItem CurrentlySelectedInventoryItem;
        [HideInInspector] public int CurrentlySelectedToolBar;
        [HideInInspector] public bool IsHoveringDropBox;
        [HideInInspector] public bool IsHoveringTrashcan;
        [HideInInspector] public bool IsDragging;
        [HideInInspector] public bool IsClicking;

        private DialogBox dialogBox;

        // Start is called before the first frame update
        void Start()
        {
            MaxAmount = 999;

            CurrentlySelectedToolBar = 0;
            dialogBox = DialogBox.instance;
        }

        // Update is called once per frame
        void Update()
        {
            //Num Key to switch selected Toolbar
            int keyNumber = GetKeyNumber();
            if (keyNumber != -1)
            {
                CurrentlySelectedToolBar = keyNumber;
            }

            //Mouse Scroll to switch selected Toolbar
            if (Input.mouseScrollDelta.y < 0)
            {
                CurrentlySelectedToolBar += 1;
                if (CurrentlySelectedToolBar > 11)
                {
                    CurrentlySelectedToolBar -= 12;
                }
            }
            else if (Input.mouseScrollDelta.y > 0)
            {
                CurrentlySelectedToolBar -= 1;
                if (CurrentlySelectedToolBar < 0)
                {
                    CurrentlySelectedToolBar += 12;
                }
            }

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                ToggleInventory();
            }

            if (Input.GetMouseButtonUp(0))
            {
                IsClicking = false;
            }

            UpdateCurrentlySelectedItem();
        }

        public void ToggleInventory()
        {
            if (IsDragging)
            {
                return;
            }

            if (inventoryUI.activeInHierarchy)
            {
                inventoryUI.SetActive(false);
                toolbarUI.SetActive(true);
                UpdateToolbar();
            }
            else
            {
                inventoryUI.SetActive(true);
                toolbarUI.SetActive(false);
            }
        }

        public void AddItem(Item item, int amount)
        {
            foreach (GameObject slot in inventorySlot)
            {
                if (slot.gameObject.transform.childCount >= 1)
                {
                    InventoryItem inventoryItem = slot.gameObject.GetComponentInChildren<InventoryItem>();
                    if (inventoryItem.Item.name == item.name && inventoryItem.Item.isStackable && inventoryItem.Amount < MaxAmount)
                    {
                        int total = inventoryItem.Amount + amount;
                        if (total <= MaxAmount)
                        {
                            inventoryItem.AddAmount(amount);
                            UpdateToolbar();
                            return;
                        }
                        else
                        {
                            inventoryItem.AddAmount(amount - (total - MaxAmount));
                            SpawnItemDrop(item, amount - (amount - (total - MaxAmount)));
                            UpdateToolbar();
                            return;
                        }
                    }
                }
            }

            foreach (GameObject slot in inventorySlot)
            {
                if (slot.gameObject.transform.childCount <= 0)
                {
                    GameObject go = Instantiate(inventoryItem, slot.transform);
                    go.GetComponent<InventoryItem>().SetItem(item, amount);
                    UpdateToolbar();
                    return;
                }
            }

            SpawnItemDrop(item, amount);
        }

        public void RemoveItem(InventoryItem inventoryItem, int amount)
        {
            inventoryItem.RemoveAmount(amount);
            UpdateToolbar();
        }

        public bool IsSlotAvailable(Item item, int amount)
        {
            foreach (GameObject slot in inventorySlot)
            {
                if (slot.gameObject.transform.childCount >= 1)
                {
                    InventoryItem inventoryItem = slot.gameObject.GetComponentInChildren<InventoryItem>();
                    if (inventoryItem.Item.name == item.name && inventoryItem.Item.isStackable && inventoryItem.Amount < MaxAmount)
                    {
                        return true;
                    }
                }
            }

            foreach (GameObject slot in inventorySlot)
            {
                if (slot.gameObject.transform.childCount <= 0)
                {
                    return true;
                }
            }

            dialogBox.Show("Inventory Full", 1f);
            return false;
        }

        public void SpawnItemDrop(Item item, int amount)
        {
            GameObject go = Instantiate(itemDrop, transform.position + new Vector3(RandomNumber(-3f, 3f, -1f, 1f), RandomNumber(-3f, 3f, -1f, 1f), 0f), transform.rotation);
            go.GetComponent<SpriteRenderer>().sprite = item.image;
            go.GetComponent<ItemDrop>().SetItem(item, amount);
        }

        public void SpawnInventoryItem(Item item, int amount)
        {
            GameObject go = Instantiate(inventoryItem, inventoryUI.transform.root);
            InventoryItem inventory = go.GetComponent<InventoryItem>();
            inventory.SetItem(item, amount);
            inventory.IsDragging = true;
            inventory.Amount = amount;
            go.GetComponent<Image>().raycastTarget = false;
            this.IsDragging = true;
        }

        public void MoveItemBetweenSlots(int sourceSlotIndex, int targetSlotIndex)
        {
            // 소스 슬롯과 타겟 슬롯의 인벤토리 아이템 가져오기
            GameObject sourceSlot = inventorySlot[sourceSlotIndex];
            GameObject targetSlot = inventorySlot[targetSlotIndex];

            InventoryItem sourceItem = sourceSlot.GetComponentInChildren<InventoryItem>();
            InventoryItem targetItem = targetSlot.GetComponentInChildren<InventoryItem>();

            // 타겟 슬롯에 아이템이 없는 경우
            if (targetItem == null)
            {
                // 소스 아이템을 타겟 슬롯으로 이동
                if (sourceItem != null)
                {
                    sourceItem.transform.SetParent(targetSlot.transform);
                    UpdateToolbar();
                }
            }
            // 타겟 슬롯에 아이템이 있는 경우
            else
            {
                // 같은 아이템이면 합치기 시도
                if (sourceItem != null && targetItem.Item.name == sourceItem.Item.name && targetItem.Item.isStackable)
                {
                    int totalAmount = targetItem.Amount + sourceItem.Amount;
                    if (totalAmount <= MaxAmount)
                    {
                        // 모두 합칠 수 있는 경우
                        targetItem.SetAmount(totalAmount);
                        Destroy(sourceItem.gameObject);
                    }
                    else
                    {
                        // 일부만 합칠 수 있는 경우
                        targetItem.SetAmount(MaxAmount);
                        sourceItem.SetAmount(totalAmount - MaxAmount);
                    }
                }
                // 다른 아이템이면 위치 교체
                else if (sourceItem != null)
                {
                    // 임시로 부모 보관
                    Transform sourceParent = sourceItem.transform.parent;
                    Transform targetParent = targetItem.transform.parent;

                    // 부모 교체
                    sourceItem.transform.SetParent(targetParent);
                    targetItem.transform.SetParent(sourceParent);
                }
            }

            UpdateToolbar();
        }

        private float RandomNumber(float minRange, float maxRange, float minExclude, float maxExclude)
        {
            float randomValue;
            do
            {
                randomValue = Random.Range(minRange, maxRange);
            }
            while (randomValue <= minExclude && randomValue >= maxExclude);

            return randomValue;
        }

        private int GetKeyNumber()
        {
            for (int i = 0; i < 9; i++)
            {
                if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + i)))
                {
                    return i;
                }
            }
            if (Input.GetKeyDown(KeyCode.Alpha0))
            {
                return 9;
            }
            if (Input.GetKeyDown(KeyCode.Minus))
            {
                return 10;
            }
            if (Input.GetKeyDown(KeyCode.Equals))
            {
                return 11;
            }
            return -1;
        }

        public void UpdateToolbar()
        {
            for (int i = 0; i < toolbarSlot.Count; i++)
            {
                ToolbarItem[] components = toolbarSlot[i].GetComponentsInChildren<ToolbarItem>();
                foreach (var component in components)
                {
                    Destroy(component.gameObject);
                }

                if (inventorySlot[i].gameObject.transform.childCount >= 1)
                {
                    GameObject go = Instantiate(toolbarItem, toolbarSlot[i].transform);
                    InventoryItem inventoryItem = inventorySlot[i].GetComponentInChildren<InventoryItem>();
                    go.GetComponent<ToolbarItem>().SetItem(inventoryItem.Item, inventoryItem.Amount);
                }
            }
            UpdateCurrentlySelectedItem();
        }

        private void UpdateCurrentlySelectedItem()
        {
            try
            {
                CurrentlySelectedInventoryItem = inventorySlot[CurrentlySelectedToolBar].GetComponentInChildren<InventoryItem>();
            }
            catch
            {
                CurrentlySelectedInventoryItem = null;
            }
        }

        // 인벤토리 슬롯 개수 반환 메서드
        public int GetInventorySlotCount()
        {
            return inventorySlot.Count;
        }

        // 특정 인덱스의 인벤토리 슬롯 반환 메서드
        public GameObject GetInventorySlot(int index)
        {
            if (index >= 0 && index < inventorySlot.Count)
            {
                return inventorySlot[index];
            }
            return null;
        }
    }
}