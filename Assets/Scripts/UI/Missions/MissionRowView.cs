using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LudoNX.UI
{
    /// <summary>
    /// One mission card. A dumb holder: <see cref="DailyMissionsScreen"/> fills it in and listens
    /// to <see cref="claimButton"/>. Kept as a component on the template so the builder can wire
    /// the references once and the screen never has to walk the hierarchy at runtime.
    /// </summary>
    public class MissionRowView : MonoBehaviour
    {
        public RectTransform layout;
        public Image card;
        public Image iconPlate;
        public Image icon;
        public TextMeshProUGUI title;
        public TextMeshProUGUI progressLabel;
        public Image progressFill;
        public RectTransform progressTrack;
        public TextMeshProUGUI rewardLabel;
        public Image rewardIcon;
        public Button claimButton;
        public Image claimBackground;
        public TextMeshProUGUI claimLabel;
        public Image tick;

        [Tooltip("Card colour at rest. The bonus row is tinted so it reads as the prize, not a fourth mission.")]
        public Color baseTint = Color.white;

        [HideInInspector] public DailyMissionsData.Mission mission;

        /// <summary>Drives the fill width from 0..1 of the track.</summary>
        public void SetProgress(float t)
        {
            if (progressFill == null || progressTrack == null) return;
            var rt = (RectTransform)progressFill.transform;
            float full = progressTrack.sizeDelta.x;
            rt.sizeDelta = new Vector2(Mathf.Max(full * Mathf.Clamp01(t), t > 0f ? 14f : 0f), rt.sizeDelta.y);
            progressFill.gameObject.SetActive(t > 0f);
        }
    }
}
