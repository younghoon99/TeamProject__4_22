using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using TMPro;

public class Npc : MonoBehaviour
{
    [Header("NPC 데이터")]
    [SerializeField] private NpcData npcData;  // NPC 데이터 참조
    [SerializeField] private string npcId;     // NPC ID
    private NpcData.NpcEntry npcEntry = null;         // 현재 NPC의 데이터 항목

    // ResourceTileSpawner 참조 추가
    private ResourceTileSpawner resourceTileSpawner;

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

    [Header("UI 설정")]
    [SerializeField] private GameObject healthBarPrefab;     // 체력바 프리팹
    [SerializeField] private Vector3 healthBarOffset = new Vector3(0, 1.5f, 0); // 체력바 위치 오프셋

    [Header("컴포넌트 참조")]
    [SerializeField] private Animator animator;              // 애니메이터 참조

    // 내부 상태 변수
    private Vector3 initialPosition;                         // 초기 위치 저장
    private Vector2 moveDirection = Vector2.zero;            // 현재 이동 방향
    private float moveTimer = 0f;                            // 이동 타이머
    private float idleTimer = 0f;                            // 정지 타이머
    private bool isMoving = false;                           // 이동 중인지 여부
    private bool canMove = true;                             // 움직임 가능 여부 (상호작용 중에는 false)
    private bool randomMovementActive = true;                // 랜덤 움직임 활성화 여부
    private Rigidbody2D rb;                                  // Rigidbody2D 참조
    private SpriteRenderer spriteRenderer;                   // SpriteRenderer 참조

    // NPC 능력치 변수
    private int currentHealth;
    private int maxHealth;
    private int attackPower;
    private int miningPower;
    private int moveSpeedStat;

    // UI 관련 변수
    private GameObject healthBarObject;
    private TextMeshProUGUI nameText;
    private TextMeshProUGUI healthText;
    private Transform healthBarTransform;

    // NPC 상태
    public enum NpcState { Idle, Moving, Interacting, Escaping }
    private NpcState currentState = NpcState.Idle;

    // NPC 작업 유형
    public enum NpcTask
    {
        None,
        Woodcutting, // 나무 채집
        Mining,      // 광물 채집
        Combat       // 전투
    }

    // 현재 작업
    private NpcTask currentTask = NpcTask.None;

    // 작업 관련 변수
    private Transform targetObject = null;
    private bool isReturningToBase = false;
    private Vector3 basePosition;
    private float resourceGatheringTimer = 0f;
    private int gatheredResources = 0;

    // 시작 시 호출됨
    private void Start()
    {
        npcEntry = null; //테스트용 npcEntry 


        // 컴포넌트 초기화
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (animator == null)
            animator = GetComponent<Animator>();

        // 초기 위치 저장
        initialPosition = transform.position;

        // ResourceTileSpawner 찾기
        if (resourceTileSpawner == null)
        {
            resourceTileSpawner = FindObjectOfType<ResourceTileSpawner>();
            if (resourceTileSpawner == null)
            {
                Debug.LogWarning($"{NpcName}: Start에서 ResourceTileSpawner를 찾을 수 없습니다. 나무/광물 채집 작업이 제대로 동작하지 않을 수 있습니다.");
            }
            else
            {
                Debug.Log($"{NpcName}: ResourceTileSpawner를 성공적으로 찾았습니다.");
            }
        }

        // 다른 에너미 오브젝트와의 충돌 무시 설정
        IgnoreCollisionsWithEnemies();

        // NPC ID로 데이터 항목 가져오기
        if (npcData != null)
        {
            
            if (!string.IsNullOrEmpty(npcId))
            {
                npcEntry = npcData.GetNpcById(npcId);
                
            }
           

            // ID로 찾지 못한 경우 랜덤 NPC 생성
            if (npcEntry == null)
            {
                Debug.Log($"{NpcName}: ID로 NPC를 찾을 수 없습니다. 랜덤 NPC를 생성합니다.");
                // 등급별 확률 계산 (노말 60%, 레어 25%, 영웅 10%, 전설 5%)
                float rarityRoll = Random.Range(0f, 1f);
                NpcData.NpcRarity rarity;

                if (rarityRoll < 0.05f)
                    rarity = NpcData.NpcRarity.전설;
                else if (rarityRoll < 0.15f)
                    rarity = NpcData.NpcRarity.영웅;
                else if (rarityRoll < 0.40f)
                    rarity = NpcData.NpcRarity.레어;
                else
                    rarity = NpcData.NpcRarity.노말;

                npcEntry = npcData.GenerateRandomNpc(rarity);
                npcId = npcEntry.npcId;
            }

            // 먼저 데이터 초기화
            InitializeFromData();

            // 데이터 초기화 후 체력바 생성
            CreateHealthBar();
        }
        else
        {
            Debug.LogError("NPC 데이터가 없습니다. NPC 데이터를 할당해 주세요.");
        }

        // 초기 상태 설정 (자동으로 움직이기 시작)
        DecideNextAction();
    }

