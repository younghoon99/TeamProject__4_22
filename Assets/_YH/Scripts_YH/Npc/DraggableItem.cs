using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 드래그 가능한 아이템 컴포넌트
public class DraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public ItemData data;                // 아이템 데이터
    private RectTransform rectTransform; // RectTransform 컴포넌트
    private CanvasGroup canvasGroup;     // CanvasGroup 컴포넌트
    private Transform originalParent;    // 원래 부모 객체
    private Vector3 originalPosition;    // 원래 위치
    
    private void Awake()
    {
        // 컴포넌트 참조 가져오기
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        
        // CanvasGroup이 없으면 추가
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }
    
    // 드래그 시작 시 호출
    public void OnBeginDrag(PointerEventData eventData)
    {
        // 현재 상태 저장
        originalParent = transform.parent;
        originalPosition = transform.position;
        
        // 드래그 중에는 아이템을 반투명하게 하고 레이캐스트를 차단하지 않음
        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;
        
        // 최상위 캔버스로 이동 (다른 UI 요소 위에 표시되도록)
        transform.SetParent(transform.root);
        
        // 디버그 로그
        Debug.Log($"{data.itemName} 아이템 드래그 시작");
    }
    
    // 드래그 중 호출
    public void OnDrag(PointerEventData eventData)
    {
        // 마우스 위치를 따라 이동
        rectTransform.position = Input.mousePosition;
    }
    
    // 드래그 종료 시 호출
    public void OnEndDrag(PointerEventData eventData)
    {
        // 원래 상태로 복구
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        
        // 유효한 드롭 타겟이 없는 경우 원래 위치로 돌아감
        if (eventData.pointerCurrentRaycast.gameObject == null ||
            !eventData.pointerCurrentRaycast.gameObject.GetComponent<ItemDropSlot>())
        {
            ReturnToOriginalPosition();
            Debug.Log($"{data.itemName} 아이템이 원래 위치로 돌아갔습니다.");
        }
        else
        {
            Debug.Log($"{data.itemName} 아이템이 드롭되었습니다.");
        }
    }
    
    // 원래 위치로 돌아가는 함수
    public void ReturnToOriginalPosition()
    {
        transform.SetParent(originalParent);
        transform.position = originalPosition;
    }
    
    // 아이템 데이터 설정
    public void SetItemData(ItemData itemData)
    {
        data = itemData;
        
        // 아이콘 업데이트
        Image image = GetComponent<Image>();
        if (image != null && data != null)
        {
            image.sprite = data.icon;
        }
    }
}
