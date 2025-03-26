using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class ResourceManager : MonoBehaviour
{
    [Header("Resource Settings")]
    public List<ResourceType> resourceTypes = new List<ResourceType>();
    public Tilemap resourceTilemap;

    [Header("Player Settings")]
    public Transform player;
    public float interactionRange = 2f;
    public LayerMask interactionLayer;

    [Header("Spawn Settings")]
    public int maxResources = 20;
    public float spawnInterval = 5f;

    private void Start()
    {
        InvokeRepeating(nameof(SpawnResources), spawnInterval, spawnInterval);
    }

    private void SpawnResources()
    {
        int currentResourceCount = CountResourceTiles();

        if (currentResourceCount >= maxResources)
            return;

        float spawnProbability = Mathf.Lerp(1f, 0f, currentResourceCount / (float)maxResources);

        if (Random.value > spawnProbability)
            return;

        Vector3Int randomCell = GetRandomTileCell(resourceTilemap.cellBounds);

        if (IsAreaClear(randomCell))
        {
            TileBase randomTile = resourceTypes[Random.Range(0, resourceTypes.Count)].resourceTile;
            resourceTilemap.SetTile(randomCell, randomTile);
        }
    }

    private int CountResourceTiles()
    {
        int count = 0;
        BoundsInt bounds = resourceTilemap.cellBounds;

        foreach (var pos in bounds.allPositionsWithin)
        {
            if (resourceTilemap.GetTile(pos) != null)
                count++;
        }

        return count;
    }

    private Vector3Int GetRandomTileCell(BoundsInt bounds)
    {
        int x = Random.Range(bounds.xMin, bounds.xMax);
        int y = Random.Range(bounds.yMin, bounds.yMax);
        return new Vector3Int(x, y, 0);
    }

    private bool IsAreaClear(Vector3Int cellPosition)
    {
        for (int x = -3; x <= 3; x++)
        {
            for (int y = -3; y <= 3; y++)
            {
                Vector3Int checkPos = new Vector3Int(cellPosition.x + x, cellPosition.y + y, cellPosition.z);
                if (resourceTilemap.GetTile(checkPos) != null)
                    return false;
            }
        }
        return true;
    }

    [System.Serializable]
    public class ResourceType
    {
        public string name;
        public Tile resourceTile;
        public Sprite icon;
        public float collectionTime = 2f;
    }
}