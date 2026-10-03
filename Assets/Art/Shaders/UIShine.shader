// Ludo NX — UI sprite with a soft light sweep, used by IconShine on the bottom-nav icons.
//
// The base colour is exactly what UI/Default draws (texture x vertex colour, no extra
// conversion), so an icon looks identical to before whenever the sweep is off screen.
//
// The glint is a soft band moving across the sprite, drawn only where the icon has pixels.
// It lerps toward the shine colour instead of adding to it: in Linear colour space an
// additive highlight blows out to white almost immediately, and a lerp can never clip.

Shader "LudoNX/UIShine"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _ShineColor     ("Shine Colour",            Color)  = (1, 1, 1, 1)
        _ShineIntensity ("Shine Intensity",         Range(0, 1)) = 0.6
        _ShineWidth     ("Shine Half-Width",        Range(0.02, 0.5)) = 0.1
        _ShineAngle     ("Sweep Angle (deg below horizontal)", Range(-90, 90)) = 25
        _ShinePos       ("Sweep Progress (0-1, <0 = off)", Float) = -1
        _UVRect         ("Sprite UV Rect (xMin,yMin,xMax,yMax)", Vector) = (0,0,1,1)

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
            Name "UISHINE"
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
                float2 uv     : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 uv            : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                float2 local         : TEXCOORD2;   // 0-1 across the sprite rect
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4    _MainTex_ST;
            fixed4    _Color;
            fixed4    _TextureSampleAdd;
            float4    _ClipRect;

            fixed4 _ShineColor;
            float  _ShineIntensity;
            float  _ShineWidth;
            float  _ShineAngle;
            float  _ShinePos;
            float4 _UVRect;

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv     = TRANSFORM_TEX(v.uv, _MainTex);
                o.local  = (v.uv - _UVRect.xy) / max(_UVRect.zw - _UVRect.xy, 1e-5);
                o.color  = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = (tex2D(_MainTex, i.uv) + _TextureSampleAdd) * i.color;

                // Distance along the sweep direction; the band's centre travels from just
                // outside one corner to just outside the opposite one as _ShinePos goes 0 -> 1.
                float a = radians(_ShineAngle);
                float2 dir = float2(cos(a), -sin(a));
                float reach = 0.5 * (abs(dir.x) + abs(dir.y)) + _ShineWidth;
                float d = dot(i.local - 0.5, dir);
                float centre = lerp(-reach, reach, _ShinePos);

                float s = saturate(1.0 - abs(d - centre) / _ShineWidth);
                s = s * s * (3.0 - 2.0 * s);
                s *= step(0.0, _ShinePos);

                col.rgb = lerp(col.rgb, _ShineColor.rgb, s * _ShineIntensity * _ShineColor.a);

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
