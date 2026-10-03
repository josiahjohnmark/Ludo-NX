using UnityEngine;
using UnityEngine.UI;

namespace LudoNX.UI
{
    /// <summary>
    /// The 8-slot emote loadout opened from the match action bar's emoji button.
    ///
    /// A tray, not a popup (match-emoji-tray.json): no scrim, no close X, the board keeps running
    /// underneath. Closes on firing an emote or on tapping the emoji button again — MatchHudView
    /// owns that toggle, since it owns the button. "Tap the board to close" from the spec is not
    /// implemented: doing that without blocking the board's own taps needs a raycast-transparent
    /// listener, which isn't a stock Unity UI pattern and isn't worth the risk of misclicking a
    /// token to get right blind, without a live Editor to check it in.
    /// </summary>
    public class MatchEmoteTray : UIModal
    {
        [Header("8 loadout slots, in fixed order")]
        public Button[] slotButtons;
        public Image[] slotCooldownOverlays;

        [Tooltip("Parallel to slotButtons — the flipbook sheet each slot fires.")]
        public Texture2D[] slotTextures;
        public int[] slotFrameCounts;
        public int[] slotContentSizes;
        public float[] slotFps;

        private MatchHudView _hud;
        private float _cooldownUntil;
        private const float CooldownSeconds = 3f;
        private const float BubbleHoldSeconds = 1.4f;

        public void Configure(MatchHudView hud)
        {
            _hud = hud;
            if (slotButtons == null) return;

            for (int i = 0; i < slotButtons.Length; i++)
            {
                if (slotButtons[i] == null) continue;
                int captured = i;
                slotButtons[i].onClick.RemoveAllListeners();
                slotButtons[i].onClick.AddListener(() => Fire(captured));
            }
        }

        private void Update()
        {
            if (slotCooldownOverlays == null) return;
            float remaining = Mathf.Max(0f, _cooldownUntil - Time.unscaledTime);
            bool cooling = remaining > 0f;
            float fill = cooling ? remaining / CooldownSeconds : 0f;

            for (int i = 0; i < slotCooldownOverlays.Length; i++)
            {
                var overlay = slotCooldownOverlays[i];
                if (overlay == null) continue;
                overlay.gameObject.SetActive(cooling);
                overlay.fillAmount = fill;
                if (slotButtons != null && i < slotButtons.Length && slotButtons[i] != null)
                    slotButtons[i].interactable = !cooling;
            }
        }

        private void Fire(int slot)
        {
            if (Time.unscaledTime < _cooldownUntil) return;
            if (_hud == null || slotTextures == null || slot >= slotTextures.Length) return;

            var sender = _hud.LocalSender();
            var card = _hud.CardFor(sender);
            card?.ShowEmoteBubble(slotTextures[slot], SafeAt(slotFrameCounts, slot, 8),
                SafeAt(slotContentSizes, slot, 96), SafeAt(slotFps, slot, 10f), BubbleHoldSeconds);

            _cooldownUntil = Time.unscaledTime + CooldownSeconds;
            Dismiss();
        }

        private static int SafeAt(int[] arr, int i, int fallback) => arr != null && i < arr.Length ? arr[i] : fallback;
        private static float SafeAt(float[] arr, int i, float fallback) => arr != null && i < arr.Length ? arr[i] : fallback;
    }
}
