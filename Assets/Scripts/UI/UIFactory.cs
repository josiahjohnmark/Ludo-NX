using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LudoNX.UI
{
    /// <summary>
    /// Construction helpers for clay interface elements.
    ///
    /// These are what the screen prefabs are authored with. Keeping them in one place is what
    /// stops forty little inconsistencies creeping in — one definition of what a card is, one
    /// definition of what a label is, one place where a colour becomes render-ready.
    /// </summary>
    public static class UIFactory
    {
        public static UITheme T => UITheme.Current;

        // ── Rects ─────────────────────────────────────────────────────────────────────────

        public static RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 pos)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            return rt;
        }

        public static RectTransform Stretch(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        // ── Clay ──────────────────────────────────────────────────────────────────────────

        public static ClayGraphic Clay(string name, Transform parent, Vector2 size, Vector2 pos,
                                       Color color, ClayGraphic.Shape shape = ClayGraphic.Shape.Panel,
                                       float radius = -1f, UISkin.Role role = UISkin.Role.None)
        {
            var rt = Rect(name, parent, size, pos);
            rt.gameObject.AddComponent<CanvasRenderer>();
            var g = rt.gameObject.AddComponent<ClayGraphic>();
            g.color = color;

            // When the active theme supplies artwork for this role, the surface draws that
            // sprite instead of the procedural clay. Everything else about it is unchanged.
            if (role != UISkin.Role.None) g.skin = UISkin.For(role);
            g.shape = shape;
            g.cornerRadius = radius < 0f ? T.radiusMd : radius;
            g.shadowSpread = T.shadowSpread;
            g.shadowAlpha = T.shadowAlpha;
            g.innerHighlight = T.innerHighlight;
            g.innerShadow = T.innerShadow;
            g.glowAlpha = 0f;
            return g;
        }

        /// <summary>Full-bleed background. No shadow, no bevel — it is the page, not an object on it.</summary>
        public static ClayGraphic Ground(Transform parent, Color color)
        {
            var rt = Stretch("Ground", parent);
            rt.gameObject.AddComponent<CanvasRenderer>();
            var g = rt.gameObject.AddComponent<ClayGraphic>();
            g.color = color;
            g.shape = ClayGraphic.Shape.Panel;
            g.cornerRadius = 0f;
            g.shadowAlpha = 0f;
            g.glowAlpha = 0f;
            g.innerHighlight = 0f;
            g.innerShadow = 0f;
            g.gradientStrength = 0f;   // a full-screen ramp bands in 8-bit
            g.raycastTarget = false;
            return g;
        }

        /// <summary>
        /// The standard page surface, matching the loader reference exactly: flat #F8F9FA with
        /// a soft pink wash bleeding out of the top-left corner and a warm cream wash in the
        /// bottom-right. Every screen opens with this, so the whole app shares one surface
        /// instead of each screen inventing its own background.
        /// </summary>
        public static void Page(Transform parent)
        {
            Ground(parent, T.groundTop);

            Sprite glow = UIIconSet.Current != null ? UIIconSet.Current.softGlow : null;
            if (glow == null) return;

            // Oversized and anchored off-screen so only the falloff shows, never a visible disc.
            Icon("WashTopLeft", parent, new Vector2(1500f, 1500f), new Vector2(-460f, 860f),
                 glow, T.washTopLeft);
            Icon("WashBottomRight", parent, new Vector2(1400f, 1400f), new Vector2(470f, -880f),
                 glow, T.washBottomRight);
        }

        /// <summary>A recessed groove — toggle tracks, progress rails, input wells.</summary>
        public static ClayGraphic Recess(string name, Transform parent, Vector2 size, Vector2 pos,
                                         ClayGraphic.Shape shape = ClayGraphic.Shape.Pill)
        {
            var g = Clay(name, parent, size, pos, T.recessed, shape, -1f,
                         shape == ClayGraphic.Shape.Pill ? UISkin.Role.TrackPill : UISkin.Role.TrackWell);
            g.shadowAlpha = 0f;
            g.innerHighlight = 0.04f;
            g.innerShadow = 0.85f;
            return g;
        }

        // ── Text ──────────────────────────────────────────────────────────────────────────

        public static TextMeshProUGUI Label(string name, Transform parent, Vector2 size, Vector2 pos,
                                            string text, TMP_FontAsset font, float fontSize, Color color,
                                            TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var rt = Rect(name, parent, size, pos);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.text = text;
            if (font != null) t.font = font;
            t.fontSize = fontSize;
            // TMP is not the clay shader, so the colour has to be linearised here.
            t.color = UITheme.Lin(color);
            t.alignment = align;
            t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.Normal;
            return t;
        }

        public static TextMeshProUGUI Display(string name, Transform parent, Vector2 size, Vector2 pos,
                                              string text, Color color)
        {
            var t = Label(name, parent, size, pos, text,
                          UIFontSet.Current != null ? UIFontSet.Current.display : null,
                          UIFontSet.SizeDisplay, color);
            t.characterSpacing = -2f;
            return t;
        }

        public static TextMeshProUGUI Body(string name, Transform parent, Vector2 size, Vector2 pos,
                                           string text, Color color, float size2 = UIFontSet.SizeBody)
        {
            return Label(name, parent, size, pos, text,
                         UIFontSet.Current != null ? UIFontSet.Current.body : null, size2, color);
        }

        // ── Buttons ───────────────────────────────────────────────────────────────────────

        /// <summary>
        /// A labelled clay button. The label sits in a child holder so it can sink with the
        /// press without fighting the button's own layout.
        /// </summary>
        public static ClayButton Button(string name, Transform parent, Vector2 size, Vector2 pos,
                                        Color color, string label, float labelSize = UIFontSet.SizeTitle,
                                        ClayGraphic.Shape shape = ClayGraphic.Shape.Pill,
                                        float glow = 0f, bool hero = false,
                                        UISkin.Role role = UISkin.Role.None)
        {
            // Shape implies the role unless the caller names one: a pill is the pill button, a
            // circle is the round icon button, anything else is the card button.
            if (role == UISkin.Role.None)
            {
                role = shape == ClayGraphic.Shape.Pill ? UISkin.Role.ButtonPill
                     : shape == ClayGraphic.Shape.Circle ? UISkin.Role.ButtonIconCircle
                     : UISkin.Role.ButtonCard;
            }

            var clay = Clay(name, parent, size, pos, color, shape,
                            shape == ClayGraphic.Shape.Panel ? T.radiusLg : -1f, role);
            clay.glowAlpha = glow;
            clay.glowSpread = 64f;
            if (hero)
            {
                // Scaled off the theme rather than fixed, so the hero reads as "a bit more
                // lift" on any palette instead of carrying a hard-coded dark-theme value.
                clay.shadowSpread = T.shadowSpread * 1.35f;
                clay.shadowAlpha = Mathf.Min(T.shadowAlpha * 1.4f, 0.9f);
                clay.innerHighlight = T.innerHighlight * 1.3f;
            }

            var btn = clay.gameObject.AddComponent<ClayButton>();
            btn.transition = Selectable.Transition.None;
            btn.targetGraphic = clay;
            // No idle pulse. A hero button that never stops moving reads as a wobble, not as
            // emphasis — the glow and the size already carry that job.
            btn.breathe = false;

            var holder = Rect("Content", clay.transform, size, Vector2.zero);
            holder.gameObject.AddComponent<CanvasRenderer>();
            btn.content = holder;

            if (!string.IsNullOrEmpty(label))
            {
                // Label colour follows the fill's luminance. On a light theme a button is either
                // saturated crimson or pale grey, and one fixed ink colour is illegible on one
                // of them.
                var t = Label("Label", holder, size, Vector2.zero, label,
                              UIFontSet.Current != null ? UIFontSet.Current.body : null,
                              labelSize, T.InkOn(color));
                t.fontStyle = FontStyles.Bold;
                t.characterSpacing = 4f;
            }
            return btn;
        }

        /// <summary>A round icon button — back arrow, settings gear, avatar.</summary>
        public static ClayButton CircleButton(string name, Transform parent, float diameter, Vector2 pos,
                                              Color color, string glyph = null, float glyphSize = 44f)
        {
            var btn = Button(name, parent, new Vector2(diameter, diameter), pos, color, null,
                             shape: ClayGraphic.Shape.Circle);
            btn.Clay.shadowSpread = 24f;
            if (!string.IsNullOrEmpty(glyph))
            {
                var t = Label("Glyph", btn.content, new Vector2(diameter, diameter), Vector2.zero, glyph,
                              UIFontSet.Current != null ? UIFontSet.Current.body : null, glyphSize,
                              T.InkOn(color));
                t.fontStyle = FontStyles.Bold;
            }
            return btn;
        }

        // ── Composites ────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Back arrow on the left, title centred. Every pushed screen gets one — it is the only
        /// navigation affordance in the game, which is what lets the tab bar go away.
        /// </summary>
        public static ClayButton TopBar(Transform parent, string title, out TextMeshProUGUI titleLabel)
        {
            var back = CircleButton("BackButton", parent, 100f, new Vector2(-432f, 806f), T.neutral);

            // A chevron drawn from two clay strokes.
            for (int i = 0; i < 2; i++)
            {
                var stroke = Clay("Chevron" + i, back.content, new Vector2(44f, 14f),
                                  new Vector2(4f, i == 0 ? 13f : -13f), T.ink, ClayGraphic.Shape.Pill);
                stroke.transform.localRotation = Quaternion.Euler(0f, 0f, i == 0 ? 45f : -45f);
                stroke.raycastTarget = false;
                stroke.shadowAlpha = 0f;
                stroke.innerHighlight = 0f;
                stroke.innerShadow = 0f;
            }

            titleLabel = Label("Title", parent, new Vector2(700f, 80f), new Vector2(0f, 806f), title,
                               UIFontSet.Current != null ? UIFontSet.Current.display : null,
                               UIFontSet.SizeHeadline, T.ink);
            return back;
        }

        /// <summary>
        /// A carved track with a sliding clay thumb. Returns the wired component; the caller
        /// only has to position it.
        /// </summary>
        public static SegmentedToggle Segmented(string name, Transform parent, Vector2 size, Vector2 pos,
                                                string[] options, Color thumbColor)
        {
            var track = Recess(name, parent, size, pos);
            var seg = track.gameObject.AddComponent<SegmentedToggle>();
            seg.track = track;

            float pad = 8f;
            float slotW = (size.x - pad * 2f) / options.Length;
            float slotH = size.y - pad * 2f;

            // Thumb first, so the labels sit on top of it.
            var thumb = Clay("Thumb", track.transform, new Vector2(slotW, slotH), Vector2.zero,
                             thumbColor, ClayGraphic.Shape.Pill, -1f, UISkin.Role.ThumbSegment);
            thumb.raycastTarget = false;
            thumb.shadowSpread = 20f;
            thumb.shadowAlpha = 0.30f;
            seg.thumb = thumb;

            for (int i = 0; i < options.Length; i++)
            {
                float x = -size.x * 0.5f + pad + slotW * (i + 0.5f);

                var slot = Rect("Slot" + i, track.transform, new Vector2(slotW, slotH), new Vector2(x, 0f));
                slot.gameObject.AddComponent<CanvasRenderer>();
                seg.slots.Add(slot);

                // Invisible hitbox so the whole segment is tappable, not just the text.
                var hit = Button("Hit" + i, track.transform, new Vector2(slotW, slotH), new Vector2(x, 0f),
                                 new Color(1f, 1f, 1f, 0f), null);
                hit.Clay.shadowAlpha = 0f;
                hit.Clay.innerHighlight = 0f;
                hit.Clay.innerShadow = 0f;
                hit.Clay.gradientStrength = 0f;
                seg.hitboxes.Add(hit);

                var lbl = Label("Label" + i, hit.content, new Vector2(slotW, slotH), Vector2.zero,
                                options[i], UIFontSet.Current != null ? UIFontSet.Current.body : null,
                                30f, T.inkDim);
                lbl.fontStyle = FontStyles.Bold;
                lbl.characterSpacing = 2f;
                seg.labels.Add(lbl);
            }

            return seg;
        }

        /// <summary>A titled card with an icon bubble — the repeating unit on setup screens.</summary>
        public static ClayGraphic Card(string name, Transform parent, Vector2 size, Vector2 pos,
                                       string title, Color accent)
        {
            var card = Clay(name, parent, size, pos, T.panel, ClayGraphic.Shape.Panel, T.radiusLg,
                            UISkin.Role.PanelCard);

            var bubble = Clay("Bubble", card.transform, new Vector2(72f, 72f),
                              new Vector2(-size.x * 0.5f + 76f, size.y * 0.5f - 74f), accent,
                              ClayGraphic.Shape.Circle);
            bubble.raycastTarget = false;
            bubble.shadowAlpha = 0.25f;

            // Title sits to the right of the bubble, left-aligned, filling the remaining width.
            const float textLeft = 130f;
            float textWidth = size.x - textLeft - 40f;
            Label("CardTitle", card.transform, new Vector2(textWidth, 60f),
                  new Vector2(-size.x * 0.5f + textLeft + textWidth * 0.5f, size.y * 0.5f - 74f),
                  title, UIFontSet.Current != null ? UIFontSet.Current.body : null,
                  UIFontSet.SizeTitle, T.ink, TextAlignmentOptions.Left);
            return card;
        }

        private static Material _spriteMaterial;

        /// <summary>
        /// Shared material for sprite artwork. Uses <c>LudoNX/UISprite</c>, which applies the
        /// sRGB → linear conversion that this render path otherwise skips.
        /// </summary>
        public static Material SpriteMaterial
        {
            get
            {
                if (_spriteMaterial != null) return _spriteMaterial;
                _spriteMaterial = Resources.Load<Material>("UISprite");
                if (_spriteMaterial == null)
                {
                    Shader s = Shader.Find("LudoNX/UISprite");
                    if (s != null) _spriteMaterial = new Material(s) { name = "UISprite (runtime)" };
                }
                return _spriteMaterial;
            }
        }

        private static Material _skinMaterial;

        /// <summary>
        /// Material for 9-sliced theme artwork. Same shader as sprites, but with the texture
        /// conversion off — Unity already converts an sRGB-flagged texture when it samples it,
        /// and doing it twice dithers the dark channels.
        /// </summary>
        public static Material SkinMaterial
        {
            get
            {
                if (_skinMaterial != null) return _skinMaterial;
                _skinMaterial = Resources.Load<Material>("UISkinMat");
                return _skinMaterial;
            }
        }

        internal static void ResetStatics() { _spriteMaterial = null; _skinMaterial = null; }

        /// <summary>
        /// A sprite, sized and tinted. Used only where real artwork beats a drawn shape.
        /// The tint is passed through as plain sRGB — the shader converts it, exactly as the
        /// clay shader does with its vertex colour.
        /// </summary>
        public static Image Icon(string name, Transform parent, Vector2 size, Vector2 pos,
                                 Sprite sprite, Color tint)
        {
            var rt = Rect(name, parent, size, pos);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = tint;
            img.material = SpriteMaterial;
            img.preserveAspect = true;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>A full-width settings row: clay slab, left-aligned label, space on the right.</summary>
        public static ClayButton Row(string name, Transform parent, float width, float y, string label,
                                     out TextMeshProUGUI labelText, float height = 128f)
        {
            var row = Button(name, parent, new Vector2(width, height), new Vector2(0f, y), T.panel, null,
                             shape: ClayGraphic.Shape.Panel);
            row.Clay.cornerRadius = T.radiusMd;
            row.Clay.shadowAlpha = 0.20f;

            labelText = Label("Label", row.content, new Vector2(width - 320f, 60f),
                              new Vector2(-width * 0.5f + 44f + (width - 320f) * 0.5f, 0f), label,
                              UIFontSet.Current != null ? UIFontSet.Current.body : null,
                              UIFontSet.SizeTitle, T.ink, TextAlignmentOptions.Left);
            return row;
        }

        /// <summary>A chevron pointing right, for rows that lead somewhere.</summary>
        public static void Chevron(Transform parent, Vector2 pos, Color color)
        {
            for (int i = 0; i < 2; i++)
            {
                var stroke = Clay("Chev" + i, parent, new Vector2(30f, 10f),
                                  pos + new Vector2(0f, i == 0 ? 9f : -9f), color, ClayGraphic.Shape.Pill);
                stroke.transform.localRotation = Quaternion.Euler(0f, 0f, i == 0 ? -45f : 45f);
                stroke.raycastTarget = false;
                stroke.shadowAlpha = 0f;
                stroke.innerHighlight = 0f;
                stroke.innerShadow = 0f;
            }
        }

        /// <summary>Big number over a caption, on a clay tile. The unit of every stats grid.</summary>
        public static TextMeshProUGUI StatTile(string name, Transform parent, Vector2 size, Vector2 pos,
                                               string value, string caption, Color accent)
        {
            var tile = Clay(name, parent, size, pos, T.panel, ClayGraphic.Shape.Panel, T.radiusMd,
                            UISkin.Role.PanelTile);
            tile.raycastTarget = false;

            var bar = Clay("Accent", tile.transform, new Vector2(size.x - 48f, 8f),
                           new Vector2(0f, size.y * 0.5f - 18f), accent, ClayGraphic.Shape.Pill);
            bar.raycastTarget = false;
            bar.shadowAlpha = 0f;

            var v = Label("Value", tile.transform, new Vector2(size.x, 76f), new Vector2(0f, 12f), value,
                          UIFontSet.Current != null ? UIFontSet.Current.display : null, 62f, T.ink);
            Label("Caption", tile.transform, new Vector2(size.x, 40f), new Vector2(0f, -46f), caption,
                  UIFontSet.Current != null ? UIFontSet.Current.body : null,
                  UIFontSet.SizeCaption, T.inkMuted).characterSpacing = 4f;
            return v;
        }

        /// <summary>A small pill chip — MODE: CLASSIC, difficulty, READY.</summary>
        public static TextMeshProUGUI Chip(string name, Transform parent, Vector2 size, Vector2 pos,
                                           string text, Color fill, Color ink)
        {
            var chip = Clay(name, parent, size, pos, fill, ClayGraphic.Shape.Pill, -1f, UISkin.Role.ChipPill);
            chip.raycastTarget = false;
            chip.shadowAlpha = 0.20f;
            var t = Label("Label", chip.transform, size, Vector2.zero, text,
                          UIFontSet.Current != null ? UIFontSet.Current.body : null, 26f, ink);
            t.characterSpacing = 4f;
            t.fontStyle = FontStyles.Bold;
            return t;
        }

        // ── Misc ──────────────────────────────────────────────────────────────────────────

        /// <summary>An invisible full-area tap catcher, used for modal scrims.</summary>
        public static Image Scrim(Transform parent, float alpha = 0.72f)
        {
            var rt = Stretch("Scrim", parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, alpha);
            return img;
        }

        public static CanvasGroup EnsureGroup(GameObject go)
        {
            var g = go.GetComponent<CanvasGroup>();
            return g != null ? g : go.AddComponent<CanvasGroup>();
        }
    }
}
