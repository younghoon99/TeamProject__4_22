using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 시너지 단계 정보를 담는 클래스
[System.Serializable]
public class SynergyTier
{
    [Header("단계 정보")]
    public int tier;                     // 단계 번호 (1, 2, 3...)
    public int requiredCount;            // 필요한 NPC 수
    [TextArea(2, 3)]
    public string effectDescription;     // 효과 설명
    
    [Header("효과 수치")]
    public float attackBonus;            // 공격력 증가 %
    public float healthBonus;            // 체력 증가 %
    public float defenseBonus;           // 방어력 증가 %
    public float speedBonus;             // 속도 증가 %
    
    [Header("채집 관련")]
    public float gatheringSpeedBonus;    // 채집 속도 보너스 %
    public float miningAmountBonus;      // 채굴량 보너스 %
    
    [Header("전투 관련")]
    public float criticalChanceBonus;    // 치명타 확률 보너스 %
    public float healingAmountBonus;     // 치유량 보너스 %
    public float debuffDurationBonus;    // 디버프 지속시간 보너스 %
    public float aoeBonus;               // 광역 공격력 보너스 %
    public float magicDamageBonus;       // 마법 피해 보너스 %
    
    [Header("방어 관련")]
    public float damageReductionBonus;   // 피해 감소 %
    public float healthRegenBonus;       // 체력 재생 보너스 %
    
    [Header("특수 효과")]
    public bool unlockSpecialAbility;    // 특수 능력 해금 여부
    public NpcAbility specialAbility;    // 해금되는 특수 능력
    public float enemyDefenseReduction;  // 적 방어력 감소 %
}

[CreateAssetMenu(fileName = "NewSynergyData", menuName = "NPC System/Synergy Data")]
public class SynergyData : ScriptableObject
{
    [Header("시너지 정보")]
    public SynergyType synergyType;      // 시너지 유형
    public string synergyName;           // 시너지 이름 (예: "채굴 전문가")
    [TextArea(2, 4)]
    public string description;           // 시너지 설명
    public Sprite icon;                  // 시너지 아이콘
    
    [Header("시너지 단계")]
    public List<SynergyTier> tiers;      // 시너지 단계별 효과
    
    [Header("시너지 발동 조건")]
    public List<string> requiredNpcIds;  // 필요한 NPC ID 목록 (예: "김광부", "김전사")
    public int requiredNpcCount;         // 필요한 NPC 수 (예: 3명 이상)
    
    // 시너지 발동 여부 확인
    public bool CheckSynergyActive(List<string> activeNpcIds, Dictionary<string, int> npcCounts)
    {
        // 시너지 유형에 따른 발동 조건 확인
        switch (synergyType)
        {
            // 단일 NPC 유형 시너지 (예: 김광부 3명 이상)
            case SynergyType.채굴전문가:
            case SynergyType.채집전문가:
            case SynergyType.전사의의지:
            case SynergyType.정밀사격:
            case SynergyType.철벽방어:
            case SynergyType.치유의빛:
            case SynergyType.공격본능:
            case SynergyType.광역폭격:
            case SynergyType.약화의손길:
            case SynergyType.마력증폭:
                // 해당 NPC가 필요한 수 이상인지 확인
                if (requiredNpcIds.Count > 0)
                {
                    string requiredNpcId = requiredNpcIds[0];
                    return npcCounts.ContainsKey(requiredNpcId) && npcCounts[requiredNpcId] >= requiredNpcCount;
                }
                return false;
                
            // 다양한 NPC 조합 시너지 (예: 김광부 + 김채집 + 김전사 + 김궁수)
            case SynergyType.노동자의힘:
            case SynergyType.방패와창:
            case SynergyType.수호천사:
            case SynergyType.불타는전장:
            case SynergyType.암흑의전략:
            case SynergyType.마나의격류:
                // 모든 필요한 NPC가 활성화되어 있는지 확인
                foreach (string npcId in requiredNpcIds)
                {
                    if (!activeNpcIds.Contains(npcId))
                    {
                        return false;
                    }
                }
                return true;
                
            default:
                return false;
        }
    }
    
    // 활성화된 시너지 티어 찾기
    public SynergyTier GetActiveTier(Dictionary<string, int> npcCounts)
    {
        if (tiers == null || tiers.Count == 0)
            return null;
            
        SynergyTier highestTier = null;
        
        // 만족하는 가장 높은 티어 찾기
        foreach (SynergyTier tier in tiers)
        {
            bool tierConditionMet = false;
            
            switch (synergyType)
            {
                // 단일 NPC 수 기반 시너지
                case SynergyType.채굴전문가:
                case SynergyType.채집전문가:
                case SynergyType.전사의의지:
                case SynergyType.정밀사격:
                case SynergyType.철벽방어:
                case SynergyType.치유의빛:
                case SynergyType.공격본능:
                case SynergyType.광역폭격:
                case SynergyType.약화의손길:
                case SynergyType.마력증폭:
                    if (requiredNpcIds.Count > 0)
                    {
                        string npcId = requiredNpcIds[0];
                        if (npcCounts.ContainsKey(npcId) && npcCounts[npcId] >= tier.requiredCount)
                        {
                            tierConditionMet = true;
                        }
                    }
                    break;
                    
                // 다양한 NPC 조합 시너지 (단순히 모든 NPC가 있는지 확인)
                default:
                    // 이미 CheckSynergyActive에서 확인되었으므로 첫 번째 티어 사용
                    tierConditionMet = true;
                    break;
            }
            
            if (tierConditionMet)
            {
                highestTier = tier;
            }
        }
        
        return highestTier;
    }
}
