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
        [SerializeField] List<GameObject> inventorySlot = new List<GameObject>();

        [Header("Prefabs")]
        [SerializeField] GameObject inventoryItem;
        [SerializeField] GameObject itemDrop;

        [Header("Config")]
        public int MaxAmount;

        [HideInInspector] public GameObject CurrentlyHoveredInventorySlot;
        [HideInInspector] public InventoryItem CurrentlySelectedInventoryItem;
        [HideInInspector] public int CurrentlySelectedInventorySlot; // 현재 선택된 인벤토리 슬롯 번호
        [HideInInspector] public bool IsHoveringDropBox;
        [HideInInspector] public bool IsHoveringTrashcan;
        [HideInInspector] public bool IsDragging;
        [HideInInspector] public bool IsClicking;

        private DialogBox dialogBox;

        // Start is called before the first frame update
        void Start()
        {
            MaxAmount = 999;
            CurrentlySelectedInventorySlot = 0; // 기본값으로 첫 번째 슬롯 선택
            dialogBox = DialogBox.instance;

            // 인벤토리 슬롯 번호 초기화
            InitializeInventorySlotNumbers();
            
            // 인벤토리 UI 항상 활성화
            if (inventoryUI != null)
            {
                inventoryUI.SetActive(true);
            }
        }

        // 인벤토리 슬롯에 번호 할당
        private void InitializeInventorySlotNumbers()
        {
            for (int i = 0; i < inventorySlot.Count; i++)
            {
                InventorySlot slot = inventorySlot[i].GetComponent<InventorySlot>();
                if (slot != null)
                {
                    slot.slotNumber = i;
                }
            }
        }

        // Update is called once per frame
        void Update()
        {
            // 숫자키로 인벤토리 슬롯 선택
            int keyNumber = GetKeyNumber();
            if (keyNumber != -1)
            {
                CurrentlySelectedInventorySlot = keyNumber;
            }

            // 마우스 스크롤로 슬롯 선택
            if (Input.mouseScrollDelta.y < 0)
            {
                CurrentlySelectedInventorySlot += 1;
                if (CurrentlySelectedInventorySlot >= inventorySlot.Count)
                {
                    CurrentlySelectedInventorySlot = 0;
                }
            }
            else if (Input.mouseScrollDelta.y > 0)
            {
                CurrentlySelectedInventorySlot -= 1;
                if (CurrentlySelectedInventorySlot < 0)
                {
                    CurrentlySelectedInventorySlot = inventorySlot.Count - 1;
                }
            }

            if (Input.GetMouseButtonUp(0))
            {
                IsClicking = false;
            }

            UpdateCurrentlySelectedItem();
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
                            return;
                        }
                        else
                        {
                            inventoryItem.AddAmount(amount - (total - MaxAmount));
                            SpawnItemDrop(item, amount - (amount - (total - MaxAmount)));
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
                    return;
                }
            }

            SpawnItemDrop(item, amount);
        }

        public void RemoveItem(InventoryItem inventoryItem, int amount)
        {
            inventoryItem.RemoveAmount(amount);
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

        public void MoveItemBetweenSlots(GameObject from, GameObject to)
        {
            if (from.transform.childCount <= 0 || to.transform.childCount >= 1)
            {
                return;
            }

            from.transform.GetChild(0).transform.SetParent(to.transform);
        }

        private float RandomNumber(float min1, float max1, float min2, float max2)
        {
            float random = Random.Range(0f, 1f);
            if (random < 0.5f)
            {
                return Random.Range(min1, max1);
            }
            else
            {
                return Random.Range(min2, max2);
            }
        }

        private int GetKeyNumber()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
            {
                return 0;
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
            {
                return 1;
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
            {
                return 2;
            }
            else if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4))
            {
                return 3;
            }
            else if (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5))
            {
                return 4;
            }
            else if (Input.GetKeyDown(KeyCode.Alpha6) || Input.GetKeyDown(KeyCode.Keypad6))
            {
                return 5;
            }
            else if (Input.GetKeyDown(KeyCode.Alpha7) || Input.GetKeyDown(KeyCode.Keypad7))
            {
                return 6;
            }
            else if (Input.GetKeyDown(KeyCode.Alpha8) || Input.GetKeyDown(KeyCode.Keypad8))
            {
                return 7;
            }
            else if (Input.GetKeyDown(KeyCode.Alpha9) || Input.GetKeyDown(KeyCode.Keypad9))
            {
                return 8;
            }
            else if (Input.GetKeyDown(KeyCode.Alpha0) || Input.GetKeyDown(KeyCode.Keypad0))
            {
                return 9;
            }
            else if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
            {
                return 10;
            }
            else if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.KeypadEquals))
            {
                return 11;
            }
            else
            {
                return -1;
            }
        }

        private void UpdateCurrentlySelectedItem()
        {
            if (CurrentlyHoveredInventorySlot != null)
            {
                if (CurrentlyHoveredInventorySlot.transform.childCount >= 1)
                {
                    CurrentlySelectedInventoryItem = CurrentlyHoveredInventorySlot.transform.GetChild(0).GetComponent<InventoryItem>();
                }
                else
                {
                    CurrentlySelectedInventoryItem = null;
                }
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

        // 아이템 드롭 메서드 (아이템을 버릴 때 사용)
        public void DropItem(Item item, int amount)
        {
            if (item != null && amount > 0)
            {
                Vector3 dropPosition = transform.position + transform.forward * 1.5f;
                GameObject droppedItem = Instantiate(itemDrop, dropPosition, Quaternion.identity);
                droppedItem.GetComponent<ItemDrop>().SetItem(item, amount);
            }
        }

        // 특정 슬롯에서 아이템 제거 메서드
        public void RemoveItemFromSlot(InventoryItem inventoryItem, int amount)
        {
            if (inventoryItem != null)
            {
                int remainingAmount = inventoryItem.Amount - amount;
                if (remainingAmount <= 0)
                {
                    Destroy(inventoryItem.gameObject);
                }
                else
                {
                    inventoryItem.Amount = remainingAmount;
                    inventoryItem.UpdateUI();
                }
            }
        }
    }
}