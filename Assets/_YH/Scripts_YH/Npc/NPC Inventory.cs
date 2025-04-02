using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Kinnly; // Kinnly 네임스페이스 사용

// 인벤토리 슬롯 클래스: 아이템을 담을 수 있는 UI 요소
// 마우스 호버링을 감지하여 플레이어 인벤토리에 현재 호버링 중인 슬롯 정보 제공
public class NPCInventory : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] Kinnly.PlayerInventory playerInventory; // 연결된 플레이어 인벤토리 참조
    [SerializeField] Npc npc; // 연결된 NPC 참조

    [SerializeField] Image slotImage; // 슬롯 이미지 (선택 시 색상 변경용)
    
    // 현재 보유 중인 아이템
    private Item currentItem;

    Color32 defaultColor = new Color32(255, 255, 255, 255);
    Color32 selectedColor = new Color32(255, 161, 161, 128);

    void Start()
    {
        // 슬롯 이미지가 할당되지 않은 경우 자동으로 찾기
        if (slotImage == null)
        {
            slotImage = GetComponent<Image>();
        }

        // 플레이어 인벤토리가 할당되지 않은 경우 자동으로 찾기
        if (playerInventory == null)
        {
            playerInventory = FindObjectOfType<PlayerInventory>();
        }
        
        // NPC 참조가 할당되지 않은 경우 부모 객체에서 찾기
        if (npc == null)
        {
            npc = GetComponentInParent<Npc>();
            if (npc == null)
            {
                Debug.LogWarning("NPC Inventory: NPC 컴포넌트를 찾을 수 없습니다!");
            }
        }
    }

    // 아이템을 인벤토리에 추가하는 메서드
    public void AddItem(Item item)
    {
        if (item != null)
        {
            currentItem = item;
            Debug.Log($"NPC 인벤토리에 {item.name} 아이템이 추가되었습니다.");
            
            // 아이템 타입에 따라 NPC 작업 설정
            SetNpcTaskBasedOnItem(item);
        }
    }
    
    // 아이템 타입에 따라 NPC 작업 설정
    private void SetNpcTaskBasedOnItem(Item item)
    {
        if (npc == null) return;
        
        // 아이템 타입 확인 및 작업 설정
        if (item.isSword)
        {
            // 검을 가지고 있으면 전투 작업 설정
            npc.SetTask(Npc.NpcTask.Combat);
            Debug.Log($"NPC {npc.NpcName}이(가) 검을 받아 전투 작업으로 설정되었습니다.");
        }
        else if (item.isAxe)
        {
            // 도끼를 가지고 있으면 나무 채집 작업 설정
            npc.SetTask(Npc.NpcTask.Woodcutting);
            Debug.Log($"NPC {npc.NpcName}이(가) 도끼를 받아 나무 채집 작업으로 설정되었습니다.");
        }
        else if (item.isPickaxe)
        {
            // 곡괭이를 가지고 있으면 광석 채집 작업 설정
            npc.SetTask(Npc.NpcTask.Mining);
            Debug.Log($"NPC {npc.NpcName}이(가) 곡괭이를 받아 광석 채집 작업으로 설정되었습니다.");
        }
    }
    
    // 현재 보유 중인 아이템 반환
    public Item GetCurrentItem()
    {
        return currentItem;
    }
    
    // 아이템 제거
    public void RemoveItem()
    {
        if (currentItem != null)
        {
            Debug.Log($"NPC 인벤토리에서 {currentItem.name} 아이템이 제거되었습니다.");
            currentItem = null;
            
            // 아이템이 없으면 NPC 작업 초기화
            if (npc != null)
            {
                npc.SetTask(Npc.NpcTask.None);
                Debug.Log($"NPC {npc.NpcName}의 작업이 초기화되었습니다.");
            }
        }
    }

    // 마우스가 슬롯 위에 들어왔을 때 호출됨
    public void OnPointerEnter(PointerEventData eventData)
    {
        // 현재 마우스가 위치한 슬롯을 플레이어 인벤토리에 알림
        playerInventory.CurrentlyHoveredInventorySlot = this.gameObject;
    }

    // 마우스가 슬롯에서 벗어났을 때 호출됨
    public void OnPointerExit(PointerEventData eventData)
    {
        // 슬롯에서 마우스가 벗어났음을 플레이어 인벤토리에 알림
        playerInventory.CurrentlyHoveredInventorySlot = null;
    }
}