using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NpcData", menuName = "NPC/NPC 데이터")]
public class NpcData : ScriptableObject
{
    // NPC 등급 정의
    public enum NpcRarity
    {
        노말 = 0,
        레어 = 1,
        영웅 = 2,
        전설 = 3
    }

    // 이름 생성을 위한 데이터
    [System.Serializable]
    public class NameData
    {
        public List<string> firstNames = new List<string>();
        public List<string> lastNames = new List<string>();
    }

    [Header("NPC 이름 데이터")]
    public NameData nameData;

    [Header("NPC 외형 프리팹")]
    public List<GameObject> npcPrefabs;

    [Header("등급별 능력치 총합")]
    public int normalStatTotal = 10;
    public int rareStatTotal = 20;
    public int epicStatTotal = 30;
    public int legendaryStatTotal = 40;

    // NPC 데이터 항목
    [System.Serializable]
    public class NpcEntry
    {
        // 기본 정보
        public string npcId;
        public string npcName;
        public string description;
        public NpcRarity rarity;
        public GameObject prefab;
        
        // 능력치
        public int attack;        // 공격력
        public int health;        // 체력
        public int miningPower;   // 채굴 능력
        public int moveSpeed;     // 이동 속도
        
        // 이동 설정
        public float idleTimeMin = 2.0f;
        public float idleTimeMax = 5.0f;
        public float moveTimeMin = 1.0f;
        public float moveTimeMax = 3.0f;
        
        // 능력치 총합 반환
        public int GetTotalStats()
        {
            return attack + health + miningPower + moveSpeed;
        }
    }

    // NPC 데이터 리스트
    public List<NpcEntry> npcEntries = new List<NpcEntry>();

    // ID로 NPC 데이터 검색
    public NpcEntry GetNpcById(string id)
    {
        return npcEntries.Find(entry => entry.npcId == id);
    }

    // 랜덤 NPC 생성
    public NpcEntry GenerateRandomNpc(NpcRarity rarity = NpcRarity.노말)
    {
        NpcEntry newNpc = new NpcEntry();
        
        // 랜덤 ID 생성 (GUID 기반)
        newNpc.npcId = System.Guid.NewGuid().ToString().Substring(0, 8);
        
        // 랜덤 이름 생성
        newNpc.npcName = GenerateRandomName();
        
        // 등급 설정
        newNpc.rarity = rarity;
        
        // 랜덤 프리팹 선택
        if (npcPrefabs != null && npcPrefabs.Count > 0)
        {
            newNpc.prefab = npcPrefabs[Random.Range(0, npcPrefabs.Count)];
        }
        
        // 등급별 설명 생성
        switch (rarity)
        {
            case NpcRarity.노말:
                newNpc.description = "평범한 NPC입니다.";
                break;
            case NpcRarity.레어:
                newNpc.description = "약간 특별한 능력을 가진 NPC입니다.";
                break;
            case NpcRarity.영웅:
                newNpc.description = "상당한 능력을 보유한 영웅적인 NPC입니다.";
                break;
            case NpcRarity.전설:
                newNpc.description = "전설적인 능력을 지닌 매우 강력한 NPC입니다.";
                break;
        }
        
        // 등급별 능력치 총합 계산
        int statTotal;
        switch (rarity)
        {
            case NpcRarity.레어:
                statTotal = rareStatTotal;
                break;
            case NpcRarity.영웅:
                statTotal = epicStatTotal;
                break;
            case NpcRarity.전설:
                statTotal = legendaryStatTotal;
                break;
            default: // 노말 등급
                statTotal = normalStatTotal;
                break;
        }
        
        // 능력치 랜덤 분배
        DistributeRandomStats(newNpc, statTotal);
        
        // 이동 시간 설정
        newNpc.idleTimeMin = Random.Range(1.5f, 3.0f);
        newNpc.idleTimeMax = newNpc.idleTimeMin + Random.Range(1.0f, 3.0f);
        newNpc.moveTimeMin = Random.Range(0.8f, 2.0f);
        newNpc.moveTimeMax = newNpc.moveTimeMin + Random.Range(1.0f, 2.0f);
        
        return newNpc;
    }
    
    // 랜덤 능력치 분배
    private void DistributeRandomStats(NpcEntry npc, int totalPoints)
    {
        // 최소 능력치 설정 (각 1)
        npc.attack = 1;
        npc.health = 1;
        npc.miningPower = 1;
        npc.moveSpeed = 1;
        
        // 남은 포인트 계산
        int remainingPoints = totalPoints - 4; // 이미 각 능력치에 1씩 할당했으므로
        
        // 남은 포인트 랜덤 분배
        while (remainingPoints > 0)
        {
            int statToIncrease = Random.Range(0, 4); // 0: 공격력, 1: 체력, 2: 채굴력, 3: 이동속도
            
            switch (statToIncrease)
            {
                case 0:
                    npc.attack++;
                    break;
                case 1:
                    npc.health++;
                    break;
                case 2:
                    npc.miningPower++;
                    break;
                case 3:
                    npc.moveSpeed++;
                    break;
            }
            
            remainingPoints--;
        }
    }
    
    // 랜덤 이름 생성
    private string GenerateRandomName()
    {
        if (nameData == null || nameData.firstNames.Count == 0 || nameData.lastNames.Count == 0)
        {
            return "NPC" + Random.Range(1000, 9999);
        }
        
        string firstName = nameData.firstNames[Random.Range(0, nameData.firstNames.Count)];
        string lastName = nameData.lastNames[Random.Range(0, nameData.lastNames.Count)];
        
        return $"{firstName} {lastName}";
    }
}
