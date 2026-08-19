using UnityEngine;
using UnityEngine.UI;

namespace LudoNX.UI
{
    /// <summary>
    /// A single clay surface — button, card, track, chip or avatar ring — drawn entirely by
    /// <c>LudoNX/ClayPanel</c>. No sprite, no 9-slice, no baked shadow.
    ///
    /// The mesh is a quad deliberately grown past the RectTransform so the drop shadow has
    /// somewhere to fall; layout still uses the real rect, and so does raycasting, so the
    /// shadow is visual only and never eats a tap.
    ///
    /// Every per-element parameter is written into vertex channels rather than set on the
    /// material. That is the whole reason this scales: a screen with forty clay elements is
    /// still one draw call, because they all share one material.
    /// </summary>
    // RequireComponent on the base Graphic class is not reliably applied to subclasses added
    // from script, and a Graphic without a CanvasRenderer silently draws nothing. Declare it here.
    [RequireComponent(typeof(CanvasRenderer))]
    [RequireComponent(typeof(RectTransform))]
    [AddComponentMenu("Ludo NX/UI/Clay Graphic")]
    public class ClayGraphic : MaskableGraphic, ILayoutElement
    {
        public enum Shape
        {
            /// <summary>Rounded rectangle using <see cref="cornerRadius"/>.</summary>
            Panel,
            /// <summary>Fully rounded ends — radius tracks the short edge.</summary>
            Pill,
            /// <summary>Circle — radius tracks the short edge, intended for square rects.</summary>
            Circle
        }

        /// <summary>
        /// Vertical shadow drop, mirroring <c>_ShadowOffsetY</c> in the shader. Kept here too
        /// because the mesh has to be padded enough to contain the offset shadow.
        /// </summary>
        public const float ShadowDrop = 18f;

        [Header("Shape")]
        public Shape shape = Shape.Panel;
        public float cornerRadius = 28f;

        [Header("Outer shadow")]
        [Tooltip("Blur radius of the soft drop shadow, in pixels.")]
        public float shadowSpread = 36f;
        [Range(0f, 1f)] public float shadowAlpha = 0.32f;

        [Header("Glow")]
        [Tooltip("Coloured halo, for hero actions. 0 disables it.")]
        public float glowSpread = 52f;
        [Range(0f, 1f)] public float glowAlpha = 0f;

        [Header("Inner bevel")]
        [Range(0f, 1f)] public float innerHighlight = 0.22f;
        [Range(0f, 1f)] public float innerShadow = 0.30f;

        [Header("Fill")]
        [Range(0f, 0.6f)]
        [Tooltip("How much darker the bottom edge is than the top. Set 0 on full-screen backgrounds — across 1920px the ramp bands visibly.")]
        public float gradientStrength = 0.12f;

        [Header("Skin")]
        [Tooltip("When set, this sprite is drawn 9-sliced instead of the procedural clay. " +
                 "Everything else — press, layout, raycasting — behaves identically, so a theme " +
                 "swap is only ever a change of sprite.")]
        public Sprite skin;

        [Header("State")]
        [Range(0f, 1f)]
        [Tooltip("0 = resting, 1 = fully pressed. Driven by ClayButton.")]
        public float press01;

        private static Material _sharedClay;

        internal static void ResetStatics() { _sharedClay = null; }

        /// <summary>
        /// Shared clay material, loaded from <c>Resources/ClayPanel</c>. It lives in Resources
        /// so the shader is guaranteed to survive build-time stripping — a shader referenced
        /// only by a runtime <c>Shader.Find</c> would be dropped and every clay element would
        /// render magenta in a player build.
        /// </summary>
        public static Material SharedClayMaterial
        {
            get
            {
                if (_sharedClay != null) return _sharedClay;
                _sharedClay = Resources.Load<Material>("ClayPanel");
                if (_sharedClay == null)
                {
                    Shader s = Shader.Find("LudoNX/ClayPanel");
                    if (s != null) _sharedClay = new Material(s) { name = "ClayPanel (runtime)" };
                }
                return _sharedClay;
            }
        }

