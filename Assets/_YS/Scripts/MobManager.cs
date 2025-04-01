using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MobManager : MonoBehaviour
{
    public GameObject[] mobPrefabs;
    public Transform[] spawnPoints; // 스폰 위치 배열
    public float spawnInterval = 2f;
    private List<GameObject> activeMobs = new List<GameObject>(); // 활성화된 몹 리스트
    private const int maxMobs = 20; // 최대 몹 수

    private void Start()
    {
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

            // Rigidbody2D 추가
            if (mob.GetComponent<Rigidbody2D>() == null)
            {
                mob.AddComponent<Rigidbody2D>();
            }

            // BoxCollider2D 추가
            if (mob.GetComponent<BoxCollider2D>() == null)
            {
                mob.AddComponent<BoxCollider2D>();
            }

            // NpcMove 컴포넌트 추가 및 초기화
            NpcMove npcMove = mob.GetComponent<NpcMove>();
            if (npcMove == null)
            {
                npcMove = mob.AddComponent<NpcMove>();
            }

            // 활성화된 몹 리스트에 추가
            activeMobs.Add(mob);

            // 몹이 파괴되었을 때 리스트에서 제거
            //npcMove.OnDestroyed += () => activeMobs.Remove(mob);
        }
    }
}
