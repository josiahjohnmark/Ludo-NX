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

    [MenuItem(Menu + "Daily Missions", priority = 60)]
    public static void OpenDailyMissions() => Open(ScreenId.DailyMissions, null);

    [MenuItem(Menu + "Match Setup - Quick Match", priority = 61)]
    public static void OpenQuickMatch() => Open(ScreenId.MatchSetup, MatchSetupScreen.Flavour.QuickMatch);

    [MenuItem(Menu + "Match Setup - Vs Computer", priority = 62)]
    public static void OpenVsComputer() => Open(ScreenId.MatchSetup, MatchSetupScreen.Flavour.VsComputer);

    [MenuItem(Menu + "Match Setup - Pass and Play", priority = 63)]
    public static void OpenPassAndPlay() => Open(ScreenId.MatchSetup, MatchSetupScreen.Flavour.PassAndPlay);

    [MenuItem(Menu + "Match Found (searching)", priority = 64)]
    public static void OpenMatchFound() => Open(ScreenId.MatchFound, 100);

    [MenuItem(Menu + "Home", priority = 65)]
    public static void OpenHome() => Open(ScreenId.Home, null);

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
}
