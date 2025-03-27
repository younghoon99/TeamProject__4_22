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
    
    // 애니메이션 컴포넌트
    private Animator animator;

    void Start()
    {
        // 시작할 때 플레이어 찾기
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        
        // 애니메이터 컴포넌트 가져오기
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        // 플레이어가 없으면 실행하지 않음
        if (player == null) return;
        
        // 플레이어와의 거리 계산
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
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
    
    // 플레이어 방향 바라보기
    private void LookAtPlayer()
    {
        // y축 회전만 적용 (2D 게임이라면 다른 축 사용)
        Vector3 direction = player.position - transform.position;
        direction.y = 0; // y축 회전만 필요한 경우
        
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }
    
    // 공격 함수
    private void Attack()
    {
        // 애니메이션 재생 (있다면)
        if (animator != null)
        {
            animator.SetTrigger("Attack");
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
    private void OnDrawGizmosSelected()
    {
        // 공격 범위 표시 (빨간색)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        // 탐지 범위 표시 (노란색)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}
