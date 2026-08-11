using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Centralized match points tracking and stats for Ludo NX.
/// Points are per-match and feed into weekly leaderboard rankings.
/// </summary>
public class PointsManager : MonoBehaviour
{
    public static PointsManager Instance { get; private set; }

    // --- Point Values (tunable in Inspector) ---
    [Header("Point Values")]
    public int capturePoints = 3;
    public int homeArrivalPoints = 5;
    public int capturedPenalty = 1;   // Deducted when YOUR token is captured
    public int doublesBonus = 1;
    public int winnerBonus = 10;
    public int secondPlaceBonus = 5;
    public int thirdPlaceBonus = 2;
    public int fourthPlaceBonus = 0;

    // --- Per-Player Match Data ---
    private Dictionary<GameManager.PlayerColor, PlayerMatchStats> playerStats =
        new Dictionary<GameManager.PlayerColor, PlayerMatchStats>();

    [System.Serializable]
    public class PlayerMatchStats
    {
        public int points = 0;
        public int captures = 0;
        public int tokensHome = 0;
        public int timesCaptured = 0;
        public int doublesRolled = 0;
        public int sixesRolled = 0;
        public int fireStreakCurrent = 0;  // Current consecutive captures without being captured
        public int fireStreakMax = 0;      // Highest fire streak this match