        public override Material defaultMaterial
        {
            get
            {
                // A skinned surface is a plain textured quad, so it uses the sprite shader —
                // which also applies the sRGB conversion this render path otherwise skips.
                if (skin != null)
                {
                    Material sm = UIFactory.SkinMaterial;
                    if (sm != null) return sm;
                }
                Material m = SharedClayMaterial;
                return m != null ? m : base.defaultMaterial;
            }
        }

        public override Texture mainTexture =>
            skin != null && skin.texture != null ? skin.texture : (Texture)Texture2D.whiteTexture;

        protected override void OnEnable()
        {
            base.OnEnable();
            EnsureCanvasChannels();
        }

        protected override void OnCanvasHierarchyChanged()
        {
            base.OnCanvasHierarchyChanged();
            EnsureCanvasChannels();
        }

        protected override void OnTransformParentChanged()
        {
            base.OnTransformParentChanged();
            EnsureCanvasChannels();
        }

        /// <summary>
        /// A Canvas only uploads the vertex streams it is told to. Without this the extra
        /// channels arrive as zero and every clay element collapses to a black rectangle.
        /// </summary>
        private void EnsureCanvasChannels()
        {
            Canvas c = canvas;
            if (c == null) return;
            c.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1
                                        | AdditionalCanvasShaderChannels.TexCoord2
                                        | AdditionalCanvasShaderChannels.TexCoord3;
        }

        /// <summary>Extra room the mesh needs beyond the layout rect to fit shadow and glow.</summary>
        private float Padding => Mathf.Max(shadowSpread + ShadowDrop, glowAlpha > 0f ? glowSpread : 0f) + 4f;

        private float ResolveRadius(float halfW, float halfH)
        {
            float shortSide = Mathf.Min(halfW, halfH);
            switch (shape)
            {
                case Shape.Pill:
                case Shape.Circle: return shortSide;
                default: return Mathf.Clamp(cornerRadius, 0f, shortSide);
            }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            if (skin != null) { PopulateSkinned(vh); return; }

            Rect r = GetPixelAdjustedRect();
            float halfW = r.width * 0.5f;
            float halfH = r.height * 0.5f;
            if (halfW <= 0f || halfH <= 0f) return;

            float cx = r.x + halfW;
            float cy = r.y + halfH;
            float pad = Padding;
            float radius = ResolveRadius(halfW, halfH);

            Color32 tint = color;
            Vector4 rectData = new Vector4(halfW, halfH, radius, press01);
            Vector4 shadowData = new Vector4(shadowSpread, shadowAlpha, glowSpread, glowAlpha);
            Vector4 innerData = new Vector4(innerHighlight, innerShadow, gradientStrength, 0f);

            float x0 = -halfW - pad, x1 = halfW + pad;
            float y0 = -halfH - pad, y1 = halfH + pad;

            AddVert(vh, x0, y0, cx, cy, tint, rectData, shadowData, innerData);
            AddVert(vh, x0, y1, cx, cy, tint, rectData, shadowData, innerData);
            AddVert(vh, x1, y1, cx, cy, tint, rectData, shadowData, innerData);
            AddVert(vh, x1, y0, cx, cy, tint, rectData, shadowData, innerData);

            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(2, 3, 0);
        }

