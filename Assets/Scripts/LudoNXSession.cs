using UnityEngine;

/// <summary>
/// The player's account state, and the configuration of the match that is about to start.
///
/// This is the seam between the menu and the board. The menu writes <see cref="pendingPlayerCount"/>
/// and <see cref="pendingDifficulty"/>, then asks GameManager to initialise; the match writes coins
/// and NX Points back when it finishes. Nothing else needs to know how the player got into a match.
///
/// Only the offline path exists right now. When online modes arrive, they set the same pending
/// fields and the rest of the flow is unchanged.
///
/// Persistence is PlayerPrefs. It is enough for a single local profile and costs nothing; a real
/// account service replaces <see cref="Load"/>/<see cref="Save"/> without touching anything else.
/// </summary>
public class LudoNXSession : MonoBehaviour
{
    public static LudoNXSession Instance { get; private set; }

    public enum Mode
    {
        OfflineQuick,   // Quick Play — one human against AI houses
        PassAndPlay,    // Play with Friends — every house is a human on this device
        OnlineQuick,    // reserved — locked
        Ranked,         // reserved — locked
        PrivateRoom     // reserved — locked
    }

    /// <summary>
    /// Which diagonal plays a two-player match. Diagonals rather than neighbours because opposite
    /// corners give both houses the same distance home — adjacent corners do not, and one player
    /// would start with a real advantage.
    /// </summary>
    public enum TwoPlayerSide
    {
        RedYellow,
        GreenBlue
    }

    // ── Config for the match about to start ────────────────────────────────────────────────
    [Header("Pending Match")]
    public Mode pendingMode = Mode.OfflineQuick;
    public int pendingPlayerCount = 4;
    public TwoPlayerSide pendingSide = TwoPlayerSide.RedYellow;
    public LudoAI.Difficulty pendingDifficulty = LudoAI.Difficulty.Normal;

    /// <summary>
    /// Safe Zone ruleset rather than Classic. Set by the Quick Play Setup screen.
    ///
    /// NOTE: the setup screen writes this, but no gameplay code reads it yet — the board still
    /// plays Classic rules either way. Wiring it up means honouring it in the capture check,
    /// which is gameplay work, not UI work.
    /// </summary>
    public bool pendingSafeZone;

    /// <summary>The house the player chose to control. Ignored in pass-and-play.</summary>
    public GameManager.PlayerColor pendingHumanColor = GameManager.PlayerColor.RED;

    /// <summary>
    /// The houses for the pending match, in turn order, with the player's chosen colour first.
    ///
    /// GameManager treats entry zero as the human, so rotating the list to start on the chosen
    /// house is what actually hands the player red, green, yellow or blue. Board order is
    /// otherwise preserved, because turn order around a Ludo board is fixed by the layout.
    /// </summary>
    public System.Collections.Generic.List<GameManager.PlayerColor> BuildPendingPlayers()
    {
        var list = new System.Collections.Generic.List<GameManager.PlayerColor>();

        if (pendingPlayerCount >= 4)
        {
            list.Add(GameManager.PlayerColor.RED);
            list.Add(GameManager.PlayerColor.GREEN);
            list.Add(GameManager.PlayerColor.YELLOW);
            list.Add(GameManager.PlayerColor.BLUE);
        }
        else
        {
            // Two-player is always played across a diagonal: opposite corners are the same
            // distance home, adjacent ones are not and hand one side a real advantage.
            bool greenBlue = pendingHumanColor == GameManager.PlayerColor.GREEN
                          || pendingHumanColor == GameManager.PlayerColor.BLUE;

            if (greenBlue)
            {
                list.Add(GameManager.PlayerColor.GREEN);
                list.Add(GameManager.PlayerColor.BLUE);
            }
            else
            {
                list.Add(GameManager.PlayerColor.RED);
                list.Add(GameManager.PlayerColor.YELLOW);
            }
        }

        // Rotate so the chosen house leads without disturbing the board's turn order.
        int start = list.IndexOf(pendingHumanColor);
        if (start > 0)
        {
            var rotated = new System.Collections.Generic.List<GameManager.PlayerColor>();
            for (int i = 0; i < list.Count; i++) rotated.Add(list[(start + i) % list.Count]);
            list = rotated;
        }

        return list;
    }

    /// <summary>Every house is a human when playing pass-and-play on one device.</summary>
    public bool AllHousesHuman => pendingMode == Mode.PassAndPlay;

    // ── Profile ────────────────────────────────────────────────────────────────────────────
    [Header("Profile (read-only at runtime)")]
    [SerializeField] private string playerName = "Player";
    [SerializeField] private int coins;
    [SerializeField] private int nxPoints;
    [SerializeField] private int matchesPlayed;
    [SerializeField] private int matchesWon;

