using UnityEngine;
using System.Collections.Generic;

public class LudoPath : MonoBehaviour
{
    public static LudoPath Instance;

    [Header("Main Board Loop (52 Tiles total)")]
    public List<Transform> mainLoop = new List<Transform>();

    [Header("Home Stretches (6 Tiles each)")]
    public List<Transform> redHomeStretch = new List<Transform>();
    public List<Transform> greenHomeStretch = new List<Transform>();
    public List<Transform> yellowHomeStretch = new List<Transform>();
    public List<Transform> blueHomeStretch = new List<Transform>();

    private const int RED_START_INDEX = 0;
    private const int GREEN_START_INDEX = 13;
    private const int YELLOW_START_INDEX = 26;
    private const int BLUE_START_INDEX = 39;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public List<Transform> GetPathForColor(GameManager.PlayerColor color)
    {
        List<Transform> path = new List<Transform>();
        int startIndex = GetStartIndex(color);
        List<Transform> homeStretch = GetHomeStretch(color);

        if (mainLoop != null && mainLoop.Count > 0)
        {
            for (int i = 0; i < mainLoop.Count; i++)
            {
                int loopIndex = (startIndex + i) % mainLoop.Count;
                path.Add(mainLoop[loopIndex]);
            }
        }

        if (homeStretch != null)
        {
            path.AddRange(homeStretch);
        }

        return path;
    }

    public Transform GetTileTransform(GameManager.PlayerColor color, int index)
    {
        List<Transform> colorPath = GetPathForColor(color);
        if (colorPath != null && index >= 0 && index < colorPath.Count)
        {
            return colorPath[index];
        }
        return null;
    }

    public int GetPathLength(GameManager.PlayerColor color)
    {
        int mainCount = mainLoop != null ? mainLoop.Count : 0;
        List<Transform> homeStretch = GetHomeStretch(color);
        int homeCount = homeStretch != null ? homeStretch.Count : 0;
        return mainCount + homeCount;
    }

    public int GetStartIndex(GameManager.PlayerColor color)
    {
        switch (color)
        {
            case GameManager.PlayerColor.RED: return RED_START_INDEX;
            case GameManager.PlayerColor.GREEN: return GREEN_START_INDEX;
            case GameManager.PlayerColor.YELLOW: return YELLOW_START_INDEX;
            case GameManager.PlayerColor.BLUE: return BLUE_START_INDEX;
        }
        return 0;
    }

    private List<Transform> GetHomeStretch(GameManager.PlayerColor color)
    {
        switch (color)
        {
            case GameManager.PlayerColor.RED: return redHomeStretch;
            case GameManager.PlayerColor.GREEN: return greenHomeStretch;
            case GameManager.PlayerColor.YELLOW: return yellowHomeStretch;
            case GameManager.PlayerColor.BLUE: return blueHomeStretch;
        }
        return null;
    }

    // Converts a color-relative tile index into the shared board index (0-51).
    // Returns -1 if the token is already inside its own home stretch (not on the shared loop).
    public int GetAbsoluteMainIndex(GameManager.PlayerColor color, int relativeIndex)
    {
        if (mainLoop == null || mainLoop.Count == 0) return -1;
        if (relativeIndex >= mainLoop.Count) return -1;
        return (GetStartIndex(color) + relativeIndex) % mainLoop.Count;
    }

    // The 4 starting tiles are the only safe zones in this version of the game
    public bool IsAbsoluteTileSafe(int absoluteIndex)
    {
        return absoluteIndex == RED_START_INDEX ||
               absoluteIndex == GREEN_START_INDEX ||
               absoluteIndex == YELLOW_START_INDEX ||
               absoluteIndex == BLUE_START_INDEX;
    }
}