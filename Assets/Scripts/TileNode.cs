using System.Collections.Generic;
using UnityEngine;

public class TileNode : MonoBehaviour
{
    [Header("Node Settings")]
    public int tileIndex; // 0 to 51 for main path
    public bool isSafeZone = false;
    public bool isHomeEntrance = false;
    public GameManager.PlayerColor homeEntranceColor;

    [Header("Connected Nodes")]
    public TileNode nextNode;
    public TileNode homeStretchNode; // Only set if isHomeEntrance is true

    [Header("Current Occupants")]
    public List<TokenMovement> tokensOnTile = new List<TokenMovement>();

    public void AddToken(TokenMovement token)
    {
        if (!tokensOnTile.Contains(token))
        {
            tokensOnTile.Add(token);
            ReorganizeTileOccupants();
        }
    }

    public void RemoveToken(TokenMovement token)
    {
        if (tokensOnTile.Contains(token))
        {
            tokensOnTile.Remove(token);
            ReorganizeTileOccupants();
        }
    }

    public void ReorganizeTileOccupants()
    {
        int count = tokensOnTile.Count;
        if (count == 0) return;

        // Visual offsets for stacked tokens on a single node
        Vector3[] offsets = count switch
        {
            1 => new Vector3[] { Vector3.zero },
            2 => new Vector3[] { new Vector3(-0.08f, 0f, 0f), new Vector3(0.08f, 0f, 0f) },
            3 => new Vector3[] { new Vector3(-0.08f, 0.08f, 0f), new Vector3(0.08f, 0.08f, 0f), new Vector3(0f, -0.08f, 0f) },
            _ => new Vector3[] { new Vector3(-0.08f, 0.08f, 0f), new Vector3(0.08f, 0.08f, 0f), new Vector3(-0.08f, -0.08f, 0f), new Vector3(0.08f, -0.08f, 0f) }
        };

        for (int i = 0; i < count; i++)
        {
            int offsetIndex = Mathf.Min(i, offsets.Length - 1);
            Vector3 targetPos = transform.position + offsets[offsetIndex];
            targetPos.z = -1f - (i * 0.01f); // Micro Z-depth layering
            tokensOnTile[i].transform.position = targetPos;
        }
    }
}