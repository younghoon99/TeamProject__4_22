using UnityEngine;
using System.Collections;

public class Enemy : MonoBehaviour
{
    [Header("공격 설정")]
    public float attackDamage = 10f;       // 공격력
    public float attackRange = 1.5f;       // 공격 범위
    public float attackCooldown = 2f;      // 공격 쿨다운
    public float attackDelay = 0.3f;       // 애니메이션 재생 후 데미지 적용까지의 지연 시간
    private float nextAttackTime = 0f;     // 다음 공격 가능 시간
    
    [Header("탐지 설정")]
    public float detectionRange = 5f;      // 플레이어 탐지 범위
    private Transform player;              // 플레이어 트랜스폼
    public string targetTag = "Player";    // 추적할 대상의 태그 (기본값: Player)
    
    [Header("이동 설정")]
    public float moveSpeed = 2f;           // 이동 속도
    public float stoppingDistance = 1f;    // 정지 거리 (이 거리보다 가까우면 멈춤)
    public bool canMove = true;            // 이동 가능 여부
    private Rigidbody2D rb;                // 리지드바디 컴포넌트
    
    [Header("방향 설정")]
    public bool facingRight = true;        // 적의 초기 방향
    
    // 애니메이션 컴포넌트
    private Animator animator;
    // 스프라이트 렌더러
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        // 시작할 때 플레이어 찾기
        player = GameObject.FindGameObjectWithTag(targetTag)?.transform;
        
        // 애니메이터 컴포넌트 가져오기 (자식 객체에 있을 경우를 위해 GetComponentInChildren 사용)
        animator = GetComponentInChildren<Animator>();
        
        // 스프라이트 렌더러 가져오기
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        
        // 리지드바디 컴포넌트 가져오기
        rb = GetComponent<Rigidbody2D>();
        
        // 리지드바디가 없으면 추가
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0; // 중력 영향 없음
            rb.freezeRotation = true; // 회전 방지
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; // 연속 충돌 감지
            rb.interpolation = RigidbodyInterpolation2D.Interpolate; // 부드러운 움직임
        }
        
        // 애니메이터가 없으면 경고 메시지
        if (animator == null)
        {
            Debug.LogWarning(gameObject.name + "에 Animator 컴포넌트가 없습니다.");
        }
        
        // 플레이어를 찾지 못했을 경우 경고 메시지
        if (player == null)
        {
            Debug.LogWarning(gameObject.name + "이(가) '" + targetTag + "' 태그를 가진 대상을 찾을 수 없습니다.");
        }
    }

    void Update()
    {
        // 플레이어가 없으면 다시 찾기 시도
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag(targetTag)?.transform;
            if (player == null) return; // 여전히 없으면 함수 종료
        }
        
        // 플레이어와의 거리 계산
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        
        // 탐지 범위 내에 있는지 확인
        if (distanceToPlayer <= detectionRange)
        {
            // 플레이어 방향 바라보기
            LookAtPlayer();
            
            // 공격 범위 내에 있고 공격 쿨다운이 지났는지 확인
            if (distanceToPlayer <= attackRange && Time.time >= nextAttackTime)
            {
                // 공격 실행
                Attack();
                
                // 다음 공격 시간 설정
                nextAttackTime = Time.time + attackCooldown;
            }
            
            // 공격 범위 밖이고 정지 거리보다 멀리 있다면 플레이어에게 이동
            else if (distanceToPlayer > stoppingDistance && canMove)
            {
                MoveTowardsPlayer();
                
                // 움직임 애니메이션 재생 (있다면)
                if (animator != null)
                {
                    animator.SetBool("1_Move", true);
                }
            }
            else
            {
                // 정지
                StopMoving();
                
                // 정지 애니메이션으로 전환 (있다면)
                if (animator != null)
                {
                    animator.SetBool("1_Move", false);
                }
            }
        }
        else
        {
            // 탐지 범위 밖이면 정지
            StopMoving();
            
            // 정지 애니메이션으로 전환 (있다면)
            if (animator != null)
            {
                animator.SetBool("1_Move", false);
            }
        }
    }
    
    // 플레이어 쪽으로 이동
    private void MoveTowardsPlayer()
    {
        if (player == null || rb == null) return;
        
        // 플레이어 방향으로 향하는 벡터 계산
        Vector2 direction = (player.position - transform.position).normalized;
        
        // 이동 속도 적용
        rb.velocity = direction * moveSpeed;
    }
    
    // 이동 정지
    private void StopMoving()
    {
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
        }
    }
    
    // 플레이어 방향 바라보기 (2D 게임용)
    private void LookAtPlayer()
    {
        if (player == null) return;
        
        // 플레이어가 적의 오른쪽에 있는지 왼쪽에 있는지 확인
        bool playerIsOnRight = player.position.x > transform.position.x;
        
        // 적 캐릭터의 방향 설정 (transform.localScale로 뒤집기)
        if ((playerIsOnRight && !facingRight) || (!playerIsOnRight && facingRight))
        {
            // 현재 방향 반전
            facingRight = !facingRight;
            
            // transform.localScale을 이용하여 X 스케일 반전
            Vector3 scale = transform.localScale;
            scale.x *= -1;
            transform.localScale = scale;
        }
    }
    
    // 공격 함수
    private void Attack()
    {
        // 공격 시 이동 정지
        StopMoving();
        
        // 애니메이션 재생 시도
        if (animator != null)
        {
            // 트리거 이름 변경 또는 확인
            // "2_Attack" 또는 "Attack" 두 가지 모두 시도
            animator.SetTrigger("2_Attack");
            
            // 애니메이션 상태를 디버그에 출력
            Debug.Log(gameObject.name + "이(가) 공격 애니메이션 트리거를 실행했습니다.");
        }
        else
        {
            Debug.LogWarning(gameObject.name + "의 애니메이터가 null입니다. 공격 애니메이션을 재생할 수 없습니다.");
        }
        
        // 애니메이션 재생 후 데미지 적용을 위한 코루틴 시작
        StartCoroutine(ApplyAttackDamage());
    }
    
    // 공격 데미지 적용 코루틴
    private IEnumerator ApplyAttackDamage()
    {
        // 애니메이션 타이밍에 맞춰 딜레이
        yield return new WaitForSeconds(attackDelay);
        
        // 공격 범위 내 모든 오브젝트 탐지
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, attackRange);
        
        // 감지된 콜라이더 중 Player 태그를 가진 것 찾기
        bool hitPlayer = false;
        foreach (Collider2D hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag(targetTag))
            {
                hitPlayer = true;
                Debug.Log("플레이어 히트: " + hitCollider.name);
                
                // PlayerHealth 컴포넌트 확인
                PlayerHealth playerHealth = hitCollider.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    // 데미지 적용
                    playerHealth.TakeDamage(attackDamage);
                    Debug.Log(gameObject.name + "이(가) 플레이어에게 " + attackDamage + " 데미지를 입혔습니다.");
                }
            }
        }
        
        if (!hitPlayer)
        {
            Debug.Log(gameObject.name + "의 공격이 대상에게 닿지 않았습니다.");
        }
    }
    
    // 공격 범위 시각화 (디버깅용)
    private void OnDrawGizmos()
    {
        // 공격 범위 표시 (빨간색)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        // 탐지 범위 표시 (노란색)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        // 정지 거리 표시 (녹색)
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, stoppingDistance);
    }
}
