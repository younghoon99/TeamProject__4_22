using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NpcInterAction : MonoBehaviour
{
    [Header("상호작용 설정")]
    [SerializeField] private float interactionDistance = 3f;  // 상호작용 가능 거리
    [SerializeField] private GameObject interactionUI;        // NPC 머리 위에 표시될 UI
    [SerializeField] private Transform uiPosition;            // UI가 표시될 위치 (주로 NPC 머리 위)

    [Header("디버그")]
    [SerializeField] private bool showDebugInfo = false;     // 디버그 정보 표시 여부

    private Transform playerTransform;                       // 플레이어 트랜스폼
    private bool isPlayerInRange = false;                    // 플레이어가 범위 내에 있는지 여부
    private bool isUIActive = false;                         // UI가 활성화되어 있는지 여부

    void Start()
    {
        // UI 초기 상태 비활성화
        if (interactionUI != null)
        {
            interactionUI.SetActive(false);
        }

        // UI 위치 설정이 없으면 현재 트랜스폼 사용
        if (uiPosition == null)
        {
            uiPosition = transform;
        }

        // 플레이어 찾기
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
        else
        {
            Debug.LogWarning("Player 태그를 가진 오브젝트를 찾을 수 없습니다.");
        }
    }

    void Update()
    {
        // 플레이어가 없으면 실행하지 않음
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
            else
            {
                return;
            }
        }

        // 플레이어와의 거리 계산
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        // 거리 내에 있는지 확인
        isPlayerInRange = distanceToPlayer <= interactionDistance;

        // 우클릭 감지 및 UI 표시 처리
        HandleInteractionInput();

        // UI 위치 업데이트 (UI가 활성화되어 있을 때만)
        if (isUIActive && interactionUI != null)
        {
            UpdateUIPosition();
        }

        // 플레이어가 범위를 벗어나면 UI 비활성화
        if (!isPlayerInRange && isUIActive)
        {
            HideInteractionUI();
        }

        // 디버그 정보 표시
        if (showDebugInfo)
        {
            Debug.DrawLine(transform.position, playerTransform.position, isPlayerInRange ? Color.green : Color.red);
            Debug.Log("NPC와 플레이어 간 거리: " + distanceToPlayer);
        }
    }

    // 상호작용 입력 처리
    private void HandleInteractionInput()
    {
        // 플레이어가 우클릭하고 범위 내에 있을 때 UI 토글
        if (Input.GetMouseButtonDown(1) && isPlayerInRange)
        {
            // 레이캐스트로 우클릭한 오브젝트가 이 NPC인지 확인
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);

            if (hit.collider != null && hit.collider.gameObject == gameObject)
            {
                ToggleInteractionUI();
            }
        }
    }

    // UI 표시 상태 전환
    private void ToggleInteractionUI()
    {
        if (interactionUI != null)
        {
            isUIActive = !isUIActive;
            interactionUI.SetActive(isUIActive);

            // Npc 스크립트 가져오기
            Npc npcController = GetComponent<Npc>();

            if (isUIActive)
            {
                // UI가 활성화될 때 위치 즉시 업데이트
                UpdateUIPosition();

                // NPC 움직임 멈추기
                if (npcController != null)
                {
                    npcController.OnInteractionStart();
                }

                Debug.Log("NPC 상호작용 UI가 활성화되었습니다.");
            }
            else
            {
                // NPC 움직임 재개
                if (npcController != null)
                {
                    npcController.OnInteractionEnd();
                }

                Debug.Log("NPC 상호작용 UI가 비활성화되었습니다.");
            }
        }
    }

    // UI 숨기기
    private void HideInteractionUI()
    {
        if (interactionUI != null && isUIActive)
        {
            isUIActive = false;

            // 인터렉션 UI 초기화 - 모든 대화 패널을 검사하여 초기화
            Transform[] allPanels = interactionUI.GetComponentsInChildren<Transform>(true); // true: 비활성화된 오브젝트도 포함

            foreach (Transform panel in allPanels)
            {
                // 패널 이름에 "Panel" 또는 "페이지"가 포함된 오브젝트 찾기
                if (panel.name.Contains("Panel") || panel.name.Contains("페이지") || panel.name.Contains("Page"))
                {
                    // 첫 번째 패널인지 확인 (이름에 "1" 또는 "First"가 포함되어 있는지)
                    bool isFirstPanel = panel.name.Contains("1") || panel.name.Contains("First") || panel.name.Contains("첫번째");

                    // 첫 번째 패널만 활성화, 나머지는 비활성화
                    panel.gameObject.SetActive(isFirstPanel);

                    Debug.Log("패널 초기화: " + panel.name + " - " + (isFirstPanel ? "활성화" : "비활성화"));
                }
            }

            // NPC 움직임 재개
            Npc npcController = GetComponent<Npc>();
            if (npcController != null)
            {
                npcController.OnInteractionEnd();
            }

            // UI 비활성화
            interactionUI.SetActive(false);

            Debug.Log("플레이어가 범위를 벗어나 NPC 상호작용 UI가 비활성화되었습니다.");
        }
    }

    // UI 위치 업데이트
    private void UpdateUIPosition()
    {
        if (uiPosition != null && interactionUI != null)
        {
            // UI가 항상 카메라를 향하도록 설정 (빌보드 효과)
            interactionUI.transform.position = uiPosition.position + Vector3.up * 0.5f;
            if (Camera.main != null)
            {
                interactionUI.transform.LookAt(interactionUI.transform.position + Camera.main.transform.forward);
            }
        }
    }

    // 범위 시각화 (에디터에서 확인용)
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
    }
}
