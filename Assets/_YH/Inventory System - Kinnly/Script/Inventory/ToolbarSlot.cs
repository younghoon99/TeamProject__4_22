using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Kinnly
{
    public class ToolbarSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        public int slotNumber;

        [SerializeField] PlayerInventory playerInventory;
        [SerializeField] Image image;

        Color32 defaultColor = new Color32(255, 255, 255, 255);
        Color32 selectedColor = new Color32(255, 161, 161, 128);

        void Start()
        {
            // 플레이어 인벤토리가 할당되지 않은 경우 자동으로 찾기
            if (playerInventory == null)
            {
                playerInventory = FindObjectOfType<PlayerInventory>();
            }
        }

        void Update()
        {
            if (playerInventory.CurrentlySelectedToolBar == slotNumber)
            {
                image.color = selectedColor;
            }
            else
            {
                image.color = defaultColor;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            playerInventory.CurrentlyHoveredToolbarSlot = this.gameObject;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            playerInventory.CurrentlyHoveredToolbarSlot = null;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // 마우스 클릭으로 툴바 선택 비활성화 (키패드로만 선택 가능)
            // playerInventory.CurrentlySelectedToolBar = slotNumber;
            
            // 만약 아이템이 있다면 클릭 이벤트 처리 (아이템 사용 등)
            if (transform.childCount > 0 && !playerInventory.IsDragging)
            {
                // 아이템 클릭 로직은 필요하다면 여기에 구현
            }
        }
    }
}