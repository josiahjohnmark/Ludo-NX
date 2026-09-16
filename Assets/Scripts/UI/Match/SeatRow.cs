using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LudoNX.UI
{
    /// <summary>
    /// One house at the table: its colour, who is sitting there, and how they play.
    ///
    /// Lives in its own file on purpose. Unity creates one script asset per file, named after the
    /// file, so a second MonoBehaviour sharing a file is serialised with no script at all — the
    /// component silently disappears from any prefab built with it.
    /// </summary>
    public class SeatRow : MonoBehaviour
    {
        public Image card;
        public Image pawn;
        public TextMeshProUGUI houseLabel;
        public TextMeshProUGUI styleLabel;
        public TMP_InputField nameField;

        [HideInInspector] public int houseIndex;
    }
}
