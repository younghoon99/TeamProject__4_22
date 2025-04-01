using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering.Universal; // Light2D를 위한 네임스페이스 추가

public class TimeControl : MonoBehaviour
{
    [Header("셰이더 설정")]
    public Material skyMaterial;                     // 하늘 재질

    [Header("시간 설정")]
    [Range(0, 1)]
    public float timeOfDay = 0;                      // 시간 (0~1)
    public float cycleSpeed = 0.033f;                // 시간 흐름 속도 (30초 주기로 설정: 1/30 = 0.033)
    public bool autoUpdateTime = true;               // 자동 시간 업데이트 여부
    private float initialTimeOfDay = 0;              // 초기 시간 값 저장 변수

    [Header("시간대 설정")]
    [Range(0, 1)] public float morningTime = 0f;     // 아침 시작 시간
    [Range(0, 1)] public float noonTime = 0.33f;     // 오후 시작 시간
    [Range(0, 1)] public float eveningTime = 0.66f;  // 저녁 시작 시간

    [Header("전환 설정")]
    public float transitionDuration = 0.167f;        // 전환 시간 (5초/30초 = 0.167)
    public float dayNightTransitionStart = 0.9f;     // 저녁→아침 전환 시작 시간 (0.9 = 하루의 90% 지점)
    public float morningTransitionDuration = 0.1f;   // 아침으로 전환되는 시간 길이 (0~morningTime 이후의 추가 전환 시간)

    [Header("조명 설정")]
    public Light2D globalLight;                      // 글로벌 라이트 참조
    public float morningLightIntensity = 1.5f;       // 아침 빛 강도
    public float noonLightIntensity = 1.5f;          // 오후 빛 강도
    public float eveningLightIntensity = 0.5f;       // 저녁 빛 강도
    public Color morningLightColor = Color.white;    // 아침 빛 색상
    public Color noonLightColor = Color.white;       // 오후 빛 색상
    public Color eveningLightColor = new Color(1f, 0.8f, 0.6f); // 저녁 빛 색상 (황금빛)

    [Header("디버그 정보")]
    public string currentTimeOfDay = "아침";         // 현재 시간대 표시
    public float currentLightIntensity;              // 현재 빛 강도 (디버깅용)
    public string currentLightPhase = "";            // 현재 빛 단계 (디버깅용)

    [Header("이벤트")]
    public UnityEvent onMorning;                     // 아침이 될 때 발생하는 이벤트
    public UnityEvent onNoon;                        // 오후가 될 때 발생하는 이벤트
    public UnityEvent onEvening;                     // 저녁이 될 때 발생하는 이벤트

    private string lastTimeOfDay = "";               // 이전 시간대
    private float lastTimeOfDayValue = 0f;           // 이전 시간 값 (전환 감지용)
    private float previousIntensity = 1.5f;          // 이전 프레임의 빛 강도
    private bool wasGamePaused = false;              // 이전 프레임의 게임 일시정지 상태
    private float initialShaderTimeOfDay = 0;        // 초기 셰이더 시간 값

    private void Start()
    {
        // 초기 시간 값 저장
        initialTimeOfDay = timeOfDay;
        
        // 초기 셰이더 시간 값 저장
        if (skyMaterial != null)
        {
            initialShaderTimeOfDay = skyMaterial.GetFloat("_TimeOfDay");
        }
        
        // 글로벌 라이트가 할당되지 않았다면 자동으로 찾기
        if (globalLight == null)
        {
            // 씬에서 Global Light 2D 컴포넌트를 가진 오브젝트 검색
            Light2D[] lights = FindObjectsOfType<Light2D>();
            foreach (Light2D light in lights)
            {
                if (light.lightType == Light2D.LightType.Global)
                {
                    globalLight = light;
                    Debug.Log("Global Light 자동 할당됨: " + globalLight.name);
                    break;
                }
            }
            
            // 찾지 못했을 경우 경고 메시지
            if (globalLight == null)
            {
                Debug.LogWarning("씬에서 Global Light 2D를 찾을 수 없습니다. 라이트 효과가 적용되지 않습니다.");
            }
        }
        
        // 초기 시간 값을 셰이더와 라이트에 전달
        UpdateShaderTime();
        UpdateGlobalLight();
        
        // 초기 강도 저장
        if (globalLight != null)
        {
            previousIntensity = globalLight.intensity;
        }
        
        // 초기 시간 저장
        lastTimeOfDayValue = timeOfDay;
    }

