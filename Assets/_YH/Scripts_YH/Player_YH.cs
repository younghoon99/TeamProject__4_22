using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_YH : MonoBehaviour
{
    // 플레이어 이동 관련 변수
    [Header("이동 설정")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 10f;
    
    // 물리 및 상태 변수
    private Rigidbody2D rb;
    private bool isGrounded;
    private bool isFacingRight = true;
    
    // 지면 체크 관련 변수
    [Header("지면 체크 설정")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;
    
    // 카메라 관련 변수
    private Camera mainCamera;

    // Start is called before the first frame update
    void Start()
    {
        // 컴포넌트 초기화
        rb = GetComponent<Rigidbody2D>();
        mainCamera = Camera.main;
        
        // 없을 경우 groundCheck 생성
        if (groundCheck == null)
        {
            GameObject check = new GameObject("GroundCheck");
            check.transform.parent = transform;
            check.transform.localPosition = new Vector3(0, -0.5f, 0);
            groundCheck = check.transform;
            Debug.Log("GroundCheck 자동 생성됨");
        }
    }

    // Update is called once per frame
    void Update()
    {
        // 지면 체크
        CheckIsGrounded();
        
        // 점프 입력 처리
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            Jump();
        }
        
        // 마우스 위치에 따른 플레이어 방향 설정
        FlipBasedOnMousePosition();
    }
    
    void FixedUpdate()
    {
        // 이동 입력 처리
        Move();
    }
    
    // 이동 처리 함수
    private void Move()
    {
        // 입력 값 받기 (좌우만 사용)
        float horizontalInput = Input.GetAxis("Horizontal");
        
        // 이동 적용 (좌우 이동만 가능하고, y 속도는 그대로 유지)
        rb.velocity = new Vector2(horizontalInput * moveSpeed, rb.velocity.y);
    }
    
    // 점프 함수
    private void Jump()
    {
        // 현재 y속도는 0으로 설정하고 jumpForce만큼 위로 힘 가함
        rb.velocity = new Vector2(rb.velocity.x, 0f);
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
    }
    
    // 지면 체크 함수
    private void CheckIsGrounded()
    {
        // 원형 캐스트로 지면 체크
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }
    
    // 마우스 위치에 따른 플레이어 방향 전환
    private void FlipBasedOnMousePosition()
    {
        // 마우스 위치를 월드 좌표로 변환
        Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        
        // 플레이어 기준 마우스가 오른쪽에 있는지 확인
        bool mouseOnRight = mousePos.x > transform.position.x;
        
        // 방향이 다를 경우 플레이어 방향 전환
        if ((mouseOnRight && !isFacingRight) || (!mouseOnRight && isFacingRight))
        {
            // 플레이어 방향 전환
            Flip();
        }
    }
    
    // 플레이어 방향 전환 함수
    private void Flip()
    {
        // 현재 방향 반전
        isFacingRight = !isFacingRight;
        
        // 스케일의 x값 반전으로 스프라이트 뒤집기
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }
    
    // 카메라 이동 처리 (LateUpdate에서 처리하여 플레이어 이동 후 카메라 이동)
    void LateUpdate()
    {
        // 메인 카메라가 있을 경우에만 실행
        if (mainCamera != null)
        {
            // 현재 카메라 위치
            Vector3 cameraPos = mainCamera.transform.position;
            
            // 카메라의 x 좌표만 플레이어를 따라가도록 업데이트 (y, z 값은 유지)
            mainCamera.transform.position = new Vector3(transform.position.x, cameraPos.y, cameraPos.z);
        }
    }
    
    // 디버깅용 그리기 함수
    private void OnDrawGizmos()
    {
        if (groundCheck != null)
        {
            // 지면 체크 영역 시각화
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
