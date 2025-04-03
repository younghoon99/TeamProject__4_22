using System;
using UnityEngine;
using System.Collections;

public class MobBehavior : MonoBehaviour
{
    public Transform player;
    public Transform HQ;
    public Transform[] Walls; // Wall 배열
    public float speed = 2f; // 이동 속도
    public event Action OnDestroyed;
    private Transform target; // 현재 추적 대상
    private bool reachedWall = false; // Wall에 도착했는지 여부
    private Transform leftWall; // 좌측 Wall
    private Transform rightWall; // 우측 Wall
    public float attackDamage = 10f; // 공격 데미지
    public float attackInterval = 1f; // 공격 간격
    private bool isAttacking = false; // 공격 중인지 여부
    private Animator animator; // 몹의 애니메이터

    [Header("탐지 설정")]
    public float detectionRange = 5f; // 탐지 범위
    private Transform detectedTarget; // 탐지된 대상
    public string[] targetTags = { "Player", "Wall" }; // 추적할 대상의 태그 배열

    [Header("이동 설정")]
    public float stoppingDistance = 1f; // 정지 거리
    public bool canMove = true; // 이동 가능 여부

    [Header("공격 설정")]
    public float attackDelay = 0.3f; // 애니메이션 재생 후 데미지 적용까지의 지연 시간
    public float attackCooldown = 2f; // 공격 쿨다운
    private float nextAttackTime = 0f; // 다음 공격 가능 시간

    public void Initialize(Transform playerTransform, Transform hqTransform, Transform[] wallTransforms, Transform leftWallTransform, Transform rightWallTransform)
    {
        player = playerTransform;
        HQ = hqTransform;
        Walls = wallTransforms;
        leftWall = leftWallTransform;
        rightWall = rightWallTransform;

        // 초기 타겟 설정
        ChooseInitialTarget();
    }

    private void ChooseInitialTarget()
    {
        // 몹의 초기 위치에 따라 좌측 Wall 또는 우측 Wall을 타겟으로 설정
        if (transform.position.x < 0 && leftWall != null)
        {
            target = leftWall;
        }
        else if (transform.position.x >= 0 && rightWall != null)
        {
            target = rightWall;
        }
    }

    private void Start()
    {
        // 애니메이터 컴포넌트 가져오기
        animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        // 탐지된 대상이 없으면 탐지 시도
        if (detectedTarget == null)
        {
            detectedTarget = FindClosestTarget();
            if (detectedTarget == null) return; // 여전히 없으면 함수 종료
        }

        // 대상과의 거리 계산
        float distanceToTarget = Vector3.Distance(transform.position, detectedTarget.position);

        // 탐지 범위 내에 있는지 확인
        if (distanceToTarget <= detectionRange)
        {
            // 대상 방향으로 이동
            if (distanceToTarget > stoppingDistance && canMove)
            {
                MoveTowardsTarget(detectedTarget);
            }
            else
            {
                StopMoving();

                // 공격 범위 내에 있으면 공격
                if (distanceToTarget <= stoppingDistance && Time.time >= nextAttackTime)
                {
                    StartCoroutine(AttackTarget(detectedTarget));
                    nextAttackTime = Time.time + attackCooldown; // 다음 공격 시간 설정
                }
            }
        }
        else
        {
            StopMoving(); // 탐지 범위 밖이면 정지
            detectedTarget = null; // 대상 초기화
        }
    }

    private Transform FindClosestTarget()
    {
        Transform closestTarget = null;
        float closestDistance = float.MaxValue;

        foreach (string tag in targetTags)
        {
            GameObject[] targets = GameObject.FindGameObjectsWithTag(tag);
            foreach (GameObject target in targets)
            {
                float distance = Vector3.Distance(transform.position, target.transform.position);
                if (distance < closestDistance && distance <= detectionRange)
                {
                    closestDistance = distance;
                    closestTarget = target.transform;
                }
            }
        }

        return closestTarget;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Wall에 도착했는지 확인
        if (!reachedWall && (other.transform == leftWall || other.transform == rightWall))
        {
            reachedWall = true;
            StartCoroutine(WaitAtWall());
        }

        // Wall을 공격
        if (other.CompareTag("Wall"))
        {
            WallHealth wallHealth = other.GetComponent<WallHealth>();
            if (wallHealth != null)
            {
                StartCoroutine(AttackWall(wallHealth));
            }
        }

        // Player를 공격
        if (other.CompareTag("Player"))
        {
            StartCoroutine(AttackPlayer(other.GetComponent<PlayerHealth>()));
        }
    }

    private void MoveTowardsTarget(Transform target)
    {
        Vector3 direction = (target.position - transform.position).normalized;
        transform.position += direction * speed * Time.deltaTime;
    }

    private void StopMoving()
    {
        // 정지 로직 (필요 시 추가)
    }

    private IEnumerator WaitAtWall()
    {
        // 10초 대기
        yield return new WaitForSeconds(10f);

        // 플레이어를 새로운 타겟으로 설정
        target = player;
        reachedWall = false; // 대기 상태 해제
    }

    private IEnumerator AttackWall(WallHealth wallHealth)
    {
        if (wallHealth == null) yield break;

        isAttacking = true;

        while (isAttacking && wallHealth != null && wallHealth.GetCurrentHealth() > 0)
        {
            // 공격 애니메이션 트리거
            if (animator != null)
            {
                animator.SetTrigger("2_Attack");
            }

            // Wall 체력 감소
            wallHealth.TakeDamage(attackDamage);
            yield return new WaitForSeconds(attackInterval);
        }

        isAttacking = false;
    }

    private IEnumerator AttackPlayer(PlayerHealth playerHealth)
    {
        if (playerHealth == null) yield break;

        isAttacking = true;

        while (isAttacking && playerHealth != null)
        {
            // 공격 애니메이션 트리거
            if (animator != null)
            {
                animator.SetTrigger("2_Attack");
            }

            // Player 체력 감소
            playerHealth.TakeDamage(attackDamage);
            yield return new WaitForSeconds(attackInterval);
        }

        isAttacking = false;
    }

    private IEnumerator AttackTarget(Transform target)
    {
        // 공격 애니메이션 트리거
        if (animator != null)
        {
            animator.SetTrigger("2_Attack");
        }

        yield return new WaitForSeconds(attackDelay);

        // 대상이 Wall 또는 Player인지 확인하고 데미지 적용
        if (target.CompareTag("Wall"))
        {
            WallHealth wallHealth = target.GetComponent<WallHealth>();
            if (wallHealth != null)
            {
                wallHealth.TakeDamage(attackDamage);
            }
        }
        else if (target.CompareTag("Player"))
        {
            PlayerHealth playerHealth = target.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(attackDamage);
            }
        }
    }

    private void OnDestroy()
    {
        OnDestroyed?.Invoke();
    }
}