    private void Update()
    {
        // 게임 일시정지 상태 확인
        bool isGamePaused = Time.timeScale == 0;
        
        // 게임이 일시정지되었을 때 시간 초기화
        if (isGamePaused && !wasGamePaused)
        {
            ResetTimeToInitial();
        }
        // 게임이 일시정지 상태에서 다시 시작될 때도 초기화
        else if (!isGamePaused && wasGamePaused)
        {
            ResetTimeToInitial();
        }
        
        // 현재 일시정지 상태 저장
        wasGamePaused = isGamePaused;
        
        if (autoUpdateTime && !isGamePaused)
        {
            // 이전 시간 저장
            lastTimeOfDayValue = timeOfDay;
            
            // 자동으로 시간 진행 (0~1 사이 루프)
            timeOfDay = (timeOfDay + Time.deltaTime * cycleSpeed) % 1.0f;

            // 셰이더에 시간 값 전달
            UpdateShaderTime();
            
            // Global Light 업데이트
            UpdateGlobalLight();

            // 현재 시간대 업데이트 및 이벤트 발생
            UpdateTimeOfDayInfo();
        }
    }

    // 시간을 초기값으로 리셋하는 메서드
    public void ResetTimeToInitial()
    {
        timeOfDay = initialTimeOfDay;
        
        // 셰이더 시간 직접 초기화
        if (skyMaterial != null)
        {
            skyMaterial.SetFloat("_TimeOfDay", initialTimeOfDay);
            
            // 0.9~1.0 구간과 0.0~0.1 구간에서 전환 정보 초기화
            float transitionValue = 0;
            
            if (initialTimeOfDay > 0.9f)
            {
                // 0.9~1.0 → 0~0.5 매핑
                transitionValue = (initialTimeOfDay - 0.9f) * 5.0f; 
            }
            else if (initialTimeOfDay < 0.1f)
            {
                // 0.0~0.1 → 0.5~1.0 매핑
                transitionValue = 0.5f + initialTimeOfDay * 5.0f;
            }
            
            skyMaterial.SetFloat("_NightTransition", transitionValue);
        }
        
        // 나머지 업데이트 수행
        UpdateShaderTime();
        UpdateGlobalLight();
        UpdateTimeOfDayInfo();
        Debug.Log("시간이 초기값으로 리셋되었습니다: " + timeOfDay);
    }

    // 게임이 종료될 때 호출
    private void OnApplicationQuit()
    {
        // 게임 종료 시 시간 초기화
        if (skyMaterial != null)
        {
            skyMaterial.SetFloat("_TimeOfDay", initialTimeOfDay);
            
            // 0.9~1.0 구간과 0.0~0.1 구간에서 전환 정보 초기화
            float transitionValue = 0;
            
            if (initialTimeOfDay > 0.9f)
            {
                // 0.9~1.0 → 0~0.5 매핑
                transitionValue = (initialTimeOfDay - 0.9f) * 5.0f; 
            }
            else if (initialTimeOfDay < 0.1f)
            {
                // 0.0~0.1 → 0.5~1.0 매핑
                transitionValue = 0.5f + initialTimeOfDay * 5.0f;
            }
            
            skyMaterial.SetFloat("_NightTransition", transitionValue);
        }
        
        Debug.Log("게임 종료 시 시간 초기화: " + initialTimeOfDay);
    }
    
    // 씬이 언로드될 때 호출
    private void OnDestroy()
    {
        // 씬 언로드 시 시간 초기화
        if (skyMaterial != null)
        {
            skyMaterial.SetFloat("_TimeOfDay", initialTimeOfDay);
            
            // 0.9~1.0 구간과 0.0~0.1 구간에서 전환 정보 초기화
            float transitionValue = 0;
            
            if (initialTimeOfDay > 0.9f)
            {
                // 0.9~1.0 → 0~0.5 매핑
                transitionValue = (initialTimeOfDay - 0.9f) * 5.0f; 
            }
            else if (initialTimeOfDay < 0.1f)
            {
                // 0.0~0.1 → 0.5~1.0 매핑
                transitionValue = 0.5f + initialTimeOfDay * 5.0f;
            }
            
            skyMaterial.SetFloat("_NightTransition", transitionValue);
        }
        
        Debug.Log("씬 언로드 시 시간 초기화: " + initialTimeOfDay);
    }

