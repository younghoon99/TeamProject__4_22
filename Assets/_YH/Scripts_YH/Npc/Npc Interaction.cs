using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NpcInteraction : MonoBehaviour
{
    [Header("상호작용 설정")]
    [SerializeField] private float interactionDistance = 3f;  // 상호작용 가능 거리
    [SerializeField] private GameObject interactionUI;        // NPC 머리 위에 표시될 UI
    [SerializeField] private Transform uiPosition;            // UI가 표시될 위치 (주로 NPC 머리 위)
    [SerializeField] private string descriptionTextName = "description";  // NPC 설명을 표시할 텍스트 오브젝트 이름

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

            // Npc 스크립트 가져오기
            Npc npcController = GetComponent<Npc>();

            if (isUIActive)
            {
                // UI 활성화
                interactionUI.SetActive(true);

                // UI가 활성화될 때 위치 즉시 업데이트
                UpdateUIPosition();

                // NPC 움직임 멈추기
                if (npcController != null)
                {
                    npcController.OnInteractionStart();
                    
                    // NPC 설명 표시 (NpcEntry에서 가져와서 텍스트 컴포넌트에 설정)
                    if (npcController.NpcEntry != null)
                    {
                        // Panel1 아래의 description 텍스트 찾기
                        Transform panelTransform = interactionUI.transform.Find("Panel1");
                        if (panelTransform != null)
                        {
                            Transform descriptionTransform = panelTransform.Find(descriptionTextName);
                            if (descriptionTransform != null)
                            {
                                // SendMessage를 통해 텍스트 설정 - TextMeshPro에서 "SetText" 메서드 호출
                                descriptionTransform.gameObject.SendMessage("SetText", npcController.NpcEntry.description, SendMessageOptions.DontRequireReceiver);
                                
                                // 또한 직접 "text" 속성에 접근하는 시도
                                // TextMeshPro의 경우 text 속성을 어떻게 접근하는지 알 수 없으므로 리플렉션 사용
                                var textComponent = descriptionTransform.GetComponent(System.Type.GetType("TMPro.TMP_Text, Unity.TextMeshPro"));
                                if (textComponent != null)
                                {
                                    System.Reflection.PropertyInfo prop = textComponent.GetType().GetProperty("text");
                                    if (prop != null)
                                    {
                                        prop.SetValue(textComponent, npcController.NpcEntry.description, null);
                                        Debug.Log("리플렉션을 통해 NPC 설명 설정: " + npcController.NpcEntry.description);
                                    }
                                }
                                
                                Debug.Log("NPC 설명 표시: " + npcController.NpcEntry.description);
                            }
                            else
                            {
                                Debug.LogWarning("설명 텍스트 오브젝트를 찾을 수 없습니다: " + descriptionTextName);
                            }
                        }
                        else
                        {
                            Debug.LogWarning("Panel1을 찾을 수 없습니다.");
                        }
                    }
                }

                Debug.Log("NPC 상호작용 UI가 활성화되었습니다.");
            }
            else
            {
                // 비활성화 시 패널 초기화 (HideInteractionUI와 동일한 초기화 로직)
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
                if (npcController != null)
                {
                    npcController.OnInteractionEnd();
                }

                // UI 비활성화
                interactionUI.SetActive(false);

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

    // 설명 텍스트 오브젝트 찾기
    private GameObject FindDescriptionTextObject()
    {
        if (interactionUI == null) return null;
        
        // 직접 이름으로 찾기
        Transform textTransform = interactionUI.transform.Find(descriptionTextName);
        if (textTransform != null)
        {
            return textTransform.gameObject;
        }
        
        // 재귀적으로 모든 자식 오브젝트에서 검색
        return FindChildWithName(interactionUI.transform, descriptionTextName);
    }
    
    // 재귀적으로 자식 오브젝트 검색
    private GameObject FindChildWithName(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
            {
                return child.gameObject;
            }
            
            GameObject found = FindChildWithName(child, name);
            if (found != null)
            {
                return found;
            }
        }
        
        return null;
    }

    // 범위 시각화 (에디터에서 확인용)
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
    }
}
