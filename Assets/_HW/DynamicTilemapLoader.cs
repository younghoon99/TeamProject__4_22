using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class DynamicTilemapLoader : MonoBehaviour
{
    [SerializeField] private Tilemap tilemap; // Reference to the Tilemap
    [SerializeField] private Transform player; // Reference to the player
    [SerializeField] private int chunkSize = 10; // Size of the chunk around the player
    [SerializeField] private float fixedYPosition = 0f; // Fixed Y position for the tilemap

    private Vector3Int previousPlayerChunk;
    private Vector3 previousTilemapPosition;

    private void Start()
    {
        // Initialize the previous tilemap position
        previousTilemapPosition = tilemap.transform.position;
    }

    private void Update()
    {
        // Get the player's current chunk position in tilemap coordinates
        Vector3Int currentPlayerChunk = GetChunkPositionFromTilemap(player.position);

        // If the player has moved to a new chunk, update the tilemap position
        if (currentPlayerChunk != previousPlayerChunk)
        {
            UpdateTilemapPosition(currentPlayerChunk);
            previousPlayerChunk = currentPlayerChunk;
        }
    }

    private Vector3Int GetChunkPositionFromTilemap(Vector3 worldPosition)
    {
        // Convert the player's world position to the tilemap's local position
        Vector3 localPosition = tilemap.transform.InverseTransformPoint(worldPosition);

        // Calculate the chunk position based on the local position
        int chunkX = Mathf.FloorToInt(localPosition.x / chunkSize);
        int chunkY = Mathf.FloorToInt(localPosition.y / chunkSize);
        return new Vector3Int(chunkX, chunkY, 0);
    }

    private void UpdateTilemapPosition(Vector3Int playerChunk)
    {
        // Calculate the new position for the tilemap
        Vector3 newTilemapPosition = new Vector3(
            playerChunk.x * chunkSize, // Adjust X based on the player's chunk position
            fixedYPosition, // Keep Y coordinate fixed
            tilemap.transform.position.z
        );

        // Calculate the offset caused by the tilemap's movement
        Vector3 offset = newTilemapPosition - previousTilemapPosition;

        // Only update if there is an actual movement
        if (offset != Vector3.zero)
        {
            MoveTilemap(offset);
            previousTilemapPosition = newTilemapPosition;
        }
    }

    private void MoveTilemap(Vector3 offset)
    {
        // Move the tilemap to the new position
        tilemap.transform.position += offset;

        // Adjust the positions of all tiles within the tilemap
        AdjustTilePositions(-offset);
    }

    private void AdjustTilePositions(Vector3 offset)
    {
        // Get all tile positions within the tilemap bounds
        BoundsInt bounds = tilemap.cellBounds;

        // Iterate through all positions in the tilemap
        foreach (Vector3Int position in bounds.allPositionsWithin)
        {
            // Get the tile at the current position
            TileBase tile = tilemap.GetTile(position);

            if (tile != null)
            {
                // Remove the tile from the current position
                tilemap.SetTile(position, null);

                // Place the tile at the adjusted position
                Vector3Int adjustedPosition = position + Vector3Int.RoundToInt(offset);
                tilemap.SetTile(adjustedPosition, tile);
            }
        }
    }
}