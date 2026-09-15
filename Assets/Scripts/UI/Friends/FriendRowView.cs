using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LudoNX.UI
{
    /// <summary>
    /// References to the parts of one Friends-screen row. The row is a 94pt tall layout slot
    /// holding the 80pt card, so collapsing the slot closes the gap to the next row smoothly.
    /// </summary>
    public class FriendRowView : MonoBehaviour
    {
        public enum Kind { Friend, Request, Player }

        [Header("Slot")]
        public LayoutElement layout;
        public CanvasGroup group;
        public RectTransform card;

        [Header("Identity")]
        public TextMeshProUGUI number;
        public TextMeshProUGUI displayName;
        public TextMeshProUGUI subtitle;
        public Image portrait;
        public Image statusDot;
        public Button avatarButton;

        [Header("Actions")]
        public Button moreButton;
        public Button acceptButton;
        public Button declineButton;
        public Button addButton;
        public TextMeshProUGUI addLabel;
        public CanvasGroup addGroup;
        public GameObject spinner;

        [HideInInspector] public Kind kind;
        [HideInInspector] public FriendPerson person;
        [HideInInspector] public bool busy;
    }
}