    // 매 프레임 업데이트
    private void Update()
    {
        // 상호작용 중이거나 움직임이 비활성화된 경우 움직이지 않음
        if (!canMove || currentState == NpcState.Interacting)
        {
            // 움직이지 않을 때는 속도를 0으로 설정
            if (rb != null) rb.velocity = Vector2.zero;

            // 움직임 애니메이션 비활성화
            if (animator != null) animator.SetBool("1_Move", false);
            return;
        }

        // 초기 위치로 돌아가는 중인 경우 우선 처리
        if (returningToInitialPosition)
        {
            // 초기 위치와의 거리 계산
            float distanceFromStart = Vector3.Distance(transform.position, initialPosition);

            // 초기 위치에 근접했는지 확인
            if (distanceFromStart <= movementRange * 0.3f) // 조금 더 작은 범위로 설정
            {
                // 초기 위치에 도착했으니 랜덤 이동 모드로 전환
                returningToInitialPosition = false;
                randomMovementActive = true;
                rb.velocity = Vector2.zero;
                if (animator != null) animator.SetBool("1_Move", false);
                currentState = NpcState.Idle;
                isMoving = false;
                Debug.Log($"{NpcName}이(가) 초기 위치에 도착하여 랜덤 이동 모드로 전환합니다.");
                DecideNextAction();

                // 애니메이션 업데이트
                UpdateAnimation();
                // 체력바 위치 업데이트
                UpdateHealthBarPosition();
                return;
            }
            else
            {
                // 계속 초기 위치로 이동
                Vector3 direction = (initialPosition - transform.position).normalized;
                rb.velocity = direction * moveSpeed;
                UpdateDirection(direction);
                if (animator != null) animator.SetBool("1_Move", true);

                // 애니메이션 업데이트
                UpdateAnimation();
                // 체력바 위치 업데이트
                UpdateHealthBarPosition();
                return;
            }
        }

        // 작업이 있는 경우 랜덤 이동을 하지 않음
        if (currentTask != NpcTask.None)
        {
            HandleTask();
            return;
        }

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

        // 체력바 위치 업데이트
        UpdateHealthBarPosition();
    }

    // 체력바 생성
    private void CreateHealthBar()
    {
        if (healthBarPrefab == null)
        {
            Debug.LogWarning("체력바 프리팹이 없습니다.");
            return;
        }

        // 체력바 생성
        healthBarObject = Instantiate(healthBarPrefab, transform.position + healthBarOffset, Quaternion.identity);
        healthBarObject.transform.SetParent(transform);

        // 이름 텍스트 및 체력 텍스트 찾기
        nameText = healthBarObject.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
        healthText = healthBarObject.transform.Find("HealthText")?.GetComponent<TextMeshProUGUI>();
        healthBarTransform = healthBarObject.transform;

        // 텍스트 설정
        if (nameText != null)
        {
            nameText.text = GetColoredRarityName();
        }

        UpdateHealthUI();
    }

