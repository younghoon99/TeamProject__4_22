using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.Events;

// 내부 스크립트 참조 - 코드 중복 방지를 위한 주석
// 참고: NpcData, NpcAbility, SynergyData 등 클래스는 별도 파일에 정의됨

public class Npc : MonoBehaviour
{
    [Header("NPC 데이터")]
    [SerializeField] private NpcData npcData;  // NPC 데이터 참조
    [SerializeField] private string npcId;  // NPC ID (컨테이너에서 찾을 때 사용)
    private NpcData.NpcEntry npcEntry;  // 현재 NPC의 데이터 항목
    
    // 외부에서 NPC 데이터 접근용 프로퍼티
    public NpcData.NpcEntry NpcEntry => npcEntry;
    
    // 접근자 속성
    public string NpcName => npcEntry != null ? npcEntry.npcName : gameObject.name;
    
    [Header("이동 설정")]
    [SerializeField] private float moveSpeed = 1.0f;         // 이동 속도
    [SerializeField] private float idleTimeMin = 2.0f;       // 최소 정지 시간
    [SerializeField] private float idleTimeMax = 5.0f;       // 최대 정지 시간
    [SerializeField] private float moveTimeMin = 1.0f;       // 최소 이동 시간
    [SerializeField] private float moveTimeMax = 3.0f;       // 최대 이동 시간
    [SerializeField] private float movementRange = 5.0f;     // 초기 위치로부터 최대 이동 가능 거리
    
    [Header("방향 설정")]
    [SerializeField] private bool facingleft = true;        // NPC의 초기 방향 (true: 왼쪽, false: 오른쪽)
    
    [Header("컴포넌트 참조")]
    [SerializeField] private Animator animator;              // 애니메이터 참조
    
    // 내부 상태 변수
    private Vector3 initialPosition;                         // 초기 위치 저장
    private Vector2 moveDirection = Vector2.zero;            // 현재 이동 방향
    private float moveTimer = 0f;                            // 이동 타이머
    private float idleTimer = 0f;                            // 정지 타이머
    private bool isMoving = false;                           // 이동 중인지 여부
    private bool canMove = true;                             // 움직임 가능 여부 (상호작용 중에는 false)
    private Rigidbody2D rb;                                  // Rigidbody2D 참조
    private SpriteRenderer spriteRenderer;                   // SpriteRenderer 참조
    
    // NPC 능력치 변수
    private int currentHealth;
    private int maxHealth;
    private int attackPower;
    private int defensePower;
    private float currentAttackSpeed;
    private float criticalChance;
    
    // 시너지 관련 필드
    private List<NpcAbility> unlockedSpecialAbilities = new List<NpcAbility>();
    
    // 시너지 효과로 인한 보너스 스탯
    private float attackBonus = 0f;
    private float healthBonus = 0f;
    private float defenseBonus = 0f;
    private float speedBonus = 0f;
    private float criticalChanceBonus = 0f;
    private float gatheringSpeedBonus = 0f;
    private float miningAmountBonus = 0f;
    private float healingAmountBonus = 0f;
    private float damageReductionBonus = 0f;
    private float healthRegenBonus = 0f;
    private float aoeBonus = 0f;
    private float debuffDurationBonus = 0f;
    private float magicDamageBonus = 0f;
    
    // NPC 상태
    public enum NpcState { Idle, Moving, Interacting, Escaping }
    private NpcState currentState = NpcState.Idle;
    
    // 시작 시 호출됨
    private void Start()
    {
        // 컴포넌트 초기화
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        if (animator == null)
            animator = GetComponent<Animator>();
        
        // 초기 위치 저장
        initialPosition = transform.position;
        
        // 다른 에너미 오브젝트와의 충돌 무시 설정
        IgnoreCollisionsWithEnemies();
        
        // NPC ID로 데이터 항목 가져오기
        if (npcData != null && !string.IsNullOrEmpty(npcId))
        {
            npcEntry = npcData.GetNpcById(npcId);
            InitializeFromData();
        }
        
        // 초기 상태 설정 (자동으로 움직이기 시작)
        DecideNextAction();
        
        // 시너지 매니저에 등록
        if (SynergyManager.Instance != null)
        {
            SynergyManager.Instance.AddNpc(this);
        }
    }
    
