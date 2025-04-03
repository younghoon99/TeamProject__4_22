using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WallHealth : MonoBehaviour
{
    [Header("체력 설정")]
    public float maxHealth = 100f; // 최대 체력
    private float currentHealth;

    [Header("UI 설정")]
    public Image healthBarImage; // 체력바 이미지 (Fill 방식)
    public float smoothSpeed = 5f; // 체력바 변화 속도
    public GameObject floatingDamageTextPrefab; // 데미지 텍스트 프리팹
    public Vector3 healthBarOffset = new Vector3(0, 2f, 0); // 체력바 위치 오프셋
    private float targetFill;

    [Header("피격 효과")]
    public bool useFlashEffect = true; // 피격 시 플래시 효과 사용 여부
    public Image damageFlashImage; // 피격 시 화면 플래시 이미지
    public float flashSpeed = 5f; // 플래시 사라지는 속도
    private Color flashColor;

    private bool isDestroyed = false; // Wall 파괴 여부

    private Canvas healthCanvas; // Wall 위에 표시될 Canvas

    void Start()
    {
        // 초기 체력 설정
        currentHealth = maxHealth;
        targetFill = 1f;

        // 플래시 이미지 초기화
        if (damageFlashImage != null)
        {
            flashColor = damageFlashImage.color;
            flashColor.a = 0f;
            damageFlashImage.color = flashColor;
        }

        // 체력바 초기화
        UpdateHealthBar();
    }

    void Update()
    {
        // 체력바 부드럽게 변화
        if (healthBarImage != null)
        {
            healthBarImage.fillAmount = Mathf.Lerp(healthBarImage.fillAmount, targetFill, Time.deltaTime * smoothSpeed);

            // 체력바 위치 업데이트
            Transform healthBarTransform = healthBarImage.transform.parent;
            if (healthBarTransform != null)
            {
                healthBarTransform.position = transform.position + healthBarOffset;

                // 체력바가 항상 카메라를 향하도록 설정 (빌보드 효과)
                if (Camera.main != null)
                {
                    healthBarTransform.LookAt(healthBarTransform.position + Camera.main.transform.forward);
                }
            }
        }

        // 피격 플래시 효과 업데이트
        if (useFlashEffect && damageFlashImage != null)
        {
            if (flashColor.a > 0)
            {
                flashColor.a = Mathf.Max(0, flashColor.a - Time.deltaTime * flashSpeed);
                damageFlashImage.color = flashColor;
            }
        }
    }

    public void TakeDamage(float damage)
    {
        // 이미 파괴된 경우 무시
        if (isDestroyed) return;

        // 체력 감소
        currentHealth = Mathf.Max(0, currentHealth - damage);

        // 체력바 업데이트
        UpdateHealthBar();

        // 데미지 텍스트 표시
        ShowDamageText(damage);

        Debug.Log("Wall이 " + damage + "의 데미지를 입었습니다. 남은 체력: " + currentHealth);

        // 파괴 확인
        if (currentHealth <= 0)
        {
            DestroyWall();
        }
    }

    public float GetCurrentHealth()
    {
        return currentHealth;
    }

    private void UpdateHealthBar()
    {
        if (healthBarImage != null)
        {
            targetFill = currentHealth / maxHealth;
        }
    }

    private void ShowDamageText(float damage)
    {
        if (floatingDamageTextPrefab != null)
        {
            // Wall 위에 데미지 텍스트 생성
            GameObject damageTextObj = Instantiate(floatingDamageTextPrefab, transform.position + healthBarOffset, Quaternion.identity);

            // TextMeshProUGUI 컴포넌트 설정
            TextMeshProUGUI damageText = damageTextObj.GetComponent<TextMeshProUGUI>();
            if (damageText != null)
            {
                damageText.text = damage.ToString("0");
                damageText.color = Color.red;
            }

            // 데미지 텍스트 애니메이션
            StartCoroutine(AnimateDamageText(damageTextObj));
        }
    }

    private IEnumerator AnimateDamageText(GameObject textObj)
    {
        float duration = 1.0f;
        float startTime = Time.time;
        Vector3 startOffset = Vector3.up * 1.2f;

        while (Time.time < startTime + duration)
        {
            float progress = (Time.time - startTime) / duration;

            // 텍스트가 위로 올라가는 효과
            textObj.transform.position = transform.position + healthBarOffset + Vector3.up * progress * 0.5f;

            // 텍스트 페이드아웃
            TextMeshProUGUI tmpText = textObj.GetComponent<TextMeshProUGUI>();
            if (tmpText != null)
            {
                Color color = tmpText.color;
                color.a = 1f - progress;
                tmpText.color = color;
            }

            yield return null;
        }

        Destroy(textObj);
    }

    private void DestroyWall()
    {
        if (isDestroyed) return;

        isDestroyed = true;
        Debug.Log("Wall 파괴됨");

        // 체력바와 관련 UI 요소 비활성화
        if (healthBarImage != null)
        {
            healthBarImage.transform.parent.gameObject.SetActive(false);
        }

        // Wall 오브젝트 제거
        Destroy(gameObject);
    }
}
