using System.Collections.Generic;
using UnityEngine;

public class SynergyManager : MonoBehaviour
{
    // 싱글톤 인스턴스
    public static SynergyManager Instance { get; private set; }
    
    [Header("시너지 설정")]
    [SerializeField] private List<SynergyData> allSynergies;  // 모든 시너지 데이터
    
    // 현재 활성화된 NPC 목록
    private List<Npc> activeNpcs = new List<Npc>();
    
    // 활성화된 NPC ID와 수량
    private List<string> activeNpcIds = new List<string>();
    private Dictionary<string, int> npcCounts = new Dictionary<string, int>();
    
    // 활성화된 시너지
    private Dictionary<SynergyType, bool> activeSynergies = new Dictionary<SynergyType, bool>();
    
    // 시너지 상태 변경 이벤트 (UI 업데이트 용)
    public System.Action<Dictionary<SynergyType, bool>> OnSynergyChanged;
    
    private void Awake()
    {
        // 싱글톤 패턴 적용
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        
        // 모든 시너지 타입 초기화
        foreach (SynergyType type in System.Enum.GetValues(typeof(SynergyType)))
        {
            activeSynergies[type] = false;
        }
    }
    
    private void Start()
    {
        // 시작 시 씬의 모든 NPC 찾기
        FindAllNpcsInScene();
    }
    
    // 씬의 모든 NPC 찾기
    private void FindAllNpcsInScene()
    {
        Npc[] npcsInScene = FindObjectsOfType<Npc>();
        foreach (Npc npc in npcsInScene)
        {
            AddNpc(npc);
        }
    }
    
    // NPC 추가
    public void AddNpc(Npc npc)
    {
        if (npc == null || npc.NpcData == null) return;
        
        if (!activeNpcs.Contains(npc))
        {
            activeNpcs.Add(npc);
            string npcId = npc.NpcData.npcId;
            
            // NPC ID 리스트에 추가
            if (!activeNpcIds.Contains(npcId))
            {
                activeNpcIds.Add(npcId);
            }
            
            // NPC 카운트 증가
            if (!npcCounts.ContainsKey(npcId))
            {
                npcCounts[npcId] = 1;
            }
            else
            {
                npcCounts[npcId]++;
            }
            
            UpdateSynergies();
            Debug.Log($"{npc.NpcData.npcName} NPC가 시너지 시스템에 추가되었습니다. 현재 {npcId} 수: {npcCounts[npcId]}");
        }
    }
    
    // NPC 제거
    public void RemoveNpc(Npc npc)
    {
        if (npc == null || npc.NpcData == null) return;
        
        if (activeNpcs.Contains(npc))
        {
            activeNpcs.Remove(npc);
            string npcId = npc.NpcData.npcId;
            
            // NPC 카운트 감소
            if (npcCounts.ContainsKey(npcId))
            {
                npcCounts[npcId]--;
                
                // 0개가 되면 리스트에서 제거
                if (npcCounts[npcId] <= 0)
                {
                    npcCounts.Remove(npcId);
                    activeNpcIds.Remove(npcId);
                }
            }
            
            UpdateSynergies();
            Debug.Log($"{npc.NpcData.npcName} NPC가 시너지 시스템에서 제거되었습니다.");
        }
    }
    
    // 시너지 업데이트
    private void UpdateSynergies()
    {
        // 모든 NPC에 시너지 효과 초기화
        foreach (Npc npc in activeNpcs)
        {
            npc.ResetSynergyEffects();
        }
        
        // 시너지 활성화 상태 확인 및 적용
        bool synergyChanged = false;
        
        foreach (SynergyData synergy in allSynergies)
        {
            bool wasActive = activeSynergies[synergy.synergyType];
            bool isActive = synergy.CheckSynergyActive(activeNpcIds, npcCounts);
            
            activeSynergies[synergy.synergyType] = isActive;
            
            if (isActive)
            {
                ApplySynergyEffects(synergy);
            }
            
            if (wasActive != isActive)
            {
                synergyChanged = true;
            }
        }
        
        // 시너지 상태가 변경되었을 때만 이벤트 발생
        if (synergyChanged)
        {
            OnSynergyChanged?.Invoke(new Dictionary<SynergyType, bool>(activeSynergies));
            LogActiveSynergies();
        }
    }
    
    // 활성화된 시너지 로그 출력
    private void LogActiveSynergies()
    {
        string synergyLog = "활성화된 시너지:\n";
        
        foreach (var synergy in activeSynergies)
        {
            if (synergy.Value)
            {
                synergyLog += $"- {synergy.Key} 시너지 활성화!\n";
            }
        }
        
        Debug.Log(synergyLog);
    }
    
