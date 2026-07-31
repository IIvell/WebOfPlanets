// Animirani "tok energije" za zrake veza među planetima (dorada vizuala,
// srpanj 2026.) — zamjena za pune Lit cilindre. Aditivna prozirnost (energija
// svijetli), pulsevi putuju uzduž zrake, fresnel rub daje volumen.
//
// Koordinata pulseva je WORLD pozicija projicirana na lokalnu Y os segmenta:
// zraka je 3 zasebna cilindra (PlanetConnection), a UV.y bi na svakom davao
// drugu gustoću pulseva jer segmenti imaju različite duljine — world projekcija
// drži gustoću u world jedinicama, identičnu na sva tri segmenta.
//
// Datoteka je u Resources NAMJERNO: materijal se stvara runtime Shader.Findom
// (PlanetConnection.CreateBeamMaterial), a shader bez ijednog asset-materijala
// u sceni player build inače strippa — isti razlog kao fallback lanac u
// VfxManager.CreateParticleMaterial.
Shader "WebOfPlanets/ConnectionBeam"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (0, 1, 0, 1)
        _FlowSpeed("Flow Speed", Float) = 3.0
        _BandDensity("Band Density (per world unit)", Float) = 0.35
        _BandSharpness("Band Sharpness", Range(1, 8)) = 3.0
        _FresnelPower("Fresnel Power", Range(0.5, 8)) = 2.5
        _Intensity("Intensity", Range(0, 4)) = 1.6
        _CoreAlpha("Core Alpha", Range(0, 1)) = 0.45
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Blend SrcAlpha One // aditivno: zrake svijetle nad tamnim svemirom
        ZWrite Off
        Cull Back

        Pass
        {
            Name "ConnectionBeamUnlit"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _FlowSpeed;
                float _BandDensity;
                float _BandSharpness;
                float _FresnelPower;
                float _Intensity;
                float _CoreAlpha;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD0;
                float3 viewWS     : TEXCOORD1;
                float  axisCoord  : TEXCOORD2; // world pozicija uzduž osi segmenta
            };

            Varyings vert(Attributes input)
            {
                Varyings o;
                VertexPositionInputs pos = GetVertexPositionInputs(input.positionOS.xyz);
                o.positionCS = pos.positionCS;
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                o.viewWS = GetWorldSpaceViewDir(pos.positionWS);

                // Lokalna Y os cilindra u world prostoru (drugi stupac O2W matrice).
                float3 axis = normalize(float3(
                    unity_ObjectToWorld._m01,
                    unity_ObjectToWorld._m11,
                    unity_ObjectToWorld._m21));
                o.axisCoord = dot(pos.positionWS, axis);
                return o;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half3 n = normalize(input.normalWS);
                half3 v = normalize(input.viewWS);

                // Fresnel: rub zrake svjetliji od sredine — dojam volumena/aure.
                half fresnel = pow(1.0 - saturate(dot(n, v)), _FresnelPower);

                // Pulsevi koji putuju uzduž zrake (uspravni segmenti: prema gore,
                // srednji: od A prema B — smjer daje orijentacija lokalne Y osi).
                half band = 0.5 + 0.5 * sin((input.axisCoord * _BandDensity - _Time.y * _FlowSpeed) * 6.2831853);
                band = pow(band, _BandSharpness);

                half glow = _CoreAlpha + band + fresnel;

                half4 c;
                c.rgb = _BaseColor.rgb * glow * _Intensity;
                c.a = saturate(_BaseColor.a * (_CoreAlpha + band * 0.6 + fresnel * 0.5));
                return c;
            }
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/Unlit"
}
