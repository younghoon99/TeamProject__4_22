using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NpcInteraction : MonoBehaviour
{
    [Header("상호작용 설정")]
    [SerializeField] private float interactionDistance = 3f;  // 상호작용 가능 거리
    [SerializeField] private GameObject interactionUI;        // NPC 머리 위에 표시될 UI
    [SerializeField] private Transform uiPosition;            // UI가 표시될 위치 (주로 NPC 머리 위)
    
    [Header("적 탐지 설정")]
    [SerializeField] private float enemyDetectionRange = 5f; // 적 탐지 범위
    [SerializeField] private float escapeSpeed = 3f;         // 도망 속도
    [SerializeField] private float safeDistance = 7f;        // 안전 거리 (이 거리 이상 멀어지면 원래 행동으로 돌아감)
    
    [Header("NPC 공격 설정")]
    [SerializeField] private float attackRange = 1.5f;       // 공격 범위
    [SerializeField] private float attackDamage = 10f;       // 공격 데미지
    [SerializeField] private float attackCooldown = 2f;      // 공격 쿨다운
    
    [Header("디버그")]
    [SerializeField] private bool showDebugInfo = false;     // 디버그 정보 표시 여부
    
    // 컴포넌트 참조
    private Transform playerTransform;                        // 플레이어 트랜스폼
    private Npc npcController;                               // NPC 스크립트
    private Rigidbody2D rb;                                  // Rigidbody2D
    private Animator animator;                                // 애니메이터
    
    // 상태 변수
    private bool isPlayerInRange = false;                     // 플레이어가 범위 내에 있는지 여부
    private bool isUIActive = false;                          // UI가 활성화되어 있는지 여부
    private bool isWaiting = false;                           // 대기 상태인지 여부
    private bool isAttacking = false;                         // 공격 상태인지 여부
    private float nextAttackTime = 0f;                        // 다음 공격 가능 시간
    
    // 적 관련 변수
    private Enemy nearestEnemy;                               // 가장 가까운 적
    private bool isEscaping = false;                          // 도망가는 중인지 여부
    private Vector2 escapeDirection;                          // 도망가는 방향
    
    void Start()
    {
        // 컴포넌트 초기화
        npcController = GetComponent<Npc>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();
        
        // UI 초기 상태 비활성화
        if (interactionUI != null)
        {
            interactionUI.SetActive(false);
        }
        
        // UI 위치 설정이 없으면 현재 트랜스폼 사용
        if (uiPosition == null)
        {
            uiPosition = transform;
        }
        
        // 플레이어 찾기
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
        else
        {
            Debug.LogWarning("Player 태그를 가진 오브젝트를 찾을 수 없습니다.");
        }
        
        // 버튼 리스너 설정
        SetupButtonListeners();
    }

    // 버튼 리스너 설정
    private void SetupButtonListeners()
    {
        if (interactionUI != null)
        {
            // 대기 버튼 찾기
            Button waitButton = FindButtonInUI("WaitButton");
            if (waitButton != null)
            {
                waitButton.onClick.AddListener(OnWaitButtonClicked);
            }
            
            // 공격 버튼 찾기
            Button attackButton = FindButtonInUI("AttackButton");
            if (attackButton != null)
            {
                attackButton.onClick.AddListener(OnAttackButtonClicked);
            }
        }
    }
    
    // UI에서 특정 이름의 버튼 찾기
    private Button FindButtonInUI(string buttonName)
    {
        Button[] buttons = interactionUI.GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            if (button.name.Contains(buttonName))
            {
                return button;
            }
        }
        Debug.LogWarning($"{buttonName}을(를) 찾을 수 없습니다.");
        return null;
    }

    void Update()
    {
        // 플레이어 참조 확인
        CheckPlayerReference();
        
        // NPC 상태에 따른 행동 처리
        if (isWaiting)
        {
            // 대기 상태에서는 움직이지 않음
            if (rb != null) rb.velocity = Vector2.zero;
        }
        else if (isAttacking)
        {
            // 공격 모드일 때 적 추적 및 공격
            AttackNearestEnemy();
        }
        else if (isEscaping)
        {
            // 도망가는 중일 때
            EscapeFromEnemy();
        }
        else
        {
            // 일반 상태일 때 적 탐지
            DetectEnemies();
        }
        
        // 플레이어와의 상호작용 처리
        HandlePlayerInteraction();
        
        // UI 위치 업데이트 (UI가 활성화되어 있을 때만)
        if (isUIActive && interactionUI != null)
        {
            UpdateUIPosition();
        }
    }
    
    // 플레이어 참조 확인
    private void CheckPlayerReference()
    {
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }
    }
    
    // 적 탐지 처리
    private void DetectEnemies()
    {
        Enemy[] enemies = FindObjectsOfType<Enemy>();
        nearestEnemy = null;
        float closestDistance = enemyDetectionRange;
        
        foreach (Enemy enemy in enemies)
        {
            float distance = Vector2.Distance(transform.position, enemy.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                nearestEnemy = enemy;
            }
        }
        
        // 적이 탐지 범위 내에 있으면 도망가기 시작
        if (nearestEnemy != null)
        {
            isEscaping = true;
            // 적의 반대 방향으로 도망가기
            escapeDirection = ((Vector2)transform.position - (Vector2)nearestEnemy.transform.position).normalized;
            
            // NPC 컨트롤러에 도망 상태 알림
            if (npcController != null)
            {
                npcController.OnEscapeStart();
            }
            
            if (showDebugInfo)
            {
                Debug.Log("적 탐지! 도망가는 중...");
            }
        }
    }
    
    // 적으로부터 도망가기
    private void EscapeFromEnemy()
    {
        if (nearestEnemy == null)
        {
            // 적 참조가 없으면 일반 상태로 돌아가기
            isEscaping = false;
            if (npcController != null)
            {
                npcController.OnEscapeEnd();
            }
            return;
        }
        
        // 적과의 거리 계산
        float distanceToEnemy = Vector2.Distance(transform.position, nearestEnemy.transform.position);
        
        // 안전 거리보다 멀어지면 일반 상태로 돌아가기
        if (distanceToEnemy > safeDistance)
        {
            isEscaping = false;
            if (npcController != null)
            {
                npcController.OnEscapeEnd();
            }
            if (showDebugInfo)
            {
                Debug.Log("안전 거리 확보, 일반 상태로 돌아감");
            }
            return;
        }
        
        // 적의 반대 방향으로 도망가기
        escapeDirection = ((Vector2)transform.position - (Vector2)nearestEnemy.transform.position).normalized;
        rb.velocity = escapeDirection * escapeSpeed;
        
        // 애니메이션 설정
        if (animator != null)
        {
            animator.SetBool("1_Move", true);
        }
    }
    
    // 가장 가까운 적 공격
    private void AttackNearestEnemy()
    {
        if (nearestEnemy == null)
        {
            // 공격할 적이 없으면 적 찾기
            Enemy[] enemies = FindObjectsOfType<Enemy>();
            float closestDistance = Mathf.Infinity;
            
            foreach (Enemy enemy in enemies)
            {
                float distance = Vector2.Distance(transform.position, enemy.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    nearestEnemy = enemy;
                }
            }
            
            // 적이 없으면 대기 상태로 전환
            if (nearestEnemy == null)
            {
                isAttacking = false;
                if (showDebugInfo)
                {
                    Debug.Log("공격할 적이 없음, 대기 상태로 전환");
                }
                return;
            }
        }
        
        // 적과의 거리 계산
        float distanceToEnemy = Vector2.Distance(transform.position, nearestEnemy.transform.position);
        
        // 공격 범위 밖이면 적에게 이동
        if (distanceToEnemy > attackRange)
        {
            // 적 방향으로 이동
            Vector2 directionToEnemy = ((Vector2)nearestEnemy.transform.position - (Vector2)transform.position).normalized;
            rb.velocity = directionToEnemy * escapeSpeed; // 도망 속도를 재활용
            
            // 움직임 애니메이션 설정
            if (animator != null)
            {
                animator.SetBool("1_Move", true);
            }
        }
        else
        {
            // 공격 범위 내에 있으면 정지 후 공격
            rb.velocity = Vector2.zero;
            
            // 공격 쿨다운 체크
            if (Time.time >= nextAttackTime)
            {
                // 공격 애니메이션 재생
                if (animator != null)
                {
                    animator.SetBool("1_Move", false);
                    animator.SetTrigger("2_Attack");
                }
                
                // 데미지 적용 코루틴 시작
                StartCoroutine(ApplyAttackDamage());
                
                // 다음 공격 시간 설정
                nextAttackTime = Time.time + attackCooldown;
            }
        }
    }
    
    // 데미지 적용 코루틴
    private IEnumerator ApplyAttackDamage()
    {
        // 애니메이션 타이밍에 맞춰 데미지 적용 (0.3초 지연)
        yield return new WaitForSeconds(0.3f);
        
        if (nearestEnemy != null && Vector2.Distance(transform.position, nearestEnemy.transform.position) <= attackRange)
        {
            // 적 체력 컴포넌트 확인
            EnemyHealth enemyHealth = nearestEnemy.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                // 데미지 적용
                enemyHealth.TakeDamage(attackDamage, (Vector2)transform.position);
                if (showDebugInfo)
                {
                    Debug.Log($"NPC가 {nearestEnemy.name}에게 {attackDamage} 데미지를 입혔습니다.");
                }
            }
        }
    }
    
    // 플레이어 상호작용 처리
    private void HandlePlayerInteraction()
    {
        if (playerTransform == null) return;
        
        // 플레이어와의 거리 계산
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        
        // 거리 내에 있는지 확인
        isPlayerInRange = distanceToPlayer <= interactionDistance;
        
        // 우클릭 감지 및 UI 표시 처리
        if (Input.GetMouseButtonDown(1) && isPlayerInRange)
        {
            // 레이캐스트로 우클릭한 오브젝트가 이 NPC인지 확인
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);
            
            if (hit.collider != null && hit.collider.gameObject == gameObject)
            {
                ToggleInteractionUI();
            }
        }
        
        // 플레이어가 범위를 벗어나면 UI 비활성화
        if (!isPlayerInRange && isUIActive)
        {
            HideInteractionUI();
        }
    }
    
    // "대기" 버튼 클릭 이벤트
    public void OnWaitButtonClicked()
    {
        isWaiting = true;
        isAttacking = false;
        isEscaping = false;
        
        // NPC 이동 중지
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
        }
        
        // 대기 애니메이션으로 전환
        if (animator != null)
        {
            animator.SetBool("1_Move", false);
        }
        
        // NPC 컨트롤러에 대기 상태 알림
        if (npcController != null)
        {
            npcController.OnInteractionStart();
        }
        
        if (showDebugInfo)
        {
            Debug.Log("NPC가 대기 상태로 전환했습니다.");
        }
    }
    
    // "적 공격" 버튼 클릭 이벤트
    public void OnAttackButtonClicked()
    {
        isWaiting = false;
        isAttacking = true;
        isEscaping = false;
        
        // NPC 컨트롤러에 공격 상태 알림
        if (npcController != null)
        {
            npcController.OnInteractionEnd();
        }
        
        if (showDebugInfo)
        {
            Debug.Log("NPC가 공격 모드로 전환했습니다.");
        }
    }
    
    // UI 표시 상태 전환
    private void ToggleInteractionUI()
    {
        if (interactionUI != null)
        {
            isUIActive = !isUIActive;
            
            if (isUIActive)
            {
                // UI 활성화
                interactionUI.SetActive(true);
                
                // UI가 활성화될 때 위치 즉시 업데이트
                UpdateUIPosition();
                
                // NPC 움직임 멈추기
                if (npcController != null)
                {
                    npcController.OnInteractionStart();
                }
                
                // 현재 도망 중이나 공격 중이었다면 일시 중지
                isEscaping = false;
                isAttacking = false;
                
                if (showDebugInfo)
                {
                    Debug.Log("NPC 상호작용 UI가 활성화되었습니다.");
                }
            }
            else
            {
                // UI 비활성화 및 상태 초기화
                ResetInteractionState();
                
                if (showDebugInfo)
                {
                    Debug.Log("NPC 상호작용 UI가 비활성화되었습니다.");
                }
            }
        }
    }
    
    // UI 숨기기
    private void HideInteractionUI()
    {
        if (interactionUI != null && isUIActive)
        {
            // UI 비활성화 및 상태 초기화
            ResetInteractionState();
            
            if (showDebugInfo)
            {
                Debug.Log("플레이어가 범위를 벗어나 NPC 상호작용 UI가 비활성화되었습니다.");
            }
        }
    }
    
    // 상호작용 상태 초기화
    private void ResetInteractionState()
    {
        isUIActive = false;
        interactionUI.SetActive(false);
        
        // 대기 상태 해제
        isWaiting = false;
        
        // NPC 움직임 재개
        if (npcController != null)
        {
            npcController.OnInteractionEnd();
        }
    }
    
    // UI 위치 업데이트
    private void UpdateUIPosition()
    {
        if (uiPosition != null && interactionUI != null)
        {
            // UI가 항상 카메라를 향하도록 설정 (빌보드 효과)
            interactionUI.transform.position = uiPosition.position + Vector3.up * 0.5f;
            if (Camera.main != null)
            {
                interactionUI.transform.LookAt(interactionUI.transform.position + Camera.main.transform.forward);
            }
        }
    }
    
    // 디버깅 시각화
    private void OnDrawGizmosSelected()
    {
        // 상호작용 범위
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
        
        // 적 탐지 범위
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, enemyDetectionRange);
        
        // 안전 거리
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, safeDistance);
        
        // 공격 범위
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
