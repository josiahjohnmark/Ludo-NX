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
        // Only the instance with ALL 4 home stretches populated (MainBoardPath)
        // should become the singleton.  The per-colour HomeStretch objects each
        // have only 1 of the 4 filled, so they must yield.
        bool iAmComplete = redHomeStretch.Count > 0
                        && greenHomeStretch.Count > 0
                        && yellowHomeStretch.Count > 0
                        && blueHomeStretch.Count > 0;

        if (Instance == null)
        {
            if (iAmComplete)
            {
                Instance = this;
            }
            else
            {
                // Partial instance loaded first — keep searching, don't destroy yet
                // (MainBoardPath hasn't loaded yet; it will claim the slot)
                Instance = this;  // temporary holder
            }
        }
        else if (iAmComplete && Instance != this)
        {
            // MainBoardPath arrived after a partial instance already claimed the slot
            Destroy(Instance.gameObject);
            Instance = this;
        }
        else if (!iAmComplete)
        {
            // A partial instance after MainBoardPath — just destroy it
            Destroy(gameObject);
        }
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