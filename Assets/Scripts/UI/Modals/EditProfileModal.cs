using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LudoNX.UI
{
    /// <summary>
    /// Rename yourself and choose your picture. Without this every player is permanently called
    /// "Player" wearing the same face, which makes the result screen's placement table read oddly.
    ///
    /// The picture grid is built at runtime rather than baked into the prefab, because the set of
    /// available pictures is data (<see cref="UIIconSet.avatars"/>) and will grow as themes are
    /// added. Building it live means adding a picture is a matter of dropping in a file, with no
    /// prefab to re-author.
    /// </summary>
    public class EditProfileModal : UIModal
    {
        [Header("Wiring")]
        public TMP_InputField nameField;
        public ClayButton saveButton;
        public ClayButton cancelButton;

        [Tooltip("Empty container the picture grid is built into.")]
        public RectTransform pickerRoot;

        private Action _onSaved;
        private int _picked;
        private readonly List<ClayGraphic> _cells = new List<ClayGraphic>();
        private readonly List<Image> _photos = new List<Image>();

        private void Awake()
        {
            if (saveButton != null) saveButton.onClick.AddListener(Save);
            if (cancelButton != null) cancelButton.onClick.AddListener(Dismiss);
            BuildPicker();
        }

        public void Configure(Action onSaved)
        {
            _onSaved = onSaved;
            var s = LudoNXSession.Instance;
            if (nameField != null) nameField.text = s != null ? s.PlayerName : "Player";

            // Open showing what the player currently has, not whatever was last tapped.
            _picked = AvatarService.Index;
            Highlight();
        }

        // ── Picture grid ──────────────────────────────────────────────────────────────────

        private const float Cell = 128f;
        private const float Gap = 18f;
        private const int PerRow = 5;

        private void BuildPicker()
        {
            if (pickerRoot == null || _cells.Count > 0) return;

            int count = AvatarService.Count;
            if (count == 0) return;

            var T = UITheme.Current;
            var circle = Resources.Load<Sprite>("ui_circle");

            int rows = Mathf.CeilToInt(count / (float)PerRow);
            float rowH = Cell + Gap;

            for (int i = 0; i < count; i++)
            {
                int row = i / PerRow;
                int col = i % PerRow;

                // Centre each row on its own, so a short final row does not sit left-aligned
                // under a full one.
                int inRow = Mathf.Min(PerRow, count - row * PerRow);
                float rowW = inRow * Cell + (inRow - 1) * Gap;
                float x = -rowW * 0.5f + Cell * 0.5f + col * (Cell + Gap);
                float y = (rows - 1) * rowH * 0.5f - row * rowH;

                var btn = UIFactory.Button("Avatar" + i, pickerRoot, new Vector2(Cell, Cell),
                                           new Vector2(x, y), T.recessed, null,
                                           shape: ClayGraphic.Shape.Circle);
                btn.Clay.shadowSpread = 14f;
                btn.Clay.shadowAlpha = 0.16f;

                var photo = AvatarService.FitInto((RectTransform)btn.content, circle,
                                                  AvatarService.At(i), Cell - 16f);
                _photos.Add(photo);
                _cells.Add(btn.Clay);

                int captured = i;
                btn.onClick.AddListener(() => { _picked = captured; Highlight(); });
            }

            _picked = AvatarService.Index;
            Highlight();
        }

        /// <summary>
        /// The chosen picture gets a gold ring and full brightness; the rest sit dimmed, so the
        /// current selection is readable at a glance without a separate tick badge crowding the
        /// small cells.
        /// </summary>
        private void Highlight()
        {
            var T = UITheme.Current;
            for (int i = 0; i < _cells.Count; i++)
            {
                bool on = i == _picked;
                _cells[i].color = on ? T.gold : T.recessed;
                _cells[i].glowAlpha = on ? 0.30f : 0f;
                _cells[i].glowSpread = on ? 22f : 0f;
                _cells[i].SetVerticesDirty();

                // Only a light knock-back on the unpicked faces: the gold ring already carries
                // the selection, and dimming them far enough to read as "off" would also stop
                // you seeing the face you are about to choose.
                if (_photos[i] != null)
                    _photos[i].color = on ? Color.white : new Color(1f, 1f, 1f, 0.88f);
            }
        }

        private void Save()
        {
            var s = LudoNXSession.Instance;
            if (s != null && nameField != null && !string.IsNullOrWhiteSpace(nameField.text))
                s.SetPlayerName(nameField.text.Trim());

            // Fires AvatarService.Changed, which repaints every live slot — top bar, profile
            // ring and the in-match card — without any of them needing to poll.
            AvatarService.Index = _picked;

            Action cb = _onSaved;
            _onSaved = null;
            Dismiss();
            if (cb != null) cb();
        }
    }
}