    // 파괴될 때 호출됨
    private void OnDestroy()
    {
        // 시너지 매니저에서 제거
        if (SynergyManager.Instance != null)
        {
            SynergyManager.Instance.RemoveNpc(this);
        }
    }
    
    // NPC 데이터로부터 초기화
    public void InitializeFromData()
    {
        if (npcEntry == null) return;
        
        // 기본 스탯 설정
        currentHealth = npcEntry.health;
        maxHealth = npcEntry.health;
        attackPower = npcEntry.attack;
        defensePower = npcEntry.defense;
        currentAttackSpeed = npcEntry.attackSpeed;
        criticalChance = npcEntry.criticalChance;
        
        // 이동 설정 적용 (NpcData에 값이 있는 경우에만)
        if (npcEntry.moveSpeed > 0) moveSpeed = npcEntry.moveSpeed;
        if (npcEntry.idleTimeMin > 0) idleTimeMin = npcEntry.idleTimeMin;
        if (npcEntry.idleTimeMax > 0) idleTimeMax = npcEntry.idleTimeMax;
        if (npcEntry.moveTimeMin > 0) moveTimeMin = npcEntry.moveTimeMin;
        if (npcEntry.moveTimeMax > 0) moveTimeMax = npcEntry.moveTimeMax;
        
        Debug.Log($"{npcEntry.npcName} NPC가 초기화되었습니다. 체력: {maxHealth}, 공격력: {attackPower}");
    }
    