    public string PlayerName => playerName;
    public int Coins => coins;
    public int NXPoints => nxPoints;
    public int MatchesPlayed => matchesPlayed;
    public int MatchesWon => matchesWon;

    // ── Reward table ───────────────────────────────────────────────────────────────────────
    // Offline pays a fraction of what online will, so an AI farm can never out-earn real play.
    // These are the numbers to tune first once matches are actually being played.
    [Header("Offline Rewards")]
    public int coinsPerWin = 120;
    public int coinsPerLoss = 30;
    public int nxPerMatchPoint = 1;
    [Range(0f, 1f)] public float offlineRewardScale = 0.5f;

    private const string KeyName = "ludonx_name";
    private const string KeyCoins = "ludonx_coins";
    private const string KeyNX = "ludonx_nx";
    private const string KeyPlayed = "ludonx_played";
    private const string KeyWon = "ludonx_won";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        Load();
    }

    // ── Level ──────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// NX Level. Costs rise linearly (level N costs N × 100), so early levels arrive quickly and
    /// later ones take real play — the shape that keeps a new player feeling progress on day one.
    /// </summary>
    public int Level
    {
        get
        {
            int level = 1;
            int spent = 0;
            while (spent + level * 100 <= nxPoints && level < 999)
            {
                spent += level * 100;
                level++;
            }
            return level;
        }
    }

    /// <summary>How far through the current level, 0–1. Drives the profile progress bar.</summary>
    public float LevelProgress01
    {
        get
        {
            int level = 1;
            int spent = 0;
            while (spent + level * 100 <= nxPoints && level < 999)
            {
                spent += level * 100;
                level++;
            }
            int cost = level * 100;
            return cost <= 0 ? 0f : Mathf.Clamp01((nxPoints - spent) / (float)cost);
        }
    }

    public float WinRate => matchesPlayed == 0 ? 0f : matchesWon / (float)matchesPlayed;

    // ── Match results ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Banks the outcome of a finished match. <paramref name="matchPoints"/> is the player's
    /// PointsManager score, so captures and home arrivals both feed NX — a player who fought well
    /// but placed third still earns, which is what stops a loss from feeling like wasted time.
    /// </summary>
    public void RecordMatchResult(bool won, int matchPoints, bool wasOffline)
    {
        float scale = wasOffline ? offlineRewardScale : 1f;

        int coinsEarned = Mathf.RoundToInt((won ? coinsPerWin : coinsPerLoss) * scale);
        int nxEarned = Mathf.RoundToInt(Mathf.Max(0, matchPoints) * nxPerMatchPoint * scale);

        coins += coinsEarned;
        nxPoints += nxEarned;
        matchesPlayed++;
        if (won) matchesWon++;

        Save();

        LastCoinsEarned = coinsEarned;
        LastNXEarned = nxEarned;
    }

    /// <summary>Set by the most recent <see cref="RecordMatchResult"/> so the result screen can show it.</summary>
    public int LastCoinsEarned { get; private set; }
    public int LastNXEarned { get; private set; }

    public bool TrySpendCoins(int amount)
    {
        if (amount <= 0 || coins < amount) return false;
        coins -= amount;
        Save();
        return true;
    }

    public void SetPlayerName(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        playerName = value.Trim();
        Save();
    }

    // ── Persistence ────────────────────────────────────────────────────────────────────────

    private void Load()
    {
        playerName = PlayerPrefs.GetString(KeyName, "Player");
        coins = PlayerPrefs.GetInt(KeyCoins, 500);   // a small starting float so the shop isn't empty on day one
        nxPoints = PlayerPrefs.GetInt(KeyNX, 0);
        matchesPlayed = PlayerPrefs.GetInt(KeyPlayed, 0);
        matchesWon = PlayerPrefs.GetInt(KeyWon, 0);
    }

    private void Save()
    {
        PlayerPrefs.SetString(KeyName, playerName);
        PlayerPrefs.SetInt(KeyCoins, coins);
        PlayerPrefs.SetInt(KeyNX, nxPoints);
        PlayerPrefs.SetInt(KeyPlayed, matchesPlayed);
        PlayerPrefs.SetInt(KeyWon, matchesWon);
        PlayerPrefs.Save();
    }

    /// <summary>Wipes the local profile. Exposed for the settings screen and for testing.</summary>
    public void ResetProfile()
    {
        PlayerPrefs.DeleteKey(KeyName);
        PlayerPrefs.DeleteKey(KeyCoins);
        PlayerPrefs.DeleteKey(KeyNX);
        PlayerPrefs.DeleteKey(KeyPlayed);
        PlayerPrefs.DeleteKey(KeyWon);
        Load();
    }
}
