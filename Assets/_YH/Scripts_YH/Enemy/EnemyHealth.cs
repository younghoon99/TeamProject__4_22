using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class EnemyHealth : MonoBehaviour
{
    [Header("체력 설정")]
    [SerializeField] private float maxHealth = 100f;      // 최대 체력
    [SerializeField] private float currentHealth;         // 현재 체력
    
    [Header("UI 설정")]
    [SerializeField] private Image healthBarImage;        // 체력바 이미지 (Fill 방식)
    [SerializeField] private float smoothSpeed = 5f;      // 체력바 변화 속도
    [SerializeField] private GameObject floatingDamageTextPrefab; // 데미지 텍스트 프리팹
    [SerializeField] private Canvas worldCanvas;          // 월드 캔버스 (없으면 자동 생성)
    [SerializeField] private Vector3 healthBarOffset = new Vector3(0, 1.5f, 0); // 체력바 위치 오프셋
    private float targetFill;                            // 목표 체력바 비율
    
    [Header("피격 효과")]
    [SerializeField] private float invincibilityTime = 0.5f; // 무적 시간
    [SerializeField] private float blinkRate = 0.1f;      // 깜빡임 간격 (초)
    private bool isInvincible = false;                    // 무적 상태 여부 
    
    // 컴포넌트 참조
    private Animator animator;
    private SpriteRenderer[] spriteRenderers;
    private Rigidbody2D rb;
    
    // 원래 색상 저장용 딕셔너리
    private Dictionary<SpriteRenderer, Color> originalColors = new Dictionary<SpriteRenderer, Color>();
    
    // 상태 관리
    private bool isDead = false;

    void Start()
    {
        // 초기 체력을 최대 체력으로 설정
        currentHealth = maxHealth;
        targetFill = 1f;
        
        // 컴포넌트 참조 가져오기
        animator = GetComponentInChildren<Animator>();
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        
        // 각 스프라이트 렌더러의 원래 색상 저장
        SaveOriginalColors();
        
        // 월드 캔버스가 없으면 생성
        SetupWorldCanvas();
        
        // 체력바 초기화
        UpdateHealthBar();
    }
    
    // 원래 색상 저장 함수
    private void SaveOriginalColors()
    {
        originalColors.Clear();
        if (spriteRenderers != null && spriteRenderers.Length > 0)
        {
            foreach (SpriteRenderer renderer in spriteRenderers)
            {
                if (renderer != null)
                {
                    originalColors[renderer] = renderer.color;
                }
            }
        }
    }
    
    void Update()
    {
        // 체력바 부드럽게 변화
        if (healthBarImage != null)
        {
            healthBarImage.fillAmount = Mathf.Lerp(healthBarImage.fillAmount, targetFill, Time.deltaTime * smoothSpeed);
            
            // 체력바가 적의 머리 위를 따라다니도록 설정
            Transform healthBarTransform = healthBarImage.transform.parent;
            if (healthBarTransform != null)
            {
                // 적 머리 위에 위치하도록 설정
                healthBarTransform.position = transform.position + healthBarOffset;
                
                // 체력바가 항상 카메라를 향하도록 설정 (빌보드 효과)
                if (Camera.main != null)
                {
                    healthBarTransform.LookAt(healthBarTransform.position + Camera.main.transform.forward);
                }
            }
        }
    }

    // 데미지 처리 함수
    public void TakeDamage(float damage, Vector2 hitPosition = default)
    {
        // 죽었거나 무적 상태면 데미지를 받지 않음
        if (isDead || isInvincible) return;
        
        // 체력 감소
        currentHealth = Mathf.Max(0, currentHealth - damage);
        
        // 데미지 텍스트 생성
        ShowDamageText(damage);
        
        // 체력바 업데이트
        UpdateHealthBar();
        
        // 애니메이션 재생
        if (animator != null)
        {
            animator.SetTrigger("3_Damaged");
        }
        
        // 피격 효과 코루틴 실행
        if (gameObject.activeInHierarchy)
        {
            isInvincible = true;
            StartCoroutine(InvincibilityCoroutine());
        }
        
        // 디버그 출력
        Debug.Log(gameObject.name + "이(가) " + damage + " 데미지를 받았습니다. 남은 체력: " + currentHealth);
        
        // 체력이 0이 되면 사망 처리
        if (currentHealth <= 0 && !isDead)
        {
            Die();
        }
    }
    
    // 무적 시간 및 깜빡임 효과 코루틴
    private System.Collections.IEnumerator InvincibilityCoroutine()
    {
        // 캐릭터 깜빡임 효과
        float endTime = Time.time + invincibilityTime;
        bool visible = false;
        
        while (Time.time < endTime)
        {
            // 캐릭터 가시성 전환
            visible = !visible;
            SetCharacterVisibility(visible);
            
            yield return new WaitForSeconds(blinkRate);
        }
        
        // 깜빡임 종료 후 원래 색상으로 복원
        RestoreOriginalColors();
        
        // 무적 해제
        isInvincible = false;
    }
    
    // 원래 색상으로 복원하는 함수
    private void RestoreOriginalColors()
    {
        if (spriteRenderers != null && spriteRenderers.Length > 0)
        {
            foreach (SpriteRenderer renderer in spriteRenderers)
            {
                if (renderer != null && originalColors.ContainsKey(renderer))
                {
                    renderer.color = originalColors[renderer];
                }
            }
        }
    }
    
    // 캐릭터 가시성 설정
    private void SetCharacterVisibility(bool visible)
    {
        if (spriteRenderers != null && spriteRenderers.Length > 0)
        {
            foreach (SpriteRenderer renderer in spriteRenderers)
            {
                if (renderer != null)
                {
                    if (visible)
                    {
                        // 원래 색상이 저장되어 있다면 사용, 아니면 흰색 사용
                        if (originalColors.ContainsKey(renderer))
                        {
                            Color originalColor = originalColors[renderer];
                            renderer.color = originalColor;
                        }
                        else
                        {
                            renderer.color = Color.white;
                        }
                    }
                    else
                    {
                        // 피격 시 흰색 적용
                        Color whiteColor = Color.white;
                        whiteColor.a = 0.5f; // 반투명 흰색
                        renderer.color = whiteColor;
                    }
                }
            }
        }
    }
    
    // 사망 처리 함수
    private void Die()
    {
        // 사망 상태로 변경
        isDead = true;
        
        // 애니메이션 재생
        if (animator != null)
        {
            animator.SetTrigger("4_Death");
            // 애니메이션 재생 시간을 고려하여 오브젝트 제거 지연
            Invoke("DestroyEnemy", 1f);
        }
        else
        {
            // 애니메이터가 없으면 바로 제거
            DestroyEnemy();
        }
        
        // 컴포넌트 비활성화 (충돌 등)
        if (GetComponent<Collider2D>() != null)
        {
            GetComponent<Collider2D>().enabled = false;
        }
        
        // Enemy 스크립트 비활성화
        Enemy enemyScript = GetComponent<Enemy>();
        if (enemyScript != null)
        {
            enemyScript.enabled = false;
        }
        
        // 디버그 출력
        Debug.Log(gameObject.name + "이(가) 사망했습니다.");
    }
    
    // 적 오브젝트 제거 함수
    private void DestroyEnemy()
    {
        Destroy(gameObject);
    }
    
    // 체력바 업데이트 함수
    private void UpdateHealthBar()
    {
        if (healthBarImage != null)
        {
            targetFill = currentHealth / maxHealth;
        }
    }
    
    // 데미지 텍스트 표시 함수
    private void ShowDamageText(float damage)
    {
        if (floatingDamageTextPrefab != null && worldCanvas != null)
        {
            // 적 머리 위에 데미지 텍스트 생성
            GameObject damageTextObj = Instantiate(floatingDamageTextPrefab, transform.position + Vector3.up * 1.2f, Quaternion.identity, worldCanvas.transform);
            
            // TextMeshProUGUI 컴포넌트 검색
            TextMeshProUGUI damageText = damageTextObj.GetComponent<TextMeshProUGUI>();
            if (damageText == null)
            {
                // 일반 TextMesh 또는 Text 컴포넌트 검색
                TextMesh textMesh = damageTextObj.GetComponent<TextMesh>();
                if (textMesh != null)
                {
                    textMesh.text = damage.ToString("0");
                    textMesh.color = Color.red;
                }
                else
                {
                    Text uiText = damageTextObj.GetComponent<Text>();
                    if (uiText != null)
                    {
                        uiText.text = damage.ToString("0");
                        uiText.color = Color.red;
                    }
                }
            }
            else
            {
                // TextMeshProUGUI 구성
                damageText.text = damage.ToString("0");
                damageText.color = Color.red;
            }
            
            // 데미지 텍스트 애니메이션
            StartCoroutine(AnimateDamageText(damageTextObj));
        }
    }
    
    // 데미지 텍스트 애니메이션 코루틴
    private System.Collections.IEnumerator AnimateDamageText(GameObject textObj)
    {
        float duration = 1.0f;
        float startTime = Time.time;
        Vector3 startOffset = Vector3.up * 1.2f;
        
        // 2초 동안 위로 움직이면서 페이드아웃
        while (Time.time < startTime + duration)
        {
            float progress = (Time.time - startTime) / duration;
            
            // 적 위치를 계속 추적하면서 점점 위로 올라가는 효과 적용
            textObj.transform.position = transform.position + startOffset + Vector3.up * progress * 0.5f;
            
            // UI가 항상 카메라를 향하도록 설정 (빌보드 효과)
            if (Camera.main != null)
            {
                textObj.transform.LookAt(textObj.transform.position + Camera.main.transform.forward);
            }
            
            // 텍스트 컴포넌트 찾아서 알파값 조절
            TextMeshProUGUI tmpText = textObj.GetComponent<TextMeshProUGUI>();
            if (tmpText != null)
            {
                Color color = tmpText.color;
                color.a = 1f - progress;
                tmpText.color = color;
            }
            else
            {
                TextMesh textMesh = textObj.GetComponent<TextMesh>();
                if (textMesh != null)
                {
                    Color color = textMesh.color;
                    color.a = 1f - progress;
                    textMesh.color = color;
                }
                else
                {
                    Text uiText = textObj.GetComponent<Text>();
                    if (uiText != null)
                    {
                        Color color = uiText.color;
                        color.a = 1f - progress;
                        uiText.color = color;
                    }
                }
            }
            
            yield return null;
        }
        
        // 애니메이션 끝나면 오브젝트 제거
        Destroy(textObj);
    }
    
    // 월드 캔버스 설정
    private void SetupWorldCanvas()
    {
        // 월드 캔버스가 없다면 생성
        if (worldCanvas == null)
        {
            // 씬에서 이미 존재하는 WorldCanvas 검색
            Canvas[] canvases = FindObjectsOfType<Canvas>();
            foreach (Canvas canvas in canvases)
            {
                if (canvas.name == "WorldCanvas")
                {
                    worldCanvas = canvas;
                    break;
                }
            }
            
            // 캔버스가 없다면 새로 생성
            if (worldCanvas == null)
            {
                GameObject canvasObj = new GameObject("WorldCanvas");
                worldCanvas = canvasObj.AddComponent<Canvas>();
                worldCanvas.renderMode = RenderMode.WorldSpace;
                
                // 캔버스 스케일러 추가
                CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.dynamicPixelsPerUnit = 100f;
                
                // 캔버스가 항상 카메라를 향하도록 빌보드 효과 추가
                canvasObj.AddComponent<Billboard>();
                
                Debug.Log("월드 캔버스가 자동 생성되었습니다.");
            }
        }
    }
    
    // 체력 회복 함수
    public void Heal(float amount)
    {
        if (isDead) return;
        
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        UpdateHealthBar();
        
        Debug.Log(gameObject.name + "이(가) " + amount + " 만큼 체력을 회복했습니다. 현재 체력: " + currentHealth);
    }
    
    // 현재 체력 비율 반환 함수 (0-1 사이 값)
    public float GetHealthPercent()
    {
        return currentHealth / maxHealth;
    }
    
    // 죽었는지 여부 반환
    public bool IsDead()
    {
        return isDead;
    }
}

// 항상 카메라를 향하게 하는 빌보드 클래스
public class Billboard : MonoBehaviour
{
    private Camera mainCamera;
    
    void Start()
    {
        mainCamera = Camera.main;
    }
    
    void LateUpdate()
    {
        if (mainCamera != null)
        {
            transform.LookAt(transform.position + mainCamera.transform.forward);
        }
    }
}
