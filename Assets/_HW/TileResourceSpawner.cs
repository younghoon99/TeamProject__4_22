    using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using DG.Tweening; // Tweening 효과를 위해 DOTween 사용

public class TileResourceSpawner : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float interactionRange = 2f;
    public LayerMask interactionLayer;
    public GameObject interactionUI;
    public GameObject collectionGaugeUI; // 게이지바 UI

    private Transform player;

    [Header("Resource Settings")]
    public List<ResourceType> resourceTypes = new List<ResourceType>();
    public Tilemap resourceTilemap;
    public int maxResources = 20;
    public float spawnInterval = 5f;
    public float tileSize = 1f;

    [Header("Spawn Settings")]
    public List<Vector3Int> spawnPositions = new List<Vector3Int>();

    private void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    private void Start()
    {
        InvokeRepeating(nameof(TrySpawnResourceTile), spawnInterval, spawnInterval);
    }

    private void Update()
    {
        HandleInteraction();
    }

    private void HandleInteraction()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(player.position, interactionRange, interactionLayer);

        foreach (var hit in hits)
        {
            Vector3Int cellPosition = resourceTilemap.WorldToCell(hit.transform.position);
            TileBase tile = resourceTilemap.GetTile(cellPosition);

            if (tile != null)
            {
                interactionUI.SetActive(true);

                if (Input.GetKeyDown(KeyCode.E)) // 상호작용 키
                {
                    StartCoroutine(CollectResource(cellPosition));
                }

                return;
            }
        }

        interactionUI.SetActive(false);
    }

    private IEnumerator CollectResource(Vector3Int cellPosition)
    {
        // 게이지바 활성화
        collectionGaugeUI.SetActive(true);

        // 채집 시간 시뮬레이션
        float collectionTime = 2f;
        float elapsedTime = 0f;

        while (elapsedTime < collectionTime)
        {
            elapsedTime += Time.deltaTime;
            // 게이지바 업데이트 (예: fillAmount)
            yield return null;
        }

        collectionGaugeUI.SetActive(false);

        // 자원 제거
        resourceTilemap.SetTile(cellPosition, null);

        // 자원 획득 연출
        PlayResourceCollectionEffect(cellPosition);

        Debug.Log("Resource collected!");
    }

    private void PlayResourceCollectionEffect(Vector3Int cellPosition)
    {
        Vector3 worldPosition = resourceTilemap.CellToWorld(cellPosition);
        GameObject resourceIcon = Instantiate(interactionUI, worldPosition, Quaternion.identity); // 임시 아이콘 생성
        resourceIcon.transform.DOMove(player.position, 1f).OnComplete(() =>
        {
            Destroy(resourceIcon);
            // 플레이어 인벤토리에 자원 추가
        });
    }

    private void TrySpawnResourceTile()
    {
        int currentResourceCount = CountResourceTiles();

        // 자원 개수에 따른 스폰 확률 계산
        float spawnProbability = Mathf.Lerp(1f, 0f, currentResourceCount / (float)maxResources);

        if (Random.value > spawnProbability) return;

        Vector3Int randomCell = GetRandomTileCell(resourceTilemap.cellBounds);

        if (IsAreaClear(randomCell))
        {
            resourceTilemap.SetTile(randomCell, resourceTypes[0].resourceTile); // 기본 자원 타일 설정
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
        public Color gizmoColor = Color.white;
        public ResourceCategory category;
        public float groundOffsetY = 0f;
    }

    public enum ResourceCategory
    {
        Stone,
        Wood
    }
}
