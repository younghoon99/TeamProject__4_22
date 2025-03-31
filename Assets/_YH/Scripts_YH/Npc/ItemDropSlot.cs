using UnityEngine;
using UnityEngine.EventSystems;

// 아이템을 드롭할 수 있는 슬롯
public class ItemDropSlot : MonoBehaviour, IDropHandler
{
    private NpcInteraction npcInteraction; // NPC 상호작용 컴포넌트 참조
    
    private void Awake()
    {
        // 부모 객체에서 NpcInteraction 컴포넌트 찾기
        npcInteraction = GetComponentInParent<NpcInteraction>();
        if (npcInteraction == null)
        {
            Debug.LogError("ItemDropSlot: NpcInteraction 컴포넌트를 찾을 수 없습니다.");
        }
    }
    
    // 아이템이 드롭될 때 호출
    public void OnDrop(PointerEventData eventData)
    {
        // 드래그 중인 객체가 있는지 확인
        if (eventData.pointerDrag != null)
        {
            // 드래그 아이템 컴포넌트 가져오기
            DraggableItem item = eventData.pointerDrag.GetComponent<DraggableItem>();
            if (item != null)
            {
                // 슬롯에 아이템 배치
                Transform previousParent = item.transform.parent;
                item.transform.SetParent(transform);
                item.transform.localPosition = Vector3.zero;
                
                // NPC에게 아이템 장착
                if (npcInteraction != null)
                {
                    npcInteraction.GiveItemToNpc(item.data.itemType);
                    Debug.Log($"NPC에게 {item.data.itemName}을(를) 장착했습니다.");
                }
                else
                {
                    // NPC 상호작용 컴포넌트가 없으면 원래 위치로 돌아감
                    item.ReturnToOriginalPosition();
                }
            }
        }
    }
}
