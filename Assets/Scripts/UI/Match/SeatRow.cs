using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LudoNX.UI
{
    /// <summary>
    /// One row in the HOUSES list: a seat number, who is sitting there, and that seat's house
    /// colour. Row 1 is always the player and is never editable; every other row can be renamed.
    ///
    /// Lives in its own file on purpose. Unity creates one script asset per file, named after the
    /// file, so a second MonoBehaviour sharing a file is serialised with no script at all — the
    /// component silently disappears from any prefab built with it.
    /// </summary>
    public class SeatRow : MonoBehaviour
    {
        public Image card;
        public Image pawn;
        public TextMeshProUGUI numberLabel;

        [Tooltip("CP / HM tag shown to the left of the row. VS Computer only.")]
        public TextMeshProUGUI roleTag;

        [Tooltip("\"TAP TO EDIT\" under the name. Hidden for the player's own, fixed row.")]
        public TextMeshProUGUI hintLabel;

        public TMP_InputField nameField;

        [Tooltip("Plain label used instead of nameField for the player's own row.")]
        public TextMeshProUGUI nameLabel;

        [HideInInspector] public int houseIndex;
    }
}
