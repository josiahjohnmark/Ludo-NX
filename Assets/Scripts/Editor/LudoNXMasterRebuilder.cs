using System;
using UnityEditor;
using UnityEngine;

namespace LudoNX.Editor
{
    /// <summary>
    /// Master rebuilder for all modern Ludo NX UI screens, modals, HUD, and components.
    /// Runs all builders in sequence and re-saves the prefab files in Assets/Resources/UI/.
    /// </summary>
    public static class LudoNXMasterRebuilder
    {
        [MenuItem("Ludo NX/UI/⚡ REBUILD ALL SCREENS & PREFABS", priority = 0)]
        public static void RebuildAll()
        {
            Debug.Log("==================================================");
            Debug.Log("[Ludo NX] STARTING FULL UI & PREFAB REBUILD");
            Debug.Log("==================================================");

            SafeRun("Reset Career & Session Stats to Fresh Zero", () => LudoNXSession.ResetAllToFreshStart());
            SafeRun("Cancel Button Asset", () => LudoNXCancelButtonGenerator.EnsureAsset());
            SafeRun("Main Menu & Nav Bar", () => LudoNXMainMenuBuilder.Build());
            SafeRun("Shop Screen (Modern)", () => LudoNXStoreBuilder.Build());
            SafeRun("Profile Screen (Modern)", () => LudoNXProfileBuilder.Build());
            SafeRun("Match Intro (Modern)", () => LudoNXMatchIntroBuilder.Build());
            SafeRun("Menu Popup Modal", () => LudoNXMenuModalBuilder.Build());
            SafeRun("Match Setup Modals & Rules", () => LudoNXMatchModalsBuilder.Build());
            SafeRun("Match HUD (Snug Roll Button & Naked Emojis)", () => LudoNXMatchHudBuilder.Build());
            SafeRun("Match Trays (Emoji & Message)", () => LudoNXEmoteBuilder.BuildTrays());
            SafeRun("Levels Screen & Doors", () => LudoNXLevelsBuilder.Build());
            SafeRun("Friends Screen", () => LudoNXFriendsBuilder.Build());
            SafeRun("Wire Scene Audio Manager", () => WireSceneAudioManager());

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("==================================================");
            Debug.Log("[Ludo NX] ALL MODERN UI PREFABS REBUILT & SAVED");
            Debug.Log("==================================================");
        }

        private static void SafeRun(string name, Action action)
        {
            try
            {
                action();
                Debug.Log($"[Ludo NX Rebuilder] ✅ {name} built successfully.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Ludo NX Rebuilder] ❌ Error building {name}: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private static void WireSceneAudioManager()
        {
            var am = UnityEngine.Object.FindAnyObjectByType<AudioManager>();
            if (am != null)
            {
                Undo.RecordObject(am, "Wire Audio Clips");
                if (am.diceRollClip == null) am.diceRollClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/dice_roll.ogg");
                if (am.tokenHopClip == null) am.tokenHopClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/token_hop.ogg");
                if (am.tokenSpawnClip == null) am.tokenSpawnClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/dice_land.ogg");
                if (am.homeStretchHopClip == null) am.homeStretchHopClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/token_hop_home.ogg");
                if (am.tokenHomeClip == null) am.tokenHomeClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/token_home_chime.ogg");
                if (am.buttonClip == null) am.buttonClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/button_click.ogg");
                if (am.turnChangeClip == null) turnChangeClip(am);
                if (am.matchMusic == null) am.matchMusic = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Music/match_music.mp3");
                if (am.menuMusic == null) am.menuMusic = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Music/menu_music.mp3");
                if (am.victoryClip == null) am.victoryClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Music/victory_fanfare_RAW.mp3");
                if (am.tokenCaptureClip == null) am.tokenCaptureClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/token_capture.ogg");
                EditorUtility.SetDirty(am);
            }
        }

        private static void turnChangeClip(AudioManager am)
        {
            am.turnChangeClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/turn_change.ogg");
        }
    }
}