    // Enemy 오브젝트들과의 충돌 무시 설정
    private void IgnoreCollisionsWithEnemies()
    {
        Collider2D npcCollider = GetComponent<Collider2D>();
        
        if (npcCollider != null)
        {
            // 모든 Enemy와의 충돌 무시
            Enemy[] enemies = FindObjectsOfType<Enemy>();
            foreach (Enemy enemy in enemies)
            {
                Collider2D enemyCollider = enemy.GetComponent<Collider2D>();
                if (enemyCollider != null)
                {
                    Physics2D.IgnoreCollision(npcCollider, enemyCollider, true);
                }
            }
            
            // 플레이어와의 충돌 무시
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                Collider2D playerCollider = player.GetComponent<Collider2D>();
                if (playerCollider != null)
                {
                    Physics2D.IgnoreCollision(npcCollider, playerCollider, true);
                }
            }
            
            // 다른 NPC와의 충돌 무시
            Npc[] npcs = FindObjectsOfType<Npc>();
            foreach (Npc otherNpc in npcs)
            {
                // 자기 자신은 제외
                if (otherNpc != this)
                {
                    Collider2D otherNpcCollider = otherNpc.GetComponent<Collider2D>();
                    if (otherNpcCollider != null)
                    {
                        Physics2D.IgnoreCollision(npcCollider, otherNpcCollider, true);
                    }
                }
            }
        }
    }
    
    void Update()
    {
        // 상호작용 중이거나 움직임이 비활성화된 경우 움직이지 않음
        if (!canMove || currentState == NpcState.Interacting) return;
        
        // 현재 상태에 따른 처리
        switch (currentState)
        {
            case NpcState.Idle:
                HandleIdleState();
                break;
                
            case NpcState.Moving:
                HandleMovingState();
                break;
        }
        
        // 애니메이션 업데이트
        UpdateAnimation();
    }
    
    // 정지 상태 처리
    private void HandleIdleState()
    {
        // 정지 타이머 증가
        idleTimer += Time.deltaTime;
        
        // 정지 시간이 지나면 이동 상태로 전환
        if (idleTimer >= UnityEngine.Random.Range(idleTimeMin, idleTimeMax))
        {
            // 이동 방향 결정 (왼쪽 또는 오른쪽)
            float directionX = UnityEngine.Random.Range(0, 2) == 0 ? -1 : 1;
            moveDirection = new Vector2(directionX, 0);
            
            // 방향에 따라 facingleft 업데이트
            if (directionX > 0) facingleft = false;
            else facingleft = true;
            
            // 타이머 초기화 및 상태 변경
            idleTimer = 0f;
            moveTimer = 0f;
            currentState = NpcState.Moving;
            isMoving = true;
            
            // 디버그 로그
            Debug.Log($"NPC가 {(directionX < 0 ? "왼쪽" : "오른쪽")}으로 이동 시작");
        }
    }
    
    // 이동 상태 처리
    private void HandleMovingState()
    {
        // 이동 타이머 증가
        moveTimer += Time.deltaTime;
        
        // 이동 범위 체크
        Vector3 potentialPosition = transform.position + (Vector3)moveDirection * moveSpeed * Time.deltaTime;
        float distanceFromStart = Vector3.Distance(initialPosition, potentialPosition);
        
        // 이동 범위를 벗어나지 않는 경우에만 이동
        if (distanceFromStart <= movementRange)
        {
            // 물리 기반 이동
            rb.velocity = moveDirection * moveSpeed;
        }
        else
        {
            // 반대 방향으로 전환
            moveDirection = -moveDirection;
            Debug.Log("NPC가 이동 범위 한계에 도달하여 방향을 바꿨습니다");
        }
        
        // 이동 시간이 지나면 정지 상태로 전환
        if (moveTimer >= UnityEngine.Random.Range(moveTimeMin, moveTimeMax))
        {
            // 속도 초기화 및 상태 변경
            rb.velocity = Vector2.zero;
            moveTimer = 0f;
            idleTimer = 0f;
            currentState = NpcState.Idle;
            isMoving = false;
            
            Debug.Log("NPC가 이동을 멈추고 대기 상태로 전환");
        }
    }
    
    // 애니메이션 업데이트
    private void UpdateAnimation()
    {
        if (animator != null)
        {
            // 이동 애니메이션 파라미터 업데이트
            animator.SetBool("1_Move", isMoving);
            
            // 스프라이트 방향 업데이트 (localScale 사용)
            Vector3 newScale = transform.localScale;
            // facingleft가 true이면 양수 스케일, false이면 음수 스케일
            newScale.x = Mathf.Abs(newScale.x) * (facingleft ? 1 : -1);
            transform.localScale = newScale;
        }
    }
    
    // 다음 행동 결정
    private void DecideNextAction()
    {
        // 랜덤하게 첫 상태 결정
        if (UnityEngine.Random.Range(0, 2) == 0)
        {
            currentState = NpcState.Idle;
            idleTimer = 0f;
        }
        else
        {
            currentState = NpcState.Moving;
            moveTimer = 0f;
            float directionX = UnityEngine.Random.Range(0, 2) == 0 ? -1 : 1;
            moveDirection = new Vector2(directionX, 0);
            isMoving = true;
            
            // 방향에 따라 facingleft 업데이트
            if (directionX > 0) facingleft = false;
            else facingleft = true;
        }
    }
    
    // 상호작용 시작 (NpcInteraction에서 호출)
    public void OnInteractionStart()
    {
        // 상호작용 시작 시 이동 중지
        canMove = false;
        rb.velocity = Vector2.zero;
        currentState = NpcState.Interacting;
        isMoving = false;
        
        // 애니메이션 업데이트 (이동 중지)
        if (animator != null)
        {
            animator.SetBool("1_Move", false);
        }
        
        Debug.Log("NPC가 플레이어와의 상호작용을 시작했습니다");
    }
    
    // 상호작용 종료 (NpcInteraction에서 호출)
    public void OnInteractionEnd()
    {
        // 상호작용 종료 시 이동 가능 상태로 복귀
        canMove = true;
        currentState = NpcState.Idle;
        idleTimer = 0f;
        
        Debug.Log("NPC가 플레이어와의 상호작용을 종료했습니다");
    }
    
    // 도망 시작 시 호출 (NpcInteraction에서 호출됨)
    public void OnEscapeStart()
    {
        // 도망 상태로 변경
        currentState = NpcState.Escaping;
        
        // 움직임 비활성화 (NpcInteraction에서 움직임 처리)
        canMove = false;
        
        // 애니메이션 설정 (이동 애니메이션)
        if (animator != null)
        {
            animator.SetBool("1_Move", true);
        }
        
        Debug.Log(gameObject.name + "이(가) 도망 상태로 전환되었습니다.");
    }
    
    // 도망 종료 시 호출 (NpcInteraction에서 호출됨)
    public void OnEscapeEnd()
    {
        // 도망 상태가 아닌 경우 무시
        if (currentState != NpcState.Escaping) return;
        
        // 원래 상태로 돌아가기
        currentState = NpcState.Idle;
        
        // 움직임 재활성화
        canMove = true;
        
        // 다음 행동 결정
        DecideNextAction();
        
        Debug.Log(gameObject.name + "이(가) 일반 상태로 돌아왔습니다.");
    }
    
    // 강제로 움직임을 중지시키는 메서드 (NpcInteraction 등에서 호출됨)
    public void ForceStopMovement()
    {
        // 물리적 속도 즉시 중지
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
        }
        
        // 이동 상태 해제
        isMoving = false;
        
        // 애니메이션 업데이트 (이동 중지)
        if (animator != null)
        {
            animator.SetBool("1_Move", false);
        }
        
        // 현재 상태가 Moving인 경우 Idle로 변경
        if (currentState == NpcState.Moving)
        {
            currentState = NpcState.Idle;
            idleTimer = 0f;
        }
        
        Debug.Log(gameObject.name + "의 움직임이 강제로 중지되었습니다.");
    }
    
    // 시너지 관련 메서드
    
    // 시너지 효과 초기화
    public void ResetSynergyEffects()
    {
        attackBonus = 0f;
        healthBonus = 0f;
        defenseBonus = 0f;
        speedBonus = 0f;
        criticalChanceBonus = 0f;
        gatheringSpeedBonus = 0f;
        miningAmountBonus = 0f;
        healingAmountBonus = 0f;
        damageReductionBonus = 0f;
        healthRegenBonus = 0f;
        aoeBonus = 0f;
        debuffDurationBonus = 0f;
        magicDamageBonus = 0f;
        
        // 특수 해금 능력 초기화
        unlockedSpecialAbilities.Clear();
        
        // 기본 스탯으로 복원
        UpdateStats();
    }
    
    // 시너지 보너스 적용
    public void ApplySynergyBonus(float attack, float health, float defense, float speed)
    {
        attackBonus += attack;
        healthBonus += health;
        defenseBonus += defense;
        speedBonus += speed;
        
        // 스탯 업데이트
        UpdateStats();
    }
    
    // 채집 보너스 적용
    public void ApplyGatheringBonus(float bonus)
    {
        gatheringSpeedBonus += bonus;
    }
    
    // 채굴 보너스 적용
    public void ApplyMiningBonus(float bonus)
    {
        miningAmountBonus += bonus;
    }
    
    // 치명타 보너스 적용
    public void ApplyCriticalChanceBonus(float bonus)
    {
        criticalChanceBonus += bonus;
        UpdateStats();
    }
    
    // 피해 감소 보너스 적용
    public void ApplyDamageReductionBonus(float bonus)
    {
        damageReductionBonus += bonus;
    }
    
    // 체력 재생 보너스 적용
    public void ApplyHealthRegenBonus(float bonus)
    {
        healthRegenBonus += bonus;
    }
    
    // 치유량 보너스 적용
    public void ApplyHealingBonus(float bonus)
    {
        healingAmountBonus += bonus;
    }
    
    // 광역 공격 보너스 적용
    public void ApplyAoeBonus(float bonus)
    {
        aoeBonus += bonus;
    }
    
    // 디버프 지속시간 보너스 적용
    public void ApplyDebuffBonus(float bonus)
    {
        debuffDurationBonus += bonus;
    }
    
    // 마법 피해 보너스 적용
    public void ApplyMagicDamageBonus(float bonus)
    {
        magicDamageBonus += bonus;
    }
    
    // 특수 능력 해금
    public void UnlockSpecialAbility(NpcAbility ability)
    {
        if (ability != null && !unlockedSpecialAbilities.Contains(ability))
        {
            unlockedSpecialAbilities.Add(ability);
            Debug.Log($"{NpcName}이(가) 새로운 특수 능력을 해금했습니다: {ability.name}");
        }
    }
    
    // 스탯 업데이트
    private void UpdateStats()
    {
        // 보너스를 적용한 최종 스탯 계산
        int finalMaxHealth = maxHealth + Mathf.RoundToInt(maxHealth * (healthBonus / 100f));
        int finalAttackPower = attackPower + Mathf.RoundToInt(attackPower * (attackBonus / 100f));
        int finalDefensePower = defensePower + Mathf.RoundToInt(defensePower * (defenseBonus / 100f));
        float finalAttackSpeed = currentAttackSpeed * (1f + (speedBonus / 100f));
        float finalCriticalChance = criticalChance + criticalChanceBonus;
        
        // 최대 체력이 증가했을 경우 현재 체력도 비율에 맞게 증가
        float healthRatio = (float)currentHealth / maxHealth;
        maxHealth = finalMaxHealth;
        currentHealth = Mathf.RoundToInt(maxHealth * healthRatio);
        
        // 다른 스탯 적용
        attackPower = finalAttackPower;
        defensePower = finalDefensePower;
        currentAttackSpeed = finalAttackSpeed;
        criticalChance = finalCriticalChance;
        
        // 이동 속도 적용
        moveSpeed = moveSpeed * (1f + (speedBonus / 100f));
    }
    
    // NPC 정보 반환 메서드
    
    // 현재 HP 반환
    public int GetCurrentHealth()
    {
        return currentHealth;
    }
    
    // 최대 HP 반환
    public int GetMaxHealth()
    {
        return maxHealth;
    }
    
    // 공격력 반환
    public int GetAttackPower()
    {
        return attackPower;
    }
    
    // 방어력 반환
    public int GetDefensePower()
    {
        return defensePower;
    }
    
    // 치명타 확률 반환
    public float GetCriticalChance()
    {
        return criticalChance;
    }
    
    // 특수 능력 목록 반환
    public List<NpcAbility> GetAbilities()
    {
        return npcEntry != null ? npcEntry.abilities : new List<NpcAbility>();
    }
    
    // 활성화된 특수 능력 목록 반환
    public List<NpcAbility> GetUnlockedAbilities()
    {
        return unlockedSpecialAbilities;
    }
    
    // 시너지 타입 목록 반환
    public List<SynergyType> GetSynergyTypes()
    {
        return npcEntry != null ? npcEntry.synergyTypes : new List<SynergyType>();
    }
    
    // 해당 시너지 타입을 가지고 있는지 확인
    public bool HasSynergyType(SynergyType type)
    {
        return npcEntry != null && npcEntry.synergyTypes != null && npcEntry.synergyTypes.Contains(type);
    }
    
    // 데미지 받기
    public void TakeDamage(int amount)
    {
        // 실제 피해량 계산 (방어력 및 피해 감소 효과 적용)
        float damageReduction = (defensePower / 100f) + (damageReductionBonus / 100f);
        int actualDamage = Mathf.Max(1, Mathf.RoundToInt(amount * (1f - damageReduction)));
        
        currentHealth -= actualDamage;
        
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
        
        Debug.Log($"{NpcName}이(가) {actualDamage}의 피해를 입었습니다. 남은 체력: {currentHealth}/{maxHealth}");
    }
    
    // 체력 회복
    public void Heal(int amount)
    {
        // 힐량 보너스 적용
        int actualHealAmount = Mathf.RoundToInt(amount * (1f + (healingAmountBonus / 100f)));
        
        currentHealth = Mathf.Min(maxHealth, currentHealth + actualHealAmount);
        
        Debug.Log($"{NpcName}이(가) {actualHealAmount}의 체력을 회복했습니다. 현재 체력: {currentHealth}/{maxHealth}");
    }
    
    // 사망 처리
    private void Die()
    {
        Debug.Log($"{NpcName}이(가) 사망했습니다.");
        // 사망 이벤트 등 추가 처리
    }
}
