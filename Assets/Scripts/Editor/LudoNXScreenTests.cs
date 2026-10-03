using LudoNX.UI;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Play-mode helpers: open a screen the way the game would, without hunting for the button.
///
/// These drive the real <see cref="ScreenManager"/>, so what appears is exactly what a player
/// gets — the point is to check the built screen, not a mock-up of it.
/// </summary>
public static class LudoNXScreenTests
{
    private const string Menu = "Ludo NX/UI/Screen Test/";

    /// <summary>
    /// Fills career stats with the numbers from the profile reference, so the built screen can
    /// be checked against it with real (if fake) data instead of all zeros. Temporary QA aid —
    /// safe to re-run any time since it fully overwrites the record rather than adding to it.
    /// </summary>
    [MenuItem(Menu + "Reset Career Stats to Zero", priority = 58)]
    public static void ResetCareerStatsToZero()
    {
        StatsService.Reset();
        LudoNXSession.ResetAllToFreshStart();
        Debug.Log("[Ludo NX] Career statistics and session reset to fresh zero.");
    }

    [MenuItem(Menu + "Seed Reference Career Stats (Debug Only)", priority = 59)]
    public static void SeedReferenceStats()
    {
        StatsService.Reset();
        for (int i = 0; i < 880; i++)
            StatsService.RecordMatch(1, 4, 40, 0, 0, 4, false, 0);
        for (int i = 0; i < 104; i++)
            StatsService.RecordMatch(2, 4, 10, 0, 0, 0, false, 0);
        var d = StatsService.Data;
        d.captures = 97;
        d.timesCaptured = 64;
        d.currentStreak = 15;
        d.bestStreak = 22;
        d.tokensHome = 3816;
        d.mostCapturesInGame = 11;
        d.mostTokensHomeInGame = 4;
        d.nxEarned = 48620;
        d.bestNxInGame = 214;
        d.tournamentWins = 8;
        StatsService.Save();
        Debug.Log("[Ludo NX] Seeded reference career stats for profile QA.");
    }

    [MenuItem(Menu + "Daily Missions", priority = 60)]
    public static void OpenDailyMissions() => Open(ScreenId.DailyMissions, null);

    [MenuItem(Menu + "Match Setup - Quick Match (Online)", priority = 61)]
    public static void OpenQuickMatch() => OpenModal(() => OnlineMatchModal.Open());

    [MenuItem(Menu + "Match Setup - Vs Computer", priority = 62)]
    public static void OpenVsComputer() => OpenModal(() => MatchSetupModal.OpenFor(MatchSetupModal.Flavour.VsComputer));

    [MenuItem(Menu + "Match Setup - Pass and Play", priority = 63)]
    public static void OpenPassAndPlay() => OpenModal(() => MatchSetupModal.OpenFor(MatchSetupModal.Flavour.PassAndPlay));

    [MenuItem(Menu + "Match Found (searching)", priority = 64)]
    public static void OpenMatchFound() => Open(ScreenId.MatchFound, 100);

    [MenuItem(Menu + "Match HUD", priority = 58)]
    public static void OpenMatchHud()
    {
        if (MatchBridge.Instance != null)
        {
            MatchBridge.Instance.StartPendingMatch();
        }
        else
        {
            var prefab = Resources.Load<GameObject>("UI/Components/MatchHud");
            if (prefab != null)
            {
                var go = Object.Instantiate(prefab);
                go.name = "MatchHud";
                var view = go.GetComponent<MatchHudView>();
                if (view != null) view.Bind();
            }
        }
    }

    [MenuItem(Menu + "Home", priority = 65)]
    public static void OpenHome() => Open(ScreenId.Home, null);

    [MenuItem(Menu + "Profile", priority = 66)]
    public static void OpenProfile() => Open(ScreenId.Profile, null);

    [MenuItem(Menu + "Shop", priority = 69)]
    public static void OpenShop() => Open(ScreenId.Shop, null);

    /// <summary>Shows the result screen as if the player had just won a four-house match.</summary>
    [MenuItem(Menu + "Result - won", priority = 67)]
    public static void OpenResultWon() => OpenResult(true);

    [MenuItem(Menu + "Result - lost", priority = 68)]
    public static void OpenResultLost() => OpenResult(false);

    [MenuItem(Menu + "Rules", priority = 70)]
    public static void OpenRules() => OpenModal(() => RulesModal.Open());

    [MenuItem(Menu + "Menu Modal", priority = 71)]
    public static void OpenMenuModal() => OpenModal(() => UIModal.Open("Menu"));

    private static void OpenResult(bool won)
    {
        var gm = GameManager.Instance;
        var me = gm != null ? gm.humanColor : GameManager.PlayerColor.RED;

        var board = new System.Collections.Generic.List<GameManager.PlayerColor>
        {
            GameManager.PlayerColor.RED,
            GameManager.PlayerColor.GREEN,
            GameManager.PlayerColor.YELLOW,
            GameManager.PlayerColor.BLUE,
        };

        // Put the player first or third so both headline states can be checked.
        board.Remove(me);
        board.Insert(won ? 0 : 2, me);

        Open(ScreenId.Result, board);
    }

    /// <summary>Raises the energy ceiling so the locked multiplier tiles can be checked.</summary>
    [MenuItem(Menu + "Set energy capacity 2000 (unlocks x100)", priority = 80)]
    public static void RaiseCapacity()
    {
        EnergyService.Rules.max = 2000;
        Debug.Log("[Ludo NX] Energy capacity set to 2000 for this session — ceiling is now x100.");
    }

    [MenuItem(Menu + "Reset energy capacity to 20", priority = 81)]
    public static void ResetCapacity()
    {
        EnergyService.Rules.max = 20;
        Debug.Log("[Ludo NX] Energy capacity back to 20 — ceiling is x5.");
    }

    /// <summary>Clears today's mission progress so the empty and claimable states can be seen.</summary>
    [MenuItem(Menu + "Complete all daily missions", priority = 82)]
    public static void CompleteMissions()
    {
        foreach (var m in DailyMissionsData.Today())
        {
            PlayerPrefs.SetInt("ludonx_mission_p_" + m.id, m.target);
        }
        PlayerPrefs.Save();
        Debug.Log("[Ludo NX] All three missions marked complete — every CLAIM is now live.");
    }

    private static void Open(ScreenId id, object args)
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[Ludo NX] Enter play mode first — screens live on a runtime canvas.");
            return;
        }
        var manager = ScreenManager.Instance;
        if (manager == null)
        {
            Debug.LogError("[Ludo NX] No ScreenManager in the scene.");
            return;
        }
        manager.Push(id, args);
    }

    /// <summary>
    /// Modals sit over Home, not on the screen stack — open Home first (if nothing better is
    /// already showing), then instantiate the modal on top of it.
    /// </summary>
    private static void OpenModal(System.Action open)
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[Ludo NX] Enter play mode first — modals live on a runtime canvas.");
            return;
        }
        var manager = ScreenManager.Instance;
        if (manager == null)
        {
            Debug.LogError("[Ludo NX] No ScreenManager in the scene.");
            return;
        }
        if (manager.Depth == 0) manager.Push(ScreenId.Home, null);
        open();
    }
}
