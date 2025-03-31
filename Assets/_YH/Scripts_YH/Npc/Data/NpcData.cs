using System.Collections.Generic;
using UnityEngine;

// NPC 등급 정의
public enum NpcRarity
{
    노말,
    레어,
    전설,
    신화
}

// NPC 직업 타입 정의
public enum NpcJobType
{
    채집가,
    전사,
    궁수,
    탱커,
    힐러,
    딜러,
    폭탄병,
    디버퍼,
    법사,
    기타
}

// 시너지 타입 정의
public enum SynergyType
{
    채굴전문가,
    채집전문가,
    전사의의지,
    정밀사격,
    노동자의힘,
    철벽방어,
    방패와창,
    치유의빛,
    수호천사,
    공격본능,
    불타는전장,
    광역폭격,
    약화의손길,
    암흑의전략,
    마력증폭,
    마나의격류
}

[CreateAssetMenu(fileName = "New NPC Data", menuName = "NPC 시스템/NPC 데이터")]
public class NpcData : ScriptableObject
{
    [Header("기본 정보")]
    public string npcId;              // 고유 식별자 (예: "김광부", "김채집" 등)
    public string npcName;            // NPC 표시 이름
    public Sprite portrait;           // 초상화 이미지
    public GameObject prefab;         // NPC 프리팹

    [Header("NPC 세부 정보")]
    public NpcRarity rarity;          // NPC 등급 (노말, 레어, 전설, 신화)
    public NpcJobType jobType;        // NPC 직업 타입

    [Header("대화 정보")]
    [TextArea(2, 5)]
    public string description;        // NPC 설명 (예: "내가 팔을 휘두르기 시작한 지 40년, 광산이 무너져도 내 곡괭이는 멈추지 않지.")

    [Header("능력치")]
    public int health = 100;          // 체력
    public int attack = 10;           // 공격력
    public int defense = 5;           // 방어력
    public float attackSpeed = 1.0f;  // 공격 속도

    [Header("특수 능력치")]
    public float criticalChance = 0f;     // 치명타 확률 (%)
    public float channelSpeed = 0f;       // 채집/채굴 속도 보너스 (%)
    public float magicPower = 0f;         // 마법 공격력
    public bool canHeal = false;          // 치유 가능 여부
    public bool canAoe = false;           // 광역 공격 가능 여부
    public bool canDebuff = false;        // 디버프 가능 여부

    [Header("시너지 정보")]
    public List<SynergyType> synergyTypes; // 시너지 타입 목록

    [Header("특수 능력")]
    public List<NpcAbility> abilities;    // 특수 능력 목록

    [Header("이동 설정")]
    public float moveSpeed = 1.0f;        // 이동 속도
    public float idleTimeMin = 2.0f;      // 최소 정지 시간
    public float idleTimeMax = 5.0f;      // 최대 정지 시간
    public float moveTimeMin = 1.0f;      // 최소 이동 시간
    public float moveTimeMax = 3.0f;      // 최대 이동 시간

    // NPC 시너지 설명 반환 메서드
    public string GetSynergyDescription()
    {
        string result = "시너지 효과:\n";

        foreach (SynergyType synergy in synergyTypes)
        {
            switch (synergy)
            {
                case SynergyType.채굴전문가:
                    result += "- 채굴 전문가: 김광부 3명 이상 사용 시 광물 채집량 +30% 증가\n";
                    break;
                case SynergyType.채집전문가:
                    result += "- 채집 전문가: 김채집 3명 이상 사용 시 채집 속도 +30% 증가\n";
                    break;
                case SynergyType.전사의의지:
                    result += "- 전사의 의지: 김전사 3명 이상 사용 시 모든 전사 계열 NPC 공격력 +10%\n";
                    break;
                case SynergyType.정밀사격:
                    result += "- 정밀 사격: 김궁수 2명 이상 사용 시 모든 궁수 계열 NPC 치명타 확률 +10%\n";
                    break;
                case SynergyType.노동자의힘:
                    result += "- 노동자의 힘: 김광부 + 김채집 + 김전사 + 김궁수 모두 사용 시, 채집 및 기본 공격력 +10%\n";
                    break;
                case SynergyType.철벽방어:
                    result += "- 철벽 방어: 김탱커 2명 이상 사용 시 아군 전체 피해 감소 +10%\n";
                    break;
                case SynergyType.방패와창:
                    result += "- 방패와 창: 김탱커 + 김전사 + 김궁수 조합 시 전투력 +15%\n";
                    break;
                case SynergyType.치유의빛:
                    result += "- 치유의 빛: 김힐러 2명 이상 사용 시 회복량 +25%\n";
                    break;
                case SynergyType.수호천사:
                    result += "- 수호천사: 김탱커 + 김힐러 조합 시 탱커의 체력 재생 속도 +20%\n";
                    break;
                case SynergyType.공격본능:
                    result += "- 공격 본능: 김딜러 2명 이상 사용 시 공격력 +20%\n";
                    break;
                case SynergyType.불타는전장:
                    result += "- 불타는 전장: 김전사 + 김딜러 + 김궁수 조합 시 공격력 +15%\n";
                    break;
                case SynergyType.광역폭격:
                    result += "- 광역 폭격: 김폭탄 2명 이상 사용 시 광역 공격력 +30%\n";
                    break;
                case SynergyType.약화의손길:
                    result += "- 약화의 손길: 김디버프 2명 이상 사용 시 디버프 지속시간 +30%\n";
                    break;
                case SynergyType.암흑의전략:
                    result += "- 암흑의 전략: 김폭탄 + 김디버프 조합 시 적 전체 방어력 감소 +20%\n";
                    break;
                case SynergyType.마력증폭:
                    result += "- 마력 증폭: 김법사 2명 이상 사용 시 모든 마법 피해 +25%\n";
                    break;
                case SynergyType.마나의격류:
                    result += "- 마나의 격류: 김힐러 + 김법사 조합 시 힐러의 회복량 +30%\n";
                    break;
                default:
                    break;
            }
        }

        return result;
    }
}
