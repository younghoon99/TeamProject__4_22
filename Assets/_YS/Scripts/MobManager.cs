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
    public Transform hqTransform; // HQ Transform
    public Transform[] wallTransforms; // Wall Transform 배열
    public GameObject[] itemPrefabs; // 아이템 프리펩 배열
    private List<GameObject> spawnedItems = new List<GameObject>(); // 스폰된 아이템 리스트
    private int destroyedMobCount = 0; // 파괴된 몹 수
    private bool isCooldownActive = false; // 쿨다운 활성화 여부
    public Transform leftWallTransform; // 좌측 Wall Transform
    public Transform rightWallTransform; // 우측 Wall Transform

    private void Start()
    {
        if (playerTransform == null)
        {
            Debug.LogError("Player Transform not assigned in MobManager!");
            return;
        }

        // Wall 콜라이더를 트리거로 설정
        if (wallTransforms != null)
        {
            foreach (var wallTransform in wallTransforms)
            {
                Collider wallCollider = wallTransform.GetComponent<Collider>();
                if (wallCollider != null)
                {
                    wallCollider.isTrigger = true;
                }
            }
        }

        // Wall 콜라이더와 플레이어 콜라이더 간 충돌 무시 설정
        if (wallTransforms != null && playerTransform != null)
        {
            foreach (var wallTransform in wallTransforms)
            {
                Collider wallCollider = wallTransform.GetComponent<Collider>();
                Collider playerCollider = playerTransform.GetComponent<Collider>();
                if (wallCollider != null && playerCollider != null)
                {
                    Physics.IgnoreCollision(wallCollider, playerCollider);
                }
            }
        }

        StartCoroutine(DelayedStart());
    }

    private IEnumerator DelayedStart()
    {
        yield return new WaitForSeconds(60f); // 게임 시작 후 60초 대기
        StartCoroutine(SpawnMobs());
    }

    private IEnumerator SpawnMobs()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);

            // 쿨다운 중이면 스폰 중단
            if (isCooldownActive) continue;

            // 모든 몹이 사라졌는지 확인
            if (activeMobs.Count == 0 && destroyedMobCount > 0)
            {
                destroyedMobCount = 0; // 파괴된 몹 수 초기화
                isCooldownActive = true;
                yield return new WaitForSeconds(60f); // 60초 대기
                isCooldownActive = false;
            }

            // 최대 몹 수 제한
            if (activeMobs.Count >= maxMobs)
            {
                StartCoroutine(StartCooldown()); // 최대 몹 수 도달 시 쿨다운 시작
                continue;
            }

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
            mobBehavior.Initialize(playerTransform, hqTransform, wallTransforms, leftWallTransform, rightWallTransform);

            // 활성화된 몹 리스트에 추가
            activeMobs.Add(mob);

            // 몹이 파괴되었을 때 리스트에서 제거 및 아이템 생성
            mobBehavior.OnDestroyed += () =>
            {
                activeMobs.Remove(mob);
                SpawnItem(mob.transform.position);

                // 파괴된 몹 수 증가
                destroyedMobCount++;

                // 20마리 몹이 파괴되면 쿨다운 시작
                if (destroyedMobCount >= 20)
                {
                    StartCoroutine(StartCooldown());
                }
            };
        }
    }

    private void SpawnItem(Vector3 position)
    {
        if (itemPrefabs.Length == 0) return;

        // 랜덤 아이템 선택
        int itemIndex = Random.Range(0, itemPrefabs.Length);

        // 아이템 생성
        GameObject item = Instantiate(itemPrefabs[itemIndex], position, Quaternion.identity);

        // 스폰된 아이템 리스트에 추가
        spawnedItems.Add(item);

        // 아이템의 수명 관리
        StartCoroutine(HandleItemLifetime(item));
    }

    private IEnumerator HandleItemLifetime(GameObject item)
    {
        yield return new WaitForSeconds(5f);

        // 5초 후 깜빡거리기 시작
        Renderer itemRenderer = item.GetComponent<Renderer>();
        if (itemRenderer != null)
        {
            float blinkDuration = 5f;
            float blinkInterval = 0.2f;
            float elapsedTime = 0f;

            while (elapsedTime < blinkDuration)
            {
                itemRenderer.enabled = !itemRenderer.enabled;
                yield return new WaitForSeconds(blinkInterval);
                elapsedTime += blinkInterval;
            }

            // 깜빡거림 종료 후 렌더러 활성화
            itemRenderer.enabled = true;
        }

        // 10초 후 아이템 제거
        yield return new WaitForSeconds(5f);
        spawnedItems.Remove(item);
        Destroy(item);
    }

    private IEnumerator StartCooldown()
    {
        isCooldownActive = true;
        destroyedMobCount = 0; // 파괴된 몹 수 초기화
        yield return new WaitForSeconds(60f); // 60초 쿨다운
        isCooldownActive = false;
    }

    private void OnDestroy()
    {
        // 씬에서 남아 있는 모든 아이템 제거
        foreach (var item in spawnedItems)
        {
            if (item != null)
            {
                Destroy(item);
            }
        }
        spawnedItems.Clear();
    }
}