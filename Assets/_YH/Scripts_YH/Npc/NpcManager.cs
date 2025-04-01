using System.Collections.Generic;
using UnityEngine;

// NPC 관리자 클래스 - 시너지 시스템을 대체하는 간단한 NPC 관리 기능 제공
public class NpcManager : MonoBehaviour
{
    // 싱글톤 인스턴스
    public static NpcManager Instance { get; private set; }
    
    [Header("NPC 관리 설정")]
    [SerializeField] private bool autoFindNpcsOnStart = true;  // 시작 시 자동으로 NPC 찾기
    
    // 현재 활성화된 NPC 목록
    private List<Npc> activeNpcs = new List<Npc>();
    
    // 등급별 NPC 수량
    private Dictionary<NpcData.NpcRarity, int> npcCountsByRarity = new Dictionary<NpcData.NpcRarity, int>();
    
    // NPC 추가/제거 이벤트
    public System.Action<List<Npc>> OnNpcListChanged;
    
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
        
        // 등급별 카운트 초기화
        foreach (NpcData.NpcRarity rarity in System.Enum.GetValues(typeof(NpcData.NpcRarity)))
        {
            npcCountsByRarity[rarity] = 0;
        }
    }
    
    private void Start()
    {
        // 시작 시 자동으로 NPC 찾기
        if (autoFindNpcsOnStart)
        {
            FindAllNpcsInScene();
        }
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
        if (npc == null || npc.NpcEntry == null) return;
        
        if (!activeNpcs.Contains(npc))
        {
            activeNpcs.Add(npc);
            
            // 등급별 카운트 증가
            NpcData.NpcRarity rarity = npc.GetRarity();
            npcCountsByRarity[rarity]++;
            
            // 이벤트 발생
            OnNpcListChanged?.Invoke(new List<Npc>(activeNpcs));
            Debug.Log($"{npc.NpcName} NPC가 추가되었습니다. (등급: {rarity})");
        }
    }
    
    // NPC 제거
    public void RemoveNpc(Npc npc)
    {
        if (npc == null || npc.NpcEntry == null) return;
        
        if (activeNpcs.Contains(npc))
        {
            activeNpcs.Remove(npc);
            
            // 등급별 카운트 감소
            NpcData.NpcRarity rarity = npc.GetRarity();
            npcCountsByRarity[rarity]--;
            
            // 이벤트 발생
            OnNpcListChanged?.Invoke(new List<Npc>(activeNpcs));
            Debug.Log($"{npc.NpcName} NPC가 제거되었습니다. (등급: {rarity})");
        }
    }
    
    // 현재 활성화된 NPC 목록 가져오기
    public List<Npc> GetActiveNpcs()
    {
        return new List<Npc>(activeNpcs);
    }
    
    // 특정 등급의 NPC 수 가져오기
    public int GetNpcCountByRarity(NpcData.NpcRarity rarity)
    {
        return npcCountsByRarity.ContainsKey(rarity) ? npcCountsByRarity[rarity] : 0;
    }
    
    // 전체 NPC 수 가져오기
    public int GetTotalNpcCount()
    {
        return activeNpcs.Count;
    }
    
    // 활성화된 NPC 목록을 등급별로 분류하여 가져오기
    public Dictionary<NpcData.NpcRarity, List<Npc>> GetNpcsByRarity()
    {
        Dictionary<NpcData.NpcRarity, List<Npc>> result = new Dictionary<NpcData.NpcRarity, List<Npc>>();
        
        // 등급별 리스트 초기화
        foreach (NpcData.NpcRarity rarity in System.Enum.GetValues(typeof(NpcData.NpcRarity)))
        {
            result[rarity] = new List<Npc>();
        }
        
        // NPC 분류
        foreach (Npc npc in activeNpcs)
        {
            NpcData.NpcRarity rarity = npc.GetRarity();
            result[rarity].Add(npc);
        }
        
        return result;
    }
    
    // 특정 등급 이상의 NPC만 가져오기
    public List<Npc> GetNpcsAboveRarity(NpcData.NpcRarity minRarity)
    {
        List<Npc> result = new List<Npc>();
        
        foreach (Npc npc in activeNpcs)
        {
            if (npc.GetRarity() >= minRarity)
            {
                result.Add(npc);
            }
        }
        
        return result;
    }
    
    // NPC 정보 문자열 생성 (디버그용)
    public string GetNpcInfoString()
    {
        string info = "== 활성화된 NPC 목록 ==\n";
        
        // 등급별 카운트 표시
        foreach (NpcData.NpcRarity rarity in System.Enum.GetValues(typeof(NpcData.NpcRarity)))
        {
            int count = GetNpcCountByRarity(rarity);
            info += $"{rarity} 등급: {count}개\n";
        }
        
        info += $"\n총 NPC 수: {GetTotalNpcCount()}개\n\n";
        
        // 개별 NPC 정보
        foreach (Npc npc in activeNpcs)
        {
            info += $"- {npc.NpcName} (등급: {npc.GetRarity()})\n";
        }
        
        return info;
    }
}
