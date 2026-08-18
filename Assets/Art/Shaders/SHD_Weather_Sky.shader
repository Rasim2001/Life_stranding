// Процедурное небо системы погоды.
//
// Проходы: один безымянный. LightMode-тега нет и быть не должно — URP не рисует небо
// собственным SRP-проходом, а делегирует движку через DrawSkyboxPass -> CreateSkyboxRendererList.
// Структура тегов сверена с Skybox/Procedural на живом редакторе:
// Queue=Background, RenderType=Background, PreviewType=Skybox, renderQueue=1000, один проход.
// Поэтому ShadowCaster / DepthOnly / DepthNormals / Meta / MotionVectors здесь отсутствуют
// не по недосмотру: небо не отбрасывает тень, не участвует в буфере глубины и не бейкается.
//
// Шейдер не знает про высотные полосы и время суток — он получает уже смешанные
// _ZenithColor/_HorizonColor/_GradientExponent (и облачные аналоги). Блендинг по высоте
// паука и суточный градиент внутри полосы считает WeatherSystem.Profiles.SkyBandBlender
// в C# (WeatherService.ApplySky()); добавление новой полосы или градиента — правка
// данных, а не этого файла.
//
// Облака рисуются прямо на куполе одним процедурным слоем: FBM (форма) + Voronoi
// (мелкая деталь), порог по покрытию. Один слой — не шесть, как у референсов вроде
// Cozy Weather: второй слой добавляется отдельной функцией, когда первый настроен
// и есть основания его усложнять. Купол один, отдельного меша под облака нет —
// скайбокс и так рисуется в Queue=Background поверх пустых пикселей, второй
// прозрачный меш поверх геометрии здесь чистый лишний fillrate.

