using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TileResourceSpawner : MonoBehaviour
{
    public Tilemap resourceTilemap;         // 자원 전용 타일맵
    public Tile resourceTile;               // 생성할 자원 타일
    public int maxResources = 20;           // 최대 자원 타일 개수
    public float spawnInterval = 5f;        // 생성 시도 간격(초)
    public float tileSize = 1f;             // 타일 크기 (타일맵 셀 크기와 동일)

    private void Start()
    {
        InvokeRepeating("TrySpawnResourceTile", spawnInterval, spawnInterval);
    }

    void TrySpawnResourceTile()
    {
        // 현재 타일맵에 배치된 자원 타일 개수 계산
        int currentResourceCount = 0;
        BoundsInt bounds = resourceTilemap.cellBounds;
        foreach (var pos in bounds.allPositionsWithin)
        {
            if (resourceTilemap.GetTile(pos) == resourceTile)
                currentResourceCount++;
        }

        // 자원 개수에 따른 스폰 확률 선형 보간
        float spawnProbability = Mathf.Lerp(1f, 0f, currentResourceCount / (float)maxResources);

        if (Random.value <= spawnProbability)
        {
            // 무작위 셀 선택
            Vector3Int randomCell = GetRandomTileCell(bounds);
            // 선택한 셀 주변 3타일 반경에 자원 타일이 없다면 배치
            if (IsAreaClear(randomCell))
            {
                // 셀에 자원 타일 배치
                resourceTilemap.SetTile(randomCell, resourceTile);
            }
        }
    }

    Vector3Int GetRandomTileCell(BoundsInt bounds)
    {
        int x = Random.Range(bounds.xMin, bounds.xMax);
        int y = Random.Range(bounds.yMin, bounds.yMax);
        return new Vector3Int(x, y, 0);
    }

    bool IsAreaClear(Vector3Int cellPosition)
    {
        // 3타일 반경 내에 이미 자원 타일이 존재하는지 확인
        for (int x = -3; x <= 3; x++)
        {
            for (int y = -3; y <= 3; y++)
            {
                Vector3Int checkPos = new Vector3Int(cellPosition.x + x, cellPosition.y + y, cellPosition.z);
                if (resourceTilemap.GetTile(checkPos) == resourceTile)
                    return false;
            }
        }
        return true;
    }
}
