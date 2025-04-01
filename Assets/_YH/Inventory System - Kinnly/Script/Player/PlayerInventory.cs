using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Kinnly
{
    /// <summary>
    /// 플레이어 인벤토리 시스템 클래스
    /// 아이템 추가, 제거, 이동 및 인벤토리 슬롯 관리를 담당합니다.
    /// </summary>
    public class PlayerInventory : MonoBehaviour
    {
        [Header("Core")]
        [SerializeField] GameObject inventoryUI;                           // 인벤토리 UI 컨테이너
        [SerializeField] List<GameObject> inventorySlot = new List<GameObject>();  // 인벤토리 슬롯 리스트

        [Header("Prefabs")]
        [SerializeField] GameObject inventoryItem;                         // 인벤토리 아이템 프리팹 (슬롯에 표시되는 아이템 UI)
        [SerializeField] GameObject itemDrop;                              // 아이템 드롭 프리팹 (바닥에 떨어지는 아이템)

        [Header("Config")]
        public int MaxAmount;                                              // 아이템 최대 스택 수량

        // 인벤토리 상태 변수들
        [HideInInspector] public GameObject CurrentlyHoveredInventorySlot; // 현재 마우스가 위치한 인벤토리 슬롯
        [HideInInspector] public InventoryItem CurrentlySelectedInventoryItem; // 현재 선택된 인벤토리 아이템
        [HideInInspector] public int CurrentlySelectedInventorySlot;       // 현재 선택된 인벤토리 슬롯 번호
        [HideInInspector] public bool IsHoveringDropBox;                   // 드롭 박스 위에 마우스가 있는지 여부
        [HideInInspector] public bool IsHoveringTrashcan;                  // 쓰레기통 위에 마우스가 있는지 여부
        [HideInInspector] public bool IsDragging;                          // 아이템을 드래그 중인지 여부
        [HideInInspector] public bool IsClicking;                          // 마우스 클릭 중인지 여부

        private DialogBox dialogBox;                                       // 다이얼로그 박스 참조

        /// <summary>
        /// 초기화 메서드
        /// </summary>
        void Start()
        {
            MaxAmount = 999;                                               // 기본 최대 스택 수량 설정
            CurrentlySelectedInventorySlot = 0;                            // 기본값으로 첫 번째 슬롯 선택
            dialogBox = DialogBox.instance;                                // 다이얼로그 박스 인스턴스 가져오기

            // 인벤토리 슬롯 번호 초기화
            InitializeInventorySlotNumbers();
            
            // 인벤토리 UI 항상 활성화
            if (inventoryUI != null)
            {
                inventoryUI.SetActive(true);
            }
        }

        /// <summary>
        /// 인벤토리 슬롯에 번호 할당하는 메서드
        /// </summary>
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

        /// <summary>
        /// 매 프레임 호출되는 업데이트 메서드
        /// 슬롯 선택 및 마우스 입력 처리
        /// </summary>
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

            // 마우스 클릭 상태 업데이트
            if (Input.GetMouseButtonUp(0))
            {
                IsClicking = false;
            }

            // 현재 선택된 아이템 업데이트
            UpdateCurrentlySelectedItem();
        }

        /// <summary>
        /// 인벤토리에 아이템 추가 메서드
        /// 스택 가능한 아이템은 기존 슬롯에 추가, 아니면 새 슬롯에 추가
        /// </summary>
        /// <param name="item">추가할 아이템</param>
        /// <param name="amount">추가할 수량</param>
        public void AddItem(Item item, int amount)
        {
            // 1. 기존 슬롯에 같은 아이템이 있고 스택 가능한 경우 해당 슬롯에 추가
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
                            // 최대 스택 수량 이내인 경우 모두 추가
                            inventoryItem.AddAmount(amount);
                            return;
                        }
                        else
                        {
                            // 최대 스택 수량을 초과하는 경우 최대치까지만 추가하고 나머지는 드롭
                            inventoryItem.AddAmount(amount - (total - MaxAmount));
                            SpawnItemDrop(item, amount - (amount - (total - MaxAmount)));
                            return;
                        }
                    }
                }
            }

            // 2. 빈 슬롯에 새로 아이템 추가
            foreach (GameObject slot in inventorySlot)
            {
                if (slot.gameObject.transform.childCount <= 0)
                {
                    GameObject go = Instantiate(inventoryItem, slot.transform);
                    go.GetComponent<InventoryItem>().SetItem(item, amount);
                    return;
                }
            }

            // 3. 모든 슬롯이 차있는 경우 아이템을 바닥에 드롭
            SpawnItemDrop(item, amount);
        }

        /// <summary>
        /// 인벤토리에서 아이템 제거 메서드
        /// </summary>
        /// <param name="inventoryItem">제거할 인벤토리 아이템</param>
        /// <param name="amount">제거할 수량</param>
        public void RemoveItem(InventoryItem inventoryItem, int amount)
        {
            inventoryItem.RemoveAmount(amount);
        }

        /// <summary>
        /// 인벤토리에 아이템을 추가할 수 있는 슬롯이 있는지 확인하는 메서드
        /// </summary>
        /// <param name="item">확인할 아이템</param>
        /// <param name="amount">확인할 수량</param>
        /// <returns>추가 가능 여부</returns>
        public bool IsSlotAvailable(Item item, int amount)
        {
            // 1. 같은 아이템이 있고 스택 가능한 경우 확인
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

            // 2. 빈 슬롯이 있는지 확인
            foreach (GameObject slot in inventorySlot)
            {
                if (slot.gameObject.transform.childCount <= 0)
                {
                    return true;
                }
            }

            // 인벤토리가 가득 찬 경우 메시지 표시
            dialogBox.Show("Inventory Full", 1f);
            return false;
        }

        /// <summary>
        /// 아이템을 바닥에 드롭하는 메서드
        /// </summary>
        /// <param name="item">드롭할 아이템</param>
        /// <param name="amount">드롭할 수량</param>
        public void SpawnItemDrop(Item item, int amount)
        {
            // 플레이어 주변에 랜덤한 위치에 아이템 드롭
            GameObject go = Instantiate(itemDrop, transform.position + new Vector3(RandomNumber(-3f, 3f, -1f, 1f), RandomNumber(-3f, 3f, -1f, 1f), 0f), transform.rotation);
            go.GetComponent<SpriteRenderer>().sprite = item.image;
            go.GetComponent<ItemDrop>().SetItem(item, amount);
        }

        /// <summary>
        /// 인벤토리 아이템을 생성하는 메서드 (드래그 앤 드롭용)
        /// </summary>
        /// <param name="item">생성할 아이템</param>
        /// <param name="amount">생성할 수량</param>
        public void SpawnInventoryItem(Item item, int amount)
        {
            // UI 루트에 인벤토리 아이템 생성
            GameObject go = Instantiate(inventoryItem, inventoryUI.transform.root);
            InventoryItem inventory = go.GetComponent<InventoryItem>();
            inventory.SetItem(item, amount);
            inventory.IsDragging = true;
            inventory.Amount = amount;
            go.GetComponent<Image>().raycastTarget = false;
            this.IsDragging = true;
        }

        /// <summary>
        /// 두 슬롯 간에 아이템 이동 메서드
        /// </summary>
        /// <param name="from">출발 슬롯</param>
        /// <param name="to">도착 슬롯</param>
        public void MoveItemBetweenSlots(GameObject from, GameObject to)
        {
            // 출발 슬롯에 아이템이 없거나 도착 슬롯에 이미 아이템이 있으면 이동 불가
            if (from.transform.childCount <= 0 || to.transform.childCount >= 1)
            {
                return;
            }

            // 아이템의 부모를 변경하여 이동
            from.transform.GetChild(0).transform.SetParent(to.transform);
        }

        /// <summary>
        /// 두 범위 내에서 랜덤한 값을 반환하는 유틸리티 메서드
        /// </summary>
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

        /// <summary>
        /// 키보드 입력으로 인벤토리 슬롯 번호를 가져오는 메서드
        /// </summary>
        /// <returns>입력된 슬롯 번호 (입력 없으면 -1)</returns>
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

        /// <summary>
        /// 현재 선택된 아이템 정보 업데이트 메서드
        /// </summary>
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

        /// <summary>
        /// 인벤토리 슬롯 개수 반환 메서드
        /// </summary>
        /// <returns>인벤토리 슬롯 개수</returns>
        public int GetInventorySlotCount()
        {
            return inventorySlot.Count;
        }

        /// <summary>
        /// 특정 인덱스의 인벤토리 슬롯 반환 메서드
        /// </summary>
        /// <param name="index">슬롯 인덱스</param>
        /// <returns>인벤토리 슬롯 게임오브젝트</returns>
        public GameObject GetInventorySlot(int index)
        {
            if (index >= 0 && index < inventorySlot.Count)
            {
                return inventorySlot[index];
            }
            return null;
        }

        /// <summary>
        /// 아이템 드롭 메서드 (아이템을 버릴 때 사용)
        /// </summary>
        /// <param name="item">버릴 아이템</param>
        /// <param name="amount">버릴 수량</param>
        public void DropItem(Item item, int amount)
        {
            if (item != null && amount > 0)
            {
                Vector3 dropPosition = transform.position + transform.forward * 1.5f;
                GameObject droppedItem = Instantiate(itemDrop, dropPosition, Quaternion.identity);
                droppedItem.GetComponent<ItemDrop>().SetItem(item, amount);
            }
        }

        /// <summary>
        /// 특정 슬롯에서 아이템 제거 메서드
        /// </summary>
        /// <param name="inventoryItem">제거할 인벤토리 아이템</param>
        /// <param name="amount">제거할 수량</param>
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