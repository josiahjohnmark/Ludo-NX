using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LudoNX.UI
{
    /// <summary>
    /// One shop grid cell: the frame with corner dice, the item art, its name ribbon and its
    /// price pill (which doubles as the buy button, matching the user's reference card).
    ///
    /// Own file for the reason documented on <see cref="SeatRow"/> and <see cref="MultiplierTile"/> -
    /// a MonoBehaviour sharing a file with the screen it belongs to loses its script reference on
    /// import and silently vanishes from the built prefab.
    /// </summary>
    public class StoreItemCard : MonoBehaviour
    {
        public Image frame;
        public Image icon;
        public Image ribbon;
        public TextMeshProUGUI nameLabel;
        public Image pricePill;
        public TextMeshProUGUI priceLabel;
        public Button buyButton;
    }
}
