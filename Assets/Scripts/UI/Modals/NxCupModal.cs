using UnityEngine.UI;

namespace LudoNX.UI
{
    /// <summary>
    /// What the NX CUP badge on Home opens until tournaments exist. Tournaments need the online
    /// server (GO_LIVE_PLAN Phase D/E), so this says so plainly instead of showing the older
    /// Events screen, which advertised a live event that does not exist.
    /// </summary>
    public class NxCupModal : UIModal
    {
        public Button levelsButton;

        private void Awake()
        {
            if (levelsButton != null) levelsButton.onClick.AddListener(() =>
            {
                Dismiss();
                DoorTransition.Play(() => ScreenManager.Instance.Push(ScreenId.Levels));
            });
        }

        public static void Show() => Open("NxCup");
    }
}
