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

    // Each colour walks 51 of the 52 main-loop tiles (it never re-visits its
    // own start tile).  After those 51 tiles it enters its 6-tile home stretch.
    private int MainTilesPerColor => mainLoop != null && mainLoop.Count > 0
        ? mainLoop.Count - 1   // 52 - 1 = 51
        : 0;

    private void Awake()
    {
        // Only the instance with the full main loop AND all 4 home stretches populated
        // (MainBoardPath) may ever become the singleton. Some per-colour HomeStretch
        // objects in the scene carry their own partial LudoPath component (only their
        // own list filled) — those must never claim Instance, and must never destroy
        // another GameObject, since Awake() order across objects isn't guaranteed and
        // destroying the wrong one would delete real waypoint Transforms other tokens
        // depend on.
        bool iAmComplete = mainLoop != null && mainLoop.Count > 0
                        && redHomeStretch.Count > 0
                        && greenHomeStretch.Count > 0
                        && yellowHomeStretch.Count > 0
                        && blueHomeStretch.Count > 0;

        if (!iAmComplete) return;

        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple complete LudoPath instances found in the scene — keeping the first one.", this);
            return;
        }

        Instance = this;
    }

    /// <summary>
    /// Fallback lookup for a complete LudoPath when Instance hasn't been set yet
    /// (e.g. this is queried from another object's Awake() before LudoPath's own
    /// Awake() has run). Unlike a plain FindAnyObjectByType&lt;LudoPath&gt;() call,
    /// this specifically filters out partial per-colour LudoPath components.
    /// </summary>
    public static LudoPath FindComplete()
    {
        if (Instance != null) return Instance;

        LudoPath[] all = Object.FindObjectsByType<LudoPath>(FindObjectsInactive.Exclude);
        foreach (LudoPath candidate in all)
        {
            if (candidate.mainLoop != null && candidate.mainLoop.Count > 0
                && candidate.redHomeStretch.Count > 0
                && candidate.greenHomeStretch.Count > 0
                && candidate.yellowHomeStretch.Count > 0
                && candidate.blueHomeStretch.Count > 0)
            {
                return candidate;
            }
        }

        return null;
    }

    public List<Transform> GetPathForColor(GameManager.PlayerColor color)
    {
        List<Transform> path = new List<Transform>();
        int startIndex = GetStartIndex(color);
        List<Transform> homeStretch = GetHomeStretch(color);

        if (mainLoop != null && mainLoop.Count > 0)
        {
            // Walk 51 main-loop tiles (relative 0 = start, relative 50 = last
            // tile before the home-stretch entrance).
            for (int i = 0; i < MainTilesPerColor; i++)
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
        int mainCount = MainTilesPerColor;  // 51
        List<Transform> homeStretch = GetHomeStretch(color);
        int homeCount = homeStretch != null ? homeStretch.Count : 0;
        return mainCount + homeCount;       // 51 + 6 = 57
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
        if (relativeIndex >= MainTilesPerColor) return -1;  // 51+ = home stretch
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