        /// <summary>
        /// Draws the skin sprite as a 9-slice: the four corners keep their size, the edges
        /// stretch along one axis and the middle stretches both ways. Written out rather than
        /// delegating to <c>Image</c> because the press animation, the raycast rect and the
        /// layout all live on this component, and splitting the visual off would mean keeping
        /// two objects in step for every button in the game.
        /// </summary>
        private void PopulateSkinned(VertexHelper vh)
        {
            Rect r = GetPixelAdjustedRect();
            if (r.width <= 0f || r.height <= 0f) return;

            Vector4 border = skin.border;          // left, bottom, right, top in pixels
            Vector4 outer = UnityEngine.Sprites.DataUtility.GetOuterUV(skin);
            Vector4 inner = UnityEngine.Sprites.DataUtility.GetInnerUV(skin);

            float bl = border.x, bb = border.y, br = border.z, bt = border.w;

            // A pill is sliced horizontally only, so its end caps are whole semicircles cut from
            // the source art. Left alone, a 720x240 pill drawn 160 tall keeps its 120px-wide cap
            // while the source rows squash 240 -> 160, and the round end renders as a flattened
            // ellipse. Scaling the cap with the rendered height keeps it a true semicircle at any
            // button size, which is what makes the curve read as soft rather than compressed.
            if (bb + bt <= 0f && bl + br > 0f && skin.rect.height > 0f)
            {
                float k = r.height / skin.rect.height;
                bl *= k;
                br *= k;
            }

            // Borders cannot exceed the rect, or opposite corners would overlap and invert.
            float shrinkX = bl + br > r.width && bl + br > 0f ? r.width / (bl + br) : 1f;
            float shrinkY = bb + bt > r.height && bb + bt > 0f ? r.height / (bb + bt) : 1f;
            bl *= shrinkX; br *= shrinkX;
            bb *= shrinkY; bt *= shrinkY;

            float[] xs = { r.xMin, r.xMin + bl, r.xMax - br, r.xMax };
            float[] ys = { r.yMin, r.yMin + bb, r.yMax - bt, r.yMax };
            float[] us = { outer.x, inner.x, inner.z, outer.z };
            float[] vs = { outer.y, inner.y, inner.w, outer.w };

            Color32 tint = color;
            int quad = 0;
            for (int yi = 0; yi < 3; yi++)
            {
                for (int xi = 0; xi < 3; xi++)
                {
                    // Skip a slice that has collapsed to nothing rather than emitting a degenerate quad.
                    if (xs[xi + 1] - xs[xi] <= 0f || ys[yi + 1] - ys[yi] <= 0f) continue;

                    int v0 = quad * 4;
                    AddSkinVert(vh, xs[xi],     ys[yi],     us[xi],     vs[yi],     tint);
                    AddSkinVert(vh, xs[xi],     ys[yi + 1], us[xi],     vs[yi + 1], tint);
                    AddSkinVert(vh, xs[xi + 1], ys[yi + 1], us[xi + 1], vs[yi + 1], tint);
                    AddSkinVert(vh, xs[xi + 1], ys[yi],     us[xi + 1], vs[yi],     tint);
                    vh.AddTriangle(v0, v0 + 1, v0 + 2);
                    vh.AddTriangle(v0 + 2, v0 + 3, v0);
                    quad++;
                }
            }
        }

        private static void AddSkinVert(VertexHelper vh, float x, float y, float u, float v, Color32 tint)
        {
            UIVertex vert = UIVertex.simpleVert;
            vert.position = new Vector3(x, y, 0f);
            vert.color = tint;
            vert.uv0 = new Vector4(u, v, 0f, 0f);
            vh.AddVert(vert);
        }

        private static void AddVert(VertexHelper vh, float lx, float ly, float cx, float cy,
                                    Color32 tint, Vector4 rectData, Vector4 shadowData, Vector4 innerData)
        {
            UIVertex v = UIVertex.simpleVert;
            v.position = new Vector3(cx + lx, cy + ly, 0f);
            v.color = tint;
            v.uv0 = new Vector4(lx, ly, 0f, 0f);   // position in px relative to rect centre
            v.uv1 = rectData;
            v.uv2 = shadowData;
            v.uv3 = innerData;
            vh.AddVert(v);
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            cornerRadius = Mathf.Max(0f, cornerRadius);
            shadowSpread = Mathf.Max(0f, shadowSpread);
            glowSpread = Mathf.Max(0f, glowSpread);
            SetVerticesDirty();
        }
#endif

        // ── ILayoutElement ────────────────────────────────────────────────────────────────
        // Reported as zero so a clay background never forces a layout group to size around it;
        // the content inside a card should drive the card's size, not the other way round.
        public void CalculateLayoutInputHorizontal() { }
        public void CalculateLayoutInputVertical() { }
        public float minWidth => -1f;
        public float preferredWidth => -1f;
        public float flexibleWidth => -1f;
        public float minHeight => -1f;
        public float preferredHeight => -1f;
        public float flexibleHeight => -1f;
        public int layoutPriority => -1;
    }
}
