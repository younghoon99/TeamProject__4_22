using UnityEngine;
using UnityEngine.Events;

public class TimeControl : MonoBehaviour
{
    [Header("셰이더 설정")]
    public Material skyMaterial;                     // 하늘 재질

    [Header("시간 설정")]
    [Range(0, 1)]
    public float timeOfDay = 0;                      // 시간 (0~1)
    public float cycleSpeed = 0.033f;                // 시간 흐름 속도 (30초 주기로 설정: 1/30 = 0.033)
    public bool autoUpdateTime = true;               // 자동 시간 업데이트 여부

    [Header("시간대 설정")]
    [Range(0, 1)] public float morningTime = 0f;     // 아침 시작 시간
    [Range(0, 1)] public float noonTime = 0.33f;     // 오후 시작 시간
    [Range(0, 1)] public float eveningTime = 0.66f;  // 저녁 시작 시간

    [Header("전환 설정")]
    public float transitionDuration = 0.167f;        // 전환 시간 (5초/30초 = 0.167)

    [Header("디버그 정보")]
    public string currentTimeOfDay = "아침";         // 현재 시간대 표시

    [Header("이벤트")]
    public UnityEvent onMorning;                     // 아침이 될 때 발생하는 이벤트
    public UnityEvent onNoon;                        // 오후가 될 때 발생하는 이벤트
    public UnityEvent onEvening;                     // 저녁이 될 때 발생하는 이벤트

    private string lastTimeOfDay = "";               // 이전 시간대

    private void Start()
    {
        // 초기 시간 값을 셰이더에 전달
        UpdateShaderTime();
    }

    private void Update()
    {
        if (autoUpdateTime)
        {
            // 자동으로 시간 진행 (0~1 사이 루프)
            timeOfDay = (timeOfDay + Time.deltaTime * cycleSpeed) % 1.0f;

            // 셰이더에 시간 값 전달
            UpdateShaderTime();

            // 현재 시간대 업데이트 및 이벤트 발생
            UpdateTimeOfDayInfo();
        }
    }

    // 현재 시간대 정보 업데이트 및 이벤트 호출
    private void UpdateTimeOfDayInfo()
    {
        // 현재 시간대 계산
        string newTimeOfDay;

        // 아침 시간
        if (timeOfDay >= morningTime && timeOfDay < noonTime - transitionDuration)
        {
            newTimeOfDay = "아침";
        }
        // 아침→오후 전환
        else if (timeOfDay >= noonTime - transitionDuration && timeOfDay < noonTime)
        {
            newTimeOfDay = "아침→오후 전환";
        }
        // 오후 시간
        else if (timeOfDay >= noonTime && timeOfDay < eveningTime - transitionDuration)
        {
            newTimeOfDay = "오후";
        }
        // 오후→저녁 전환
        else if (timeOfDay >= eveningTime - transitionDuration && timeOfDay < eveningTime)
        {
            newTimeOfDay = "오후→저녁 전환";
        }
        // 저녁 시간 (일반)
        else if (timeOfDay >= eveningTime && timeOfDay < 1.0f - transitionDuration)
        {
            newTimeOfDay = "저녁";
        }
        // 저녁→아침 전환 (1.0 근처)
        else if (timeOfDay >= 1.0f - transitionDuration)
        {
            newTimeOfDay = "저녁→아침 전환";
        }
        // 저녁 시간 (0 근처)
        else if (timeOfDay >= 0 && timeOfDay < morningTime - transitionDuration)
        {
            newTimeOfDay = "저녁";
        }
        // 저녁→아침 전환 (0 근처)
        else
        {
            newTimeOfDay = "저녁→아침 전환";
        }

        // 시간대 변경 감지 및 이벤트 호출
        if (newTimeOfDay != lastTimeOfDay)
        {
            currentTimeOfDay = newTimeOfDay;
            lastTimeOfDay = newTimeOfDay;

            // 시간대별 이벤트 호출
            if (newTimeOfDay == "아침") onMorning?.Invoke();
            else if (newTimeOfDay == "오후") onNoon?.Invoke();
            else if (newTimeOfDay == "저녁") onEvening?.Invoke();
        }
    }

    // 셰이더에 시간 및 관련 값 전달
    private void UpdateShaderTime()
    {
        if (skyMaterial != null)
        {
            // 0.9~1.0 구간과 0.0~0.1 구간에서 전환 정보 추가
            float transitionValue = 0;
            
            if (timeOfDay > 0.9f)
            {
                // 0.9~1.0 → 0~0.5 매핑
                transitionValue = (timeOfDay - 0.9f) * 5.0f; 
            }
            else if (timeOfDay < 0.1f)
            {
                // 0.0~0.1 → 0.5~1.0 매핑
                transitionValue = 0.5f + timeOfDay * 5.0f;
            }
            
            // 기본 시간값 전달
            skyMaterial.SetFloat("_TimeOfDay", timeOfDay);
            skyMaterial.SetFloat("_NightTransition", transitionValue);

            // 시간대 설정 전달
            skyMaterial.SetFloat("_MorningTime", morningTime);
            skyMaterial.SetFloat("_NoonTime", noonTime);
            skyMaterial.SetFloat("_EveningTime", eveningTime);
            skyMaterial.SetFloat("_TransitionDuration", transitionDuration);
        }
    }

    // 외부에서 시간을 직접 설정할 수 있는 메서드
    public void SetTimeOfDay(float newTime)
    {
        timeOfDay = Mathf.Clamp01(newTime);  // 0~1 사이로 제한
        UpdateShaderTime();
        UpdateTimeOfDayInfo();
    }

    // 시간대 직접 설정 메서드들
    public void SetToMorning() { SetTimeOfDay(morningTime + 0.01f); }
    public void SetToNoon() { SetTimeOfDay(noonTime + 0.01f); }
    public void SetToEvening() { SetTimeOfDay(eveningTime + 0.01f); }
}