// Облачный купол — настоящий меш (MSH_Weather_DomeClouds, пологая чаша ниже неба
// по высоте — WeatherDomeMeshGenerator: heightFraction/yFlatten меньше, чем у неба),
// рисуется поверх SHD_Weather_DomeSky. Разница формы этих двух мешей и даёт
// ощущение купола — см. комментарий в SHD_Weather_DomeSky.shader.
//
// Раньше облака и небо жили в одном непрозрачном скайбоксе, и облака вручную
// подмешивались в цвет неба лерпом. Теперь это отдельный прозрачный меш НАД небом —
// значит здесь настоящая альфа-композиция (Blend SrcAlpha OneMinusSrcAlpha), а три
// яруса (перистые/кучевые/грозовые) складываются друг на друга вручную оператором
// "over" внутри одного прохода, а не через три подряд идущих Pass с блендом от GPU —
// дешевле по геометрии (один проход вместо трёх), и porядок компоновки виден в одном
// месте кода, а не разнесён по Pass-блокам.
//
// Проекция du мoUV из SHD_Weather_SkyProcedural (dir.xz/(dir.y+curvature)) здесь
// не нужна вообще: направление берётся из object-space позиции вершины, шум сэмплится
// прямо по нему в 3D — форму пласта даёт геометрия меша, не формула. Общая математика
// шума — в Assets/Art/Shaders/WeatherNoise.hlsl.

