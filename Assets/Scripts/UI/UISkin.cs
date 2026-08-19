using UnityEngine;

namespace LudoNX.UI
{
    /// <summary>
    /// The sprite set for one visual theme.
    ///
    /// Every surface in the game asks this asset for its skin by role rather than by filename.
    /// A theme swap is therefore one field on <see cref="UITheme"/> pointing at a different
    /// UISkin — no prefab edits, no code changes. Leaving a role empty is legal and falls back
    /// to the procedural clay shader for that surface, so a half-finished theme still runs.
    /// </summary>
    [CreateAssetMenu(fileName = "UISkin", menuName = "Ludo NX/UI Skin", order = 3)]
    public class UISkin : ScriptableObject
    {
        private static UISkin _current;

        internal static void ResetStatics() { _current = null; }

        /// <summary>The active skin, or null to draw everything procedurally.</summary>
        public static UISkin Current
        {
            get
            {
                if (_current == null) _current = Resources.Load<UISkin>("UISkin");
                return _current;
            }
        }

        [Header("Buttons")]
        public Sprite buttonPill;
        public Sprite buttonCard;
        public Sprite buttonIconSquare;
        public Sprite buttonIconCircle;

        [Header("Panels")]
        public Sprite panelCard;
        public Sprite panelModal;
        public Sprite panelTile;
        public Sprite cardPlayer;

        [Header("Tracks and controls")]
        public Sprite trackPill;
        public Sprite trackWell;
        public Sprite thumbSegment;
        public Sprite toggleTrack;
        public Sprite toggleKnob;
        public Sprite chipPill;

        [Header("Avatars and pips")]
        public Sprite avatarRing;
        public Sprite avatarSlot;
        public Sprite pipEmpty;
        public Sprite pipFilled;
        public Sprite dieFrame;

        /// <summary>
        /// The roles a surface can ask for. Deliberately about PURPOSE, not shape — that is what
        /// lets a theme give a pill button and a card button completely different silhouettes.
        /// </summary>
        public enum Role
        {
            None, ButtonPill, ButtonCard, ButtonIconSquare, ButtonIconCircle,
            PanelCard, PanelModal, PanelTile, CardPlayer,
            TrackPill, TrackWell, ThumbSegment, ToggleTrack, ToggleKnob, ChipPill,
            AvatarRing, AvatarSlot, PipEmpty, PipFilled, DieFrame
        }

        public Sprite Get(Role role)
        {
            switch (role)
            {
                case Role.ButtonPill: return buttonPill;
                case Role.ButtonCard: return buttonCard;
                case Role.ButtonIconSquare: return buttonIconSquare;
                case Role.ButtonIconCircle: return buttonIconCircle;
                case Role.PanelCard: return panelCard;
                case Role.PanelModal: return panelModal;
                case Role.PanelTile: return panelTile;
                case Role.CardPlayer: return cardPlayer;
                case Role.TrackPill: return trackPill;
                case Role.TrackWell: return trackWell;
                case Role.ThumbSegment: return thumbSegment;
                case Role.ToggleTrack: return toggleTrack;
                case Role.ToggleKnob: return toggleKnob;
                case Role.ChipPill: return chipPill;
                case Role.AvatarRing: return avatarRing;
                case Role.AvatarSlot: return avatarSlot;
                case Role.PipEmpty: return pipEmpty;
                case Role.PipFilled: return pipFilled;
                case Role.DieFrame: return dieFrame;
                default: return null;
            }
        }

        /// <summary>Convenience for builders: the sprite for a role, or null when unskinned.</summary>
        public static Sprite For(Role role)
        {
            UISkin s = Current;
            return s != null ? s.Get(role) : null;
        }
    }
}
