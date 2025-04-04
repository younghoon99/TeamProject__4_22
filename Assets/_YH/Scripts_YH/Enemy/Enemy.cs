using UnityEngine;
using System.Collections;

public class Enemy : MonoBehaviour
{
    [Header("공격 설정")]
    public float attackDamage = 10f;
    public float attackRange = 1.5f;
    public float attackCooldown = 2f;
    public float attackDelay = 0.3f;
    private float nextAttackTime = 0f;
    private bool isAttacking = false;

    [Header("탐지 설정")]
    public float detectionRange = 5f;
    private Transform currentTarget;
    public string[] targetTags = { "Player", "NPC" };

    [Header("이동 설정")]
    public float moveSpeed = 2f;
    public float stoppingDistance = 1f;
    public bool canMove = true;
    private Rigidbody2D rb;

    [Header("방향 설정")]
    public bool facingRight = true;

    private Animator animator;
    private SpriteRenderer spriteRenderer;

    public int currentHealth = 100;

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();

        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        FindTarget();
        IgnoreCollisionsWithEnemiesAndNpcs();
    }

    private void IgnoreCollisionsWithEnemiesAndNpcs()
    {
        Collider2D enemyCollider = GetComponent<Collider2D>();

        if (enemyCollider != null)
        {
            // 다른 모든 Enemy와의 충돌 무시
            Enemy[] enemies = FindObjectsOfType<Enemy>();
            foreach (Enemy otherEnemy in enemies)
            {
                // 자기 자신은 제외
                if (otherEnemy != this)
                {
                    Collider2D otherCollider = otherEnemy.GetComponent<Collider2D>();
                    if (otherCollider != null)
                    {
                        Physics2D.IgnoreCollision(enemyCollider, otherCollider, true);
                    }
                }
            }

            // 모든 NPC와의 충돌 무시
            Npc[] npcs = FindObjectsOfType<Npc>();
            foreach (Npc npc in npcs)
            {
                Collider2D npcCollider = npc.GetComponent<Collider2D>();
                if (npcCollider != null)
                {
                    Physics2D.IgnoreCollision(enemyCollider, npcCollider, true);
                }
            }
        }
    }

    void Update()
    {
        // 주기적으로 타겟 재탐색 (매 프레임마다 하면 성능 저하 가능성 있음)
        if (Time.frameCount % 30 == 0) // 약 0.5초마다 타겟 재탐색
        {
            FindTarget();
        }

        // 타겟이 없으면 동작하지 않음
        if (currentTarget == null)
        {
            // 타겟 재탐색 시도
            FindTarget();
            if (currentTarget == null) return; // 여전히 없으면 함수 종료
        }

        // 타겟과의 거리 계산
        float distanceToTarget = Vector2.Distance(transform.position, currentTarget.position);

        // 탐지 범위 내에 있는지 확인
        if (distanceToTarget <= detectionRange)
        {
            // 타겟 방향 바라보기
            LookAtTarget();

            // 공격 범위 내에 있고 공격 쿨다운이 지났는지 확인
            if (distanceToTarget <= attackRange && Time.time >= nextAttackTime)
            {
                // 공격 전 완전히 정지
                StopMoving();

                // 공격 실행
                Attack();

                // 다음 공격 시간 설정
                nextAttackTime = Time.time + attackCooldown;
            }

            // 공격 범위 밖이고 정지 거리보다 멀리 있다면 타겟에게 이동
            // 공격 중이 아닐 때만 이동
            else if (distanceToTarget > attackRange && canMove && !isAttacking)
            {
                MoveTowardsTarget();

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
                // 공격 중이 아닐 때만 애니메이션 상태 변경
                if (animator != null && !isAttacking)
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
            // 공격 중이 아닐 때만 애니메이션 상태 변경
            if (animator != null && !isAttacking)
            {
                animator.SetBool("1_Move", false);
            }
        }
    }

    private void MoveTowardsTarget()
    {
        if (currentTarget == null || rb == null) return;

        // 타겟 방향으로 향하는 벡터 계산
        Vector2 direction = (currentTarget.position - transform.position).normalized;

        // 이동 속도 적용
        rb.velocity = direction * moveSpeed;
    }

    private void StopMoving()
    {
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
        }
    }

    private void LookAtTarget()
    {
        if (currentTarget == null) return;

        // 타겟이 적의 오른쪽에 있는지 왼쪽에 있는지 확인
        bool targetIsOnRight = currentTarget.position.x > transform.position.x;

        // 적 캐릭터의 방향 설정 (transform.localScale로 뒤집기)
        if ((targetIsOnRight && !facingRight) || (!targetIsOnRight && facingRight))
        {
            // 현재 방향 반전
            facingRight = !facingRight;

            // transform.localScale을 이용하여 X 스케일 반전
            Vector3 scale = transform.localScale;
            scale.x *= -1;
            transform.localScale = scale;
        }
    }

    private void Attack()
    {
        // 공격 시 이동 정지
        StopMoving();

        // 공격 상태로 설정
        isAttacking = true;

        // 애니메이션 재생 시도
        if (animator != null)
        {
            // 이동 애니메이션 중지
            animator.SetBool("1_Move", false);

            // 공격 애니메이션 트리거
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

    private IEnumerator ApplyAttackDamage()
    {
        // 공격 중에는 계속 정지 상태 유지
        StopMoving();

        // 애니메이션 타이밍에 맞춰 딜레이
        yield return new WaitForSeconds(attackDelay);

        // 다시 한번 정지 상태 확인
        StopMoving();

        // 공격 범위 내 모든 오브젝트 탐지
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, attackRange);

        // 감지된 콜라이더 중 타겟 태그를 가진 것 찾기
        bool hitTarget = false;

        // 모든 타겟 태그에 대해 검색
        foreach (string tag in targetTags)
        {
            foreach (Collider2D hitCollider in hitColliders)
            {
                if (hitCollider.CompareTag(tag))
                {
                    hitTarget = true;
                    ApplyDamageToTarget(hitCollider);
                    break; // 하나의 타겟만 공격
                }
            }

            if (hitTarget) break; // 타겟을 찾았으면 반복 중단
        }

        if (!hitTarget)
        {
            Debug.Log(gameObject.name + "의 공격이 대상에게 닿지 않았습니다.");
        }

        // 공격 애니메이션이 완전히 끝날 때까지 추가 대기
        yield return new WaitForSeconds(0.7f);

        // 한번 더 정지 상태 확인
        StopMoving();

        // 공격 상태 해제
        isAttacking = false;

        // 현재 타겟과의 거리 다시 계산
        if (currentTarget != null)
        {
            float currentDistance = Vector2.Distance(transform.position, currentTarget.position);

            // 여전히 공격 범위 내에 있다면 바로 다음 공격 준비
            if (currentDistance <= attackRange)
            {
                // 다음 공격 시간을 약간 앞당김
                nextAttackTime = Mathf.Min(nextAttackTime, Time.time + 0.2f);
            }
        }
    }

    // 타겟에 데미지 적용 함수
    private void ApplyDamageToTarget(Collider2D targetCollider)
    {
        string targetType = targetCollider.tag;

        Debug.Log(targetType + " 히트: " + targetCollider.name);

        // 플레이어인 경우
        if (targetType == "Player")
        {
            // PlayerHealth 컴포넌트 확인
            Player player = targetCollider.GetComponent<Player>();
            if (player != null)
            {
                // 데미지 적용 (Player 클래스에 TakeDamage 메서드 호출)
                player.TakeDamage((int)attackDamage);
                Debug.Log(gameObject.name + "이(가) 플레이어에게 " + attackDamage + " 데미지를 입혔습니다.");
            }

            // EnemyHealth 컴포넌트 확인 (플레이어에게 EnemyHealth 컴포넌트가 있을 수 있음)
            EnemyHealth playerHealth = targetCollider.GetComponent<EnemyHealth>();
            if (playerHealth != null)
            {
                // 데미지 적용
                playerHealth.TakeDamage(attackDamage, (Vector2)transform.position);
            }
        }
        // NPC인 경우
        else if (targetType == "NPC")
        {
            // Npc 컴포넌트 확인
            Npc npc = targetCollider.GetComponent<Npc>();
            if (npc != null)
            {
                // 데미지 적용 (Npc 클래스에 TakeDamage 메서드가 있다고 가정)
                npc.TakeDamage((int)attackDamage);
            }

            // EnemyHealth 컴포넌트 확인 (NPC에게 EnemyHealth 컴포넌트가 있을 수 있음)
            EnemyHealth npcHealth = targetCollider.GetComponent<EnemyHealth>();
            if (npcHealth != null)
            {
                // 데미지 적용
                npcHealth.TakeDamage(attackDamage, (Vector2)transform.position);
            }
        }
    }

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

    public void TakeDamage(int damage)
    {
        // 현재 체력에서 데미지 차감
        currentHealth -= damage;

        // 체력이 0 이하면 사망 처리
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        // 사망 효과 또는 애니메이션 재생
        if (animator != null)
        {
            animator.SetTrigger("4_Death");
        }

        // 일정 시간 후 오브젝트 제거 또는 비활성화
        Destroy(gameObject, 1f);
    }

    private void FindTarget()
    {
        Transform closestTarget = null;
        float closestDistance = float.MaxValue;

        // 모든 타겟 태그를 검색하여 가장 가까운 대상 찾기
        foreach (string tag in targetTags)
        {
            GameObject[] targets = GameObject.FindGameObjectsWithTag(tag);

            foreach (GameObject target in targets)
            {
                // 자기 자신은 제외
                if (target == gameObject) continue;

                float distance = Vector2.Distance(transform.position, target.transform.position);

                // 탐지 범위 내에 있고 현재까지 발견한 것보다 가까우면 갱신
                if (distance <= detectionRange && distance < closestDistance)
                {
                    closestTarget = target.transform;
                    closestDistance = distance;
                }
            }
        }

        currentTarget = closestTarget;

    }

    private Transform FindClosestTarget(GameObject[] targets)
    {
        Transform closest = null;
        float closestDistance = float.MaxValue;

        foreach (GameObject target in targets)
        {
            // 자기 자신은 제외
            if (target == gameObject) continue;

            float distance = Vector2.Distance(transform.position, target.transform.position);

            // 탐지 범위 내에 있고 현재까지 발견한 것보다 가까우면 갱신
            if (distance <= detectionRange && distance < closestDistance)
            {
                closest = target.transform;
                closestDistance = distance;
            }
        }

        return closest;
    }
}
