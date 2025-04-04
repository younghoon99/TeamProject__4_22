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
    private AudioSource audioSource; // 오디오 소스
    private MobManager mobManager; // MobManager 참조
    private bool facingRight = true; // 몹의 초기 방향 설정

    [Header("탐지 설정")]
    public float detectionRange = 10f; // 탐지 범위
    private Transform detectedTarget; // 탐지된 대상
    public string[] targetTags = { "Player", "Wall", "NPC", "HQ" }; // 추적할 대상의 태그 배열

    [Header("이동 설정")]
    public float stoppingDistance = 1f; // 정지 거리
    public bool canMove = true; // 이동 가능 여부

    [Header("공격 설정")]
    public float attackDelay = 0.3f; // 애니메이션 재생 후 데미지 적용까지의 지연 시간
    public float attackCooldown = 2f; // 공격 쿨다운
    private float nextAttackTime = 0f; // 다음 공격 가능 시간

    [Header("추가 설정")]
    public string npcTag = "NPC"; // NPC 태그

    public float maxHealth = 100f; // 몹의 최대 체력
    private float currentHealth; // 몹의 현재 체력
    private bool isDead = false; // 몹의 사망 여부

    public void Initialize(Transform playerTransform, Transform hqTransform, Transform[] wallTransforms, Transform leftWallTransform, Transform rightWallTransform)
    {
        player = playerTransform;
        HQ = hqTransform;
        Walls = wallTransforms;
        leftWall = leftWallTransform;
        rightWall = rightWallTransform;

        // 초기 타겟 설정
        ChooseInitialTarget();

        // 왼쪽에서 나타나는 몹은 오른쪽을 향하도록 설정
        if (transform.position.x < 0 && !facingRight)
        {
            Flip();
        }
    }

    private void ChooseInitialTarget()
    {
        // 가장 가까운 타겟을 찾음
        target = FindClosestTarget();
    }

    private Transform FindClosestTarget()
    {
        Transform closestTarget = null;
        float closestDistance = float.MaxValue;

        // 모든 가능한 타겟을 확인
        foreach (string tag in targetTags)
        {
            GameObject[] targets = GameObject.FindGameObjectsWithTag(tag);
            foreach (GameObject target in targets)
            {
                float distance = Vector3.Distance(transform.position, target.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestTarget = target.transform;
                }
            }
        }

        return closestTarget;
    }

    private void Start()
    {
        // 애니메이터 컴포넌트 가져오기
        animator = GetComponentInChildren<Animator>();

        // MobManager 가져오기
        mobManager = FindObjectOfType<MobManager>();
        if (mobManager != null && mobManager.attackSound != null)
        {
            // 오디오 소스 추가
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.clip = mobManager.attackSound;
            audioSource.playOnAwake = false;
        }

        // 초기 체력 설정
        currentHealth = maxHealth;
    }

    private void Update()
    {
        // 가장 가까운 타겟을 계속 추적
        if (target == null || Vector3.Distance(transform.position, target.position) > detectionRange)
        {
            target = FindClosestTarget();
        }

        if (target != null)
        {
            float distanceToTarget = Vector3.Distance(transform.position, target.position);

            // 타겟 방향으로 이동
            if (distanceToTarget > stoppingDistance && canMove)
            {
                MoveTowardsTarget(target);
            }
            else
            {
                StopMoving();

                // 공격 범위 내에 있으면 공격
                if (distanceToTarget <= stoppingDistance && Time.time >= nextAttackTime)
                {
                    StartCoroutine(AttackTarget(target));
                    nextAttackTime = Time.time + attackCooldown; // 다음 공격 시간 설정
                }
            }
        }
        else
        {
            StopMoving(); // 타겟이 없으면 정지
        }
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

                // 공격 사운드 재생
                if (audioSource != null && mobManager != null && mobManager.attackSound != null)
                {
                    audioSource.Play();
                }
            }

            // Wall 체력 감소
            wallHealth.TakeDamage(attackDamage);
            yield return new WaitForSeconds(attackInterval);
        }

        isAttacking = false;

        // Wall 파괴 후 NPC를 새로운 타겟으로 설정
        if (wallHealth != null && wallHealth.GetCurrentHealth() <= 0)
        {
            detectedTarget = FindClosestNpc();
        }
    }

    private Transform FindClosestNpc()
    {
        GameObject[] npcs = GameObject.FindGameObjectsWithTag("NPC");
        Transform closestNpc = null;
        float closestDistance = float.MaxValue;

        foreach (GameObject npc in npcs)
        {
            float distance = Vector3.Distance(transform.position, npc.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestNpc = npc.transform;
            }
        }

        return closestNpc;
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

                // 공격 사운드 재생
                if (audioSource != null && mobManager != null && mobManager.attackSound != null)
                {
                    audioSource.Play();
                }
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

            // 공격 사운드 재생
            if (audioSource != null && mobManager != null && mobManager.attackSound != null)
            {
                audioSource.Play();
            }
        }

        yield return new WaitForSeconds(attackDelay);

        // 대상이 Player, Wall, NPC, 또는 HQ인지 확인하고 데미지 적용
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
        else if (target.CompareTag("NPC"))
        {
            NpcHealth npcHealth = target.GetComponent<NpcHealth>();
            if (npcHealth != null)
            {
                npcHealth.TakeDamage(attackDamage);
            }
        }
        else if (target.CompareTag("HQ"))
        {
            HQHealth hqHealth = target.GetComponent<HQHealth>();
            if (hqHealth != null)
            {
                hqHealth.TakeDamage(attackDamage);
            }
        }
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return; // 이미 사망한 경우 무시

        // 체력 감소
        currentHealth = Mathf.Max(0, currentHealth - damage);
        Debug.Log(gameObject.name + "이(가) " + damage + "의 데미지를 입었습니다. 남은 체력: " + currentHealth);

        // 사망 확인
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (isDead) return;

        isDead = true;
        Debug.Log(gameObject.name + "이(가) 사망했습니다.");

        // 몹 제거
        OnDestroyed?.Invoke();
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        OnDestroyed?.Invoke();
    }

    private void Flip()
    {
        // 몹의 방향을 반전
        facingRight = !facingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }
}