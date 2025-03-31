using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NpcInteraction : MonoBehaviour
{
    [Header("상호작용 설정")]
    [SerializeField] private float interactionRange = 2.0f;
    [SerializeField] private KeyCode interactionKey = KeyCode.F;
    
    [Header("UI 오프셋 설정")]
    [SerializeField] private Vector3 promptOffset = new Vector3(0, 2.0f, 0); // NPC 머리 위 오프셋
    [SerializeField] private Vector3 infoOffset = new Vector3(0, 2.5f, 0);    // 정보 패널 오프셋

    [Header("UI 설정")]
    [SerializeField] private GameObject interactionPrompt;    // NPC와 상호작용할 수 있을 때 표시되는 프롬프트 UI (예: "F키를 눌러 대화하기")
    [SerializeField] private GameObject npcInfoPanel;         // NPC 정보를 표시하는 패널 (이름, 등급, 능력치 등을 포함)
    [SerializeField] private TextMeshProUGUI npcInfoText;     // NPC 세부 정보를 표시하는 텍스트 컴포넌트
    [SerializeField] private GameObject interactionButtonsPanel; // 상호작용 버튼들이 포함된 패널

    [Header("드래그 앤 드롭 인벤토리")]
    [SerializeField] private GameObject inventoryPanel;       // 인벤토리 패널
    [SerializeField] private GameObject itemSlotPrefab;       // 아이템 슬롯 프리팹
    [SerializeField] private Transform inventoryContainer;    // 아이템 슬롯들이 배치될 컨테이너
    [SerializeField] private Transform npcItemSlot;           // NPC가 아이템을 장착할 슬롯
    [SerializeField] private List<ItemData> availableItems = new List<ItemData>();  // 사용 가능한 아이템 목록
    [SerializeField] private Image npcItemSlotImage;          // NPC가 들고 있는 아이템 표시 이미지
    
    [Header("아이템 이미지")]
    [SerializeField] private Sprite axeSprite;                // 도끼 이미지
    [SerializeField] private Sprite pickaxeSprite;            // 곡괭이 이미지
    [SerializeField] private Sprite swordSprite;              // 검 이미지
    
    // 참조 변수
    private Transform playerTransform;
    private Npc currentNpc;
    private NpcAbility currentNpcAbility;
    private bool isInteracting = false;
    private bool isNpcFollowing = false;
    
    private NpcItemType currentEquippedItem = NpcItemType.None;
    
    private void Start()
    {
        // 플레이어 트랜스폼 찾기
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        
        if (playerTransform == null)
        {
            Debug.LogError("플레이어를 찾을 수 없습니다. 'Player' 태그가 설정되었는지 확인하세요.");
        }
        
        // UI 초기 상태 설정
        if (interactionPrompt) interactionPrompt.SetActive(false);
        if (npcInfoPanel) npcInfoPanel.SetActive(false);
        if (inventoryPanel) inventoryPanel.SetActive(false);
        
        // 인벤토리 초기화
        InitializeInventory();
    }
    
    // 인벤토리 초기화
    private void InitializeInventory()
    {
        if (inventoryContainer == null || itemSlotPrefab == null) return;
        
        // 사용 가능한 아이템이 비어있으면 기본 아이템 추가
        if (availableItems.Count == 0)
        {
            // 기본 아이템 데이터 생성
            // 사용자가 Inspector에서 아이템을 설정하지 않았을 경우에만 실행
            CreateDefaultItems();
        }
        
        // 기존 슬롯 삭제
        foreach (Transform child in inventoryContainer)
        {
            Destroy(child.gameObject);
        }
        
        // 새 아이템 슬롯 생성
        foreach (ItemData item in availableItems)
        {
            GameObject slotObj = Instantiate(itemSlotPrefab, inventoryContainer);
            DraggableItem draggable = slotObj.GetComponent<DraggableItem>();
            if (draggable != null)
            {
                draggable.data = item;
                Image icon = slotObj.GetComponent<Image>();
                if (icon != null)
                {
                    icon.sprite = item.icon;
                }
            }
        }
    }
    
    // 기본 아이템 생성
    private void CreateDefaultItems()
    {
        // 도끼 아이템
        ItemData axe = new ItemData
        {
            itemName = "도끼",
            icon = axeSprite,
            itemType = NpcItemType.Axe,
            description = "나무를 자르는 데 사용됩니다."
        };
        
        // 곡괭이 아이템
        ItemData pickaxe = new ItemData
        {
            itemName = "곡괭이",
            icon = pickaxeSprite,
            itemType = NpcItemType.Pickaxe,
            description = "광물을 채굴하는 데 사용됩니다."
        };
        
        // 검 아이템
        ItemData sword = new ItemData
        {
            itemName = "검",
            icon = swordSprite,
            itemType = NpcItemType.Sword,
            description = "전투에 사용됩니다."
        };
        
        // 아이템 목록에 추가
        availableItems.Add(axe);
        availableItems.Add(pickaxe);
        availableItems.Add(sword);
    }
    
    private void Update()
    {
        if (playerTransform == null) return;
        
        // 가장 가까운 NPC 찾기
        Npc nearestNpc = FindNearestNpc();
        
        // NPC와의 상호작용 처리
        if (nearestNpc != null)
        {
            // 상호작용 프롬프트 표시
            if (interactionPrompt && !isInteracting)
            {
                interactionPrompt.SetActive(true);
                
                // 프롬프트 위치를 NPC 머리 위로 설정
                interactionPrompt.transform.position = Camera.main.WorldToScreenPoint(
                    nearestNpc.transform.position + promptOffset);
            }
            
            // 상호작용 키 입력 감지
            if (Input.GetKeyDown(interactionKey) && !isInteracting)
            {
                StartInteraction(nearestNpc);
            }
            else if (Input.GetKeyDown(interactionKey) && isInteracting)
            {
                EndInteraction();
            }
        }
        else
        {
            // 가까운 NPC가 없으면 프롬프트 숨기기
            if (interactionPrompt)
            {
                interactionPrompt.SetActive(false);
            }
            
            // 상호작용 중이었다면 종료
            if (isInteracting)
            {
                EndInteraction();
            }
        }
        
        // UI 위치 업데이트
        UpdateUIPositions();
        
        // NPC 따라오기 로직
        UpdateFollowing();
    }

    private void UpdateUIPositions()
    {
        if (currentNpc != null)
        {
            // UI 요소 위치 업데이트
            if (interactionPrompt && interactionPrompt.activeSelf)
            {
                interactionPrompt.transform.position = Camera.main.WorldToScreenPoint(
                    currentNpc.transform.position + promptOffset);
            }

            if (npcInfoPanel && npcInfoPanel.activeSelf)
            {
                // LookAt 제거하고 위치만 업데이트
                npcInfoPanel.transform.position = Camera.main.WorldToScreenPoint(
                    currentNpc.transform.position + infoOffset);
            }

            // 상호작용 버튼 패널도 NPC 위치에 따라 업데이트
            if (interactionButtonsPanel && interactionButtonsPanel.activeSelf)
            {
                interactionButtonsPanel.transform.position = Camera.main.WorldToScreenPoint(
                    currentNpc.transform.position + new Vector3(0, 3.0f, 0));
            }
            
            // 인벤토리 패널 위치 업데이트
            if (inventoryPanel && inventoryPanel.activeSelf)
            {
                inventoryPanel.transform.position = Camera.main.WorldToScreenPoint(
                    currentNpc.transform.position + new Vector3(0, 3.5f, 0));
            }
        }
    }

    // 가장 가까운 NPC 찾기
    private Npc FindNearestNpc()
    {
        Npc closestNpc = null;
        float closestDistance = interactionRange;
        
        Npc[] allNpcs = FindObjectsOfType<Npc>();
        foreach (Npc npc in allNpcs)
        {
            float distance = Vector3.Distance(playerTransform.position, npc.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestNpc = npc;
            }
        }
        
        return closestNpc;
    }
    
    // NPC와 상호작용 시작
    private void StartInteraction(Npc npc)
    {
        currentNpc = npc;
        currentNpcAbility = npc.GetComponent<NpcAbility>();
        isInteracting = true;
        
        // NPC 정보 패널 표시
        if (npcInfoPanel)
        {
            npcInfoPanel.SetActive(true);
            
            // NPC 정보 텍스트 업데이트
            if (npcInfoText)
            {
                npcInfoText.text = npc.GetNpcInfoText();
            }
        }
        
        // 상호작용 프롬프트 숨기기
        if (interactionPrompt)
        {
            interactionPrompt.SetActive(false);
        }
        
        // NPC에게 상호작용 시작 알림
        npc.OnInteractionStart();
        
        // 인벤토리 패널 활성화
        if (inventoryPanel)
        {
            inventoryPanel.SetActive(true);
            
            // 현재 장착된 아이템 상태 갱신
            currentEquippedItem = npc.GetEquippedItem();
            UpdateItemSlotImage();
        }
    }
    
    // NPC와 상호작용 종료
    private void EndInteraction()
    {
        // NPC 상호작용 종료 알림
        if (currentNpc != null)
        {
            currentNpc.OnInteractionEnd();
            
            // 채굴 중이었다면 중지
            if (currentNpcAbility != null)
            {
                currentNpcAbility.StopGathering();
            }
        }
        
        // UI 패널 숨기기
        if (npcInfoPanel)
        {
            npcInfoPanel.SetActive(false);
        }
        
        if (interactionButtonsPanel)
        {
            interactionButtonsPanel.SetActive(false);
        }
        
        if (inventoryPanel)
        {
            inventoryPanel.SetActive(false);
        }
        
        // 상태 초기화
        isInteracting = false;
        currentNpc = null;
        currentNpcAbility = null;
    }
    
    // NPC에게 아이템 지급
    public void GiveItemToNpc(NpcItemType itemType)
    {
        if (currentNpc == null) return;
        
        // 아이템 장착
        currentEquippedItem = itemType;
        currentNpc.EquipItem(itemType);
        
        // 아이템 이미지 업데이트
        UpdateItemSlotImage();
        
        // 아이템에 따른 작업 시작
        switch (itemType)
        {
            case NpcItemType.Axe:
                StartWoodcuttingTask();
                break;
            case NpcItemType.Pickaxe:
                StartMiningTask();
                break;
            case NpcItemType.Sword:
                StartCombatTask();
                break;
        }
    }

    // 아이템 슬롯 이미지 업데이트
    private void UpdateItemSlotImage()
    {
        if (npcItemSlotImage == null) return;
        
        switch (currentEquippedItem)
        {
            case NpcItemType.Axe:
                npcItemSlotImage.sprite = axeSprite;
                break;
            case NpcItemType.Pickaxe:
                npcItemSlotImage.sprite = pickaxeSprite;
                break;
            case NpcItemType.Sword:
                npcItemSlotImage.sprite = swordSprite;
                break;
            case NpcItemType.None:
                npcItemSlotImage.sprite = null;
                npcItemSlotImage.color = new Color(1, 1, 1, 0); // 투명하게
                break;
        }
        
        npcItemSlotImage.color = currentEquippedItem != NpcItemType.None ? 
            new Color(1, 1, 1, 1) : new Color(1, 1, 1, 0);
    }

    // NPC에게서 아이템 회수
    public void RemoveItemFromNpc()
    {
        if (currentNpc == null) return;
        
        // 작업 중지
        StopCurrentTask();
        
        // 아이템 제거
        currentEquippedItem = NpcItemType.None;
        currentNpc.EquipItem(NpcItemType.None);
        
        // NPC 슬롯에서 아이템 제거
        if (npcItemSlot != null)
        {
            foreach (Transform child in npcItemSlot)
            {
                if (child.GetComponent<DraggableItem>() != null)
                {
                    // 인벤토리로 돌려보내기
                    child.SetParent(inventoryContainer);
                    child.localPosition = Vector3.zero;
                }
            }
        }
        
        // 아이템 이미지 업데이트
        UpdateItemSlotImage();
    }
    
    // 나무 채집 작업 시작
    private void StartWoodcuttingTask()
    {
        if (currentNpc == null) return;
        
        Debug.Log($"{currentNpc.NpcName}이(가) 나무 채집 작업을 시작합니다.");
        
        // NPC에게 나무 채집 임무 부여
        currentNpc.SetTask(Npc.NpcTask.Woodcutting);
    }

    // 광물 채집 작업 시작
    private void StartMiningTask()
    {
        if (currentNpc == null) return;
        
        Debug.Log($"{currentNpc.NpcName}이(가) 광물 채집 작업을 시작합니다.");
        
        // NPC에게 광물 채집 임무 부여
        currentNpc.SetTask(Npc.NpcTask.Mining);
    }

    // 전투 작업 시작
    private void StartCombatTask()
    {
        if (currentNpc == null) return;
        
        Debug.Log($"{currentNpc.NpcName}이(가) 전투 임무를 시작합니다.");
        
        // NPC에게 전투 임무 부여
        currentNpc.SetTask(Npc.NpcTask.Combat);
    }

    // 현재 작업 중지
    private void StopCurrentTask()
    {
        if (currentNpc == null) return;
        
        Debug.Log($"{currentNpc.NpcName}의 현재 작업이 중지되었습니다.");
        
        // NPC 작업 초기화
        currentNpc.SetTask(Npc.NpcTask.None);
    }
    
    // NPC 따라오기 로직 업데이트
    private void UpdateFollowing()
    {
        if (isNpcFollowing && currentNpc != null && playerTransform != null)
        {
            float distance = Vector3.Distance(playerTransform.position, currentNpc.transform.position);
            
            // 일정 거리 이상 떨어지면 NPC가 플레이어를 향해 이동
            if (distance > 3.0f)
            {
                // 플레이어 방향으로 이동하는 로직 (실제 이동은 Npc 클래스에서 구현)
                Debug.Log($"{currentNpc.NpcName}이(가) 플레이어를 따라갑니다. 거리: {distance:F2}");
            }
        }
    }
}
