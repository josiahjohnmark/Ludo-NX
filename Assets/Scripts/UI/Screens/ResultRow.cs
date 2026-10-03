using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LudoNX.UI
{
    /// <summary>
    /// One line of the final placement table: where a house finished, who was driving it, and
    /// what they scored. The player's own line is marked, because the first thing anyone looks
    /// for on a result screen is themselves.
    ///
    /// Own file: Unity writes one script asset per file, so a MonoBehaviour sharing a file with
    /// another loses its script reference and vanishes from the prefab.
    /// </summary>
    public class ResultRow : MonoBehaviour
    {
        public Image card;
        public Image pawn;
        public TextMeshProUGUI place;
        public TextMeshProUGUI nameLabel;
        public TextMeshProUGUI scoreLabel;
        public Image highlight;

        public void SetHighlighted(bool on)
        {
            if (highlight != null) highlight.gameObject.SetActive(on);
            if (nameLabel != null) nameLabel.color = on ? new Color32(28, 22, 16, 255) : new Color32(78, 74, 65, 255);

            // Gold placement text as well as the glow: on a cream card the glow alone is too
            // quiet to answer "which one am I" at a glance.
            if (place != null)
                place.color = on ? new Color32(198, 140, 26, 255) : new Color32(150, 112, 74, 255);
        }
    }
}
