using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// NPC 능력치 관련 클래스
/// </summary>
public class NpcAbility : MonoBehaviour
{
    private Npc npc;

    [Header("채굴 관련")]
    [SerializeField] private float gatheringCoolTime = 1.0f;
    [SerializeField] private ParticleSystem gatheringEffect;
    [SerializeField] private AudioSource gatheringSound;
    
    private float currentGatheringCoolTime;
    private bool isGathering = false;

    private void Start()
    {
        npc = GetComponent<Npc>();
        currentGatheringCoolTime = 0f;
    }

    private void Update()
    {
        if (isGathering)
        {
            currentGatheringCoolTime -= Time.deltaTime;
            if (currentGatheringCoolTime <= 0f)
            {
                Gathering();
                currentGatheringCoolTime = gatheringCoolTime / npc.GetMiningPower();
            }
        }
    }

    /// <summary>
    /// 채굴 시작
    /// </summary>
    public void StartGathering()
    {
        isGathering = true;
        currentGatheringCoolTime = 0f;
        
        if (gatheringEffect != null && !gatheringEffect.isPlaying)
        {
            gatheringEffect.Play();
        }
        
        if (gatheringSound != null)
        {
            gatheringSound.Play();
        }
    }

    /// <summary>
    /// 채굴 중지
    /// </summary>
    public void StopGathering()
    {
        isGathering = false;
        
        if (gatheringEffect != null && gatheringEffect.isPlaying)
        {
            gatheringEffect.Stop();
        }
        
        if (gatheringSound != null)
        {
            gatheringSound.Stop();
        }
    }

    /// <summary>
    /// 채굴 실행
    /// </summary>
    private void Gathering()
    {
        int resourceAmount = Mathf.RoundToInt(npc.GetMiningPower() / 2f);
        if (resourceAmount < 1) resourceAmount = 1;
    }

    /// <summary>
    /// NPC가 전투 시 데미지를 계산
    /// </summary>
    /// <returns>계산된 데미지</returns>
    public int CalculateDamage()
    {
        int baseDamage = 5;
        int totalDamage = baseDamage * npc.GetAttackPower();
        return totalDamage;
    }
}
