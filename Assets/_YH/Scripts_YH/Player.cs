using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    // 플레이어 이동 관련 변수
    [Header("이동 설정")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 10f;
    
    // 물리 및 상태 변수
    private Rigidbody2D rb;
    private bool isGrounded;
    [Header("플레이어 방향 설정")]
    [SerializeField] private bool isFacingRight = false;
    
    // 지면 체크 관련 변수
    [Header("지면 체크 설정")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.4f;
    [SerializeField] private LayerMask groundLayer;
    
    // 카메라 관련 변수
    private Camera mainCamera;
    
    // 애니메이션 관련 변수
    [SerializeField] Animator animator;
    private float horizontalInput;

    // Start is called before the first frame update
    void Start()
    {
        // 마우스 커서를 숨기고 중앙에 고정
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Confined;
        
        // 컴포넌트 초기화
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();
        
        // Rigidbody2D 관성 제거
        if (rb != null)
        {
            rb.drag = 0f;         // 공기 저항 0으로 설정
            rb.gravityScale = 3f; // 중력 스케일 설정
            rb.freezeRotation = true; // 회전 방지
            rb.interpolation = RigidbodyInterpolation2D.Interpolate; // 부드러운 이동
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; // 연속 충돌 감지
            // 관성 제거를 위해 속도 즉시 적용
            rb.inertia = 0f;
        }
        
        // 없을 경우 groundCheck 생성
        if (groundCheck == null)
        {
            GameObject check = new GameObject("GroundCheck");
            check.transform.parent = transform;
            check.transform.localPosition = new Vector3(0, -1f, 0);
            groundCheck = check.transform;
            Debug.Log("GroundCheck 자동 생성됨");
        }
        
        // 카메라 참조가 없을 경우 메인 카메라로 설정
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("메인 카메라를 찾을 수 없습니다!");
                enabled = false; // 스크립트 비활성화
                return;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        // 입력 값 받기
        horizontalInput = Input.GetAxis("Horizontal");
        
        // 지면 체크
        CheckIsGrounded();
        
        // 점프 입력 처리
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            Jump();
        }
        
        // 마우스 위치에 따른 플레이어 방향 설정
        FlipBasedOnMousePosition();
        
        // 마우스 좌클릭 입력 처리
        if (Input.GetMouseButtonDown(0))
        {
            // 좌클릭 시 6_Other 트리거 실행
            if (animator != null)
            {
                animator.SetTrigger("6_Other");
            }
        }
        
        // 애니메이션 파라미터 업데이트
        UpdateAnimationParameters();
    }
    
    void FixedUpdate()
    {
        // 이동 입력 처리
        Move();
    }
    
    // 이동 처리 함수
    private void Move()
    {
        // 이동 적용 (좌우 이동만 가능하고, y 속도는 그대로 유지)
        rb.velocity = new Vector2(horizontalInput * moveSpeed, rb.velocity.y);
    }
    
    // 애니메이션 파라미터 업데이트
    private void UpdateAnimationParameters()
    {
        if (animator != null)
        {
            // 움직임 감지 (절대값이 0.1보다 크면 움직이는 것으로 간주)
            bool isMoving = Mathf.Abs(horizontalInput) > 0.1f;
            
            // 움직임 파라미터 설정
            animator.SetBool("1_Move", isMoving);
        }
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
        // 마우스 위치가 유효한지 확인 (화면 내부에 있는지)
        if (Input.mousePosition.x >= 0 && Input.mousePosition.x <= Screen.width &&
            Input.mousePosition.y >= 0 && Input.mousePosition.y <= Screen.height)
        {
            // 마우스 위치를 월드 좌표로 변환
            Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0; // z값을 플레이어와 동일하게 설정
            
            // 플레이어 기준 마우스가 오른쪽에 있는지 확인
            bool mouseOnRight = mousePos.x > transform.position.x;
            
            // 마우스가 오른쪽에 있을 때 플레이어도 오른쪽을 바라보도록, 왼쪽일 때는 왼쪽을 바라보도록 처리
            if ((mouseOnRight && !isFacingRight) || (!mouseOnRight && isFacingRight))
            {
                // 플레이어 방향 전환
                Flip();
            }
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
