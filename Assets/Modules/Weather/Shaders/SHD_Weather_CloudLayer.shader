// Мировой слой облаков — не часть неба. Плоский меш, который следует за камерой по XZ
// и держит фиксированный Y (см. CloudLayer.cs): неподвижность по высоте и даёт эффект
// «долетел до слоя — пролетел сквозь — облака остались внизу».
//
// Облака — процедурный шум прямо в шейдере, не текстура: авторского арт-ассета под это
// пока нет, а прототип не должен на него блокироваться. Замена на текстуру позже —
// одна правка сэмпла в CloudDensity(), не архитектурная работа.
//
// Двусторонний (Cull Off) — проходим сквозь с обеих сторон. Без ShadowCaster: тонкий
// полупрозрачный слой с жёсткой тенью выглядит неправильно. ZWrite Off — стандартный
// unlit-transparent, исключает z-fighting с геометрией уровня по построению, не за счёт
// подбора offset.

Shader "SpiderRig/Weather/CloudLayer"
{
    Properties
    {
        _CloudColor ("Cloud Color", Color) = (1, 1, 1, 1)
        _CloudScale ("Cloud Scale", Range(0.001, 0.05)) = 0.01
        _CloudThreshold ("Cloud Threshold", Range(0, 1)) = 0.45
        _CloudSoftness ("Cloud Softness", Range(0.01, 1)) = 0.35
        _CloudOpacity ("Cloud Opacity", Range(0, 1)) = 0.85
        _PanSpeed ("Pan Speed", Vector) = (0.002, 0.0015, 0, 0)

        // 0..1, пишет WeatherService каждый кадр по расстоянию паука до высоты слоя.
        // Не путать с _CloudOpacity — это художественная плотность, а это игровой фейд.
        _Alpha ("Altitude Fade", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "Unlit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 worldXZ : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _CloudColor;
                half _CloudScale;
                half _CloudThreshold;
                half _CloudSoftness;
                half _CloudOpacity;
                half4 _PanSpeed;
                half _Alpha;
            CBUFFER_END

            half Hash21(float2 p)
            {
                p = frac(p * float2(123.34h, 456.21h));
                p += dot(p, p + 45.32h);
                return frac(p.x * p.y);
            }

            half ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0h - 2.0h * f);
                half a = Hash21(i);
                half b = Hash21(i + float2(1, 0));
                half c = Hash21(i + float2(0, 1));
                half d = Hash21(i + float2(1, 1));
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            // Три октавы value noise — мягкие рваные пятна вместо чистого шума.
            half CloudDensity(float2 p)
            {
                half n = ValueNoise(p) * 0.5h;
                n += ValueNoise(p * 2.03h) * 0.25h;
                n += ValueNoise(p * 4.01h) * 0.125h;
                return n;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                float3 posWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionCS = TransformWorldToHClip(posWS);
                OUT.worldXZ = posWS.xz;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                float2 pan = _PanSpeed.xy * _Time.y;
                half density = CloudDensity(IN.worldXZ * _CloudScale + pan);
                half mask = smoothstep(_CloudThreshold, _CloudThreshold + _CloudSoftness, density);

                half alpha = mask * _CloudOpacity * _Alpha;
                return half4(_CloudColor.rgb, alpha);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