    // 등급에 따른 색상 이름 반환
    private string GetColoredRarityName()
    {
        if (npcEntry == null) return gameObject.name;

        string colorCode;

        switch (npcEntry.rarity)
        {
            case NpcData.NpcRarity.노말:
                colorCode = "white";
                break;
            case NpcData.NpcRarity.레어:
                colorCode = "blue";
                break;
            case NpcData.NpcRarity.영웅:
                colorCode = "purple";
                break;
            case NpcData.NpcRarity.전설:
                colorCode = "orange";
                break;
            default:
                colorCode = "white";
                break;
        }

        // ID 대신 이름만 표시
        return $"<color={colorCode}>{npcEntry.npcName}</color>";
    }

    // 체력 UI 업데이트
    private void UpdateHealthUI()
    {
        if (healthText != null)
        {
            // 유효한 체력 값인지 확인
            if (currentHealth <= 0 || maxHealth <= 0)
            {
                currentHealth = (currentHealth <= 0) ? 10 : currentHealth;
                maxHealth = (maxHealth <= 0) ? 10 : maxHealth;
                Debug.LogWarning($"{npcEntry.npcName}의 체력이 유효하지 않아 기본값으로 설정했습니다.");
            }

            healthText.text = $"{currentHealth}/{maxHealth}";
        }
    }

    // 체력바 위치 업데이트
    private void UpdateHealthBarPosition()
    {
        if (healthBarTransform != null)
        {
            healthBarTransform.position = transform.position + healthBarOffset;
        }
    }

