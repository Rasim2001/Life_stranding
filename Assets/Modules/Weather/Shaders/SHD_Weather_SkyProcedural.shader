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

Shader "SpiderRig/Weather/SkyProcedural"
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
        // Частота базового шума. Меньше 1 — одно пятно на полнеба, больше 6 — мелкая рябь.
        _CloudScale ("Cloud Scale", Range(0.1, 10)) = 2
        _CloudSoftness ("Cloud Softness", Range(0.01, 1)) = 0.35
        _WindSpeed ("Wind Speed", Range(0, 1)) = 0.05
        // Насколько непогода "приходит с горизонта": сдвигает эффективное покрытие
        // у горизонта вверх, поэтому фронт накатывает к зениту, а не проявляется разом.
        _CloudRollBias ("Weather Roll-in Bias", Range(0, 1)) = 0.25
        // 0 — честная гномоника (dir.xz/dir.y): небо как пласт над головой, растягивается
        // к горизонту, как у настоящего плоского слоя облаков снизу. Большие значения
        // приближаются к нашему прежнему изотропному куполу без растяжения.
        _DomeCurvature ("Dome Curvature", Range(0.05, 2)) = 0.35

        [Header(Cumulus detail)]
        // Недостающий у нас слой из Cozy: 3-октавный Voronoi-FBM поверх базовой формы,
        // на порядок мельче её. Без него силуэт гладкий и аморфный — с ним рваный край
        // "цветной капусты". Amount=0 отключает слой целиком (сравнение "было/стало").
        // Диапазон и дефолт подобраны замером: при Amount=1 (внутренний множитель 0.20)
        // отклик Coverage->видимая доля неба остаётся монотонным по всему диапазону;
        // на Amount=2.5 (пробовали при калибровке) уже 75-й процентиль плотности
        // упирался в 1.0 — деталь захлёстывала небо вместо того чтобы рвать края.
        _CloudDetailScale ("Detail Scale", Range(0.1, 10)) = 1
        _CloudDetailAmount ("Detail Amount", Range(0, 2)) = 1

        [Header(Cumulus)]
        _CloudColor ("Cumulus Lit Color", Color) = (1, 1, 1, 1)
        _CloudShadowColor ("Cumulus Shadow Color", Color) = (0.55, 0.6, 0.7, 1)
        _CloudHighlightColor ("Cumulus Sun Highlight", Color) = (1, 0.95, 0.85, 1)
        _CloudHighlightFalloff ("Sun Highlight Falloff", Range(1, 64)) = 8
        // Самозатенение: вторая выборка плотности со смещением к солнцу.
        _ShadowSampleDistance ("Self Shadow Distance", Range(0.01, 1)) = 0.25
        _ShadowDensity ("Self Shadow Density", Range(0, 8)) = 2.5
        // Второй голос Voronoi как толща: темнит плотные куски и гасит на них ободок.
        _CloudThickness ("Cloud Thickness", Range(0, 1)) = 0.5

        [Header(Storm)]
        _StormColor ("Storm Lit Color", Color) = (0.62, 0.64, 0.70, 1)
        _StormShadowColor ("Storm Shadow Color", Color) = (0.22, 0.24, 0.30, 1)
        _StormScale ("Storm Scale", Range(0.1, 10)) = 1.3
        // Ниже какого покрытия грозовых нет вовсе; выше — набирают до единицы.
        _StormThreshold ("Storm Threshold", Range(0, 1)) = 0.55
        // Направление на грозовой фронт: гроза стоит с одной стороны неба, а не
        // ровным слоем по всему куполу (приём из Cozy Desktop, CZY_StormDirection).
        _StormDirection ("Storm Direction", Vector) = (1, 0.15, 0, 0)
        _StormFrontFalloff ("Storm Front Falloff", Range(0.5, 16)) = 3

        [Header(Border light transport)]
        // Радиальный член: облака у горизонта ловят свет иначе, чем над головой.
        // У Cozy радиус берётся из UV купола; у нас развёртки нет, считаем от dir.y.
        _BorderEffect ("Border Effect", Range(0, 1)) = 0.35
        _BorderHeight ("Border Height", Range(0.2, 6)) = 2

        [Header(Cirrus)]
        _CirrusColor ("Cirrus Color", Color) = (1, 1, 1, 1)
        // Независимая ручка: перистые бывают и на чистом небе, к _CloudCoverage не привязаны.
        _CirrusCoverage ("Cirrus Coverage", Range(0, 1)) = 0.35
        _CirrusScale ("Cirrus Scale", Range(0.1, 10)) = 3
        _CirrusSpeed ("Cirrus Wind Speed", Range(0, 1)) = 0.12
        _CirrusOpacity ("Cirrus Opacity", Range(0, 1)) = 0.55

        [Header(Sun)]
        // Диск солнца в самом небе. До этого солнце у нас было только источником света,
        // и в кадре его не было вовсе — самый заметный пробел против Cozy.
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
        // Одна ручка на всё небо разом: обесцветить и притемнить под грозу, вместо
        // правки десятка цветов по отдельности. Ключевой приём Cozy для панели погоды.
        _FilterColor ("Filter Color", Color) = (1, 1, 1, 1)
        _FilterSaturation ("Filter Saturation", Range(-1, 1)) = 0
        _FilterValue ("Filter Value", Range(-1, 1)) = 0
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
            // По умолчанию таргет 2.5 — четыре развёрнутых 27-итерационных цикла Voronoi
            // и полный Simplex во фрагменте это не проходят на урезанных фича-левелах.
            #pragma target 3.0

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

            // Глобалы из WeatherService.RotateSunMoonPivot() — направления К светилам,
            // не свойства материала: нужны не только небу, позже туману и воде.
            half3 _SR_SunDirection;
            half3 _SR_MoonDirection;

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
                half _DomeCurvature;
                half _CloudDetailScale;
                half _CloudDetailAmount;

                half4 _CloudColor;
                half4 _CloudShadowColor;
                half4 _CloudHighlightColor;
                half _CloudHighlightFalloff;
                half _ShadowSampleDistance;
                half _ShadowDensity;
                half _CloudThickness;

                half4 _StormColor;
                half4 _StormShadowColor;
                half _StormScale;
                half _StormThreshold;
                half4 _StormDirection;
                half _StormFrontFalloff;

                half _BorderEffect;
                half _BorderHeight;

                half4 _CirrusColor;
                half _CirrusCoverage;
                half _CirrusScale;
                half _CirrusSpeed;
                half _CirrusOpacity;

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

            // --- Simplex noise (Ashima Arts / Stefan Gustavson, McEwan et al.) ----------
            // Тот же шум, что у Cozy в Stylized Clouds (Desktop), но их версия 2D под UV
            // купола, а здесь 3D под наше направление взгляда. Публичная реализация
            // (MIT), перенесена без изменения математики.
            //
            // Почему именно он, а не наш прежний value noise: у Simplex непрерывная
            // производная, форма получается гладкой сама по себе. Value noise
            // интерполирует случайные значения по углам ячейки билинейно, и в силуэте
            // проступает решётка — billow её маскирует, но не убирает. Это разница
            // в типе шума, ползунками она не лечится.
            //
            // Всё во float: внутри модуль 289 и множитель 42, в half это развалится.

            float3 SR_mod289(float3 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
            float4 SR_mod289(float4 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
            float4 SR_permute(float4 x) { return SR_mod289(((x * 34.0) + 1.0) * x); }
            float4 SR_taylorInvSqrt(float4 r) { return 1.79284291400159 - 0.85373472095314 * r; }

            float SR_snoise(float3 v)
            {
                const float2 C = float2(1.0 / 6.0, 1.0 / 3.0);
                const float4 D = float4(0.0, 0.5, 1.0, 2.0);

                float3 i  = floor(v + dot(v, C.yyy));
                float3 x0 = v - i + dot(i, C.xxx);

                float3 g = step(x0.yzx, x0.xyz);
                float3 l = 1.0 - g;
                float3 i1 = min(g.xyz, l.zxy);
                float3 i2 = max(g.xyz, l.zxy);

                float3 x1 = x0 - i1 + C.xxx;
                float3 x2 = x0 - i2 + C.yyy;
                float3 x3 = x0 - D.yyy;

                i = SR_mod289(i);
                float4 p = SR_permute(SR_permute(SR_permute(
                             i.z + float4(0.0, i1.z, i2.z, 1.0))
                           + i.y + float4(0.0, i1.y, i2.y, 1.0))
                           + i.x + float4(0.0, i1.x, i2.x, 1.0));

                float n_ = 0.142857142857;
                float3 ns = n_ * D.wyz - D.xzx;

                float4 j = p - 49.0 * floor(p * ns.z * ns.z);

                float4 x_ = floor(j * ns.z);
                float4 y_ = floor(j - 7.0 * x_);

                float4 x = x_ * ns.x + ns.yyyy;
                float4 y = y_ * ns.x + ns.yyyy;
                float4 h = 1.0 - abs(x) - abs(y);

                float4 b0 = float4(x.xy, y.xy);
                float4 b1 = float4(x.zw, y.zw);

                float4 s0 = floor(b0) * 2.0 + 1.0;
                float4 s1 = floor(b1) * 2.0 + 1.0;
                float4 sh = -step(h, 0.0);

                float4 a0 = b0.xzyw + s0.xzyw * sh.xxyy;
                float4 a1 = b1.xzyw + s1.xzyw * sh.zzww;

                float3 p0 = float3(a0.xy, h.x);
                float3 p1 = float3(a0.zw, h.y);
                float3 p2 = float3(a1.xy, h.z);
                float3 p3 = float3(a1.zw, h.w);

                float4 norm = SR_taylorInvSqrt(float4(dot(p0, p0), dot(p1, p1), dot(p2, p2), dot(p3, p3)));
                p0 *= norm.x;
                p1 *= norm.y;
                p2 *= norm.z;
                p3 *= norm.w;

                float4 m = max(0.6 - float4(dot(x0, x0), dot(x1, x1), dot(x2, x2), dot(x3, x3)), 0.0);
                m = m * m;
                return 42.0 * dot(m * m, float4(dot(p0, x0), dot(p1, x1), dot(p2, x2), dot(p3, x3)));
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

            // Voronoi ровно как у Cozy, два отличия от нашей прежней версии:
            //   1) точка ячейки прогоняется через sin(o*2PI)*0.5+0.5, а не берётся
            //      сырым хэшем — распределение узлов другое, ячейки ровнее;
            //   2) возвращается 0.5*dot(r,r), то есть ПОЛОВИНА КВАДРАТА расстояния,
            //      а не length(r). Падение к центру ячейки квадратичное, край мягче.
            half Voronoi3D(float3 p)
            {
                float3 n = floor(p);
                float3 f = frac(p);
                half F1 = 8.0h;

                [unroll]
                for (int z = -1; z <= 1; z++)
                {
                    [unroll]
                    for (int y = -1; y <= 1; y++)
                    {
                        [unroll]
                        for (int x = -1; x <= 1; x++)
                        {
                            float3 g = float3(x, y, z);
                            float3 o = Hash33(n + g);
                            o = sin(o * 6.2831h) * 0.5h + 0.5h;
                            float3 r = f - g - o;
                            half d = 0.5h * dot(r, r);
                            F1 = min(F1, d);
                        }
                    }
                }

                return F1;
            }

            // --- Проекция купола из направления, без меша ------------------------------
            // Гномоника с ограничителем: domeUV = dir.xz / (dir.y + curvature).
            // При curvature -> 0 это тот самый dir.xz/dir.y, что раньше уходил
            // в бесконечность у горизонта — знаменатель просто больше не даёт нуля.
            // Растяжение к горизонту при этом остаётся, и это не баг: так выглядит
            // снизу любой настоящий плоский слой облаков в перспективе. Изотропный
            // 3D-шум по сфере направлений этого не давал в принципе — отсюда шарообразные
            // комки вместо ощущения пласта над головой.
            float2 SR_DomeUV(half3 dir)
            {
                return dir.xz / (dir.y + _DomeCurvature);
            }

            // Один преобладающий ветер на всё небо, а не расходящиеся по слоям векторы.
            // Раньше база и Voronoi кучевых ехали под 97° друг к другу — и поскольку
            // у нас (в отличие от Cozy) второй голос Voronoi реально управляет толщой
            // и гасит солнечный ободок, тёмные пятна визуально ползли поперёк облака,
            // а не вместе с ним. (0.6,-0.8) уже единичный вектор.
            static const half2 kWindDir = half2(0.6h, -0.8h);

            // Слои разнесены по третьей координате шума — один и тот же кусок шумового
            // поля не повторяется в кучевых, грозовых и перистых при равном масштабе.
            static const half kCumulusLayerZ = 0.0h;
            static const half kStormLayerZ = 37.0h;
            static const half kCirrusLayerZ = 91.0h;

            // Ветер сдвигает domeUV ДО умножения на масштаб — поэтому скорость облаков
            // не зависит от их размера. Было наоборот (dir*scale + wind*t): поднял
            // _CloudScale, чтобы сделать облака мельче, и они тут же поехали медленнее —
            // артист-ручки были незаметно связаны друг с другом.
            float3 SR_CloudSamplePoint(float2 domeUV, half layerZ, half scale, half windSpeed)
            {
                domeUV += kWindDir * (_Time.y * windSpeed);
                return float3(domeUV.x, layerZ, domeUV.y) * scale;
            }

            // --- Плотности ярусов ------------------------------------------------------
            // Частотные отношения Voronoi взяты у Cozy буквально: база 100/Scale,
            // Voronoi-A 140/Scale, Voronoi-B 500/Scale — то есть 1.0 : 1.4 : 5.0.
            static const half kVoroDetailFreq = 1.4h;
            static const half kVoroThickFreq = 5.0h;

            // 3-октавный Voronoi-FBM поверх базовой формы — недостающий у нас слой
            // из Cozy (CZY_DetailScale/DetailAmount). У них он на порядок мельче базы
            // и замешивается в плотность ДО решения об альфе — это половина их силуэта,
            // а не украшение поверх готовой формы. Без него силуэт гладкий и аморфный.
            half DetailVoronoiFBM(float3 p)
            {
                half voro = 0.0h;
                half fade = 0.5h;
                [unroll]
                for (int o = 0; o < 3; o++)
                {
                    voro += fade * saturate(Voronoi3D(p));
                    p *= 2.0h;
                    fade *= 0.5h;
                }
                return voro / 0.875h; // сумма весов трёх октав (0.5+0.25+0.125)
            }

            // Общая перенормировка плотности на покрытие: после вычитания порога
            // делим на само покрытие, поэтому остаток снова занимает весь [0,1].
            // Без этого при низком покрытии над порогом остаётся узкая полоска
            // значений, и затенка на редких облаках вырождается в плоское пятно.
            half CloudRel(half density, half coverage)
            {
                return saturate((density - (1.0h - coverage)) / max(coverage, 0.001h));
            }

            // Возвращает сырую плотность (до порога покрытия, который накладывает
            // вызывающая сторона через CloudRel) и вторым каналом — толщину. Второй
            // голос Voronoi у Cozy идёт не в плотность, а отдельно в CloudThicknessDetails:
            // он темнит толщу и гасит на ней солнечный ободок.
            half CumulusDensity(float2 domeUV, out half thickness)
            {
                float3 pBase = SR_CloudSamplePoint(domeUV, kCumulusLayerZ, _CloudScale, _WindSpeed);

                // ФИКС регрессии: Simplex [-1,1]->[0,1] даёт медиану 0.5, но σ≈0.10 —
                // 99% значений сидят в [0.24,0.76], а не заполняют [0,1]. Без ремапа
                // Coverage почти не влиял на видимую долю неба, а самозатенение ниже
                // выходило плоским (transmittance менялся только в 0.15..0.37 на всём
                // облаке — освещённая и теневая стороны почти не отличались).
                half baseNoise = saturate((SR_snoise(pBase) * 0.5h + 0.5h - 0.30h) / 0.40h);

                half voroA = saturate(Voronoi3D(pBase * kVoroDetailFreq));
                thickness = saturate(Voronoi3D(pBase * kVoroThickFreq));
                half shape = min(baseNoise, 1.0h - voroA);

                half detailFreq = kVoroThickFreq * 2.4h * _CloudDetailScale;
                half detail = 1.0h - DetailVoronoiFBM(pBase * detailFreq);
                // 0.20 — калибровочный потолок вклада при Amount=1, замерен прогоном
                // точной копии этой формулы по 12k направлений: без него (голый ремап
                // в [0,1]) уже при Amount=0.5 75-й процентиль плотности упирался в 1.0 —
                // деталь забивала небо целиком вместо тонкой порчи края.
                detail = saturate((detail - 0.55h) / 0.35h) * 0.20h * _CloudDetailAmount;

                return saturate(shape + detail);
            }

            // Грозовые — крупнее и однороднее кучевых: обычный FBM без billow и без
            // Voronoi-детали. Рваная "капуста" тут не нужна, нужна плотная тяжёлая масса.
            half StormDensity(float2 domeUV)
            {
                float3 p = SR_CloudSamplePoint(domeUV, kStormLayerZ, _StormScale, _WindSpeed * 0.7h);
                half d = CloudShapeFBM(p);
                return saturate((d - 0.23h) / 0.54h);
            }

            // Перистые — анизотропный шум: сжимаем пространство выборки по X, отчего
            // пятна вытягиваются в длинные полосы. Дёшево: две октавы, без Voronoi
            // и без самозатенения — у тонких перистых объёма нет по определению.
            half CirrusDensity(float2 domeUV)
            {
                float3 p = SR_CloudSamplePoint(domeUV, kCirrusLayerZ, _CirrusScale, _CirrusSpeed);
                float3 stretched = p * half3(0.22h, 1.0h, 1.0h);
                half d = ValueNoise3D(stretched) * 0.62h;
                d += ValueNoise3D(stretched * 2.7h) * 0.38h;
                return saturate((d - 0.20h) / 0.60h);
            }

            // --- Освещение -------------------------------------------------------------
            // Самозатенение одной дополнительной выборкой: смотрим плотность на шаг
            // в сторону солнца в domeUV-пространстве. Гуще там — значит этот участок
            // закрыт от света. Экспонента — закон Бугера-Ламберта-Бера, честное
            // затухание в среде.
            //
            // Точка апгрейда: если одной выборки не хватит, здесь же меняется на цикл
            // 4-6 шагов вдоль sunDir с накоплением плотности — весь остальной шейдер
            // это не затрагивает.
            half CloudTransmittance(float2 domeUV, half coverage)
            {
                float2 sunUV = SR_DomeUV(_SR_SunDirection);
                float2 sunStep = normalize(sunUV - domeUV + 1e-4h) * _ShadowSampleDistance;
                half ignoredThickness;
                half raw = CumulusDensity(domeUV + sunStep, ignoredThickness);
                // ФИКС П.1: та же перенормировка, что и у видимой плотности. Раньше сюда
                // шла сырая CumulusDensity — самозатенение работало на диапазоне
                // [0.24,0.76] вместо [0,1] и было почти незаметно на всём облаке.
                return exp(-CloudRel(raw, coverage) * _ShadowDensity);
            }

            // Гроза лерпилась по transmittance кучевого поля — другой масштаб, другой
            // слой шума. Тёмные полосы ползли по грозе в отрыве от её собственного
            // силуэта. Считаем самозатенение от её же плотности.
            half StormTransmittance(float2 domeUV, half stormGate)
            {
                float2 sunUV = SR_DomeUV(_SR_SunDirection);
                float2 sunStep = normalize(sunUV - domeUV + 1e-4h) * _ShadowSampleDistance;
                half raw = StormDensity(domeUV + sunStep);
                return exp(-CloudRel(raw, stormGate) * _ShadowDensity);
            }

            // --- Светила ----------------------------------------------------------------
            // Диск: порог по dot, как у Cozy, но через smoothstep, а не тернарник.
            // У них край диска ступенчатый и на нём видна лесенка; ширина сглаживания
            // берётся долей от самого порога, поэтому не зависит от _SunSize.
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

            // --- Туман у горизонта --------------------------------------------------------
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

            // --- Фильтр погоды -------------------------------------------------------------
            half3 RGBToHSV(half3 c)
            {
                half4 K = half4(0.0h, -1.0h / 3.0h, 2.0h / 3.0h, -1.0h);
                half4 p = lerp(half4(c.bg, K.wz), half4(c.gb, K.xy), step(c.b, c.g));
                half4 q = lerp(half4(p.xyw, c.r), half4(c.r, p.yzx), step(p.x, c.r));
                half d = q.x - min(q.w, q.y);
                // Cozy берёт 1e-10 — в half это ноль, то есть деление на ноль.
                // Для half нужен заметно больший эпсилон.
                half e = 1.0e-4h;
                return half3(abs(q.z + (q.w - q.y) / (6.0h * d + e)), d / (q.x + e), q.x);
            }

            half3 HSVToRGB(half3 c)
            {
                half4 K = half4(1.0h, 2.0h / 3.0h, 1.0h / 3.0h, 3.0h);
                half3 p = abs(frac(c.xxx + K.xyz) * 6.0h - K.www);
                return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);
            }

            // Cozy прогоняет фильтр через каждый цвет по отдельности (десяток RGB->HSV
            // за пиксель). Применяем один раз к итогу: результат для равномерного тона
            // тот же, а преобразований одно вместо десяти.
            half3 ApplyWeatherFilter(half3 c)
            {
                half3 hsv = RGBToHSV(c);
                hsv.y = saturate(hsv.y + _FilterSaturation);
                // Яркость МНОЖИМ, а не вычитаем. У Cozy здесь сложение (hsv.z + Value),
                // и на уже тёмных цветах (тень грозовых, V~0.3) вычитание того же
                // порядка обнуляет яркость — в кадре появляются чёрные дыры.
                // Множитель гасит пропорционально и до чёрного доходит только при -1.
                hsv.z = max(hsv.z * (1.0h + _FilterValue), 0.0h);
                return HSVToRGB(hsv) * _FilterColor.rgb;
            }

            half3 ApplyClouds(half3 dir, half3 skyColor)
            {
                // Ниже линии горизонта облаков нет: узкий фейд, искажений проекции
                // здесь нет (шум 3D на domeUV), но на открытой площадке иначе видно
                // облака под горизонтом — их место не там.
                half horizonFade = smoothstep(-0.05h, 0.05h, dir.y);
                if (horizonFade <= 0.0h)
                    return skyColor;

                float2 domeUV = SR_DomeUV(dir);

                // 0 в зените, 1 у горизонта. Приём из Cozy Luxury: у горизонта поднимаем
                // эффективное покрытие, поэтому фронт непогоды приходит оттуда и
                // накатывает к зениту, а не проступает по всему небу разом.
                half horizonDist = saturate(1.0h - dir.y);
                half coverage = saturate(_CloudCoverage + horizonDist * _CloudRollBias);

                half sunDot = saturate(dot(dir, _SR_SunDirection));
                half highlight = pow(sunDot * 0.5h + 0.5h, _CloudHighlightFalloff);

                half3 color = skyColor;

                // --- Перистые: самый высокий ярус, рисуются первыми (дальше всех).
                half cirrus = CirrusDensity(domeUV);
                half cirrusCov = saturate(_CirrusCoverage + horizonDist * _CloudRollBias);
                half cirrusRel = CloudRel(cirrus, cirrusCov);
                half cirrusMask = smoothstep(0.0h, _CloudSoftness, cirrusRel) * _CirrusOpacity;
                color = lerp(color, _CirrusColor.rgb * lerp(1.0h, 1.15h, highlight), cirrusMask);

                // --- Кучевые: основной ярус с объёмом.
                half thickness;
                half cumulus = CumulusDensity(domeUV, thickness);
                half cumulusRel = CloudRel(cumulus, coverage);
                half cumulusMask = smoothstep(0.0h, _CloudSoftness, cumulusRel);

                // Самозатенение считаем, только если маска вообще что-то покажет —
                // иначе это два лишних Simplex + четыре 27-тапных Voronoi на пиксель зря.
                half transmittance = (cumulusMask > 0.001h) ? CloudTransmittance(domeUV, coverage) : 1.0h;

                half3 cumulusColor = lerp(_CloudShadowColor.rgb, _CloudColor.rgb, transmittance);
                // Подсветка солнцем поверх затенки, а не вместо: освещённая сторона
                // теплеет, теневая остаётся холодной. Толща гасит ободок — приём Cozy:
                // на плотном куске света не видно, он весь рассеялся внутри.
                cumulusColor = lerp(cumulusColor, _CloudHighlightColor.rgb,
                    highlight * transmittance * (1.0h - thickness));
                // Толща темнит УЖЕ ЗАТЕНЁННЫЙ цвет, а не константу от освещённого —
                // раньше лерп шёл к _CloudColor*0.566, и в тени это подмешивало красный
                // вместо того чтобы просто темнить.
                cumulusColor = lerp(cumulusColor, cumulusColor * 0.566h, thickness * _CloudThickness);

                // Борта: радиальный вклад в перенос света, сильнее у горизонта и только
                // там, где есть плотность. Даёт глубину — облака над головой и у горизонта
                // освещены по-разному. Зажат явно — раньше комментарий обещал зажим,
                // которого не было в коде.
                half border = min(pow(horizonDist, _BorderHeight) * _BorderEffect * cumulusRel, 0.5h);
                cumulusColor += border.xxx * _CloudHighlightColor.rgb;

                color = lerp(color, cumulusColor, cumulusMask);

                // --- Грозовые: нижний ярус, поверх остальных. Ступенчатое окно —
                // до _StormThreshold их нет вовсе, выше набирают до сплошной пелены.
                half stormGate = saturate((coverage - _StormThreshold) / max(1.0h - _StormThreshold, 0.01h));

                // Фронт направленный: гроза с одной стороны неба, а не ровным слоем.
                half stormFront = saturate(dot(dir, normalize(_StormDirection.xyz)) * 0.5h + 0.5h);
                stormGate *= pow(stormFront, _StormFrontFalloff);

                if (stormGate > 0.001h)
                {
                    half storm = StormDensity(domeUV);
                    half stormRel = CloudRel(storm, stormGate);
                    half stormMask = smoothstep(0.0h, _CloudSoftness, stormRel);

                    // Своя тень, не кучевая: другой масштаб, другой слой шума — раньше
                    // тёмные полосы ползли по грозе в отрыве от её собственного силуэта.
                    half stormTrans = StormTransmittance(domeUV, stormGate);
                    half3 stormColor = lerp(_StormShadowColor.rgb, _StormColor.rgb, stormTrans);
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

                // Светила до облаков: облака обязаны перекрывать солнце, а не наоборот.
                skyColor += SunHalo(dir);
                skyColor += SunDisk(dir);
                skyColor += MoonHalo(dir);

                // Облака поверх звёзд и светил.
                skyColor = ApplyClouds(dir, skyColor);

                // Туман предпоследним: он ближе всех к зрителю и топит в себе и небо,
                // и облака у горизонта — иначе на воде виден шов.
                skyColor = ApplyHorizonFog(dir, skyColor);

                // Фильтр погоды поверх всего — одна ручка на весь кадр неба.
                skyColor = ApplyWeatherFilter(skyColor);

                return half4(skyColor, 1.0h);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
