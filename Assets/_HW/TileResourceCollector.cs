using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TileResourceCollector : MonoBehaviour
{
    public Tilemap resourceTilemap;   // 자원 타일맵
    public Tile resourceTile;         // 자원 타일
    public float collectionRange = 2f; // 플레이어 주변 탐색 범위
    public float collectionTime = 2f;  // 채집에 걸리는 시간

    private bool isCollecting = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E) && !isCollecting)
        {
            // 플레이어 주변 자원 타일 위치 찾기
            Vector3Int targetCell;
            if (FindNearestResourceTile(out targetCell))
            {
                StartCoroutine(CollectResource(targetCell));
            }
        }
    }

    bool FindNearestResourceTile(out Vector3Int targetCell)
    {
        targetCell = Vector3Int.zero;
        float minDistance = float.MaxValue;
        bool found = false;

        BoundsInt bounds = resourceTilemap.cellBounds;
        foreach (var pos in bounds.allPositionsWithin)
        {
            if (resourceTilemap.GetTile(pos) == resourceTile)
            {
                Vector3 worldPos = resourceTilemap.GetCellCenterWorld(pos);
                float distance = Vector3.Distance(transform.position, worldPos);
                if (distance < collectionRange && distance < minDistance)
                {
                    minDistance = distance;
                    targetCell = pos;
                    found = true;
                }
            }
        }
        return found;
    }

    IEnumerator CollectResource(Vector3Int cell)
    {
        isCollecting = true;
        float elapsed = 0f;

        // (여기서 UI 게이지나 효과 연출 가능)

        while (elapsed < collectionTime)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        // 채집 완료 후 해당 셀의 자원 타일 제거
        resourceTilemap.SetTile(cell, null);
        // 플레이어 인벤토리에 자원 추가 로직 등 추가 가능
        isCollecting = false;
    }
}
