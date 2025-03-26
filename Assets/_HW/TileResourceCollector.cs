using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TileResourceCollector : MonoBehaviour
{
    private Animator animator; // Animator component for animations
    private bool isCollecting = false;
    private Coroutine currentCollectionCoroutine;

    [Header("Resource Settings")]
    public Tilemap resourceTilemap;   // Resource tilemap
    public Tile resourceTile;         // Resource tile
    public float collectionRange = 2f; // Range to search for resources
    public float collectionTime = 2f;  // Time required to collect resources

    private Vector3 lastPosition;

    void Start()
    {
        animator = GetComponent<Animator>();
        lastPosition = transform.position;
    }

    void Update()
    {
        // Check if the player moved during collection
        if (isCollecting && Vector3.Distance(transform.position, lastPosition) > 0.01f)
        {
            CancelCollection();
        }

        lastPosition = transform.position;

        if (Input.GetKeyDown(KeyCode.E) && !isCollecting)
        {
            Vector3Int targetCell;
            if (FindNearestResourceTile(out targetCell))
            {
                currentCollectionCoroutine = StartCoroutine(CollectResource(targetCell));
            }
        }
    }

    private bool FindNearestResourceTile(out Vector3Int targetCell)
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

    private IEnumerator CollectResource(Vector3Int cell)
    {
        isCollecting = true;

        // Trigger animation if animator exists
        if (animator != null)
        {
            animator.SetTrigger("Collect");
        }

        float elapsed = 0f;

        while (elapsed < collectionTime)
        {
            // Check if collection was canceled
            if (!isCollecting)
            {
                yield break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Remove the resource tile after collection
        resourceTilemap.SetTile(cell, null);

        // Add logic to update inventory or other systems here

        isCollecting = false;
    }

    private void CancelCollection()
    {
        if (isCollecting)
        {
            isCollecting = false;

            // Stop the current collection coroutine
            if (currentCollectionCoroutine != null)
            {
                StopCoroutine(currentCollectionCoroutine);
                currentCollectionCoroutine = null;
            }

            // Trigger cancel animation if needed
            if (animator != null)
            {
                animator.SetTrigger("Cancel");
            }
        }
    }
}