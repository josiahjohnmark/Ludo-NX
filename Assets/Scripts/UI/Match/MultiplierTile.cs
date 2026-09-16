using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LudoNX.UI
{
    /// <summary>
    /// One stake tile in the multiplier grid: the multiple, its locked state, and the glow that
    /// marks the chosen one.
    ///
    /// Its own file for the reason given in <see cref="SeatRow"/> — sharing a file cost this
    /// component its script reference once already, which showed up as an empty STAKE section.
    /// </summary>
    public class MultiplierTile : MonoBehaviour
    {
        public Button button;
        public Image background;
        public Image glow;
        public Image lockIcon;
        public TextMeshProUGUI label;

        [HideInInspector] public int multiplier = 1;
    }
}
