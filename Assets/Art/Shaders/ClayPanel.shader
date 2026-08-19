// Ludo NX — "Deep Table" claymorphism, generated procedurally.
//
// Draws a rounded rectangle with a soft colour-tinted drop shadow, an optional outer glow,
// an inner top-left highlight and an inner bottom-right shadow. Everything a CSS box-shadow
// stack would give you, in one draw call.
//
// Per-element parameters travel in VERTEX DATA, not material properties, so every clay
// element on a screen batches together instead of breaking into one draw call each:
//
//   COLOR      base tint (top of the gradient)
//   TEXCOORD0  xy = position in pixels relative to the rect centre
//   TEXCOORD1  x = half width, y = half height, z = corner radius, w = press 0..1
//   TEXCOORD2  x = shadow spread, y = shadow alpha, z = glow spread, w = glow alpha
//   TEXCOORD3  x = inner highlight alpha, y = inner shadow alpha
//
// The quad is expanded past the RectTransform by ClayGraphic so the shadow has room to
// fall outside the layout rect. Values that are part of the design language rather than
// per-element (inner bevel size, gradient depth, shadow drop) are plain uniforms below.

Shader "LudoNX/ClayPanel"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _InnerOffset      ("Inner bevel offset (px)", Float) = 4
        _InnerBlur        ("Inner bevel blur (px)",   Float) = 12
        _GradientStrength ("Gradient depth",          Range(0,0.6)) = 0.12
        _ShadowOffsetY    ("Shadow drop (px, -Y)",    Float) = -18

        _StencilComp      ("Stencil Comparison", Float) = 8
        _Stencil          ("Stencil ID",         Float) = 0
        _StencilOp        ("Stencil Operation",  Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask  ("Stencil Read Mask",  Float) = 255
        _ColorMask        ("Color Mask",         Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"             = "Transparent"
            "IgnoreProjector"   = "True"
            "RenderType"        = "Transparent"
            "PreviewType"       = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref       [_Stencil]
            Comp      [_StencilComp]
            Pass      [_StencilOp]
            ReadMask  [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "CLAY"
        CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color  : COLOR;
                float2 uv0    : TEXCOORD0;
                float4 uv1    : TEXCOORD1;
                float4 uv2    : TEXCOORD2;
                float4 uv3    : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 local         : TEXCOORD0;
                float4 rect          : TEXCOORD1;
                float4 shadow        : TEXCOORD2;
                float4 inner         : TEXCOORD3;
                float4 worldPosition : TEXCOORD4;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            fixed4 _Color;
            float4 _ClipRect;
            float  _InnerOffset;
            float  _InnerBlur;
            float  _GradientStrength;
            float  _ShadowOffsetY;

            // Signed distance to a rounded box. Negative inside, positive outside.
            float sdRoundBox(float2 p, float2 b, float r)
            {
                r = min(r, min(b.x, b.y));
                float2 q = abs(p) - b + r;
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - r;
            }

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color  = v.color * _Color;
                o.local  = v.uv0;
                o.rect   = v.uv1;
                o.shadow = v.uv2;
                o.inner  = v.uv3;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 p       = i.local;
                float2 ext     = i.rect.xy;   // half extents
                float  radius  = i.rect.z;
                float  press   = saturate(i.rect.w);

                // Antialias width, in pixels, derived from screen-space derivatives so the
                // edge stays exactly one pixel soft at any canvas scale or device DPI.
                float aa = max(fwidth(p.x), fwidth(p.y)) * 0.5 + 0.0001;

                float d = sdRoundBox(p, ext, radius);

                // Vertex colours arrive as 8-bit sRGB. Converting here — rather than storing
                // linear values on the CPU side — is what keeps dark tones alive: linear #0E1729
                // quantises to Color32(1,2,6) and collapses to black, while the same colour in
                // sRGB quantises to (14,23,41) and survives intact.
                float3 src = i.color.rgb;
                #ifndef UNITY_COLORSPACE_GAMMA
                src = GammaToLinearSpace(src);
                #endif

                // ── Fill ───────────────────────────────────────────────────────────────
                float fill = 1.0 - smoothstep(-aa, aa, d);

                // Vertical gradient: top is the base tint, bottom is darker. Reads as volume on
                // a button, but across a full-screen background the same ramp bands visibly in
                // 8-bit, so the strength is per-element and backgrounds set it to zero.
                float t = saturate((ext.y - p.y) / max(ext.y * 2.0, 0.0001));
                float3 baseRgb = src * (1.0 - i.inner.z * t);

                // ── Outer shadow + glow (behind the fill) ──────────────────────────────
                // Pressing sinks the element: the shadow pulls in tight and fades.
                float pressScale   = lerp(1.0, 0.38, press);
                float shadowSpread = max(i.shadow.x * pressScale, 0.5);
                float shadowA      = i.shadow.y * lerp(1.0, 0.7, press);
                float glowSpread   = max(i.shadow.z, 0.5);
                float glowA        = i.shadow.w;

                float2 shadowP = p - float2(0.0, _ShadowOffsetY * pressScale);
                float  ds      = sdRoundBox(shadowP, ext, radius);
                float  shadowMask = (1.0 - smoothstep(-shadowSpread, shadowSpread, ds)) * shadowA;

                float dg       = sdRoundBox(p, ext, radius);
                float glowMask = (1.0 - smoothstep(-glowSpread, glowSpread, dg)) * glowA;

                // A drop shadow has to be darker than whatever it falls on in EVERY channel,
                // or it tints rather than shades. At 0.06 a red button's shadow still had more
                // red in it than the navy ground did, so the button sat in a pink halo. 0.015
                // keeps a trace of hue while landing below the ground in all three channels.
                // Coloured halos are the glow channel's job, deliberately kept separate.
                float3 shadowRgb = src * 0.015;
                float3 glowRgb   = src;

                // ── Inner bevel ────────────────────────────────────────────────────────
                // Top-left highlight and bottom-right shadow, each an inset shadow cast
                // from the opposite corner. Pressing deepens the bottom-right.
                float innerOff  = _InnerOffset;
                float innerBlur = max(_InnerBlur, 0.5);

                float dHi = sdRoundBox(p - float2( innerOff, -innerOff), ext, radius);
                float dLo = sdRoundBox(p - float2(-innerOff,  innerOff), ext, radius);

                float hiMask = smoothstep(-innerBlur, innerBlur, dHi) * fill * i.inner.x;
                float loMask = smoothstep(-innerBlur, innerBlur, dLo) * fill * i.inner.y
                             * lerp(1.0, 1.45, press);

                // ── Composite: glow, then shadow, then fill, then bevel ─────────────────
                float3 rgb = glowRgb;
                float  a   = glowMask;

                a   = shadowMask + a * (1.0 - shadowMask);
                rgb = lerp(rgb, shadowRgb, shadowMask / max(a, 0.0001));

                a   = fill + a * (1.0 - fill);
                rgb = lerp(rgb, baseRgb, fill / max(a, 0.0001));

                // Bevel toward a lifted and a deepened version of the element's OWN colour,
                // not toward white and black. Pure white reads as a hard drawn outline; a
                // lifted tint reads as light landing on a soft material, which is the point.
                float3 hiColor = min(baseRgb * 2.4 + 0.055, 1.0);
                float3 loColor = baseRgb * 0.32;
                rgb = lerp(rgb, hiColor, saturate(hiMask));
                rgb = lerp(rgb, loColor, saturate(loMask));

                fixed4 col = fixed4(rgb, a * i.color.a);

                #ifdef UNITY_UI_CLIP_RECT
                col.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(col.a - 0.001);
                #endif

                return col;
            }
        ENDCG
        }
    }

    Fallback "UI/Default"
}