Shader "SpiderRig/Weather/DomeClouds"
{
    Properties
    {
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

        [Header(Cumulus detail)]
        // 3-октавный Voronoi-FBM поверх базовой формы, на порядок мельче её. Без него
        // силуэт гладкий и аморфный — с ним рваный край "цветной капусты".
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
        _BorderEffect ("Border Effect", Range(0, 1)) = 0.35
        _BorderHeight ("Border Height", Range(0.2, 6)) = 2

        [Header(Cirrus)]
        _CirrusColor ("Cirrus Color", Color) = (1, 1, 1, 1)
        // Независимая ручка: перистые бывают и на чистом небе, к _CloudCoverage не привязаны.
        _CirrusCoverage ("Cirrus Coverage", Range(0, 1)) = 0.35
        _CirrusScale ("Cirrus Scale", Range(0.1, 10)) = 3
        _CirrusSpeed ("Cirrus Wind Speed", Range(0, 1)) = 0.12
        _CirrusOpacity ("Cirrus Opacity", Range(0, 1)) = 0.55

        [Header(Weather filter)]
        // Та же тройка свойств, что в SHD_Weather_DomeSky — обе ведёт WeatherService
        // одним значением, иначе гроза красит небо, а облака остаются нетронутыми.
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
            "Queue" = "Transparent-50"
        }

        Cull Front
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // Четыре развёрнутых 27-итерационных цикла Voronoi и полный Simplex —
            // не проходят на урезанных фича-левелах ниже таргета 3.0.
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

            // Глобал из WeatherService.RotateSunMoonPivot() — направление К солнцу.
            half3 _SR_SunDirection;

            CBUFFER_START(UnityPerMaterial)
                half _CloudCoverage;
                half _CloudScale;
                half _CloudSoftness;
                half _WindSpeed;
                half _CloudRollBias;
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

                half4 _FilterColor;
                half _FilterSaturation;
                half _FilterValue;
            CBUFFER_END

            // Один преобладающий ветер на всё небо. Раньше у кучевых база и Voronoi-деталь
            // ехали под 97° друг к другу (разные векторы для формы и для толщи) — и
            // поскольку толща реально управляет затенением и гасит солнечный ободок,
            // тёмные пятна визуально ползли поперёк облака, а не вместе с ним. Единая
            // точка выборки на весь ярус убирает расхождение не подбором, а структурно.
            static const half3 kWindDir = half3(0.6h, 0.0h, -0.8h);

            // Слои разнесены смещением по шумовому пою — один и тот же кусок поля
            // не повторяется в кучевых, грозовых и перистых при равном масштабе.
            // Полноценный 3D-сдвиг, а не сплющивание в одну ось (так было в domeUV-версии,
            // где вертикаль 3D-точки была занята под эту константу) — здесь ось дана
            // направлением, оставляем её живой.
            static const half3 kCumulusLayerOffset = half3(0.0h, 0.0h, 0.0h);
            static const half3 kStormLayerOffset = half3(37.0h, 11.0h, -19.0h);
            static const half3 kCirrusLayerOffset = half3(-53.0h, 29.0h, 91.0h);

            // Ветер добавляется К направлению ДО умножения на масштаб — поэтому скорость
            // облаков не зависит от их размера. Было наоборот (dir*scale + wind*t):
            // поднял _CloudScale, чтобы сделать облака мельче, и они тут же поехали
            // медленнее — артист-ручки были незаметно связаны друг с другом.
            float3 SR_CloudSamplePoint(float3 dir, half3 layerOffset, half scale, half windSpeed)
            {
                return (dir + layerOffset + kWindDir * (_Time.y * windSpeed)) * scale;
            }

            // --- Плотности ярусов --------------------------------------------------------
            // Частотные отношения Voronoi взяты у Cozy буквально: база 100/Scale,
            // Voronoi-A 140/Scale, Voronoi-B 500/Scale — то есть 1.0 : 1.4 : 5.0.
            static const half kVoroDetailFreq = 1.4h;
            static const half kVoroThickFreq = 5.0h;

            // Оценка плотности кучевых В ТОЧКЕ — вынесена отдельно от построения точки,
            // чтобы CloudTransmittance мог переиспользовать её со смещённой точкой
            // (самозатенение), не пересобирая ветер/масштаб заново.
            half CumulusDensityAtPoint(float3 pBase, out half thickness)
            {
                // Ремап под фактическое распределение при чистом 3D-семплинге dir*scale
                // (замерено заново после отказа от domeUV-проекции — старая калибровка
                // 0.30/0.40 была подогнана под widely-spread domeUV-точки и здесь давала
                // смещённый отклик, cov=0.2 показывал 39% неба вместо ожидаемых ~20%).
                // p01≈0.115, p50≈0.509, p99≈0.894 по 15k выборкам — окно 0.10..0.90
                // почти не отсекает края и центрирует медиану около 0.5.
                half baseNoise = saturate((SR_snoise(pBase) * 0.5h + 0.5h - 0.10h) / 0.80h);

                half voroA = saturate(Voronoi3D(pBase * kVoroDetailFreq));
                thickness = saturate(Voronoi3D(pBase * kVoroThickFreq));
                half shape = min(baseNoise, 1.0h - voroA);

                half detailFreq = kVoroThickFreq * 2.4h * _CloudDetailScale;
                half detail = 1.0h - DetailVoronoiFBM(pBase * detailFreq);
                // 0.20 — калибровочный потолок вклада при Amount=1, замерен прогоном
                // точной копии этой формулы по 12k направлений: без него уже при
                // Amount=0.5 75-й процентиль плотности упирался в 1.0 — деталь забивала
                // небо целиком вместо тонкой порчи края.
                detail = saturate((detail - 0.55h) / 0.35h) * 0.20h * _CloudDetailAmount;

                return saturate(shape + detail);
            }

            half CumulusDensity(float3 dir, out half thickness)
            {
                float3 pBase = SR_CloudSamplePoint(dir, kCumulusLayerOffset, _CloudScale, _WindSpeed);
                return CumulusDensityAtPoint(pBase, thickness);
            }

            // Самозатенение одной дополнительной выборкой: смотрим плотность на шаг
            // в сторону солнца от уже построенной точки кучевых. Гуще там — значит
            // этот участок закрыт от света. Экспонента — закон Бугера-Ламберта-Бера.
            //
            // Точка апгрейда: если одной выборки не хватит, здесь же меняется на цикл
            // 4-6 шагов вдоль sunDir с накоплением плотности — весь остальной шейдер
            // это не затрагивает.
            half CloudTransmittance(float3 dir, half coverage)
            {
                float3 pBase = SR_CloudSamplePoint(dir, kCumulusLayerOffset, _CloudScale, _WindSpeed);
                float3 shadowPoint = pBase + _SR_SunDirection * _ShadowSampleDistance;
                half ignoredThickness;
                half raw = CumulusDensityAtPoint(shadowPoint, ignoredThickness);
                // ФИКС: та же перенормировка, что и у видимой плотности — иначе
                // самозатенение работает на диапазоне [0.24,0.76] вместо [0,1]
                // и почти незаметно на всём облаке.
                return exp(-CloudRel(raw, coverage) * _ShadowDensity);
            }

            // Грозовые — крупнее и однороднее кучевых: обычный FBM без billow и без
            // Voronoi-детали. Рваная "капуста" тут не нужна, нужна плотная тяжёлая масса.
            half StormDensityAtPoint(float3 p)
            {
                half d = CloudShapeFBM(p);
                return saturate((d - 0.23h) / 0.54h);
            }

            half StormDensity(float3 dir)
            {
                float3 p = SR_CloudSamplePoint(dir, kStormLayerOffset, _StormScale, _WindSpeed * 0.7h);
                return StormDensityAtPoint(p);
            }

            // Своя тень, не кучевая: другой масштаб, другой слой шума — иначе тёмные
            // полосы ползут по грозе в отрыве от её собственного силуэта.
            half StormTransmittance(float3 dir, half stormGate)
            {
                float3 p = SR_CloudSamplePoint(dir, kStormLayerOffset, _StormScale, _WindSpeed * 0.7h);
                float3 shadowPoint = p + _SR_SunDirection * _ShadowSampleDistance;
                half raw = StormDensityAtPoint(shadowPoint);
                return exp(-CloudRel(raw, stormGate) * _ShadowDensity);
            }

            // Перистые — анизотропный шум: сжимаем пространство выборки по X, отчего
            // пятна вытягиваются в длинные полосы. Дёшево: две октавы, без Voronoi
            // и без самозатенения — у тонких перистых объёма нет по определению.
            half CirrusDensity(float3 dir)
            {
                float3 p = SR_CloudSamplePoint(dir, kCirrusLayerOffset, _CirrusScale, _CirrusSpeed);
                float3 stretched = p * half3(0.22h, 1.0h, 1.0h);
                half d = ValueNoise3D(stretched) * 0.62h;
                d += ValueNoise3D(stretched * 2.7h) * 0.38h;
                return saturate((d - 0.20h) / 0.60h);
            }

            // Компоновка "слой поверх уже накопленного" — обычная альфа-композиция
            // (Porter-Duff over), без премультиплицированной альфы: тройка ярусов
            // складывается внутри одного фрагмента вместо трёх Pass с GPU-блендом.
            void CompositeOver(inout half3 color, inout half alpha, half3 srcColor, half srcAlpha)
            {
                half outAlpha = srcAlpha + alpha * (1.0h - srcAlpha);
                color = (srcColor * srcAlpha + color * alpha * (1.0h - srcAlpha)) / max(outAlpha, 1.0e-4h);
                alpha = outAlpha;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                // Купол не вращается — object-space позиция вершины уже направление
                // от камеры (см. тот же приём в SHD_Weather_DomeSky.shader).
                OUT.dirOS = IN.positionOS.xyz;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                half3 dir = normalize(IN.dirOS);

                // Ниже линии горизонта облаков нет: узкий фейд у нижнего края меша.
                half horizonFade = smoothstep(-0.05h, 0.05h, dir.y);
                if (horizonFade <= 0.0h)
                    return half4(0.0h, 0.0h, 0.0h, 0.0h);

                // 0 в зените, 1 у горизонта. У горизонта поднимаем эффективное покрытие,
                // поэтому фронт непогоды приходит оттуда и накатывает к зениту, а не
                // проступает по всему небу разом (приём из Cozy Luxury).
                half horizonDist = saturate(1.0h - dir.y);
                half coverage = saturate(_CloudCoverage + horizonDist * _CloudRollBias);

                half sunDot = saturate(dot(dir, _SR_SunDirection));
                half highlight = pow(sunDot * 0.5h + 0.5h, _CloudHighlightFalloff);

                half3 color = half3(0.0h, 0.0h, 0.0h);
                half alpha = 0.0h;

                // --- Перистые: самый высокий ярус — рисуются первыми (дальше всех).
                half cirrus = CirrusDensity(dir);
                half cirrusCov = saturate(_CirrusCoverage + horizonDist * _CloudRollBias);
                half cirrusRel = CloudRel(cirrus, cirrusCov);
                half cirrusMask = smoothstep(0.0h, _CloudSoftness, cirrusRel) * _CirrusOpacity;
                CompositeOver(color, alpha, _CirrusColor.rgb * lerp(1.0h, 1.15h, highlight), cirrusMask);

                // --- Кучевые: основной ярус с объёмом.
                half thickness;
                half cumulus = CumulusDensity(dir, thickness);
                half cumulusRel = CloudRel(cumulus, coverage);
                half cumulusMask = smoothstep(0.0h, _CloudSoftness, cumulusRel);

                // Самозатенение считаем, только если маска вообще что-то покажет —
                // иначе это лишний Simplex + два 27-тапных Voronoi на пиксель зря.
                half transmittance = (cumulusMask > 0.001h) ? CloudTransmittance(dir, coverage) : 1.0h;

                half3 cumulusColor = lerp(_CloudShadowColor.rgb, _CloudColor.rgb, transmittance);
                // Подсветка солнцем поверх затенки, а не вместо: освещённая сторона
                // теплеет, теневая остаётся холодной. Толща гасит ободок — на плотном
                // куске света не видно, он весь рассеялся внутри.
                cumulusColor = lerp(cumulusColor, _CloudHighlightColor.rgb,
                    highlight * transmittance * (1.0h - thickness));
                // Толща темнит УЖЕ ЗАТЕНЁННЫЙ цвет, а не константу от освещённого —
                // иначе в тени подмешивался бы чужой оттенок вместо простого затемнения.
                cumulusColor = lerp(cumulusColor, cumulusColor * 0.566h, thickness * _CloudThickness);

                // Борта: радиальный вклад в перенос света, сильнее у горизонта и только
                // там, где есть плотность. Даёт глубину — облака над головой и у горизонта
                // освещены по-разному. Явно зажат, чтобы не выбивать в пересвет.
                half border = min(pow(horizonDist, _BorderHeight) * _BorderEffect * cumulusRel, 0.5h);
                cumulusColor += border.xxx * _CloudHighlightColor.rgb;

                CompositeOver(color, alpha, cumulusColor, cumulusMask);

                // --- Грозовые: нижний ярус, поверх остальных. Ступенчатое окно —
                // до _StormThreshold их нет вовсе, выше набирают до сплошной пелены.
                half stormGate = saturate((coverage - _StormThreshold) / max(1.0h - _StormThreshold, 0.01h));

                // Фронт направленный: гроза с одной стороны неба, а не ровным слоем.
                half stormFront = saturate(dot(dir, normalize(_StormDirection.xyz)) * 0.5h + 0.5h);
                stormGate *= pow(stormFront, _StormFrontFalloff);

                if (stormGate > 0.001h)
                {
                    half storm = StormDensity(dir);
                    half stormRel = CloudRel(storm, stormGate);
                    half stormMask = smoothstep(0.0h, _CloudSoftness, stormRel);

                    half stormTrans = StormTransmittance(dir, stormGate);
                    half3 stormColor = lerp(_StormShadowColor.rgb, _StormColor.rgb, stormTrans);
                    CompositeOver(color, alpha, stormColor, stormMask);
                }

                color = SR_ApplyWeatherFilter(color, _FilterColor, _FilterSaturation, _FilterValue);

                return half4(color, alpha * horizonFade);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
