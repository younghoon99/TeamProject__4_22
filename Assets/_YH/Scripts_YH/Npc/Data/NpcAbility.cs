using UnityEngine;

// 능력 유형 정의
public enum AbilityType
{
    공격,     // 적에게 피해를 주는 능력
    치유,     // 아군을 치유하는 능력
    버프,     // 능력치를 올려주는 능력
    디버프,   // 적의 능력치를 낮추는 능력
    채집,     // 채집 관련 능력
    특수,     // 특수 효과를 제공하는 능력
    소환      // 다른 개체를 소환하는 능력
}

[CreateAssetMenu(fileName = "NewNpcAbility", menuName = "NPC System/NPC Ability")]
public class NpcAbility : ScriptableObject
{
    [Header("능력 정보")]
    public string abilityName;           // 능력 이름
    [TextArea(2, 4)]
    public string description;           // 능력 설명
    public Sprite icon;                  // 능력 아이콘
    public AbilityType type;             // 능력 유형
    
    [Header("능력 설정")]
    public float cooldown;               // 쿨다운 시간 (초)
    public int energyCost;               // 사용 비용 (에너지/마나 등)
    
    [Header("효과 수치")]
    public int damageAmount;             // 피해량
    public int healAmount;               // 회복량
    public float buffDuration;           // 버프 지속시간
    public float buffAmount;             // 버프 효과량
    public float channelTime;            // 시전 시간
    
    [Header("범위 설정")]
    public bool isAoe;                   // 광역 효과 여부
    public float effectRadius;           // 효과 반경 (광역일 경우)
    
    [Header("시각 및 오디오 효과")]
    public GameObject effectPrefab;      // 이펙트 프리팹
    public AudioClip soundEffect;        // 효과음
    
    // 능력 사용 메서드 (실제 효과를 구현하는 기본 로직)
    public virtual void UseAbility(GameObject user, GameObject target = null)
    {
        Debug.Log($"{user.name}이(가) {abilityName} 능력을 사용했습니다.");
        
        // 능력 유형에 따른 기본 처리
        switch (type)
        {
            case AbilityType.공격:
                // 공격 로직
                ApplyDamage(user, target);
                break;
                
            case AbilityType.치유:
                // 치유 로직
                ApplyHeal(user, target);
                break;
                
            case AbilityType.버프:
                // 버프 로직
                ApplyBuff(user, target);
                break;
                
            case AbilityType.디버프:
                // 디버프 로직
                ApplyDebuff(user, target);
                break;
                
            case AbilityType.채집:
                // 채집 로직
                ApplyGathering(user, target);
                break;
                
            case AbilityType.특수:
                // 특수 로직
                ApplySpecialEffect(user, target);
                break;
                
            case AbilityType.소환:
                // 소환 로직
                ApplySummon(user, target);
                break;
        }
        
        // 이펙트 및 사운드 처리
        PlayEffectsAndSounds(user, target);
    }
    
    // 피해 적용
    protected virtual void ApplyDamage(GameObject user, GameObject target)
    {
        if (target == null) return;
        
        // 적에게 데미지 적용 (EnemyHealth 컴포넌트가 있다고 가정)
        EnemyHealth enemyHealth = target.GetComponent<EnemyHealth>();
        if (enemyHealth != null)
        {
            // 광역 공격인 경우
            if (isAoe)
            {
                // 반경 내의 모든 적을 찾아 데미지 적용
                Collider2D[] colliders = Physics2D.OverlapCircleAll(target.transform.position, effectRadius);
                foreach (Collider2D col in colliders)
                {
                    EnemyHealth eh = col.GetComponent<EnemyHealth>();
                    if (eh != null)
                    {
                        eh.TakeDamage(damageAmount);
                        Debug.Log($"{col.name}에게 {damageAmount}의 광역 피해를 입혔습니다.");
                    }
                }
            }
            else
            {
                // 단일 대상 공격
                enemyHealth.TakeDamage(damageAmount);
                Debug.Log($"{target.name}에게 {damageAmount}의 피해를 입혔습니다.");
            }
        }
    }
    
    // 치유 적용
    protected virtual void ApplyHeal(GameObject user, GameObject target)
    {
        if (target == null) target = user; // 타겟이 없으면 자신을 타겟으로
        
        // 플레이어나 NPC에게 치유 적용
        // 실제 구현은 Player, Npc 클래스에 힐링 메서드가 있다고 가정
        Player player = target.GetComponent<Player>();
        if (player != null)
        {
            // 플레이어 치유 로직 (구현 필요)
            Debug.Log($"{target.name}의 체력을 {healAmount} 회복했습니다.");
            return;
        }
        
        Npc npc = target.GetComponent<Npc>();
        if (npc != null)
        {
            // NPC 치유 로직 (구현 필요)
            Debug.Log($"{target.name}의 체력을 {healAmount} 회복했습니다.");
        }
    }
    
    // 버프 적용
    protected virtual void ApplyBuff(GameObject user, GameObject target)
    {
        if (target == null) target = user; // 타겟이 없으면 자신을 타겟으로
        
        // 버프 적용 로직 (구현 필요)
        Debug.Log($"{target.name}에게 {abilityName} 버프를 {buffDuration}초 동안 적용했습니다.");
    }
    
    // 디버프 적용
    protected virtual void ApplyDebuff(GameObject user, GameObject target)
    {
        if (target == null) return;
        
        // 디버프 적용 로직 (구현 필요)
        Debug.Log($"{target.name}에게 {abilityName} 디버프를 {buffDuration}초 동안 적용했습니다.");
    }
    
    // 채집 효과 적용
    protected virtual void ApplyGathering(GameObject user, GameObject target)
    {
        if (target == null) return;
        
        // 채집 로직 (구현 필요)
        Debug.Log($"{user.name}이(가) {target.name}에 채집 능력을 사용했습니다.");
    }
    
    // 특수 효과 적용
    protected virtual void ApplySpecialEffect(GameObject user, GameObject target)
    {
        // 특수 효과 로직 (구현 필요)
        Debug.Log($"{user.name}이(가) 특수 능력 {abilityName}을(를) 사용했습니다.");
    }
    
    // 소환 효과 적용
    protected virtual void ApplySummon(GameObject user, GameObject target)
    {
        // 소환 로직 (구현 필요)
        Debug.Log($"{user.name}이(가) 소환 능력 {abilityName}을(를) 사용했습니다.");
    }
    
    // 이펙트 및 사운드 재생
    protected virtual void PlayEffectsAndSounds(GameObject user, GameObject target)
    {
        // 이펙트 생성
        if (effectPrefab != null)
        {
            Vector3 position = (target != null) ? target.transform.position : user.transform.position;
            GameObject effect = Instantiate(effectPrefab, position, Quaternion.identity);
            Destroy(effect, 2f); // 2초 후 이펙트 제거
        }
        
        // 효과음 재생
        if (soundEffect != null)
        {
            AudioSource.PlayClipAtPoint(soundEffect, user.transform.position);
        }
    }
}