Shader "SpiderRig/Weather/Sky"
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

        [Header(Clouds shared)]
        // Одна ручка на всю облачность: кучевые набирают силу первыми, грозовые
        // включаются позже по ступенчатому окну (см. StormGate во фрагменте).
        _CloudCoverage ("Cloud Coverage", Range(0, 1)) = 0.4
        // Частота 3D-шума на единичной сфере направлений — не UV, проекции нет.
        // Меньше 1 — одно пятно на полнеба, больше 6 — мелкая рябь.
        _CloudScale ("Cloud Scale", Range(0.1, 10)) = 2
        _CloudSoftness ("Cloud Softness", Range(0.01, 1)) = 0.35
        _WindSpeed ("Wind Speed", Range(0, 1)) = 0.05
        // Насколько непогода "приходит с горизонта": сдвигает эффективное покрытие
        // у горизонта вверх, поэтому фронт накатывает к зениту, а не проявляется разом.
        _CloudRollBias ("Weather Roll-in Bias", Range(0, 1)) = 0.25

        [Header(Cumulus)]
        _CloudColor ("Cumulus Lit Color", Color) = (1, 1, 1, 1)
        _CloudShadowColor ("Cumulus Shadow Color", Color) = (0.55, 0.6, 0.7, 1)
        _CloudHighlightColor ("Cumulus Sun Highlight", Color) = (1, 0.95, 0.85, 1)
        _CloudHighlightFalloff ("Sun Highlight Falloff", Range(1, 64)) = 8
        // Самозатенение: вторая выборка плотности со смещением к солнцу.
        _ShadowSampleDistance ("Self Shadow Distance", Range(0.01, 1)) = 0.25
        _ShadowDensity ("Self Shadow Density", Range(0, 8)) = 2.5

        [Header(Storm)]
        _StormColor ("Storm Lit Color", Color) = (0.62, 0.64, 0.70, 1)
        _StormShadowColor ("Storm Shadow Color", Color) = (0.22, 0.24, 0.30, 1)
        _StormScale ("Storm Scale", Range(0.1, 10)) = 1.3
        // Ниже какого покрытия грозовых нет вовсе; выше — набирают до единицы.
        _StormThreshold ("Storm Threshold", Range(0, 1)) = 0.55

        [Header(Cirrus)]
        _CirrusColor ("Cirrus Color", Color) = (1, 1, 1, 1)
        // Независимая ручка: перистые бывают и на чистом небе, к _CloudCoverage не привязаны.
        _CirrusCoverage ("Cirrus Coverage", Range(0, 1)) = 0.35
        _CirrusScale ("Cirrus Scale", Range(0.1, 10)) = 3
        _CirrusSpeed ("Cirrus Wind Speed", Range(0, 1)) = 0.12
        _CirrusOpacity ("Cirrus Opacity", Range(0, 1)) = 0.55
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Background"
            "RenderType" = "Background"
            "PreviewType" = "Skybox"
        }

        Cull Off
        ZWrite Off

        Pass
        {
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
                float3 dirWS : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // Глобал из WeatherService.RotateSunMoonPivot() — направление К солнцу,
            // не свойство материала: понадобится не только небу.
            half3 _SR_SunDirection;

            CBUFFER_START(UnityPerMaterial)
                half4 _ZenithColor;
                half4 _HorizonColor;
                half _GradientExponent;
                half _NightFactor;
                half4 _NightTint;
                half _StarDensity;

                half _CloudCoverage;
                half _CloudScale;
                half _CloudSoftness;
                half _WindSpeed;
                half _CloudRollBias;

                half4 _CloudColor;
                half4 _CloudShadowColor;
                half4 _CloudHighlightColor;
                half _CloudHighlightFalloff;
                half _ShadowSampleDistance;
                half _ShadowDensity;

                half4 _StormColor;
                half4 _StormShadowColor;
                half _StormScale;
                half _StormThreshold;

                half4 _CirrusColor;
                half _CirrusCoverage;
                half _CirrusScale;
                half _CirrusSpeed;
                half _CirrusOpacity;
            CBUFFER_END

            // Дешёвый хэш направления на звёзды — не объекты, шум по небу.
            // Не идеально равномерен на сфере, для стилизованного прототипа достаточно.
            half Hash13(half3 p)
            {
                p = frac(p * 0.1031h);
                p += dot(p, p.yzx + 33.33h);
                return frac((p.x + p.y) * p.z);
            }

            half Stars(half3 dir, half density)
            {
                half3 cell = floor(dir * 400.0h);
                half h = Hash13(cell);
                return step(1.0h - density, h);
            }

            // --- Облака: FBM (форма) + Voronoi (деталь), шум прямо в 3D -----------------
            //
            // Cozy разворачивает шум по UV физического меша-купола (equirectangular:
            // u=азимут, v=полярный угол) — у такой развёртки искажение уходит не в
            // горизонт, а в точку зенита (там сходятся все меридианы), плюс шов на
            // азимуте 0/360°, если шум не сшит по краю специально.
            //
            // У нас меша нет — и это развязывает руки: шум можно оценивать прямо
            // по 3D-вектору направления (dir * scale), а не проецировать его сперва
            // в 2D UV. Тогда нет ни полюса, ни шва, ни сжатия у горизонта в принципе —
            // это буквально шум на самой сфере, а не на её развёртке. Тот же трюк,
            // что уже используется для звёзд (Hash13 по 3D-направлению) — здесь то же
            // хэш-семейство, только со сглаживанием (value noise) вместо порога.

            half ValueNoise3D(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                float3 u = f * f * (3.0h - 2.0h * f);
                half c000 = Hash13(i + half3(0, 0, 0));
                half c100 = Hash13(i + half3(1, 0, 0));
                half c010 = Hash13(i + half3(0, 1, 0));
                half c110 = Hash13(i + half3(1, 1, 0));
                half c001 = Hash13(i + half3(0, 0, 1));
                half c101 = Hash13(i + half3(1, 0, 1));
                half c011 = Hash13(i + half3(0, 1, 1));
                half c111 = Hash13(i + half3(1, 1, 1));
                half x00 = lerp(c000, c100, u.x);
                half x10 = lerp(c010, c110, u.x);
                half x01 = lerp(c001, c101, u.x);
                half x11 = lerp(c011, c111, u.x);
                half y0 = lerp(x00, x10, u.y);
                half y1 = lerp(x01, x11, u.y);
                return lerp(y0, y1, u.z);
            }

            // Веса октав (0.5+0.25+0.125=0.875) не суммируются в 1 — без деления на эту
            // сумму итоговая FBM в среднем сидит около 0.44 и почти никогда не подходит
            // к 0.875. Порог покрытия ниже (density - (1-coverage)) рассчитан на диапазон
            // [0,1]: без нормализации даже Coverage=0.5 давал почти всегда mask=0 —
            // видимого неба без единого облака, что и было при первой проверке в Play Mode.
            half CloudShapeFBM(float3 p)
            {
                half n = ValueNoise3D(p) * 0.5h;
                n += ValueNoise3D(p * 2.03h) * 0.25h;
                n += ValueNoise3D(p * 4.01h) * 0.125h;
                return n / 0.875h;
            }

            // Billow: |2n-1| перевёрнутый — вместо равномерной дымки обычного FBM даёт
            // округлые "шапки", тот самый силуэт цветной капусты у кучевых. Классика
            // для cumulus, и это ровно то, что у Cozy Luxury нарисовано руками в текстуре.
            half BillowFBM(float3 p)
            {
                half n = (1.0h - abs(2.0h * ValueNoise3D(p) - 1.0h)) * 0.5h;
                n += (1.0h - abs(2.0h * ValueNoise3D(p * 2.03h) - 1.0h)) * 0.25h;
                n += (1.0h - abs(2.0h * ValueNoise3D(p * 4.01h) - 1.0h)) * 0.125h;
                return n / 0.875h;
            }

            // Хэш в точку внутри ячейки — джиттер узла 3D Voronoi-решётки.
            half3 Hash33(float3 p)
            {
                float3 q = float3(
                    dot(p, float3(127.1h, 311.7h, 74.7h)),
                    dot(p, float3(269.5h, 183.3h, 246.1h)),
                    dot(p, float3(113.5h, 271.9h, 124.6h)));
                return frac(sin(q) * 43758.5453h);
            }

            // Расстояние до ближайшего узла 3x3x3 окрестности — тот же Worley/Voronoi,
            // что был в 2D, продлённый на третье измерение.
            half Voronoi3D(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                half minDist = 8.0h;

                [unroll]
                for (int z = -1; z <= 1; z++)
                {
                    [unroll]
                    for (int y = -1; y <= 1; y++)
                    {
                        [unroll]
                        for (int x = -1; x <= 1; x++)
                        {
                            float3 neighbor = float3(x, y, z);
                            float3 nodePoint = Hash33(i + neighbor);
                            half dist = length(neighbor + nodePoint - f);
                            minDist = min(minDist, dist);
                        }
                    }
                }

                return minDist;
            }

            // Ветер сдвигает точку сэмплирования только по горизонтали (X/Z) — облака
            // едут по небу, а не "всплывают" вертикально.
            static const half3 kCloudWindDir = half3(0.7071h, 0.0h, 0.7071h);

            // --- Плотности ярусов ------------------------------------------------------
            // Каждая уже отремаплена в [0,1]: диапазоны эмпирические, замерены прогоном
            // копии этой же математики по 20k выборок (сумма октав концентрируется около
            // среднего и краёв [0,1] не достигает — без ремапа Coverage перестаёт
            // соответствовать видимой доле неба).

            // Константы ремапа у каждого яруса свои: billow и обычный FBM дают разные
            // распределения, и общий множитель тут молча ломает связь Coverage -> небо.
            // Замерено по 15k выборок, интервал взят по 1-му и 99-му процентилю так,
            // чтобы медиана легла ровно в 0.5 (тогда Coverage=0.5 ≈ полнеба).
            half CumulusDensity(float3 p)
            {
                half d = BillowFBM(p) * 0.72h
                    + (1.0h - saturate(Voronoi3D(p * 3.0h))) * 0.28h;
                return saturate((d - 0.39h) / 0.50h);
            }

            // Грозовые — крупнее и однороднее кучевых: обычный FBM без billow и без
            // Voronoi. Рваная "капуста" тут не нужна, нужна плотная тяжёлая масса.
            half StormDensity(float3 p)
            {
                half d = CloudShapeFBM(p);
                return saturate((d - 0.23h) / 0.54h);
            }

            // Перистые — анизотропный шум: сжимаем пространство выборки по X, отчего
            // пятна вытягиваются в длинные полосы. Дёшево: две октавы, без Voronoi
            // и без самозатенения — у тонких перистых объёма нет по определению.
            half CirrusDensity(float3 p)
            {
                float3 stretched = p * float3(0.22h, 1.6h, 1.0h);
                half d = ValueNoise3D(stretched) * 0.62h;
                d += ValueNoise3D(stretched * 2.7h) * 0.38h;
                return saturate((d - 0.20h) / 0.60h);
            }

            // --- Освещение -------------------------------------------------------------
            // Самозатенение одной дополнительной выборкой: смотрим плотность на шаг
            // в сторону солнца. Гуще там — значит этот участок закрыт от света.
            // Экспонента — закон Бугера-Ламберта-Бера, честное затухание в среде.
            //
            // Точка апгрейда: если одной выборки не хватит, здесь же меняется на цикл
            // 4-6 шагов вдоль sunDir с накоплением плотности — весь остальной шейдер
            // это не затрагивает.
            half CloudTransmittance(float3 p)
            {
                half densityTowardSun = CumulusDensity(p + _SR_SunDirection * _ShadowSampleDistance);
                return exp(-densityTowardSun * _ShadowDensity);
            }

            half3 ApplyClouds(half3 dir, half3 skyColor)
            {
                // Ниже линии горизонта облаков нет: узкий фейд, искажений проекции
                // здесь нет (шум 3D), но на открытой площадке иначе видно облака
                // под горизонтом — их место не там.
                half horizonFade = smoothstep(-0.05h, 0.05h, dir.y);
                if (horizonFade <= 0.0h)
                    return skyColor;

                // 0 в зените, 1 у горизонта. Приём из Cozy Luxury: у горизонта поднимаем
                // эффективное покрытие, поэтому фронт непогоды приходит оттуда и
                // накатывает к зениту, а не проступает по всему небу разом.
                half horizonDist = saturate(1.0h - dir.y);
                half coverage = saturate(_CloudCoverage + horizonDist * _CloudRollBias);

                half time = _Time.y;
                half sunDot = saturate(dot(dir, _SR_SunDirection));
                half highlight = pow(sunDot * 0.5h + 0.5h, _CloudHighlightFalloff);

                half3 color = skyColor;

                // --- Перистые: самый высокий ярус, значит рисуются первыми (дальше всех).
                // Свой ветер, заметно быстрее нижних — разная скорость по ярусам и даёт
                // ощущение глубины неба.
                float3 pCirrus = dir * _CirrusScale + kCloudWindDir * time * _CirrusSpeed;
                half cirrus = CirrusDensity(pCirrus);
                half cirrusCov = saturate(_CirrusCoverage + horizonDist * _CloudRollBias);
                half cirrusMask = smoothstep(0.0h, _CloudSoftness, cirrus - (1.0h - cirrusCov));
                cirrusMask *= _CirrusOpacity;
                color = lerp(color, _CirrusColor.rgb * lerp(1.0h, 1.15h, highlight), cirrusMask);

                // --- Кучевые: основной ярус с объёмом.
                float3 pCumulus = dir * _CloudScale + kCloudWindDir * time * _WindSpeed;
                half cumulus = CumulusDensity(pCumulus);
                half cumulusMask = smoothstep(0.0h, _CloudSoftness, cumulus - (1.0h - coverage));

                half transmittance = CloudTransmittance(pCumulus);
                half3 cumulusColor = lerp(_CloudShadowColor.rgb, _CloudColor.rgb, transmittance);
                // Подсветка солнцем поверх затенки, а не вместо: освещённая сторона
                // теплеет, теневая остаётся холодной.
                cumulusColor = lerp(cumulusColor, _CloudHighlightColor.rgb, highlight * transmittance);
                color = lerp(color, cumulusColor, cumulusMask);

                // --- Грозовые: нижний ярус, поверх остальных. Ступенчатое окно —
                // до _StormThreshold их нет вовсе, выше набирают до сплошной пелены.
                half stormGate = saturate((coverage - _StormThreshold) / max(1.0h - _StormThreshold, 0.01h));
                if (stormGate > 0.0h)
                {
                    float3 pStorm = dir * _StormScale + kCloudWindDir * time * _WindSpeed * 0.7h;
                    half storm = StormDensity(pStorm);
                    half stormMask = smoothstep(0.0h, _CloudSoftness, storm - (1.0h - stormGate));

                    half3 stormColor = lerp(_StormShadowColor.rgb, _StormColor.rgb, transmittance);
                    color = lerp(color, stormColor, stormMask);
                }

                return lerp(skyColor, color, horizonFade);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                // Небо рисуется мешем вокруг камеры: позиция вершины и есть направление взгляда.
                // Через ObjectToWorld, чтобы поворот скайбокса в настройках освещения учитывался.
                OUT.dirWS = TransformObjectToWorldDir(IN.positionOS.xyz, false);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                half3 dir = normalize(IN.dirWS);
                half upness = pow(saturate(dir.y), _GradientExponent);

                half3 dayColor = lerp(_HorizonColor.rgb, _ZenithColor.rgb, upness);
                half3 skyColor = lerp(dayColor, dayColor * _NightTint.rgb, _NightFactor);

                // Звёзды только в верхней полусфере и только когда действительно ночь —
                // не полагаемся на то, что density-порог сам уйдёт в 0, гасим множителем явно.
                half star = Stars(dir, _StarDensity) * _NightFactor * saturate(dir.y);
                skyColor += star.xxx;

                // Облака поверх звёзд, а не наоборот: непрозрачный слой должен их закрывать.
                skyColor = ApplyClouds(dir, skyColor);

                return half4(skyColor, 1.0h);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
