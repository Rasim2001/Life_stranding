// Небесный купол — настоящий меш (MSH_Weather_DomeSky, полная сфера), не скайбокс.
//
// До этого небо жило в SHD_Weather_SkyProcedural как скайбокс с гномонической
// проекцией направления в UV купола (dir.xz / (dir.y + curvature)) — формула,
// которая пыталась сымитировать купольность БЕЗ геометрии. Она давала либо
// бесконечное растяжение у горизонта (curvature=0), либо ощущение плоского потолка
// (curvature побольше) — купольность формулой не выходит в принципе, её даёт форма
// меша, как у Cozy (у них авторский FBX с разными мешами под небо и облака).
//
// Поэтому здесь направление взгляда — это НОРМАЛИЗОВАННАЯ OBJECT-SPACE ПОЗИЦИЯ
// ВЕРШИНЫ, не dirWS. Купол не вращается (WeatherDome.cs его только двигает и
// масштабирует), поэтому object-space позиция уже и есть направление от камеры —
// TransformObjectToWorldDir не нужен. Сама сплющенность/купольность "зашита"
// в форму меша (WeatherDomeMeshGenerator: heightFraction + yFlatten), шейдер
// об этом ничего не знает и знать не должен.
//
// Разделено с облаками на два шейдера (SHD_Weather_DomeClouds) — совмещённый файл
// на ~800 строк было неудобно читать и держать в голове. Общая математика шума —
// в Assets/Art/Shaders/WeatherNoise.hlsl.
//
// Настоящий меш, не скайбокс — поэтому нужен LightMode=UniversalForward, иначе URP
// не включит проход в отрисовку обычных объектов сцены.

