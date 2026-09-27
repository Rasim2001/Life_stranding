// Диск луны — отдельный объект (встроенная Unity Sphere), не часть купола неба.
//
// У кози луна тоже отдельный меш со своим шейдером (Stylized Moon.shader), а не
// нарисована внутри неба, как солнце (солнце там процедурный диск в шейдере неба,
// объекта у него нет вовсе). Причина разницы: у луны нужна фаза — реальная нормаль
// поверхности и dot(normal, sunDir), которых у процедурного диска на плоском куполе
// просто нет.
//
// Фаза идёт в АЛЬФУ, не в цвет (приём кози): освещённая половина сферы непрозрачна,
// теневая гаснет до нуля. Поэтому не нужен отдельный проход тени/освещения — весь
// эффект получается одним dot-произведением по касательной нормали.
//
// Сейчас солнце и луна разведены на 180° на одном пивоте (WeatherService) — то есть
// у полусферы, обращённой к камере, dot(normal,sunDir) почти везде ≈ 1: получится
// всегда полная луна. Математика фазы всё равно настоящая (не заглушка) — когда луна
// получит свою орбиту независимо от солнца, фазы заработают без правки шейдера.

Shader "SpiderRig/Weather/Moon"
{
    Properties
    {
        _MoonTex ("Moon Texture (RGB albedo, A disk mask)", 2D) = "white" {}
        _MoonNormalMap ("Moon Normal Map", 2D) = "bump" {}
        _MoonColor ("Moon Color", Color) = (0.75, 0.8, 0.9, 1)

        [Header(Phase)]
        // У кози это _AlphaScale/_AlphaOffset на их "Stylized Moon" материале (6.13/0).
        // Больше Sharpness — резче терминатор; Offset сдвигает его, вплоть до вечной
        // полной луны при большом положительном значении.
        _PhaseSharpness ("Phase Sharpness", Range(0.5, 16)) = 6.13
        _PhaseOffset ("Phase Offset", Range(-1, 1)) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent-52"
        }

        // Смотрим на луну снаружи (в отличие от куполов, где камера внутри) —
        // обычная отбраковка задней стороны.
        Cull Back
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // Глобал из WeatherService.RotateSunMoonPivot() — направление К солнцу,
            // тот же, что использует небо и облака.
            half3 _SR_SunDirection;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half3 tangentWS : TEXCOORD2;
                half3 bitangentWS : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _MoonTex_ST;
                half4 _MoonColor;
                half _PhaseSharpness;
                half _PhaseOffset;
            CBUFFER_END

            TEXTURE2D(_MoonTex);
            SAMPLER(sampler_MoonTex);
            TEXTURE2D(_MoonNormalMap);
            SAMPLER(sampler_MoonNormalMap);

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MoonTex);

                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.tangentWS = TransformObjectToWorldDir(IN.tangentOS.xyz);
                half tangentSign = IN.tangentOS.w * GetOddNegativeScale();
                OUT.bitangentWS = cross(OUT.normalWS, OUT.tangentWS) * tangentSign;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                half3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_MoonNormalMap, sampler_MoonNormalMap, IN.uv));
                half3x3 tangentToWorld = half3x3(IN.tangentWS, IN.bitangentWS, IN.normalWS);
                half3 normalWS = normalize(mul(normalTS, tangentToWorld));

                half phase = saturate(dot(normalWS, _SR_SunDirection) * _PhaseSharpness + _PhaseOffset);

                half4 tex = SAMPLE_TEXTURE2D(_MoonTex, sampler_MoonTex, IN.uv);
                half3 color = _MoonColor.rgb * tex.rgb;
                half alpha = _MoonColor.a * tex.a * phase;

                return half4(color, alpha);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
