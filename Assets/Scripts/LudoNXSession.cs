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
        PrivateRoom,    // reserved — locked
        QuickMatch      // 1v1 over four houses; the opponent is a bot until there is a server
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

    // ── Stakes and seats, written by the Match Setup screen ────────────────────────────────

    /// <summary>
    /// What the match is worth. Multiplies the NX Points won or lost, never the energy price —
    /// see <see cref="LudoNX.UI.MultiplierService"/> for why, and for the capacity ladder that
    /// decides which multipliers this account may stake.
    /// </summary>
    public int pendingMultiplier = 1;

    /// <summary>
    /// Two controllers, four houses: each side plays a diagonal, so one player drives red and
    /// yellow while the other drives green and blue. Diagonals rather than neighbours because
    /// opposite corners are the same distance home.
    /// </summary>
    public bool pendingTeamPlay;

    /// <summary>Display name of the other side in a quick match; empty offline.</summary>
    public string pendingOpponentName = "";

    /// <summary>
    /// Names for the houses that are not the player's, in board order. Pass-and-play fills this
    /// with the friends sharing the phone; vs-computer fills it with the chosen bot names.
    /// </summary>
    public System.Collections.Generic.List<string> pendingSeatNames = new System.Collections.Generic.List<string>();

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

        if (pendingPlayerCount >= 4 || pendingTeamPlay || (pendingMode == Mode.QuickMatch && pendingPlayerCount == 2))
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
    public int PlayerLevel => Mathf.Max(1, 1 + nxPoints / 250);
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
    /// Player level. Driven by cumulative NX points across 10 prestige badges.
    /// See <see cref="LudoNX.UI.NXLevelService"/>.
    /// </summary>
    public int Level => LudoNX.UI.NXLevelService.GetLevel(nxPoints);

    /// <summary>How far through the current level, 0–1 (progress to next badge).</summary>
    public float LevelProgress01 => LudoNX.UI.NXLevelService.GetProgressToNextLevel(nxPoints);

    public float WinRate => matchesPlayed == 0 ? 0f : matchesWon / (float)matchesPlayed;

    // ── Profile identity stubs ─────────────────────────────────────────────────────────────
    // These are placeholders for a future account service. The values are derived from what
    // is already persisted so the Profile and Stats screens compile and display something
    // sensible in offline play. Swap out the getters when a backend exists.

    /// <summary>Six-digit display ID derived from name hash. Stable for a given save.</summary>
    public string PlayerId
    {
        get
        {
            int hash = playerName.GetHashCode();
            return Mathf.Abs(hash % 1000000).ToString("D6");
        }
    }

    /// <summary>Rank title based on level bracket.</summary>
    public string RankTitle
    {
        get
        {
            int lv = Level;
            if (lv < 5)  return "BRONZE I";
            if (lv < 10) return "BRONZE II";
            if (lv < 20) return "SILVER I";
            if (lv < 35) return "SILVER II";
            if (lv < 50) return "GOLD I";
            if (lv < 70) return "GOLD II";
            if (lv < 90) return "PLATINUM";
            return "DIAMOND";
        }
    }

    /// <summary>League display name. Will be driven by matchmaking tier.</summary>
    public string LeagueName => "OPEN LEAGUE";

    /// <summary>Country code. Will be pulled from the account service.</summary>
    public string Country => "—";

    /// <summary>Join date. Will be pulled from the account service.</summary>
    public string JoinDate => "—";

    /// <summary>Current XP within this level (equals nxPoints for simplicity until levels are tuned).</summary>
    public int Xp => LudoNX.UI.MissionService.DoneCount;

    /// <summary>XP needed to reach next level.</summary>
    public int XpToNextLevel
    {
        get
        {
            return 5;
        }
    }

    /// <summary>Progress through current level, 0–1. Alias for LevelProgress01.</summary>
    public float XpFraction => LevelProgress01;



    /// <summary>
    /// Banks the outcome of a finished match. <paramref name="matchPoints"/> is the player's
    /// PointsManager score, so captures and home arrivals both feed NX — a player who fought well
    /// but placed third still earns, which is what stops a loss from feeling like wasted time.
    /// </summary>
    /// <param name="multiplier">
    /// The stake chosen at setup. A win pays the match's points multiplied by it; a loss gives
    /// back half of what the same stake would have won, so the ladder rewards nerve without
    /// making a bad night unrecoverable. NX Points never fall below zero — a leaderboard that can
    /// punish a new player into negative numbers only teaches them to stop playing.
    /// </param>
    public void RecordMatchResult(bool won, int matchPoints, bool wasOffline, int multiplier = 1)
    {
        float scale = wasOffline ? offlineRewardScale : 1f;
        multiplier = Mathf.Max(1, multiplier);

        int coinsEarned = Mathf.RoundToInt((won ? coinsPerWin : coinsPerLoss) * scale);
        int basePoints = Mathf.RoundToInt(Mathf.Max(0, matchPoints) * nxPerMatchPoint * scale);

        int nxEarned = won
            ? basePoints * multiplier
            : -Mathf.RoundToInt(basePoints * multiplier * 0.5f);

        coins += coinsEarned;
        nxPoints = Mathf.Max(0, nxPoints + nxEarned);
        matchesPlayed++;
        if (won) matchesWon++;

        Save();

        LastCoinsEarned = coinsEarned;
        LastNXEarned = nxEarned;
    }

    /// <summary>Set by the most recent <see cref="RecordMatchResult"/> so the result screen can show it.</summary>
    public int LastCoinsEarned { get; private set; }
    public int LastNXEarned { get; private set; }

    /// <summary>
    /// Credits a reward that did not come from a match — a claimed mission, a daily reward.
    /// Separate from <see cref="RecordMatchResult"/> because it must not touch the played/won
    /// record: claiming a mission is not a match, and counting it as one would corrupt the
    /// win rate shown on the profile.
    /// </summary>
    public void AddRewards(int coinsEarned, int nxEarned)
    {
        if (coinsEarned <= 0 && nxEarned <= 0) return;
        coins += Mathf.Max(0, coinsEarned);
        nxPoints += Mathf.Max(0, nxEarned);
        Save();
    }

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

    private const string KeyFreshStart = "ludonx_fresh_start_v6";

    public static void ResetAllToFreshStart()
    {
        PlayerPrefs.SetInt(KeyFreshStart, 1);
        PlayerPrefs.SetInt(KeyCoins, 0);
        PlayerPrefs.SetInt(KeyNX, 0);
        PlayerPrefs.SetInt(KeyPlayed, 0);
        PlayerPrefs.SetInt(KeyWon, 0);
        PlayerPrefs.DeleteKey("ludonx_career");
        PlayerPrefs.DeleteKey("ludonx_joined");
        PlayerPrefs.SetInt("ludonx_energy_count", 50);
        PlayerPrefs.Save();
        LudoNX.UI.StatsService.ResetStatics();
        LudoNX.UI.EnergyService.ResetStatics();
        if (Instance != null)
        {
            Instance.coins = 0;
            Instance.nxPoints = 0;
            Instance.matchesPlayed = 0;
            Instance.matchesWon = 0;
            Instance.Load();
        }
    }

    private void CheckFreshStart()
    {
        if (PlayerPrefs.GetInt(KeyFreshStart, 0) == 0)
        {
            ResetAllToFreshStart();
        }
    }

    private void Load()
    {
        CheckFreshStart();
        playerName = PlayerPrefs.GetString(KeyName, "Player");
        coins = PlayerPrefs.GetInt(KeyCoins, 0);   // fresh starting points: 0
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
