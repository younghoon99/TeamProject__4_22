using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Kinnly; // Kinnly 네임스페이스 사용

// 인벤토리 슬롯 클래스: 아이템을 담을 수 있는 UI 요소
// 마우스 호버링을 감지하여 플레이어 인벤토리에 현재 호버링 중인 슬롯 정보 제공
public class InventorySlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] Kinnly.PlayerInventory playerInventory; // 연결된 플레이어 인벤토리 참조

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