Shader "SpiderRig/Weather/DomeSky"
{
    Properties
    {
        _ZenithColor  ("Zenith",  Color) = (0.20, 0.42, 0.75, 1)
        _HorizonColor ("Horizon", Color) = (0.55, 0.72, 0.85, 1)
        // Больше значение — плотнее горизонт прижат к линии горизонта.
        _GradientExponent ("Gradient Exponent", Range(0.1, 8)) = 1.5

        [Header(Night)]
        // 0 — день, 1 — ночь. Ведётся WeatherService из elevation солнца (по времени суток).
        _NightFactor ("Night Factor", Range(0, 1)) = 0
        _NightTint ("Night Tint", Color) = (0.12, 0.14, 0.28, 1)
        // Порог хэш-шума на звёзды: больше значение — больше звёзд.
        _StarDensity ("Star Density", Range(0, 0.05)) = 0.006

        [Header(Sun)]
        _SunColor ("Sun Disk Color", Color) = (1, 0.97, 0.88, 1)
        _SunSize ("Sun Size", Range(0.5, 8)) = 2.5
        _SunHaloColor ("Sun Halo Color", Color) = (1, 0.72, 0.42, 1)
        _SunHaloFalloff ("Sun Halo Falloff", Range(0, 1)) = 0.35
        _SunHaloIntensity ("Sun Halo Intensity", Range(0, 3)) = 1

        [Header(Moon)]
        _MoonFlareColor ("Moon Halo Color", Color) = (0.55, 0.66, 1, 1)
        _MoonFlareFalloff ("Moon Halo Falloff", Range(0, 1)) = 0.5
        _MoonFlareIntensity ("Moon Halo Intensity", Range(0, 3)) = 0.8

        [Header(Horizon fog)]
        // Небо у горизонта втапливается в цвет тумана, иначе на открытой воде виден
        // шов между наземным туманом и небом.
        _SkyFogColor ("Sky Fog Color", Color) = (0.78, 0.82, 0.86, 1)
        _SkyFogAmount ("Sky Fog Amount", Range(0, 1)) = 0.45
        _SkyFogHeight ("Sky Fog Height", Range(0.01, 1)) = 0.18
        // Сплющивает вертикаль при расчёте зарева на тумане — закатное пятно
        // растягивается по горизонтали, как в жизни (приём CZY_LightFlareSquish).
        _SkyFogGlowSquish ("Fog Glow Squish", Range(0.1, 6)) = 2.5

        [Header(Weather filter)]
        // Одна ручка на всё небо разом: обесцветить и притемнить под грозу.
        // Та же тройка свойств и в SHD_Weather_DomeClouds — иначе гроза обесцветит
        // небо, но не тронет облака.
        _FilterColor ("Filter Color", Color) = (1, 1, 1, 1)
        _FilterSaturation ("Filter Saturation", Range(-1, 1)) = 0
        _FilterValue ("Filter Value", Range(-1, 1)) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent-99"
        }

        // Камера физически внутри купола — рисуем изнанку. Как у Cozy (Cull Front
        // на их Skydome), не наша придумка.
        Cull Front
        ZWrite Off
        Blend Off

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "WeatherNoise.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 dirOS : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // Глобалы из WeatherService.RotateSunMoonPivot() — направления К светилам,
            // не свойства материала: нужны не только небу, но и облакам, позже туману и воде.
            half3 _SR_SunDirection;
            half3 _SR_MoonDirection;

            CBUFFER_START(UnityPerMaterial)
                half4 _ZenithColor;
                half4 _HorizonColor;
                half _GradientExponent;
                half _NightFactor;
                half4 _NightTint;
                half _StarDensity;

                half4 _SunColor;
                half _SunSize;
                half4 _SunHaloColor;
                half _SunHaloFalloff;
                half _SunHaloIntensity;

                half4 _MoonFlareColor;
                half _MoonFlareFalloff;
                half _MoonFlareIntensity;

                half4 _SkyFogColor;
                half _SkyFogAmount;
                half _SkyFogHeight;
                half _SkyFogGlowSquish;

                half4 _FilterColor;
                half _FilterSaturation;
                half _FilterValue;
            CBUFFER_END

            // --- Светила -----------------------------------------------------------------
            // Диск: порог по dot через smoothstep, а не тернарник — тернарник у Cozy
            // даёт ступенчатый край с видимой лесенкой. Ширина сглаживания берётся
            // долей от самого порога, поэтому не зависит от _SunSize.
            half3 SunDisk(half3 dir)
            {
                half thr = pow(_SunSize, 3.0h) * 0.0007h;
                half d = 1.0h - dot(dir, _SR_SunDirection);
                return _SunColor.rgb * (1.0h - smoothstep(thr * 0.8h, thr, d));
            }

            half3 SunHalo(half3 dir)
            {
                half sunDot = dot(dir, _SR_SunDirection);
                // saturate до pow обязателен: при отрицательном основании pow не определён.
                // Именно на этом у Cozy висит предупреждение компилятора в их же шейдере.
                half halo = pow(saturate(sunDot * 0.5h + 0.4h), _SunHaloFalloff * 40.0h + 5.0h);
                return _SunHaloColor.rgb * halo * _SunHaloIntensity;
            }

            half3 MoonHalo(half3 dir)
            {
                half moonDot = dot(dir, _SR_MoonDirection);
                half halo = pow(saturate(moonDot * 0.5h + 0.4h), _MoonFlareFalloff * 20.0h + 5.0h);
                return _MoonFlareColor.rgb * halo * _MoonFlareIntensity;
            }

            // --- Туман у горизонта ---------------------------------------------------------
            half3 ApplyHorizonFog(half3 dir, half3 color)
            {
                half fogT = 1.0h - saturate(dir.y / _SkyFogHeight);
                fogT = fogT * fogT * _SkyFogAmount;

                // Сплющиваем вертикаль перед замером угла на солнце: зарево на тумане
                // растягивается по горизонтали, а не остаётся круглым пятном.
                half3 squished = normalize(half3(dir.x, dir.y * _SkyFogGlowSquish, dir.z));
                half glow = pow(saturate(dot(squished, _SR_SunDirection) * 0.5h + 0.5h), 6.0h);

                half3 fogColor = lerp(_SkyFogColor.rgb, _SunHaloColor.rgb, glow * 0.6h);
                return lerp(color, fogColor, saturate(fogT));
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                // Купол не вращается — object-space позиция вершины уже направление
                // от камеры. WeatherDome.cs двигает и масштабирует transform, но не крутит.
                OUT.dirOS = IN.positionOS.xyz;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                half3 dir = normalize(IN.dirOS);
                half upness = pow(saturate(dir.y), _GradientExponent);

                half3 dayColor = lerp(_HorizonColor.rgb, _ZenithColor.rgb, upness);
                half3 skyColor = lerp(dayColor, dayColor * _NightTint.rgb, _NightFactor);

                // Звёзды только в верхней полусфере и только когда действительно ночь —
                // не полагаемся на то, что density-порог сам уйдёт в 0, гасим множителем явно.
                half star = Stars(dir, _StarDensity) * _NightFactor * saturate(dir.y);
                skyColor += star.xxx;

                skyColor += SunHalo(dir);
                skyColor += SunDisk(dir);
                skyColor += MoonHalo(dir);

                // Туман перед фильтром: он ближе всех к зрителю и топит в себе небо
                // у горизонта — иначе на воде виден шов.
                skyColor = ApplyHorizonFog(dir, skyColor);

                // Фильтр погоды поверх всего — одна ручка на весь кадр неба.
                skyColor = SR_ApplyWeatherFilter(skyColor, _FilterColor, _FilterSaturation, _FilterValue);

                return half4(skyColor, 1.0h);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
