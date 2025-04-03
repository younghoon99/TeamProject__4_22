using System.Collections;
using System.Collections.Generic;
using System.Linq; // For LINQ methods like Contains
using UnityEngine;
using UnityEngine.Tilemaps;

public class ResourceTileSpawner : MonoBehaviour
{
    
    [SerializeField] private float fixedYPosition = 0f; // 스폰 위치의 고정된 Y 좌표
    [SerializeField] private Tilemap tilemap; // 타일맵 참조
    [SerializeField] private Tile[] woodTiles; // 나무 타일 변형 배열
    [SerializeField] private Tile[] stoneTiles; // 돌 타일 변형 배열
    [SerializeField] private int maxWoodTiles = 10; // 최대 나무 타일 개수
    [SerializeField] private int maxStoneTiles = 10; // 최대 돌 타일 개수
    [SerializeField] private float spawnInterval = 5f; // 스폰 시도 간격
    [SerializeField] private GameObject woodPrefab; // 나무 타일 제거 시 생성할 오브젝트
    [SerializeField] private GameObject stonePrefab; // 돌 타일 제거 시 생성할 오브젝트

    // 외부에서 접근 가능하도록 public으로 변경
    public List<Vector3> spawnedWoodTilePositions = new List<Vector3>();
    public List<Vector3> spawnedStoneTilePositions = new List<Vector3>();

    public Tile[] GetWoodTiles()
    {
        return woodTiles;
    }
    
    public Tile[] GetStoneTiles()
    {
        return stoneTiles;
    }
    
    // 가장 가까운 나무 타일 위치 반환
    public Vector3 GetNearestWoodTilePosition(Vector3 fromPosition)
    {
        if (spawnedWoodTilePositions.Count == 0)
            return Vector3.zero;
            
        Vector3 nearestPosition = Vector3.zero;
        float minDistance = float.MaxValue;
        
        foreach (Vector3 position in spawnedWoodTilePositions)
        {
            float distance = Vector3.Distance(fromPosition, position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearestPosition = position;
            }
        }
        
        return nearestPosition;
    }
    
    // 가장 가까운 돌 타일 위치 반환
    public Vector3 GetNearestStoneTilePosition(Vector3 fromPosition)
    {
        if (spawnedStoneTilePositions.Count == 0)
            return Vector3.zero;
            
        Vector3 nearestPosition = Vector3.zero;
        float minDistance = float.MaxValue;
        
        foreach (Vector3 position in spawnedStoneTilePositions)
        {
            float distance = Vector3.Distance(fromPosition, position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearestPosition = position;
            }
        }
        
        return nearestPosition;
    }

    private void Start()
    {
        StartCoroutine(SpawnTiles());
    }

