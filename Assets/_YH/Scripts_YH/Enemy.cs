using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("공격 설정")]
    public float attackDamage = 10f;       // 공격력
    public float attackRange = 1.5f;       // 공격 범위
    public float attackCooldown = 2f;      // 공격 쿨다운
    private float nextAttackTime = 0f;     // 다음 공격 가능 시간
    
    [Header("탐지 설정")]
    public float detectionRange = 5f;      // 플레이어 탐지 범위
    private Transform player;              // 플레이어 트랜스폼
    
    [Header("방향 설정")]
    public bool facingRight = true;        // 적의 초기 방향
    
    // 애니메이션 컴포넌트
    private Animator animator;
    // 스프라이트 렌더러
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        // 시작할 때 플레이어 찾기
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        
        // 애니메이터 컴포넌트 가져오기 (자식 객체에 있을 경우를 위해 GetComponentInChildren 사용)
        animator = GetComponentInChildren<Animator>();
        
        // 스프라이트 렌더러 가져오기
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        
        // 애니메이터가 없으면 경고 메시지
        if (animator == null)
        {
            Debug.LogWarning(gameObject.name + "에 Animator 컴포넌트가 없습니다.");
        }
    }

    void Update()
    {
        // 플레이어가 없으면 실행하지 않음
        if (player == null) return;
        
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
        }
    }
    
    // 플레이어 방향 바라보기 (2D 게임용)
    private void LookAtPlayer()
    {
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
        
        // 플레이어 체력 감소시키기
        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(attackDamage);
            Debug.Log(gameObject.name + "이(가) 플레이어에게 " + attackDamage + " 데미지를 입혔습니다.");
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
    }
}
