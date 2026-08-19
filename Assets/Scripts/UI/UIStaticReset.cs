using UnityEngine;

namespace LudoNX.UI
{
    /// <summary>
    /// Clears every cached static before a play session starts.
    ///
    /// This project has Enter Play Mode Options on with domain reload disabled
    /// (<c>m_EnterPlayModeOptions: 1</c>), which makes entering play mode much faster but means
    /// statics keep whatever they held last time. Without this, a second play session in the
    /// editor starts with a stale <c>ScreenManager.Instance</c> pointing at a destroyed object,
    /// a navigation stack from the previous run, and cached theme and stats data.
    ///
    /// Symptom seen before this existed: the manager reported "active screen: Profile, depth 2"
    /// on a fresh run that was actually showing Home.
    ///
    /// Builds always start clean, so this is purely an editor-fidelity fix — but an editor that
    /// lies about state is worse than a slow one.
    /// </summary>
    internal static class UIStaticReset
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            ScreenManager.ResetStatics();
            MatchBridge.ResetStatics();
            UITheme.ResetStatics();
            UIFontSet.ResetStatics();
            UIIconSet.ResetStatics();
            UISkin.ResetStatics();
            ClayGraphic.ResetStatics();
            UIFactory.ResetStatics();
            UITween.ResetStatics();
            StatsService.ResetStatics();
            SettingsService.ResetStatics();
            AvatarService.ResetStatics();
        }
    }
}