    private IEnumerator SpawnTiles()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);

            // 나무 타일 생성
            if (spawnedWoodTilePositions.Count < maxWoodTiles && Random.value < 1f / (spawnedWoodTilePositions.Count + 1))
            {
                SpawnTile(woodTiles, spawnedWoodTilePositions);
            }

            // 돌 타일 생성
            if (spawnedStoneTilePositions.Count < maxStoneTiles && Random.value < 1f / (spawnedStoneTilePositions.Count + 1))
            {
                SpawnTile(stoneTiles, spawnedStoneTilePositions);
            }
        }
    }

    private void SpawnTile(Tile[] tileArray, List<Vector3> spawnedTilePositions)
    {
        // 배열에서 랜덤 타일 선택
        int randomIndex = Random.Range(0, tileArray.Length);

        // 타일맵에 랜덤 위치 선택
        Vector3 randomPosition = GetRandomTilePosition();

        // Z 값이 일관되게 유지되도록 함
        randomPosition.z = 0f; // Z 값을 0으로 설정

        // 랜덤 위치를 셀 좌표로 변환
        Vector3Int cellPosition = tilemap.WorldToCell(randomPosition);

        // 셀 위치가 타일맵 경계 내에 있는지 확인
        if (!tilemap.cellBounds.Contains(cellPosition))
        {
            Debug.LogWarning($"위치 {cellPosition}가 타일맵 경계를 벗어났습니다.");
            return;
        }

        // 위치가 이미 점유되어 있는지 확인
        if (tilemap.GetTile(cellPosition) == null)
        {
            // 타일맵에 타일 배치
            tilemap.SetTile(cellPosition, tileArray[randomIndex]);

            // 타일이 올바르게 렌더링되도록 새로고침
            tilemap.RefreshTile(cellPosition);

            // 생성된 타일 목록에 위치 추가
            Vector3 cellCenterPosition = tilemap.CellToWorld(cellPosition);
            spawnedTilePositions.Add(cellCenterPosition);

            // 필요한 경우 타일맵 경계 크기 조정
            if (!tilemap.cellBounds.Contains(cellPosition))
            {
                tilemap.ResizeBounds();
            }
        }
    }

    [SerializeField] private float minX = -10f; // 스폰 가능한 최소 X 위치
    [SerializeField] private float maxX = 10f;  // 스폰 가능한 최대 X 위치
    [SerializeField] private float minDistanceBetweenTiles = 3f; // 생성된 타일 간 최소 거리

    private Vector3 GetRandomTilePosition()
    {
        Vector3 randomPosition;
        bool positionIsValid;

        do
        {
            // 지정된 범위 내에서 랜덤 X 위치 생성
            float randomX = Random.Range(minX, maxX);
            randomPosition = new Vector3(randomX, fixedYPosition, 0f);

            // 위치가 모든 생성된 타일로부터 최소 거리 이상 떨어져 있는지 확인
            positionIsValid = true;
            foreach (var position in spawnedWoodTilePositions)
            {
                if (Vector3.Distance(randomPosition, position) < minDistanceBetweenTiles)
                {
                    positionIsValid = false;
                    break;
                }
            }

            if (positionIsValid)
            {
                foreach (var position in spawnedStoneTilePositions)
                {
                    if (Vector3.Distance(randomPosition, position) < minDistanceBetweenTiles)
                    {
                        positionIsValid = false;
                        break;
                    }
                }
            }
        } while (!positionIsValid);

        return randomPosition;
    }

    private readonly object tileLock = new object(); // 동기화를 위한 lock 객체
    private float prefabYOffset = 1.0f; // 프리팹 생성 시 Y축 오프셋

    // 타일 삭제 처리
    public void RemoveTile(Vector3Int cellPosition)
    {
        lock (tileLock) // 동기화 블록
        {
            // 타일맵에서 타일 삭제
            TileBase tile = tilemap.GetTile(cellPosition);
            if (tile != null)
            {
                // 월드 좌표로 변환
                Vector3 worldPosition = tilemap.CellToWorld(cellPosition);

                // 나무 타일 제거 처리
                if (spawnedWoodTilePositions.Contains(worldPosition))
                {
                    // 나무 타일 위치에 오브젝트 생성 (Y값 조정)
                    if (woodPrefab != null)
                    {
                        Vector3 spawnPosition = new Vector3(worldPosition.x, worldPosition.y + prefabYOffset, worldPosition.z);
                        Instantiate(woodPrefab, spawnPosition, Quaternion.identity);
                        Debug.Log($"나무 오브젝트 생성됨: {spawnPosition}");
                    }

                    // 나무 타일 제거
                    spawnedWoodTilePositions.Remove(worldPosition);
                    Debug.Log($"나무 타일 삭제됨: {cellPosition}");
                }
                // 돌 타일 제거 처리
                else if (spawnedStoneTilePositions.Contains(worldPosition))
                {
                    // 돌 타일 위치에 오브젝트 생성 (Y값 조정)
                    if (stonePrefab != null)
                    {
                        Vector3 spawnPosition = new Vector3(worldPosition.x, worldPosition.y + prefabYOffset, worldPosition.z);
                        Instantiate(stonePrefab, spawnPosition, Quaternion.identity);
                        Debug.Log($"돌 오브젝트 생성됨: {spawnPosition}");
                    }

                    // 돌 타일 제거
                    spawnedStoneTilePositions.Remove(worldPosition);
                    Debug.Log($"돌 타일 삭제됨: {cellPosition}");
                }

                // 타일맵에서 타일 삭제
                tilemap.SetTile(cellPosition, null);
                tilemap.RefreshTile(cellPosition); // 타일맵 갱신
            }
        }
    }
}