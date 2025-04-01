using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Kinnly; // Kinnly 네임스페이스 사용

// 인벤토리 슬롯 클래스: 아이템을 담을 수 있는 UI 요소
// 마우스 호버링을 감지하여 플레이어 인벤토리에 현재 호버링 중인 슬롯 정보 제공
public class InventorySlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] Kinnly.PlayerInventory playerInventory; // 연결된 플레이어 인벤토리 참조
    
    public int slotNumber; // 슬롯 번호 (0부터 시작)
    
    [SerializeField] Image slotImage; // 슬롯 이미지 (선택 시 색상 변경용)
    
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
    }
    
    void Update()
    {
        // 현재 선택된 슬롯인 경우 색상 변경
        if (playerInventory.CurrentlySelectedInventorySlot == slotNumber)
        {
            slotImage.color = selectedColor;
        }
        else
        {
            slotImage.color = defaultColor;
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

    // 마우스 클릭 이벤트 처리
    public void OnPointerClick(PointerEventData eventData)
    {
        // 좌클릭으로 슬롯 선택
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            // 현재 슬롯 번호로 인벤토리 슬롯 선택
            playerInventory.CurrentlySelectedInventorySlot = slotNumber;
        }
        // 우클릭 기능은 비활성화
    }
}