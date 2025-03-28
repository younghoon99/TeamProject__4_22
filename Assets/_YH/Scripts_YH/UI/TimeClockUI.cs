using UnityEngine;
using UnityEngine.UI;

public class TimeClockUI : MonoBehaviour
{
    [Header("시계 UI 구성요소")]
    [SerializeField] private Image clockBackground;        // 시계 배경 이미지
    [SerializeField] private Image clockHand;              // 시계 침 이미지
    [SerializeField] private TimeControl timeControl;      // TimeControl 스크립트 참조
    
    [Header("시간대별 색상")]
    [SerializeField] private Color morningColor = new Color(0.95f, 0.85f, 0.6f);  // 아침 색상 (노란색)
    [SerializeField] private Color noonColor = new Color(0.95f, 0.95f, 0.95f);    // 오후 색상 (흰색)
    [SerializeField] private Color eveningColor = new Color(0.4f, 0.4f, 0.7f);    // 저녁 색상 (보라색)
    
    [Header("섹터 설정")]
    [SerializeField] private Image[] sectorImages;        // 각 섹터 이미지 배열
    
    private void Start()
    {
        // TimeControl 스크립트가 할당되지 않았으면 찾기
        if (timeControl == null)
        {
            timeControl = FindObjectOfType<TimeControl>();
            if (timeControl == null)
            {
                Debug.LogError("TimeControl 스크립트를 찾을 수 없습니다!");
                enabled = false;
                return;
            }
        }
        
        // 시작 시 시계 모양 초기화
        InitializeClockSectors();
        UpdateClockUI();
    }
    
    private void Update()
    {
        // 매 프레임마다 시계 UI 업데이트
        UpdateClockUI();
    }
    
    // 시계 섹터 초기화 (색상 배치)
    private void InitializeClockSectors()
    {
        if (sectorImages == null || sectorImages.Length == 0)
        {
            Debug.LogWarning("섹터 이미지가 할당되지 않았습니다!");
            return;
        }
        
        // 시간대별 섹터 색상 설정
        for (int i = 0; i < sectorImages.Length; i++)
        {
            if (sectorImages[i] == null) continue;
            
            // 각 섹터의 위치에 따라 시간대 색상 할당
            float sectorPosition = (float)i / sectorImages.Length;
            
            // 시간대 계산
            if (sectorPosition >= timeControl.morningTime && sectorPosition < timeControl.noonTime)
            {
                sectorImages[i].color = morningColor;
            }
            else if (sectorPosition >= timeControl.noonTime && sectorPosition < timeControl.eveningTime)
            {
                sectorImages[i].color = noonColor;
            }
            else
            {
                sectorImages[i].color = eveningColor;
            }
        }
    }
    
    // 시계 UI 업데이트 (침 회전)
    private void UpdateClockUI()
    {
        if (timeControl == null) return;
        
        // 시계 침 회전 (0~1의 timeOfDay 값을 0~360도로 변환)
        if (clockHand != null)
        {
            float rotationAngle = timeControl.timeOfDay * 360f;
            clockHand.rectTransform.rotation = Quaternion.Euler(0, 0, -rotationAngle);
        }
        
        // 현재 시간대에 맞춰 배경색 변경 (선택 사항)
        if (clockBackground != null)
        {
            Color targetColor;
            
            // 현재 시간대 확인
            if (timeControl.timeOfDay >= timeControl.morningTime && timeControl.timeOfDay < timeControl.noonTime)
            {
                targetColor = morningColor;
            }
            else if (timeControl.timeOfDay >= timeControl.noonTime && timeControl.timeOfDay < timeControl.eveningTime)
            {
                targetColor = noonColor;
            }
            else
            {
                targetColor = eveningColor;
            }
            
            // 배경색 부드럽게 변경
            clockBackground.color = Color.Lerp(clockBackground.color, targetColor, Time.deltaTime * 2f);
        }
    }
}
