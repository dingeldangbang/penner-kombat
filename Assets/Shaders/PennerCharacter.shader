Shader "PennerKombat/Character"
{
    // Ein Shader für alle Charakter-Signaturen aus docs/VISUALS.md §4.1.
    // _EffectMode wählt das visuelle Vokabular:
    //   0 = Aus · 1 = Grease (Le Binde) · 2 = Pulse (Mell) · 3 = Gold (Mojo Bob)
    //   4 = Fire (TetraPak) · 5 = Matrix (Sigi) · 6 = Rat (Rolf)
    // Gesteuert wird er zur Laufzeit von CharacterShaderBinder.cs.

    Properties
    {
        _BaseMap        ("Base Map", 2D) = "white" {}
        _BaseColor      ("Base Color", Color) = (1,1,1,1)

        [Header(Rim)]
        _RimColor       ("Rim Color", Color) = (1, 0.42, 0, 1)
        _RimPower       ("Rim Power", Range(0.5, 8)) = 3
        _RimIntensity   ("Rim Intensity", Range(0, 4)) = 1

        [Header(Signature Effect)]
        _EffectMode     ("Effect Mode", Float) = 0
        _EffectColor    ("Effect Color", Color) = (1, 0.84, 0, 1)
        _EffectIntensity("Effect Intensity", Range(0, 4)) = 1
        _EffectSpeed    ("Effect Speed", Range(0, 12)) = 2
        _NoiseTex       ("Noise / Pattern", 2D) = "gray" {}

        [Header(Hit Flash)]
        _FlashColor     ("Flash Color", Color) = (1,1,1,1)
        _Flash          ("Flash Amount", Range(0,1)) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        LOD 200

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float2 uv         : TEXCOORD2;
            };

            TEXTURE2D(_BaseMap);  SAMPLER(sampler_BaseMap);
            TEXTURE2D(_NoiseTex); SAMPLER(sampler_NoiseTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float4 _RimColor;
                float  _RimPower;
                float  _RimIntensity;
                float  _EffectMode;
                float4 _EffectColor;
                float  _EffectIntensity;
                float  _EffectSpeed;
                float4 _FlashColor;
                float  _Flash;
            CBUFFER_END

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs pos = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs nrm = GetVertexNormalInputs(IN.normalOS);
                OUT.positionCS = pos.positionCS;
                OUT.positionWS = pos.positionWS;
                OUT.normalWS   = nrm.normalWS;
                OUT.uv         = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;

                float3 N = normalize(IN.normalWS);
                float3 V = normalize(GetWorldSpaceViewDir(IN.positionWS));

                // --- Basisbeleuchtung (Lambert + Ambient) ---
                Light mainLight = GetMainLight();
                half ndotl = saturate(dot(N, mainLight.direction));
                half3 lit = albedo.rgb * (mainLight.color * ndotl + unity_AmbientSky.rgb);

                // --- Rim-Light (Cover-Look: warme Kante gegen die Nacht) ---
                half rim = pow(1.0 - saturate(dot(N, V)), _RimPower);
                lit += _RimColor.rgb * rim * _RimIntensity;

                // --- Signatur-Effekt ---
                half t = _Time.y * _EffectSpeed;
                half3 fx = 0;
                int mode = (int)round(_EffectMode);

                if (mode == 1)          // Grease: öliger Film, wandert über die Oberfläche
                {
                    half grease = saturate(dot(N, V) * 1.4);
                    half band = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex,
                                                 IN.uv * 2.0 + float2(0, t * 0.05)).r;
                    fx = _EffectColor.rgb * grease * band * 1.6;
                }
                else if (mode == 2)     // Pulse: Herzschlag auf der Haut
                {
                    half beat = pow(abs(sin(t)), 6.0);
                    fx = _EffectColor.rgb * (0.25 + beat) * rim;
                }
                else if (mode == 3)     // Gold: metallischer Glanz, blitzt bei Krit
                {
                    half spec = pow(saturate(dot(reflect(-V, N), mainLight.direction)), 24);
                    fx = _EffectColor.rgb * (spec * 2.0 + rim * 0.6);
                }
                else if (mode == 4)     // Fire: Flammen tanzen auf der Kleidung
                {
                    half fire = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex,
                                                 IN.uv * 3.0 + float2(0, -t * 0.5)).r;
                    fx = _EffectColor.rgb * pow(fire, 2.0) * saturate(1.0 - IN.uv.y);
                }
                else if (mode == 5)     // Matrix: fallende Zeichen
                {
                    half m = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex,
                                              float2(IN.uv.x * 12.0, IN.uv.y * 4.0 - t * 0.3)).g;
                    fx = _EffectColor.rgb * step(0.72, m);
                }
                else if (mode == 6)     // Rat: Silhouetten laufen über den Körper
                {
                    half r = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex,
                                              IN.uv * 4.0 + float2(t * 0.2, 0)).r;
                    fx = _EffectColor.rgb * step(0.82, r) * 0.8;
                }

                lit += fx * _EffectIntensity;

                // --- Treffer-Flash (Überbelichtung) ---
                lit = lerp(lit, _FlashColor.rgb, saturate(_Flash));

                return half4(lit, albedo.a);
            }
            ENDHLSL
        }

        // Schattenwurf über den URP-Standardpass
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }

    Fallback "Universal Render Pipeline/Lit"
}