    // Global Light 업데이트 메서드
    private void UpdateGlobalLight()
    {
        if (globalLight == null) return;
        
        float intensity = 1.0f;
        Color lightColor = Color.white;
        
        // 하루 끝과 시작 사이의 전환 감지 (1.0 → 0.0)
        bool dayChanged = lastTimeOfDayValue > 0.9f && timeOfDay < 0.1f;
        
        // 시간대별 빛 강도와 색상 보간
        if (timeOfDay >= dayNightTransitionStart && timeOfDay <= 1.0f)
        {
            // 하루 끝에서 다음날 시작으로의 전환 시작 (0.9 ~ 1.0)
            float progress = Mathf.InverseLerp(dayNightTransitionStart, 1.0f, timeOfDay);
            
            // 저녁 밝기에서 중간 밝기로 서서히 전환 (0.5 -> 0.75)
            intensity = Mathf.Lerp(eveningLightIntensity, eveningLightIntensity + 0.25f, progress);
            lightColor = Color.Lerp(eveningLightColor, morningLightColor, progress * 0.3f);
            
            currentLightPhase = "저녁→새벽 전환";
        }
        else if (timeOfDay >= 0f && timeOfDay < morningTime + morningTransitionDuration)
        {
            // 새날 시작에서 아침으로 전환 (0.0 ~ morningTime + morningTransitionDuration)
            // 전환 시간을 늘려서 더 서서히 밝아지게 함
            float extendedMorningTime = morningTime + morningTransitionDuration;
            float progress = Mathf.InverseLerp(0f, extendedMorningTime, timeOfDay);
            
            // 하루가 변경된 경우(1.0→0.0), 저장된 이전 강도에서 부드럽게 전환
            if (dayChanged)
            {
                // 이전 강도를 시작점으로 사용하여 부드럽게 전환
                intensity = Mathf.Lerp(previousIntensity, morningLightIntensity, progress);
                currentLightPhase = "날 변경 전환";
            }
            else
            {
                // 서서히 밝아짐 (0.75 -> 1.5)
                float startIntensity = eveningLightIntensity + 0.25f; // 이전 단계의 마지막 밝기
                intensity = Mathf.Lerp(startIntensity, morningLightIntensity, progress);
                currentLightPhase = "새벽→아침 전환";
            }
            
            lightColor = Color.Lerp(eveningLightColor, morningLightColor, 0.3f + progress * 0.7f);
        }
        else if (timeOfDay >= morningTime + morningTransitionDuration && timeOfDay < noonTime)
        {
            // 아침에서 오후로 변화
            float t = Mathf.InverseLerp(morningTime + morningTransitionDuration, noonTime, timeOfDay);
            intensity = Mathf.Lerp(morningLightIntensity, noonLightIntensity, t);
            lightColor = Color.Lerp(morningLightColor, noonLightColor, t);
            currentLightPhase = "아침→오후";
        }
        else if (timeOfDay >= noonTime && timeOfDay < eveningTime)
        {
            // 오후에서 저녁으로 변화
            float t = Mathf.InverseLerp(noonTime, eveningTime, timeOfDay);
            intensity = Mathf.Lerp(noonLightIntensity, eveningLightIntensity, t);
            lightColor = Color.Lerp(noonLightColor, eveningLightColor, t);
            currentLightPhase = "오후→저녁";
        }
        else if (timeOfDay >= eveningTime && timeOfDay < dayNightTransitionStart)
        {
            // 저녁 시간
            intensity = eveningLightIntensity;
            lightColor = eveningLightColor;
            currentLightPhase = "저녁";
        }
        
        // 라이트에 값 적용
        globalLight.intensity = intensity;
        globalLight.color = lightColor;
        
        // 현재 값 저장 (디버깅 및 다음 프레임 전환용)
        currentLightIntensity = intensity;
        previousIntensity = intensity;
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
        UpdateGlobalLight();
        UpdateTimeOfDayInfo();
    }

    // 시간대 직접 설정 메서드들
    public void SetToMorning() { SetTimeOfDay(morningTime + 0.01f); }
    public void SetToNoon() { SetTimeOfDay(noonTime + 0.01f); }
    public void SetToEvening() { SetTimeOfDay(eveningTime + 0.01f); }
    
    // 초기 시간 값 설정 메서드
    public void SetInitialTimeOfDay(float time)
    {
        initialTimeOfDay = Mathf.Clamp01(time);
    }
}