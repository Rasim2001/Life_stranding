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
        // Ночь больше не отдельный множитель — она тёмный конец _ZenithColor/_HorizonColor,
        // как у Cozy. См. .scratch/sky-night-and-star-dome/spec.md, решение #1.
        // Звёзды — текстура звёздной карты на вращающейся сфере, не процедурный шум;
        // яркость ведётся тем же суточным градиентом, что и небо (решение #6).
        [NoScaleOffset] _StarDomeTexture ("Star Dome", CUBE) = "black" {}
        _StarColor ("Star Color", Color) = (1, 1, 1, 1)
        // Наклон звёздной сферы. См. решение #11 — авторская величина, не константа в коде.
        _Latitude ("Latitude", Range(-90, 90)) = 0

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
        // шов между наземным туманом и небом. Цвет больше не свойство материала —
        // приходит глобалом _SR_FogFarColor (тот же дальний стоп, что у тумана на
        // геометрии), см. .scratch/fog-generation-and-layers/spec.md, решение #13.
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
            // Ради одного _SR_FogFarColor: объявления глобалов тумана живут там же, где его
            // математика (тикет 04), чтобы не разъезжались по типу и по имени между шейдерами.
            #include "WeatherFog.hlsl"

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

            // Глобалы из WeatherService.RotateSunMoonPivot() — направления К светилам
            // и время суток, не свойства материала: нужны не только небу, но и облакам,
            // позже туману и воде.
            half3 _SR_SunDirection;
            half3 _SR_MoonDirection;
            half _SR_TimeOfDay01;

            // Дальний стоп тумана (_SR_FogFarColor) — тот же цвет, в который втапливается
            // горизонтная дымка купола. Объявлен в WeatherFog.hlsl выше, не здесь: глобал,
            // общий с fullscreen-пассом, водой и стеклом, и второе объявление рядом с общим
            // означало бы redefinition при любой попытке подключить сюда его математику.
            samplerCUBE _StarDomeTexture;

            CBUFFER_START(UnityPerMaterial)
                half4 _ZenithColor;
                half4 _HorizonColor;
                half _GradientExponent;
                half4 _StarColor;
                half _Latitude;

                half4 _SunColor;
                half _SunSize;
                half4 _SunHaloColor;
                half _SunHaloFalloff;
                half _SunHaloIntensity;

                half4 _MoonFlareColor;
                half _MoonFlareFalloff;
                half _MoonFlareIntensity;

                half _SkyFogAmount;
                half _SkyFogHeight;
                half _SkyFogGlowSquish;

                half4 _FilterColor;
                half _FilterSaturation;
                half _FilterValue;
            CBUFFER_END

            // --- Звёздная сфера -----------------------------------------------------------
            // Купол не вращается (см. комментарий вверху файла), поэтому крутим не геометрию,
            // а направление выборки перед сэмплом кубмапы — два поворота, как у Cozy, но без
            // их годовой доли (тут нет понятия года, см. .scratch/sky-night-and-star-dome/
            // spec.md, решение #10). Долгота — оборот за сутки вокруг мировой вертикали;
            // широта — постоянный наклон оси, задаёт, как высоко ходит полюс над горизонтом.
            half3 RotateY(half3 v, half angle)
            {
                half s = sin(angle);
                half c = cos(angle);
                return half3(v.x * c + v.z * s, v.y, -v.x * s + v.z * c);
            }

            half3 RotateX(half3 v, half angle)
            {
                half s = sin(angle);
                half c = cos(angle);
                return half3(v.x, v.y * c - v.z * s, v.y * s + v.z * c);
            }

            half3 StarDir(half3 dir)
            {
                half spin = _SR_TimeOfDay01 * 6.28318530718h;
                half3 spun = RotateY(dir, spin);
                return RotateX(spun, radians(_Latitude));
            }

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

                half3 fogColor = lerp(_SR_FogFarColor.rgb, _SunHaloColor.rgb, glow * 0.6h);
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

                // Ночь больше не отдельный множитель — просто тёмный конец градиента,
                // как у Cozy (см. решение #1). Что нарисовано в ключах на нужное время
                // суток, то и видно, без домножения сверху.
                half3 skyColor = lerp(_HorizonColor.rgb, _ZenithColor.rgb, upness);

                // Звёзды — только в верхней полусфере (та же причина, что раньше: под
                // куполом нет смысла их рисовать). Яркость целиком из _StarColor — его
                // суточный градиент сам гасит их днём, отдельного гейта по ночи не нужно.
                half3 starColor = texCUBE(_StarDomeTexture, StarDir(dir)).rgb * _StarColor.rgb;
                skyColor += starColor * saturate(dir.y);

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
