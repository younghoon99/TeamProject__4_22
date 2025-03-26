using UnityEngine;

public class DayCycleController : MonoBehaviour
{
    [Header("셰이더 설정")]
    public Material skyMaterial;                     // 하늘 재질

    [Header("시간 설정")]
    [Range(0, 1)]
    public float timeOfDay = 0;                      // 시간 (0~1)
    public float cycleSpeed = 0.1f;                  // 시간 흐름 속도
    public bool autoUpdateTime = true;               // 자동 시간 업데이트 여부

    [Header("주기 설정")]
    public float dawnTime = 0.25f;                   // 새벽 시간
    public float dayTime = 0.4f;                     // 낮 시간
    public float sunsetTime = 0.75f;                 // 노을 시간
    public float nightTime = 0.9f;                   // 밤 시간

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
        }
    }

    // 셰이더에 시간 및 관련 값 전달
    private void UpdateShaderTime()
    {
        if (skyMaterial != null)
        {
            // 기본 시간값 전달
            skyMaterial.SetFloat("_TimeOfDay", timeOfDay);

            // 추가 파라미터 설정 예시
            skyMaterial.SetFloat("_DawnTime", dawnTime);
            skyMaterial.SetFloat("_DayTime", dayTime);
            skyMaterial.SetFloat("_SunsetTime", sunsetTime);
            skyMaterial.SetFloat("_NightTime", nightTime);
        }
    }

    // 외부에서 시간을 직접 설정할 수 있는 메서드
    public void SetTimeOfDay(float newTime)
    {
        timeOfDay = Mathf.Clamp01(newTime);  // 0~1 사이로 제한
        UpdateShaderTime();
    }
}