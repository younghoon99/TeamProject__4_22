using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MobManager : MonoBehaviour
{
    public GameObject[] mobPrefabs; // 몹 프리펩 배열
    public Transform[] spawnPoints; // 스폰 위치 배열
    public float spawnInterval = 2f;
    private List<GameObject> activeMobs = new List<GameObject>(); // 활성화된 몹 리스트
    private const int maxMobs = 20; // 최대 몹 수
    public Transform playerTransform; // 기존 플레이어 Transform

    private void Start()
    {
        if (playerTransform == null)
        {
            Debug.LogError("Player Transform not assigned in MobManager!");
            return;
        }

        StartCoroutine(SpawnMobs());
    }

    private IEnumerator SpawnMobs()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);

            // 최대 몹 수 제한
            if (activeMobs.Count >= maxMobs) continue;

            // 랜덤 Mob 선택
            int mobIndex = Random.Range(0, mobPrefabs.Length);

            // 랜덤 스폰 위치 선택
            int spawnPointIndex = Random.Range(0, spawnPoints.Length);

            // Mob 생성
            Vector3 spawnPosition = spawnPoints[spawnPointIndex].position;
            spawnPosition.z = 0f; // z축 고정
            GameObject mob = Instantiate(mobPrefabs[mobIndex], spawnPosition, Quaternion.identity);

            // MobBehavior 컴포넌트 추가 및 초기화
            MobBehavior mobBehavior = mob.AddComponent<MobBehavior>();
            mobBehavior.Initialize(playerTransform);

            // 활성화된 몹 리스트에 추가
            activeMobs.Add(mob);

            // 몹이 파괴되었을 때 리스트에서 제거
            mobBehavior.OnDestroyed += () => activeMobs.Remove(mob);
        }
    }
}