    // 시너지 효과 적용
    private void ApplySynergyEffects(SynergyData synergy)
    {
        // 활성화된 티어 찾기
        SynergyTier activeTier = synergy.GetActiveTier(npcCounts);
        if (activeTier == null) return;
        
        // 시너지 효과 적용 대상 NPC 찾기
        List<Npc> affectedNpcs = new List<Npc>();
        
        switch (synergy.synergyType)
        {
            // 특정 NPC 유형만 영향 받는 시너지
            case SynergyType.채굴전문가:
            case SynergyType.채집전문가:
            case SynergyType.전사의의지:
            case SynergyType.정밀사격:
                foreach (Npc npc in activeNpcs)
                {
                    if (synergy.requiredNpcIds.Contains(npc.NpcData.npcId))
                    {
                        affectedNpcs.Add(npc);
                    }
                }
                break;
                
            // 모든 NPC에 영향을 주는 시너지
            case SynergyType.노동자의힘:
            case SynergyType.철벽방어:
                affectedNpcs.AddRange(activeNpcs);
                break;
                
            // 특정 직업 타입에만 영향을 주는 시너지
            case SynergyType.수호천사:
                foreach (Npc npc in activeNpcs)
                {
                    if (npc.NpcData.jobType == NpcJobType.탱커)
                    {
                        affectedNpcs.Add(npc);
                    }
                }
                break;
                
            case SynergyType.마나의격류:
                foreach (Npc npc in activeNpcs)
                {
                    if (npc.NpcData.jobType == NpcJobType.힐러)
                    {
                        affectedNpcs.Add(npc);
                    }
                }
                break;
                
            case SynergyType.불타는전장:
            case SynergyType.방패와창:
                foreach (Npc npc in activeNpcs)
                {
                    if (npc.NpcData.jobType == NpcJobType.전사 || 
                        npc.NpcData.jobType == NpcJobType.궁수 ||
                        npc.NpcData.jobType == NpcJobType.딜러)
                    {
                        affectedNpcs.Add(npc);
                    }
                }
                break;
                
            // 시너지 효과가 특정 범위에 영향을 주는 경우
            default:
                // 시너지 타입에 맞는 보정이 필요한 경우 여기에 추가
                affectedNpcs.AddRange(activeNpcs);
                break;
        }
        
        // 영향 받는 모든 NPC에 시너지 효과 적용
        foreach (Npc npc in affectedNpcs)
        {
            // 기본 스탯 보너스 적용
            npc.ApplySynergyBonus(
                activeTier.attackBonus,
                activeTier.healthBonus,
                activeTier.defenseBonus,
                activeTier.speedBonus
            );
            
            // 시너지 타입에 따른 특수 효과 적용
            switch (synergy.synergyType)
            {
                case SynergyType.채굴전문가:
                    npc.ApplyMiningBonus(activeTier.miningAmountBonus);
                    break;
                    
                case SynergyType.채집전문가:
                    npc.ApplyGatheringBonus(activeTier.gatheringSpeedBonus);
                    break;
                    
                case SynergyType.정밀사격:
                    npc.ApplyCriticalChanceBonus(activeTier.criticalChanceBonus);
                    break;
                    
                case SynergyType.철벽방어:
                    npc.ApplyDamageReductionBonus(activeTier.damageReductionBonus);
                    break;
                    
                case SynergyType.수호천사:
                    npc.ApplyHealthRegenBonus(activeTier.healthRegenBonus);
                    break;
                    
                case SynergyType.치유의빛:
                    npc.ApplyHealingBonus(activeTier.healingAmountBonus);
                    break;
                    
                case SynergyType.광역폭격:
                    npc.ApplyAoeBonus(activeTier.aoeBonus);
                    break;
                    
                case SynergyType.약화의손길:
                    npc.ApplyDebuffBonus(activeTier.debuffDurationBonus);
                    break;
                    
                case SynergyType.마력증폭:
                    npc.ApplyMagicDamageBonus(activeTier.magicDamageBonus);
                    break;
            }
            
            // 특수 능력 해금
            if (activeTier.unlockSpecialAbility && activeTier.specialAbility != null)
            {
                npc.UnlockSpecialAbility(activeTier.specialAbility);
            }
            
            Debug.Log($"{npc.NpcData.npcName}에게 {synergy.synergyType} 시너지 효과가 적용되었습니다.");
        }
    }
    
    // UI용 활성화된 시너지 정보 반환
    public Dictionary<SynergyType, bool> GetActiveSynergies()
    {
        return new Dictionary<SynergyType, bool>(activeSynergies);
    }
    
    // 특정 시너지 데이터 가져오기
    public SynergyData GetSynergyData(SynergyType type)
    {
        return allSynergies.Find(s => s.synergyType == type);
    }
}
