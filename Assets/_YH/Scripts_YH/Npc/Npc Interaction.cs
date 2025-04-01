using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NpcInteraction : MonoBehaviour
{
    [Header("상호작용 설정")]
    [SerializeField] private float interactionRange = 2.0f;
    [SerializeField] private KeyCode interactionKey = KeyCode.F;
    
    [Header("UI 오프셋 설정")]
    [SerializeField] private Vector3 promptOffset = new Vector3(0, 2.0f, 0); // NPC 머리 위 오프셋
    [SerializeField] private Vector3 infoOffset = new Vector3(0, 2.5f, 0);    // 정보 패널 오프셋

    [Header("UI 설정")]
    [SerializeField] private GameObject interactionPrompt;    // NPC와 상호작용할 수 있을 때 표시되는 프롬프트 UI (예: "F키를 눌러 대화하기")
    [SerializeField] private GameObject npcInfoPanel;         // NPC 정보를 표시하는 패널 (이름, 등급, 능력치 등을 포함)
    [SerializeField] private TextMeshProUGUI npcInfoText;     // NPC 세부 정보를 표시하는 텍스트 컴포넌트
    [SerializeField] private GameObject interactionButtonsPanel; // 상호작용 버튼들이 포함된 패널 (따라오기, 거래, 채굴 등의 버튼 포함)
    [SerializeField] private Button followButton;             // NPC가 플레이어를 따라오도록 지시하는 버튼
    [SerializeField] private Button tradeButton;              // NPC와 거래 기능을 활성화하는 버튼
    [SerializeField] private Button miningButton;             // NPC에게 채굴 작업을 지시하는 버튼
    
    // 참조 변수
    private Transform playerTransform;
    private Npc currentNpc;
    private NpcAbility currentNpcAbility;
    private bool isInteracting = false;
    private bool isNpcFollowing = false;
    
    private void Start()
    {
        // 플레이어 트랜스폼 찾기
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        
        if (playerTransform == null)
        {
            Debug.LogError("플레이어를 찾을 수 없습니다. 'Player' 태그가 설정되었는지 확인하세요.");
        }
        
        // UI 초기 상태 설정
        if (interactionPrompt) interactionPrompt.SetActive(false);
        if (npcInfoPanel) npcInfoPanel.SetActive(false);
    }
    
    private void Update()
    {
        if (playerTransform == null) return;
        
        // 가장 가까운 NPC 찾기
        Npc nearestNpc = FindNearestNpc();
        
        // NPC와의 상호작용 처리
        if (nearestNpc != null)
        {
            // 상호작용 프롬프트 표시
            if (interactionPrompt && !isInteracting)
            {
                interactionPrompt.SetActive(true);
                
                // 프롬프트 위치를 NPC 머리 위로 설정
                interactionPrompt.transform.position = Camera.main.WorldToScreenPoint(
                    nearestNpc.transform.position + promptOffset);
            }
            
            // 상호작용 키 입력 감지
            if (Input.GetKeyDown(interactionKey) && !isInteracting)
            {
                StartInteraction(nearestNpc);
            }
            else if (Input.GetKeyDown(interactionKey) && isInteracting)
            {
                EndInteraction();
            }
        }
        else
        {
            // 가까운 NPC가 없으면 프롬프트 숨기기
            if (interactionPrompt)
            {
                interactionPrompt.SetActive(false);
            }
            
            // 상호작용 중이었다면 종료
            if (isInteracting)
            {
                EndInteraction();
            }
        }
        
        // UI 위치 업데이트
        UpdateUIPositions();
        
        // NPC 따라오기 로직
        UpdateFollowing();
    }

    private void UpdateUIPositions()
    {
        if (currentNpc != null)
        {
            // UI 요소 위치 업데이트
            if (interactionPrompt && interactionPrompt.activeSelf)
            {
                interactionPrompt.transform.position = Camera.main.WorldToScreenPoint(
                    currentNpc.transform.position + promptOffset);
            }

            if (npcInfoPanel && npcInfoPanel.activeSelf)
            {
                // LookAt 제거하고 위치만 업데이트
                npcInfoPanel.transform.position = Camera.main.WorldToScreenPoint(
                    currentNpc.transform.position + infoOffset);
            }

            // 상호작용 버튼 패널도 NPC 위치에 따라 업데이트
            if (interactionButtonsPanel && interactionButtonsPanel.activeSelf)
            {
                interactionButtonsPanel.transform.position = Camera.main.WorldToScreenPoint(
                    currentNpc.transform.position + new Vector3(0, 3.0f, 0));
            }
        }
    }

    // 가장 가까운 NPC 찾기
    private Npc FindNearestNpc()
    {
        Npc closestNpc = null;
        float closestDistance = interactionRange;
        
        Npc[] allNpcs = FindObjectsOfType<Npc>();
        foreach (Npc npc in allNpcs)
        {
            float distance = Vector3.Distance(playerTransform.position, npc.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestNpc = npc;
            }
        }
        
        return closestNpc;
    }
    
    // NPC와 상호작용 시작
    private void StartInteraction(Npc npc)
    {
        currentNpc = npc;
        currentNpcAbility = npc.GetComponent<NpcAbility>();
        isInteracting = true;
        
        // NPC 정보 패널 표시
        if (npcInfoPanel)
        {
            npcInfoPanel.SetActive(true);
            
            // NPC 정보 텍스트 업데이트
            if (npcInfoText)
            {
                npcInfoText.text = npc.GetNpcInfoText();
            }
        }
        
        // 상호작용 프롬프트 숨기기
        if (interactionPrompt)
        {
            interactionPrompt.SetActive(false);
        }
        
        // NPC에게 상호작용 시작 알림
        npc.OnInteractionStart();
    }
    
    // NPC와 상호작용 종료
    private void EndInteraction()
    {
        // NPC 상호작용 종료 알림
        if (currentNpc != null)
        {
            currentNpc.OnInteractionEnd();
            
            // 채굴 중이었다면 중지
            if (currentNpcAbility != null)
            {
                currentNpcAbility.StopGathering();
            }
        }
        
        // UI 패널 숨기기
        if (npcInfoPanel)
        {
            npcInfoPanel.SetActive(false);
        }
        
        if (interactionButtonsPanel)
        {
            interactionButtonsPanel.SetActive(false);
        }
        
        // 상태 초기화
        isInteracting = false;
        currentNpc = null;
        currentNpcAbility = null;
    }
    
    // NPC 따라오기 로직 업데이트
    private void UpdateFollowing()
    {
        if (isNpcFollowing && currentNpc != null && playerTransform != null)
        {
            float distance = Vector3.Distance(playerTransform.position, currentNpc.transform.position);
            
            // 일정 거리 이상 떨어지면 NPC가 플레이어를 향해 이동
            if (distance > 3.0f)
            {
                // 플레이어 방향으로 이동하는 로직 (실제 이동은 Npc 클래스에서 구현)
                Debug.Log($"{currentNpc.NpcName}이(가) 플레이어를 따라갑니다. 거리: {distance:F2}");
            }
        }
    }
}
