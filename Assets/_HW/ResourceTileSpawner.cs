using System.Collections;
using System.Collections.Generic;
using System.Linq; // For LINQ methods like Contains
using UnityEngine;
using UnityEngine.Tilemaps;

public class ResourceTileSpawner : MonoBehaviour
{
    
    [SerializeField] private float fixedYPosition = 0f; // Fixed Y position for spawning
    [SerializeField] private Tilemap tilemap; // Reference to the Tilemap
    [SerializeField] private Tile[] woodTiles; // Array of wood tile variations
    [SerializeField] private Tile[] stoneTiles; // Array of stone tile variations
    [SerializeField] private int maxWoodTiles = 10; // Maximum number of wood tiles
    [SerializeField] private int maxStoneTiles = 10; // Maximum number of stone tiles
    [SerializeField] private float spawnInterval = 5f; // Time interval for spawn attempts
    [SerializeField] private GameObject woodPrefab; // Wood 타일 제거 시 생성할 오브젝트
    [SerializeField] private GameObject stonePrefab; // Stone 타일 제거 시 생성할 오브젝트

    private List<Vector3> spawnedWoodTilePositions = new List<Vector3>();
    private List<Vector3> spawnedStoneTilePositions = new List<Vector3>();

    public Tile[] GetWoodTiles()
    {
        return woodTiles;
    }
     public Tile[] GetStoneTiles()
    {
        return stoneTiles;
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

            // Spawn wood tile
            if (spawnedWoodTilePositions.Count < maxWoodTiles && Random.value < 1f / (spawnedWoodTilePositions.Count + 1))
            {
                SpawnTile(woodTiles, spawnedWoodTilePositions);
            }

            // Spawn stone tile
            if (spawnedStoneTilePositions.Count < maxStoneTiles && Random.value < 1f / (spawnedStoneTilePositions.Count + 1))
            {
                SpawnTile(stoneTiles, spawnedStoneTilePositions);
            }
        }
    }

    private void SpawnTile(Tile[] tileArray, List<Vector3> spawnedTilePositions)
    {
        // Choose a random tile from the array
        int randomIndex = Random.Range(0, tileArray.Length);

        // Choose a random position on the Tilemap
        Vector3 randomPosition = GetRandomTilePosition();

        // Ensure the Z value is consistent
        randomPosition.z = 0f; // Set Z value to 0

        // Convert the random position to cell coordinates
        Vector3Int cellPosition = tilemap.WorldToCell(randomPosition);

        // Ensure the cell position is within the tilemap bounds
        if (!tilemap.cellBounds.Contains(cellPosition))
        {
            Debug.LogWarning($"Position {cellPosition} is outside the tilemap bounds.");
            return;
        }

        // Check if the position is already occupied
        if (tilemap.GetTile(cellPosition) == null)
        {
            // Place the tile on the Tilemap
            tilemap.SetTile(cellPosition, tileArray[randomIndex]);

            // Refresh the tile to ensure it is rendered correctly
            tilemap.RefreshTile(cellPosition);

            // Add the position to the list of spawned tiles
            Vector3 cellCenterPosition = tilemap.CellToWorld(cellPosition);
            spawnedTilePositions.Add(cellCenterPosition);

            // Optionally resize the tilemap bounds if needed
            if (!tilemap.cellBounds.Contains(cellPosition))
            {
                tilemap.ResizeBounds();
            }
        }
    }

    [SerializeField] private float minX = -10f; // Minimum X position for spawning
    [SerializeField] private float maxX = 10f;  // Maximum X position for spawning
    [SerializeField] private float minDistanceBetweenTiles = 3f; // Minimum distance between spawned tiles

    private Vector3 GetRandomTilePosition()
    {
        Vector3 randomPosition;
        bool positionIsValid;

        do
        {
            // Generate a random X position within the specified range
            float randomX = Random.Range(minX, maxX);
            randomPosition = new Vector3(randomX, fixedYPosition, 0f);

            // Check if the position is at least minDistanceBetweenTiles away from all spawned tiles
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

                // Wood 타일 제거 처리
                if (spawnedWoodTilePositions.Contains(worldPosition))
                {
                    // Wood 타일 위치에 오브젝트 생성
                    if (woodPrefab != null)
                    {
                        Instantiate(woodPrefab, worldPosition, Quaternion.identity);
                        Debug.Log($"Wood 오브젝트 생성됨: {worldPosition}");
                    }

                    // Wood 타일 제거
                    spawnedWoodTilePositions.Remove(worldPosition);
                    Debug.Log($"Wood 타일 삭제됨: {cellPosition}");
                }
                // Stone 타일 제거 처리
                else if (spawnedStoneTilePositions.Contains(worldPosition))
                {
                    // Stone 타일 위치에 오브젝트 생성
                    if (stonePrefab != null)
                    {
                        Instantiate(stonePrefab, worldPosition, Quaternion.identity);
                        Debug.Log($"Stone 오브젝트 생성됨: {worldPosition}");
                    }

                    // Stone 타일 제거
                    spawnedStoneTilePositions.Remove(worldPosition);
                    Debug.Log($"Stone 타일 삭제됨: {cellPosition}");
                }

                // 타일맵에서 타일 삭제
                tilemap.SetTile(cellPosition, null);
                tilemap.RefreshTile(cellPosition); // 타일맵 갱신
            }
        }
    }
}