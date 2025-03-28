using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NpcMove : MonoBehaviour
{
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
    
    // NPC 상태
    public enum NpcState { Idle, Moving, Interacting }
    private NpcState currentState = NpcState.Idle;
    
    void Start()
    {
        // 컴포넌트 초기화
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // 애니메이터가 지정되지 않은 경우 자동으로 찾기
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
        
        // 초기 위치 저장
        initialPosition = transform.position;
        
        // 첫 번째 행동 결정
        DecideNextAction();
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
        if (moveTimer >= Random.Range(moveTimeMin, moveTimeMax))
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
}