        public void Reset()
        {
            points = 0;
            captures = 0;
            tokensHome = 0;
            timesCaptured = 0;
            doublesRolled = 0;
            sixesRolled = 0;
            fireStreakCurrent = 0;
            fireStreakMax = 0;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Initialize/reset stats for all active players at match start.
    /// </summary>
    public void InitializeForMatch(List<GameManager.PlayerColor> players)
    {
        playerStats.Clear();
        foreach (var color in players)
        {
            playerStats[color] = new PlayerMatchStats();
        }
        Debug.Log($"[PointsManager] Initialized stats for {players.Count} players.");
    }

    // ===========================
    // POINT AWARD/DEDUCTION METHODS
    // ===========================

    /// <summary>
    /// Called when a player captures an opponent's token.
    /// Awards +capturePoints to the capturer.
    /// </summary>
    public void OnTokenCaptured(GameManager.PlayerColor capturer, GameManager.PlayerColor victim)
    {
        if (!playerStats.ContainsKey(capturer) || !playerStats.ContainsKey(victim)) return;

        // Award points to capturer
        playerStats[capturer].points += capturePoints;
        playerStats[capturer].captures++;
        playerStats[capturer].fireStreakCurrent++;
        if (playerStats[capturer].fireStreakCurrent > playerStats[capturer].fireStreakMax)
        {
            playerStats[capturer].fireStreakMax = playerStats[capturer].fireStreakCurrent;
        }

        // Deduct points from victim
        playerStats[victim].points -= capturedPenalty;
        playerStats[victim].timesCaptured++;
        playerStats[victim].fireStreakCurrent = 0; // Break their fire streak

        Debug.Log($"[PointsManager] {capturer} captured {victim}'s token! " +
                  $"{capturer}: {playerStats[capturer].points} PTS (streak: {playerStats[capturer].fireStreakCurrent}), " +
                  $"{victim}: {playerStats[victim].points} PTS");

        // Notify UI
        if (UIManager.Instance != null)
        {
            UIManager.Instance.OnPointsChanged(capturer, playerStats[capturer].points, capturePoints);
            UIManager.Instance.OnPointsChanged(victim, playerStats[victim].points, -capturedPenalty);
        }
    }

    /// <summary>
    /// Called when a player's token reaches home.
    /// Awards +homeArrivalPoints.
    /// </summary>
    public void OnTokenReachedHome(GameManager.PlayerColor color)
    {
        if (!playerStats.ContainsKey(color)) return;

        playerStats[color].points += homeArrivalPoints;
        playerStats[color].tokensHome++;

        Debug.Log($"[PointsManager] {color} token reached home! +{homeArrivalPoints} PTS. " +
                  $"Total: {playerStats[color].points} PTS, Homes: {playerStats[color].tokensHome}/4");

        if (UIManager.Instance != null)
        {
            UIManager.Instance.OnPointsChanged(color, playerStats[color].points, homeArrivalPoints);
        }
    }

    /// <summary>
    /// Called when a player rolls doubles.
    /// Awards +doublesBonus.
    /// </summary>
    public void OnDoublesRolled(GameManager.PlayerColor color)
    {
        if (!playerStats.ContainsKey(color)) return;

        playerStats[color].points += doublesBonus;
        playerStats[color].doublesRolled++;

        Debug.Log($"[PointsManager] {color} rolled doubles! +{doublesBonus} PT. Total: {playerStats[color].points} PTS");

        if (UIManager.Instance != null)
        {
            UIManager.Instance.OnPointsChanged(color, playerStats[color].points, doublesBonus);
        }
    }

    /// <summary>
    /// Called when a player rolls a 6 (for "luckiest roller" stat).
    /// </summary>
    public void OnSixRolled(GameManager.PlayerColor color)
    {
        if (!playerStats.ContainsKey(color)) return;
        playerStats[color].sixesRolled++;
    }

    /// <summary>
    /// Award placement bonuses at the end of the match.
    /// </summary>
    public void AwardPlacementBonuses(List<GameManager.PlayerColor> leaderBoard)
    {
        for (int i = 0; i < leaderBoard.Count; i++)
        {
            var color = leaderBoard[i];
            if (!playerStats.ContainsKey(color)) continue;

            int bonus = i switch
            {
                0 => winnerBonus,
                1 => secondPlaceBonus,
                2 => thirdPlaceBonus,
                _ => fourthPlaceBonus
            };

            playerStats[color].points += bonus;
            Debug.Log($"[PointsManager] {color} placed #{i + 1}, awarded +{bonus} PTS. Final: {playerStats[color].points} PTS");
        }
    }

    // ===========================
    // QUERY METHODS
    // ===========================

    public int GetPoints(GameManager.PlayerColor color)
    {
        return playerStats.ContainsKey(color) ? playerStats[color].points : 0;
    }

    public PlayerMatchStats GetStats(GameManager.PlayerColor color)
    {
        return playerStats.ContainsKey(color) ? playerStats[color] : null;
    }

    public int GetFireStreak(GameManager.PlayerColor color)
    {
        return playerStats.ContainsKey(color) ? playerStats[color].fireStreakCurrent : 0;
    }

    public int GetTokensHome(GameManager.PlayerColor color)
    {
        return playerStats.ContainsKey(color) ? playerStats[color].tokensHome : 0;
    }

    /// <summary>
    /// Determines the MVP (highest points EXCLUDING placement bonus).
    /// Call this BEFORE AwardPlacementBonuses for accurate MVP.
    /// </summary>
    public GameManager.PlayerColor DetermineMVP()
    {
        GameManager.PlayerColor mvp = GameManager.PlayerColor.RED;
        int highestPoints = int.MinValue;

        foreach (var kvp in playerStats)
        {
            if (kvp.Value.points > highestPoints)
            {
                highestPoints = kvp.Value.points;
                mvp = kvp.Key;
            }
        }

        Debug.Log($"[PointsManager] MVP: {mvp} with {highestPoints} PTS (before placement bonuses)");
        return mvp;
    }

    /// <summary>
    /// Finds the player who rolled the most 6s ("Luckiest Roller").
    /// </summary>
    public GameManager.PlayerColor GetLuckiestRoller()
    {
        GameManager.PlayerColor luckiest = GameManager.PlayerColor.RED;
        int mostSixes = -1;

        foreach (var kvp in playerStats)
        {
            if (kvp.Value.sixesRolled > mostSixes)
            {
                mostSixes = kvp.Value.sixesRolled;
                luckiest = kvp.Key;
            }
        }

        return luckiest;
    }

    /// <summary>
    /// Returns match points clamped to 0 for leaderboard contribution.
    /// Negative match scores contribute 0 to the weekly leaderboard.
    /// If the player already has a weekly score, negative match points
    /// WILL subtract from it (handled by the leaderboard system, not here).
    /// </summary>
    public int GetLeaderboardContribution(GameManager.PlayerColor color)
    {
        int pts = GetPoints(color);
        return Mathf.Max(0, pts);
    }
}