    // NPC 데이터로부터 초기화
    public void InitializeFromData()
    {
        if (npcEntry == null)
        {
            Debug.LogError("NPC 데이터 항목이 null입니다.");
            return;
        }

        // 기본 스탯 설정
        maxHealth = npcEntry.health * 10; // 체력을 10배로 늘림
        currentHealth = maxHealth;        // 현재 체력도 최대 체력으로 설정
        attackPower = npcEntry.attack;
        miningPower = npcEntry.miningPower;
        moveSpeedStat = npcEntry.moveSpeed;

        // 능력치가 0이하인 경우 최소값으로 설정 (데이터 누락 방지)
        if (currentHealth <= 0) currentHealth = 100; // 최소 체력도 10배로 늘림
        if (maxHealth <= 0) maxHealth = 100;        // 최소 체력도 10배로 늘림
        if (attackPower <= 0) attackPower = 1;
        if (miningPower <= 0) miningPower = 1;
        if (moveSpeedStat <= 0) moveSpeedStat = 1;

        // 이동 설정 적용
        moveSpeed = 0.5f + (moveSpeedStat * 0.1f); // 이동 속도는 기본 0.5 + 스탯의 10%
        idleTimeMin = npcEntry.idleTimeMin;
        idleTimeMax = npcEntry.idleTimeMax;
        moveTimeMin = npcEntry.moveTimeMin;
        moveTimeMax = npcEntry.moveTimeMax;

        Debug.Log($"{npcEntry.npcName} NPC가 초기화되었습니다: 등급-{npcEntry.rarity}, 체력-{maxHealth}, 공격력-{attackPower}, 채굴력-{miningPower}, 이동속도-{moveSpeedStat}");
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

    // 정지 상태 처리
    private void HandleIdleState()
    {
        // 정지 타이머 증가
        idleTimer += Time.deltaTime;

        // 정지 시간이 지나면 이동 상태로 전환
        if (idleTimer >= Random.Range(idleTimeMin, idleTimeMax))
        {
            // 이동 방향 결정 (왼쪽 또는 오른쪽)
            float directionX = Random.Range(0, 2) == 0 ? -1 : 1;
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
            Debug.Log($"NPC {NpcName}이(가) {(directionX < 0 ? "왼쪽" : "오른쪽")}으로 이동 시작");
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
            Debug.Log($"NPC {NpcName}이(가) 이동 범위 한계에 도달하여 방향을 바꿨습니다");

            // 방향을 바꾸고 즉시 이동하도록 설정
            rb.velocity = moveDirection * moveSpeed;

            // 방향 업데이트
            UpdateDirection(moveDirection);
        }

        // 이동 시간이 지나면 정지 상태로 전환
        if (moveTimer >= Random.Range(moveTimeMin, moveTimeMax))
        {
            // 속도 초기화 및 상태 변경
            rb.velocity = Vector2.zero;
            moveTimer = 0f;
            idleTimer = 0f;
            currentState = NpcState.Idle;
            isMoving = false;

            Debug.Log($"NPC {NpcName}이(가) 이동을 멈추고 대기 상태로 전환");
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
        if (Random.Range(0, 2) == 0)
        {
            currentState = NpcState.Idle;
            idleTimer = 0f;
        }
        else
        {
            currentState = NpcState.Moving;
            moveTimer = 0f;
            float directionX = Random.Range(0, 2) == 0 ? -1 : 1;
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

        Debug.Log($"NPC {NpcName}이(가) 플레이어와의 상호작용을 시작했습니다");
    }

    // 상호작용 종료 (NpcInteraction에서 호출)
    public void OnInteractionEnd()
    {
        // 상호작용 종료 시 이동 가능 상태로 복귀
        canMove = true;
        currentState = NpcState.Idle;
        idleTimer = 0f;

        Debug.Log($"NPC {NpcName}이(가) 플레이어와의 상호작용을 종료했습니다");
    }

    // 데미지 받기
    public void TakeDamage(int damage)
    {
        if (damage <= 0) return;

        currentHealth -= damage;

        // 체력 최소 0으로 제한
        if (currentHealth < 0)
            currentHealth = 0;

        // 체력 UI 업데이트
        UpdateHealthUI();

        Debug.Log($"NPC {NpcName}이(가) {damage}의 피해를 입었습니다. 현재 체력: {currentHealth}/{maxHealth}");

        // 사망 처리
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // 체력 회복
    public void Heal(int healAmount)
    {
        if (healAmount <= 0) return;

        currentHealth += healAmount;

        // 최대 체력 제한
        if (currentHealth > maxHealth)
            currentHealth = maxHealth;

        // 체력 UI 업데이트
        UpdateHealthUI();

        Debug.Log($"NPC {NpcName}이(가) {healAmount}만큼 회복되었습니다. 현재 체력: {currentHealth}/{maxHealth}");
    }

    // 사망 처리
    private void Die()
    {
        Debug.Log($"NPC {NpcName}이(가) 사망했습니다.");

        // 사망 애니메이션 호출 (있는 경우)
        if (animator != null && animator.HasState(0, Animator.StringToHash("Death")))
        {
            animator.SetTrigger("Death");
        }
        else
        {
            // 애니메이션이 없는 경우 즉시 파괴
            Destroy(gameObject, 0.1f);
        }

        // 이동 정지
        canMove = false;
        rb.velocity = Vector2.zero;
    }

    // NPC 정보 반환 (상호작용 UI용)
    public string GetNpcInfoText()
    {
        if (npcEntry == null) return "NPC 정보가 없습니다.";

        string coloredName = GetColoredRarityName();
        string statInfo = $"<b>공격력:</b> {attackPower}\n<b>체력:</b> {currentHealth}/{maxHealth}\n<b>채굴능력:</b> {miningPower}\n<b>이동속도:</b> {moveSpeedStat}";

        return $"{coloredName}\n\n<b>[등급 {npcEntry.rarity}]</b>\n\n{statInfo}\n\n{npcEntry.description}";
    }

    // 능력치 값들 반환 메서드
    public int GetAttackPower() => attackPower;
    public int GetMaxHealth() => maxHealth;
    public int GetCurrentHealth() => currentHealth;
    public int GetMiningPower() => miningPower;
    public int GetMoveSpeedStat() => moveSpeedStat;
    public NpcData.NpcRarity GetRarity() => npcEntry != null ? npcEntry.rarity : NpcData.NpcRarity.노말;

    // 작업 처리
    private void HandleTask()
    {
        switch (currentTask)
        {
            case NpcTask.Woodcutting:
                HandleWoodcuttingTask();
                break;
            case NpcTask.Mining:
                HandleMiningTask();
                break;
            case NpcTask.Combat:
                HandleCombatTask();
                break;
        }
    }

    // 자원 채굴 관련 변수
    private bool isWoodcutting = false;
    private bool isMining = false;
    private float resourceTimer = 0f;
    private Vector3 currentResourcePosition;
    private float resourceGatheringDuration = 3f; // 채굴 시간
    private bool isResourceAnimationPlaying = false; // 채집 애니메이션 재생 여부 - 현재 사용하지 않음

    private void HandleWoodcuttingTask()
    {
        // ResourceTileSpawner가 없는 경우 처리
        if (resourceTileSpawner == null)
        {
            resourceTileSpawner = FindObjectOfType<ResourceTileSpawner>();
            if (resourceTileSpawner == null)
            {
                Debug.LogWarning($"{NpcName}: ResourceTileSpawner를 찾을 수 없습니다.");
                DecideNextAction();
                return;
            }
        }

        // 이미 채굴 중이면 채굴 진행
        if (isWoodcutting)
        {
            resourceTimer += Time.deltaTime;

            // 채굴 완료
            if (resourceTimer >= resourceGatheringDuration)
            {
                // 채굴 완료 후 애니메이션 상태 초기화
                if (animator != null)
                {
                    animator.SetBool("6_Other", false);
                    animator.SetTrigger("CancelMining");
                }
                isResourceAnimationPlaying = false;

                // 타일 제거
                Tilemap tilemap = resourceTileSpawner.GetComponent<Tilemap>();
                if (tilemap != null)
                {
                    Vector3Int cellPosition = tilemap.WorldToCell(currentResourcePosition);
                    resourceTileSpawner.RemoveTile(cellPosition);
                    Debug.Log($"{NpcName}이(가) 나무 채굴을 완료했습니다.");
                }

                // 채굴 상태 초기화
                isWoodcutting = false;
                resourceTimer = 0f;
            }
            return;
        }

        // 가장 가까운 나무 타일 위치 찾기
        Vector3 nearestWoodPosition = resourceTileSpawner.GetNearestWoodTilePosition(transform.position);

        // 나무가 없는 경우
        if (nearestWoodPosition == Vector3.zero)
        {
            Debug.Log($"{NpcName}: 근처에 나무가 없습니다.");
            rb.velocity = Vector2.zero;
            if (animator != null) animator.SetBool("1_Move", false);
            return;
        }

        // 나무로 이동
        float distanceToWood = Vector3.Distance(transform.position, nearestWoodPosition);

        if (distanceToWood > 0.8f) // 더 가까이 접근하도록 거리 조정
        {
            // 나무로 이동
            Vector3 direction = (nearestWoodPosition - transform.position).normalized;
            rb.velocity = direction * moveSpeed;

            // 애니메이션 업데이트 - 이동 시 반드시 1_Move 애니메이션만 활성화
            if (animator != null)
            {
                animator.SetBool("1_Move", true);
                animator.SetBool("6_Other", false); // 이동 시에는 채집 애니메이션 비활성화
            }

            // 방향 설정
            UpdateDirection(direction);
            Debug.Log($"{NpcName}이(가) 나무를 향해 이동 중입니다.");
        }
        else
        {
            // 나무 근처에 도착하면 채굴 시작
            rb.velocity = Vector2.zero;

            // 애니메이션 업데이트 (이동 중지, 채굴 시작)
            if (animator != null)
            {
                animator.SetBool("1_Move", false);
                animator.SetBool("6_Other", true); // 채굴 애니메이션 시작
                isResourceAnimationPlaying = true; // 채집 애니메이션 재생 상태 표시
            }

            // 현재 자원 위치 저장
            currentResourcePosition = nearestWoodPosition;

            // 채굴 상태 설정
            isWoodcutting = true;
            resourceTimer = 0f;

            Debug.Log($"{NpcName}이(가) 나무 채굴을 시작합니다.");
        }
    }

    // 광물 채집 처리
    private void HandleMiningTask()
    {
        // ResourceTileSpawner가 없는 경우 처리
        if (resourceTileSpawner == null)
        {
            resourceTileSpawner = FindObjectOfType<ResourceTileSpawner>();
            if (resourceTileSpawner == null)
            {
                Debug.LogWarning($"{NpcName}: ResourceTileSpawner를 찾을 수 없습니다.");
                rb.velocity = Vector2.zero;
                if (animator != null) animator.SetBool("1_Move", false);
                return;
            }
        }

        // 이미 채굴 중이면 채굴 진행
        if (isMining)
        {
            resourceTimer += Time.deltaTime;

            // 채굴 완료
            if (resourceTimer >= resourceGatheringDuration)
            {
                // 채굴 완료 후 애니메이션 상태 초기화
                if (animator != null)
                {
                    animator.SetBool("6_Other", false);
                    animator.SetTrigger("CancelMining");
                }
                isResourceAnimationPlaying = false;

                // 타일 제거
                Tilemap tilemap = resourceTileSpawner.GetComponent<Tilemap>();
                if (tilemap != null)
                {
                    Vector3Int cellPosition = tilemap.WorldToCell(currentResourcePosition);
                    resourceTileSpawner.RemoveTile(cellPosition);
                    Debug.Log($"{NpcName}이(가) 돌 채굴을 완료했습니다.");
                }

                // 채굴 상태 초기화
                isMining = false;
                resourceTimer = 0f;
            }
            return;
        }

        // 가장 가까운 돌 타일 위치 찾기
        Vector3 nearestStonePosition = resourceTileSpawner.GetNearestStoneTilePosition(transform.position);

        // 돌이 없는 경우
        if (nearestStonePosition == Vector3.zero)
        {
            Debug.Log($"{NpcName}: 근처에 돌이 없습니다.");
            rb.velocity = Vector2.zero;
            if (animator != null) animator.SetBool("1_Move", false);
            return;
        }

        // 돌로 이동
        float distanceToStone = Vector3.Distance(transform.position, nearestStonePosition);

        if (distanceToStone > 0.8f) // 더 가까이 접근하도록 거리 조정
        {
            // 돌로 이동
            Vector3 direction = (nearestStonePosition - transform.position).normalized;
            rb.velocity = direction * moveSpeed;

            // 애니메이션 업데이트 - 이동 시 반드시 1_Move 애니메이션만 활성화
            if (animator != null)
            {
                animator.SetBool("1_Move", true);
                animator.SetBool("6_Other", false); // 이동 시에는 채집 애니메이션 비활성화
            }

            // 방향 설정
            UpdateDirection(direction);
            Debug.Log($"{NpcName}이(가) 돌을 향해 이동 중입니다.");
        }
        else
        {
            // 돌 근처에 도착하면 채굴 시작
            rb.velocity = Vector2.zero;

            // 애니메이션 업데이트 (이동 중지, 채굴 시작)
            if (animator != null)
            {
                animator.SetBool("1_Move", false);
                animator.SetBool("6_Other", true); // 채굴 애니메이션 시작
                isResourceAnimationPlaying = true; // 채집 애니메이션 재생 상태 표시
            }

            // 현재 자원 위치 저장
            currentResourcePosition = nearestStonePosition;

            // 채굴 상태 설정
            isMining = true;
            resourceTimer = 0f;

            Debug.Log($"{NpcName}이(가) 돌 채굴을 시작합니다.");
        }
    }

    // 전투 처리 - 적에게 이동만 구현
    private void HandleCombatTask()
    {
        // 적 찾기 및 이동 로직
        GameObject nearestEnemy = FindNearestObjectWithTag("Enemy");
        if (nearestEnemy != null)
        {
            // 적으로 이동
            float distanceToEnemy = Vector3.Distance(transform.position, nearestEnemy.transform.position);

            if (distanceToEnemy > 1.0f)
            {
                // 적에게 이동
                Vector3 direction = (nearestEnemy.transform.position - transform.position).normalized;
                rb.velocity = direction * moveSpeed;

                // 애니메이션 업데이트
                if (animator != null) animator.SetBool("1_Move", true);

                // 방향 설정
                UpdateDirection(direction);
                Debug.Log($"{NpcName}이(가) 적을 향해 이동 중입니다.");
            }
            else
            {
                // 적 근처에 도착하면 정지
                rb.velocity = Vector2.zero;
                if (animator != null) animator.SetBool("1_Move", false);
                Debug.Log($"{NpcName}이(가) 적에게 도착했습니다.");
            }
        }
        else
        {
            // 적이 없는 경우 정지
            rb.velocity = Vector2.zero;
            if (animator != null) animator.SetBool("1_Move", false);
            Debug.Log($"{NpcName}: 근처에 적이 없습니다.");
        }
    }

    // 태그로 가장 가까운 오브젝트 찾기
    private GameObject FindNearestObjectWithTag(string tag)
    {
        GameObject[] objects = GameObject.FindGameObjectsWithTag(tag);
        GameObject nearest = null;
        float minDistance = float.MaxValue;

        foreach (GameObject obj in objects)
        {
            float distance = Vector3.Distance(transform.position, obj.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = obj;
            }
        }

        return nearest;
    }

    // 방향 업데이트
    private void UpdateDirection(Vector3 direction)
    {
        if (direction.x > 0)
        {
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
            facingleft = false;
        }
        else if (direction.x < 0)
        {
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
            facingleft = true;
        }
    }

    // 현재 작업 중지
    private void StopCurrentTask()
    {
        if (currentTask == NpcTask.None) return;

        Debug.Log($"{NpcName}의 현재 작업이 중지되었습니다.");

        // NPC 작업 초기화
        SetTask(NpcTask.None);
    }

    // 작업 설정
    private bool returningToInitialPosition = false; // 초기 위치로 돌아가는 중인지 표시

    public void SetTask(NpcTask task)
    {
        currentTask = task;

        if (task != NpcTask.None)
        {
            // 새로운 작업이 설정되면 초기 위치로 돌아가는 상태 초기화
            returningToInitialPosition = false;
            randomMovementActive = false;
            Debug.Log($"{NpcName}이(가) {task} 작업을 시작합니다.");
        }
        else
        {
            // 작업이 초기화되면 NPC를 초기 위치 근처로 이동시키고 랜덤 이동 상태로 설정
            Debug.Log($"{NpcName}이(가) 작업을 중지하고 랜덤 이동 모드로 전환합니다.");

            // 현재 위치가 초기 위치에서 멀어졌다면 초기 위치 근처로 이동
            float distanceFromStart = Vector3.Distance(transform.position, initialPosition);
            if (distanceFromStart > movementRange * 0.5f)
            {
                // 초기 위치로 돌아가는 상태로 설정
                returningToInitialPosition = true;
                randomMovementActive = false; // 랜덤 이동 비활성화

                // 초기 위치 방향으로 이동하는 로직 추가
                Vector3 direction = (initialPosition - transform.position).normalized;
                rb.velocity = direction * moveSpeed;

                // 애니메이션 업데이트
                if (animator != null) 
                {
                    animator.SetTrigger("CancelMining");
                    animator.SetBool("1_Move", true);
                }
                
                // 방향 설정
                UpdateDirection(direction);
                Debug.Log($"{NpcName}이(가) 초기 위치로 돌아가는 중입니다.");

                // 상태 변경
                currentState = NpcState.Moving;
                isMoving = true;
            }
            else
            {
                // 초기 위치 근처에 있는 경우 정지 후 다음 행동 결정
                returningToInitialPosition = false;
                randomMovementActive = true; // 랜덤 이동 활성화
                rb.velocity = Vector2.zero;
                if (animator != null) animator.SetBool("1_Move", false);
                currentState = NpcState.Idle;
                isMoving = false;
                DecideNextAction();
            }
        }
    }
}
