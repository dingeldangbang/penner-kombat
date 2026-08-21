Shader "PennerKombat/Glow"
{
    // Additiver Radial-Glow für Auren, Mojo-Säule, Schockwellen-Ringe und
    // Buff-Marker. Reines Unlit — bewusst ohne Beleuchtung, damit die Farbe
    // exakt der Palette aus PennerPalette.cs entspricht.

    Properties
    {
        _Color         ("Color", Color) = (1, 0.843, 0, 1)   // #FFD700
        _GlowIntensity ("Glow Intensity", Range(0, 5)) = 1
        _Falloff       ("Falloff", Range(0.5, 6)) = 2
        _Pulse         ("Pulse Speed (0 = aus)", Range(0, 10)) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType"  = "Transparent"
            "Queue"       = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Blend SrcAlpha One      // additiv
        ZWrite Off
        Cull Off

        Pass
        {
            Name "Glow"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float  _GlowIntensity;
                float  _Falloff;
                float  _Pulse;
            CBUFFER_END

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                // radialer Abfall vom Quad-Mittelpunkt
                half dist = distance(IN.uv, half2(0.5, 0.5)) * 2.0;
                half alpha = saturate(1.0 - dist);
                alpha = pow(alpha, _Falloff);

                half intensity = _GlowIntensity;
                if (_Pulse > 0.001)
                    intensity *= 0.65 + 0.35 * (sin(_Time.y * _Pulse) * 0.5 + 0.5);

                return half4(_Color.rgb * intensity * alpha, alpha * _Color.a);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
