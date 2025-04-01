using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Kinnly; // Kinnly 네임스페이스 추가

// 인벤토리 아이템 클래스: 인벤토리 슬롯 내에 표시되는 아이템 UI 관리
// 아이템 드래그 앤 드롭, 아이템 스택, 아이템 분할 등의 기능 제공
public class InventoryItem : MonoBehaviour, IPointerClickHandler
{
    [Header("Core - UI 요소")]
    [SerializeField] RectTransform rectTransform; // 위치 및 크기 조정용
    [SerializeField] Image image;               // 아이템 이미지
    [SerializeField] TMP_Text amountText;       // 아이템 수량 텍스트

    [Header("아이템 정보")]
    public Kinnly.Item Item;      // 아이템 데이터
    public int Amount;     // 아이템 수량

    // 드래그 상태 관리 
    // 로컬 드래그 상태(현재 이 아이템이 드래그 중인지)와 
    // 전역 드래그 상태(PlayerInventory.IsDragging)를 구분하여 관리
    [HideInInspector] public bool IsDragging;

    // 플레이어와 인벤토리 참조
    private GameObject player;
    private Kinnly.PlayerInventory playerInventory;
    // 원래 슬롯(드래그 취소 시 돌아갈 위치)
    private GameObject originalSlot;

    // 최대 스택 가능 개수
    private int maxAmount;

    // 초기화
    void Start()
    {
        // 플레이어 객체 찾기 시도
        if (player == null)
        {
            // 태그로 찾기
            player = GameObject.FindGameObjectWithTag("Player");
            
            // 태그로 찾지 못했다면 이름으로 찾기
            if (player == null)
            {
                player = GameObject.Find("Player");
            }
            
            if (player != null)
            {
                playerInventory = player.GetComponent<Kinnly.PlayerInventory>();
                if (playerInventory != null)
                {
                    maxAmount = playerInventory.MaxAmount;
                }
                else
                {
                    // 기본값 설정
                    maxAmount = 999;
                    Debug.LogWarning("PlayerInventory를 찾을 수 없어 최대 스택 수를 기본값(999)으로 설정합니다.");
                }
            }
            else
            {
                // 기본값 설정
                maxAmount = 999;
                Debug.LogWarning("Player를 찾을 수 없어 최대 스택 수를 기본값(999)으로 설정합니다.");
            }
        }
    }

