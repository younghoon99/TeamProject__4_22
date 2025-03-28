using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NpcMove : MonoBehaviour
{
    Rigidbody2D rigid;
    Animator anim;
    CapsuleCollider2D capsulecollider;

    [Header("랜덤 이동 설정")]
    public float moveSpeed = 2f;        // NPC 이동 속도
    public float minX = -10f;           // 이동 가능한 최소 X 좌표
    public float maxX = 10f;            // 이동 가능한 최대 X 좌표
    private Vector2 targetPosition;     // 목표 위치
    public int nextMove;                // 이동 방향 (-1: 왼쪽, 0: 정지, 1: 오른쪽)

    void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        capsulecollider = GetComponent<CapsuleCollider2D>();

        rigid.freezeRotation = true;      // 회전 방지
        ChooseRandomPosition();           // 최초 랜덤 목표 위치 선택
        Invoke("Think", 5);               // 초기 상태 결정
    }

    void FixedUpdate()
    {
        // 현재 위치와 목표 위치를 비교하여 이동
        Vector2 currentPosition = transform.position;
        Vector2 direction = (targetPosition - currentPosition).normalized;

        // NPC 이동 (nextMove 방향 활용)
        rigid.velocity = new Vector2(nextMove * moveSpeed, rigid.velocity.y);

        // 목표 위치에 도달했는지 확인
        if (Vector2.Distance(currentPosition, targetPosition) < 0.1f)
        {
            ChooseRandomPosition();       // 새로운 목표 위치 선택
        }

        // Ray를 사용한 지형 체크
        Vector2 frontVec = new Vector2(rigid.position.x + nextMove * 0.5f, rigid.position.y);
        Debug.DrawRay(frontVec, Vector3.down * 2f, new Color(0, 1, 0));  // 길이 2f로 설정
        RaycastHit2D rayHit = Physics2D.Raycast(frontVec, Vector3.down, 2f, LayerMask.GetMask("Platform"));
        if (rayHit.collider == null)
        {
            Debug.Log("NPC 방향 전환 필요");
            Turn(); // 방향 전환
        }
    }

    void ChooseRandomPosition()
    {
        // 새로운 랜덤 목표 위치 선택
        float randomX = Random.Range(minX, maxX);
        targetPosition = new Vector2(randomX, transform.position.y);

        // 이동 방향 설정
        nextMove = randomX > transform.position.x ? 1 : -1;
        anim.SetInteger("WalkSpeed", nextMove); // 애니메이션 전환

        Debug.Log("새 목표 위치: " + targetPosition);
    }

    void Turn()
    {
        // 이동 방향 전환
        nextMove *= -1;
        transform.localScale = new Vector3(nextMove > 0 ? 1 : -1, 1, 1);
        Debug.Log("NPC 방향 전환: " + nextMove);

        CancelInvoke();
        Invoke("Think", 2); // 방향 전환 후 행동 결정
    }

    void Think()
    {
        // 다음 행동 랜덤 결정
        nextMove = Random.Range(-1, 2);
        anim.SetInteger("WalkSpeed", nextMove); // 애니메이션 전환

        // 재귀적으로 행동 결정 시간 설정
        float nextThinkTime = Random.Range(2f, 5f);
        Invoke("Think", nextThinkTime);
    }
}