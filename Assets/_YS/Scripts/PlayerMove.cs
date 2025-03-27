//PlayerMove 스크립트 파일

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    public GameManager gameManager;
    public float maxSpeed;
    public float jumpPower;
    Rigidbody2D rigid;
    SpriteRenderer spriteRenderer;
    Animator anim;
    CapsuleCollider2D capsulecollider;
    AudioSource audioSource;

    //Audio Clip 변수들
    public AudioClip audioJump;
    public AudioClip audioAttack;
    public AudioClip audioDamaged;
    public AudioClip audioItem;
    public AudioClip audioDie;
    public AudioClip audioFinish;


    void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        capsulecollider = GetComponent<CapsuleCollider2D>();
        audioSource = GetComponent<AudioSource>();

        rigid.freezeRotation = true;    //이동시 굴러가기 방지
    }

    void PlaySound(string action)
    {
        switch (action)
        {
            case "JUMP":
                audioSource.clip = audioJump;
                break;
            case "ATTACK":
                audioSource.clip = audioAttack;
                break;
            case "DAMAGED":
                audioSource.clip = audioDamaged;
                break;
            case "ITEM":
                audioSource.clip = audioItem;
                break;
            case "DIE":
                audioSource.clip = audioDie;
                break;
            case "FINISH":
                audioSource.clip = audioFinish;
                break;
        }

        audioSource.Play();
    }

    void Update()
    {
        //점프
        if (Input.GetButtonDown("Jump") && !anim.GetBool("isJumping"))
        {    //스페이스바, 점프 애니메이션이 작동중이지 않다면
            rigid.AddForce(Vector2.up * jumpPower, ForceMode2D.Impulse);
            anim.SetBool("isJumping", true);    //Jump 애니메이션 추가
            PlaySound("JUMP");  //사운드 재생
        }

        //멈출 때 속도
        if (Input.GetButtonUp("Horizontal"))
        {
            //normalized : 벡터 크기를 1로 만든 상태
            rigid.velocity = new Vector2(rigid.velocity.normalized.x * 0.5f, rigid.velocity.y);
        }

        //방향 전환
        if (Input.GetButton("Horizontal"))
            spriteRenderer.flipX = Input.GetAxisRaw("Horizontal") == -1;    //-1은 왼쪽 방향

        //애니메이션 전환
        if (Mathf.Abs(rigid.velocity.x) < 0.3)   //정지 상태, Mathf : 수학관련 함수를 제공하는 클래스
            anim.SetBool("isWalking", false);
        else
            anim.SetBool("isWalking", true);
    }

    void FixedUpdate()
    {
        float h = Input.GetAxisRaw("Horizontal");       //좌,우 A, D

        rigid.AddForce(Vector2.right * h, ForceMode2D.Impulse);

        //오른쪽 속도 조절
        if (rigid.velocity.x > maxSpeed)    //velocity : 리지드바디의 현재 속도
            rigid.velocity = new Vector2(maxSpeed, rigid.velocity.y);   //y축 값을 0으로 하면 멈춤
        //왼쪽 속도 조절
        else if (rigid.velocity.x < maxSpeed * (-1))
            rigid.velocity = new Vector2(maxSpeed * (-1), rigid.velocity.y);

        //점프 후 착지시 애니메이션 전환(레이캐스트)
        if (rigid.velocity.y < 0)
        { //y축 속도 값이 0보다 클때만. 땅에 있으면 레이 표시 X
            Debug.DrawRay(rigid.position, Vector3.down, new Color(0, 1, 0)); //레이 표시
            RaycastHit2D rayHit = Physics2D.Raycast(rigid.position, Vector3.down, 1, LayerMask.GetMask("Platform")); //레이에 닿는 물체
            if (rayHit.collider != null) //레이에 닿는 물체가 있다면
                if (rayHit.distance < 0.5f)
                    anim.SetBool("isJumping", false);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {    //적과 충돌시
        if (collision.gameObject.tag == "Enemy")
        {
            //몬스터보다 위에 있고, 낙하 중이라면 밟음(공격)
            if (rigid.velocity.y < 0 && transform.position.y > collision.transform.position.y)
            {
                OnAttack(collision.transform);
            }
            //아니라면, 피해를 받음
            else
                OnDamaged(collision.transform.position);

        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Item")
        {
            //점수 얻기
            bool isBronze = collision.gameObject.name.Contains("Bronze");
            bool isSilver = collision.gameObject.name.Contains("Silver");
            bool isGold = collision.gameObject.name.Contains("Gold");

            if (isBronze)
                gameManager.stagePoint += 50;
            else if (isSilver)
                gameManager.stagePoint += 100;
            else if (isGold)
                gameManager.stagePoint += 300;

            //아이템 비활성화
            collision.gameObject.SetActive(false);

            //사운드 재생
            PlaySound("ITEM");
        }
        else if (collision.gameObject.tag == "Finish")
        {
            //사운드 재생
            PlaySound("FINISH");

            //다음 스테이지로 이동
            gameManager.NextStage();
        }
    }

    void OnAttack(Transform enemy)
    {    //몬스터 공격 함수
        //점수 얻기
        gameManager.stagePoint += 100;

        //사운드 재생
        PlaySound("ATTACK");

        //몬스터 밟았을때 반발력
        rigid.AddForce(Vector2.up * 10, ForceMode2D.Impulse);

        //몬스터 사망
        EnemyMove enemyMove = enemy.GetComponent<EnemyMove>();
        enemyMove.OnDamaged();
    }

    void OnDamaged(Vector2 targetPos)
    {   //충돌 이벤트 무적 효과 함수
        //체력 감소
        gameManager.HealthDown();

        //사운드 재생
        PlaySound("DAMAGED");

        //레이어 변경(무적)
        gameObject.layer = 11;  //11번 레이어, PlayerDamaged

        //충돌시 스프라이트 색상 변화
        spriteRenderer.color = new Color(1, 1, 1, 0.4f);    //Color(R,G,B,투명도)

        //충돌시 튕겨짐
        //목표물 기준 왼쪽에서 닿으면 왼쪽으로, 오른쪽에서 닿으면 오른쪽으로
        int dirc = transform.position.x - targetPos.x > 0 ? 1 : -1;
        rigid.AddForce(new Vector2(dirc, 1) * 7, ForceMode2D.Impulse);

        //애니메이션 변경
        anim.SetTrigger("doDamaged");

        Invoke("OffDamaged", 3);    //3초 뒤 OffDamaged 함수 실행
    }

    void OffDamaged()
    { //충돌 이벤트 무적 해제 함수
        //레이어 변경(원래대로)
        gameObject.layer = 10;  //10번 레이어, Player
        spriteRenderer.color = new Color(1, 1, 1, 1);
    }

    public void OnDie()
    {  //플레이어 사망
        //스프라이트 반투명화
        spriteRenderer.color = new Color(1, 1, 1, 0.4f);

        //스프라이트 Y축 뒤집기
        spriteRenderer.flipY = true;

        //물리 충돌 제거
        capsulecollider.enabled = false;

        //사운드 재생
        PlaySound("DIE");

        //사망 모션(점프)
        rigid.AddForce(Vector2.up * 5, ForceMode2D.Impulse);
    }

    public void VelocityZero()
    {
        rigid.velocity = Vector2.zero;
    }
}