    // 매 프레임 업데이트
    void Update()
    {
        // UI 업데이트 (수량 표시 등)
        UpdateUI();

        // 아이템 드래깅 중일 때 처리
        if (IsDragging)
        {
            // 마우스 위치 따라가기
            transform.position = Input.mousePosition;

            // ESC키 누르면 드래깅 취소
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CancelDragging();
            }

            // 좌클릭으로 아이템 배치
            if (Input.GetMouseButtonDown(0))
            {
                HandleItemDrop();
            }
        }
    }

    // 아이템 설정
    public void SetItem(Kinnly.Item item, int amount)
    {
        // 아이템 정보 및 이미지 설정
        this.Item = item;
        this.Amount = amount;
        this.image.sprite = Item.image;
        this.IsDragging = false;

        // 플레이어 찾기 및 인벤토리 참조 설정
        if (player == null)
        {
            // 태그로 플레이어 찾기
            player = GameObject.FindGameObjectWithTag("Player");
            
            // 태그로 찾지 못하면 이름으로 찾기
            if (player == null)
            {
                player = GameObject.Find("Player"); 
            }
        }
        
        if (player != null)
        {
            playerInventory = player.GetComponent<Kinnly.PlayerInventory>();
        }
        else
        {
            Debug.LogError("Player를 찾을 수 없습니다!");
        }
    }

    // 마우스 클릭 이벤트 처리
    public void OnPointerClick(PointerEventData eventData)
    {
        // 이미 다른 아이템을 드래그 중이거나 클릭 처리 중이면 무시
        if (playerInventory != null && playerInventory.IsDragging)
        {
            return;
        }

        if (playerInventory != null && playerInventory.IsClicking)
        {
            return;
        }

        // 좌클릭: 아이템 드래깅 시작
        if (eventData.button == PointerEventData.InputButton.Left && IsDragging == false)
        {
            StartDragging();
        }

        // 우클릭 기능 비활성화
    }

    // 아이템 개수 추가
    public void AddAmount(int amount)
    {
        this.Amount += amount;
        UpdateUI();
    }

    // 아이템 개수 감소
    public void RemoveAmount(int amount)
    {
        this.Amount -= amount;
        UpdateUI();
    }

    // 아이템 개수 설정
    public void SetAmount(int amount)
    {
        this.Amount = amount;
        UpdateUI();
    }

    // UI 업데이트 (아이템 개수 표시)
    public void UpdateUI()
    {
        // 아이템이 없거나 개수가 0 이하면 오브젝트 파괴
        if (Item == null || Amount <= 0)
        {
            Destroy(gameObject);
            return;
        }

        // 수량 텍스트 업데이트 (1개일 때는 표시 안함)
        if (Amount > 1)
        {
            amountText.text = Amount.ToString();
        }
        else
        {
            amountText.text = "";
        }
    }

    // 드래깅 시작
    public void StartDragging()
    {
        if (playerInventory != null && !playerInventory.IsDragging)
        {
            // 처음 부모 저장
            Transform oldParent = transform.parent;
            originalSlot = oldParent.gameObject;
            
            // 캔버스 루트로 이동
            transform.SetParent(transform.root);
            
            // 레이캐스트 타겟 비활성화 (아래 UI 요소와 상호작용 가능하게)
            image.raycastTarget = false;
            
            // 드래그 상태 설정
            IsDragging = true;
            playerInventory.IsDragging = true;
            
            // 현재 선택된 인벤토리 아이템으로 설정
            playerInventory.CurrentlySelectedInventoryItem = this;

            // Z오더를 최상위로 설정
            transform.SetAsLastSibling();
            
            Debug.Log(Item.name + " 드래그 시작");
        }
    }

    // 드래깅 취소 (ESC 키)
    public void CancelDragging()
    {
        if (IsDragging)
        {
            Debug.Log(Item.name + " 드래그 취소");
            
            // 원래 슬롯으로 돌아감
            if (originalSlot != null)
            {
                transform.SetParent(originalSlot.transform);
                transform.localPosition = Vector3.zero;
                // 레이캐스트 타겟 활성화
                image.raycastTarget = true;
            }
            else
            {
                // 원래 슬롯이 없으면 파괴
                Destroy(gameObject);
            }

            // 드래그 상태 초기화
            IsDragging = false;
            if (playerInventory != null)
            {
                playerInventory.IsDragging = false;
            }
        }
    }

    // 아이템 배치 처리 (드래그 중 좌클릭)
    private void HandleItemDrop()
    {
        if (playerInventory == null) return;
        
        if (playerInventory.IsClicking)
        {
            return;
        }

        Debug.Log("아이템 드롭 처리 중: " + Item.name);
        
        // 슬롯 위에 드롭한 경우
        if (playerInventory.CurrentlyHoveredInventorySlot != null)
        {
            Debug.Log("슬롯 위에 드롭: " + playerInventory.CurrentlyHoveredInventorySlot.name);
            InventoryItem hoveredItem = playerInventory.CurrentlyHoveredInventorySlot.GetComponentInChildren<InventoryItem>();

            if (hoveredItem != null)
            {
                // 1. 같은 종류의 아이템이고 스택 가능하며 최대 스택 수 이하인 경우
                if (hoveredItem.Item.name == this.Item.name && Item.isStackable && hoveredItem.Amount + this.Amount <= maxAmount)
                {
                    // 아이템 병합
                    hoveredItem.AddAmount(this.Amount);
                    Destroy(this.gameObject);
                    playerInventory.IsDragging = false;
                    Debug.Log("아이템 병합 완료");
                }
                // 2. 같은 종류의 아이템이고 스택 가능하지만 최대 스택 수를 초과하는 경우
                else if (hoveredItem.Item.name == this.Item.name && Item.isStackable && hoveredItem.Amount + this.Amount > maxAmount)
                {
                    // 최대치까지만 병합하고 나머지는 드래그 중인 아이템에 남김
                    int excess = maxAmount - hoveredItem.Amount;
                    hoveredItem.AddAmount(excess);
                    this.Amount = this.Amount - excess;
                    Debug.Log("아이템 일부 병합: " + excess + "개");
                }
                // 3. 다른 종류의 아이템이거나 스택 불가능한 경우
                else
                {
                    // 아이템 위치 교환
                    SwapItems(hoveredItem);
                }
            }
            // 빈 슬롯인 경우
            else
            {
                // 빈 슬롯에 배치
                PlaceInEmptySlot();
            }
        }

        // 드랍 박스 위에 드롭한 경우 (인벤토리 외부 드롭)
        if (playerInventory.IsHoveringDropBox == true)
        {
            playerInventory.SpawnItemDrop(Item, Amount);
            playerInventory.IsDragging = false;
            Destroy(this.gameObject);
        }

        // 휴지통 위에 드롭한 경우 (아이템 삭제)
        if (playerInventory.IsHoveringTrashcan == true)
        {
            Destroy(this.gameObject);
            playerInventory.IsDragging = false;
        }

        // 클릭 중 상태 설정 (연속 클릭 방지)
        playerInventory.IsClicking = true;
    }

    // 아이템 교환
    private void SwapItems(InventoryItem hoveredItem)
    {
        Debug.Log(Item.name + " <-> " + hoveredItem.Item.name + " : 아이템 교환 중");

        // 호버된 아이템의 부모 저장
        Transform hoveredParent = hoveredItem.transform.parent;
        
        // 현재 아이템을 호버된 슬롯으로 이동
        transform.SetParent(hoveredParent);
        rectTransform.localPosition = Vector3.zero;
        image.raycastTarget = true;
        
        // 호버된 아이템을 원래 슬롯으로 이동
        hoveredItem.transform.SetParent(originalSlot.transform);
        hoveredItem.transform.localPosition = Vector3.zero;
        hoveredItem.image.raycastTarget = true;

        // 드래그 상태 초기화
        IsDragging = false;
        playerInventory.IsDragging = false;
        
        Debug.Log("아이템 교환 완료");
    }

    // 빈 슬롯에 아이템 배치
    private void PlaceInEmptySlot()
    {
        // 현재 호버 중인 슬롯의 자식으로 설정
        transform.SetParent(playerInventory.CurrentlyHoveredInventorySlot.transform);
        // 정중앙에 위치시킴
        rectTransform.localPosition = Vector3.zero;
        // 레이캐스트 타겟 활성화
        image.raycastTarget = true;
        // 드래그 상태 해제
        IsDragging = false;
        playerInventory.IsDragging = false;
    }

    // 분할된 아이템 스폰
    private void SpawnSplitItem(int amount)
    {
        if (playerInventory != null)
        {
            playerInventory.SpawnInventoryItem(Item, amount);
        }
    }
}