using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyMove : MonoBehaviour
{
    Rigidbody2D rigid;
    Animator anim;
    SpriteRenderer spriteRenderer;
    CapsuleCollider2D capsulecollider;
    public int nextMove;

    void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        capsulecollider = GetComponent<CapsuleCollider2D>();

        rigid.freezeRotation = true;    //이동시 굴러가기 방지
        Invoke("Think", 5);
    }

    void FixedUpdate()
    {
        //이동
        rigid.velocity = new Vector2(nextMove, rigid.velocity.y);

        //Ray를 사용한 지형 체크
        Vector2 frontVec = new Vector2(rigid.position.x + nextMove * 0.2f, rigid.position.y);
        Debug.DrawRay(frontVec, Vector3.down, new Color(0, 1, 0));
        RaycastHit2D rayHit = Physics2D.Raycast(frontVec, Vector3.down, 1, LayerMask.GetMask("Platform"));
        if (rayHit.collider == null)
            Turn();

    }

    void Think()
    {
        //행동 지표 결정
        nextMove = Random.Range(-1, 2);

        //애니메이션 전환
        anim.SetInteger("WalkSpeed", nextMove);

        //애니메이션 방향 전환
        if (nextMove != 0)
            spriteRenderer.flipX = nextMove == 1;

        //재귀
        float nextThinkTime = Random.Range(2f, 5f);
        Invoke("Think", nextThinkTime);
    }

    void Turn()
    {
        //방향 전환
        nextMove *= -1;
        spriteRenderer.flipX = nextMove == 1;

        CancelInvoke();
        Invoke("Think", 2);
    }

    public void OnDamaged()
    {   //몬스터 공격 받았을때 함수
        //스프라이트 반투명화
        spriteRenderer.color = new Color(1, 1, 1, 0.4f);

        //스프라이트 Y축 뒤집기
        spriteRenderer.flipY = true;

        //물리 충돌 제거
        capsulecollider.enabled = false;

        //사망 모션(점프)
        rigid.AddForce(Vector2.up * 5, ForceMode2D.Impulse);

        //몬스터 제거
        Invoke("DeActive", 5);
    }

    void DeActive()
    {   //비활성화 함수
        gameObject.SetActive(false);
    }
}