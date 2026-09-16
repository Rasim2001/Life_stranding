// Шейдер окружения сегментов. Каркас пассов и прагмы скопированы дословно из
// Packages/com.unity.render-pipelines.universal/Shaders/Lit.shader (URP 17.3.0,
// com.unity.render-pipelines.universal@37e06a5b08b3). Наш код — ENV_LitInput.hlsl
// (что за поверхность) и ENV_LitForwardPass.hlsl (форк ForwardLit с правками,
// см. комментарии "ENV_Lit:" в нём). ShadowCaster/DepthOnly/DepthNormals/Meta —
// пассы URP без единой правки, они читают наш ENV_LitInput.hlsl напрямую.
//
// Пропущенные пассы — решение, не забывчивость:
//   GBuffer         — проект на Forward+, не Deferred (см. urp-shader-authoring).
//   Universal2D     — не 2D-проект.
//   MotionVectors / XRMotionVectors — не входят в список того, что §6 называет
//                     унаследованным "даром"; TAA/motion blur этим шейдером сейчас
//                     не заявлены. Добавить отдельным тикетом, если понадобятся.
//
// Спек: docs/lighting-and-shading.md §6.
Shader "SpiderRig/ENV_Lit"
{
    Properties
    {
        [MainTexture] _BaseMap("Albedo", 2D) = "white" {}
        [MainColor] _BaseColor("Color", Color) = (1,1,1,1)

        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        // Режим маски (_MASKMAP_SEPARATE): упакованная одна карта или три раздельные —
        // см. спек .scratch/env-lit-shader/spec.md, "Два режима карт — оба финальные".
        [ToggleUI] _MaskMapSeparate("Separate Metallic/AO/Smoothness Maps", Float) = 0.0

        // Пространство проекции — режим материала, общий для градиента по высоте и для
        // будущих слоёв наноса/узора (см. .scratch/env-lit-layers/spec.md). World даёт
        // непрерывность через стыки модулей (четыре плиты пола — одна поверхность),
        // Local прибивает эффект к мешу и терпит перемещение объекта (башня из повторяющихся
        // этажей). Один переключатель на материал — не три вырожденных случая.
        [Enum(Local, 0, World, 1)] _ProjectionSpace("Projection Space", Float) = 0.0
        [NoScaleOffset] _MaskMap("Mask Map (R:Metallic G:AO B:Height A:Smoothness)", 2D) = "white" {}
        [NoScaleOffset] _MetallicMap("Metallic", 2D) = "white" {}
        [NoScaleOffset] _OcclusionMap("Occlusion", 2D) = "white" {}
        [NoScaleOffset] _SmoothnessMap("Smoothness", 2D) = "white" {}
        [NoScaleOffset] _HeightMap("Height", 2D) = "white" {}
        // Бамп из высоты базы (_HEIGHT_BUMP) — псевдо-нормаль конечной разностью карты
        // высоты, без смещения геометрии. 0 — поверхность плоская, дефолт 1 — рельеф
        // в базовом виде карты. См. ENV_HeightBumpTS в ENV_LitInput.hlsl.
        _HeightStrength("Height Strength", Range(0.0, 6.0)) = 1.0
        // Дефолты консервативные, а не «прозрачный множитель»: слоты карт по умолчанию
        // "white", поэтому скаляр 1.0 дал бы материалу без единой текстуры metallic = 1
        // и smoothness = 1, то есть зеркало. Те же числа, что у стокового URP Lit.
        _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5
        _OcclusionStrength("Occlusion Strength", Range(0.0, 1.0)) = 1.0

        _BumpScale("Scale", Range(0.0, 4.0)) = 1.0
        [Normal] _BumpMap("Normal Map", 2D) = "bump" {}
        // Ручки инверсии зелёного канала здесь сознательно нет. Она применялась бы только
        // в ForwardLit: пассы DepthNormals / ShadowCaster / DepthOnly — пакетные, они
        // сэмплят нормаль сами и про материальное свойство не знают. То есть включённая
        // галочка чинила бы картинку и одновременно ломала _CameraNormalsTexture, а с ней
        // SSAO и SSGI, причём тихо. Конвенция чинится в файле (Substance/Photoshop), а не
        // в материале, и проверяется глазами на объекте при скользящем свете: автоматического
        // теста по одной карте не существует — инверсия G даёт такую же легальную нормаль.

        [HDR] _EmissionColor("Color", Color) = (0,0,0)
        _EmissionMap("Emission", 2D) = "white" {}

        // Правка альбедо (_ALBEDO_ADJUST) — до освещения, порядок фиксирован: сдвиг тона,
        // потом контраст/яркость. Один keyword на оба, см. спек.
        [ToggleUI] _AlbedoAdjust("Albedo Adjust", Float) = 0.0
        _HueShift("Hue Shift", Range(-0.5, 0.5)) = 0.0
        _Contrast("Contrast", Range(0.0, 2.0)) = 1.0
        _Brightness("Brightness", Range(-0.5, 0.5)) = 0.0

        // Градиент по высоте (_HEIGHT_GRADIENT) — lerp к цвету, не умножение. Пространство
        // проекции — свойство материала _ProjectionSpace выше, не своё.
        [ToggleUI] _HeightGradient("Height Gradient", Float) = 0.0
        // Границы градиента ползунками, а не полями ввода: их крутят на глаз, а не набирают
        // числом. Диапазон ±10 взят под Local — пространство по умолчанию, где высота
        // отсчитывается от опорной точки чанка или пропса и в эти пределы укладывается.
        // Для World на высокой геометрии десяти единиц не хватит: там Range придётся
        // расширить или вернуть Float.
        _GradientMinHeight("Min Height", Range(-10.0, 10.0)) = 0.0
        _GradientMaxHeight("Max Height", Range(-10.0, 10.0)) = 1.0
        _GradientColor01("Color 01", Color) = (1,1,1,1)
        _GradientColor02("Color 02", Color) = (1,1,1,1)
        _GradientStrength("Strength", Range(0.0, 1.0)) = 0.0

        // Слой наноса (_OVERLAY_LAYER_0) — снег/пыль/грязь поверх основного материала.
        // Индекс 0 в именах свойств с самого начала: второй слой заложен именованием,
        // не реализуется (.scratch/env-lit-layers/spec.md, "Один слой, второй заложен
        // именованием"). Маска считается по нормали ПОСЛЕ карты нормалей и по высоте
        // микрорельефа — см. ComputeOverlayMask0 в ENV_LitInput.hlsl. Пространство проекции —
        // общее свойство материала _ProjectionSpace выше, не своё.
        [ToggleUI] _OverlayLayer0("Top Projection Layer", Float) = 0.0
        _OverlayMap0("Albedo Top", 2D) = "white" {}
        // Комплект материальных карт наноса (тикет 07) — та же раскладка режима _MASKMAP_SEPARATE,
        // что у базы: упакованная Mask Map либо три раздельные. Keyword _OVERLAY_MAPS_0 выводится
        // из наличия текстуры активного режима (ENV_LitShaderGUI.ValidateMaterial), вручную не ставится.
        // Координаты — те же, что у Albedo Top (_OverlayMap0_ST, тикет 2-03): карты одного слоя лежат
        // друг на друге.
        [NoScaleOffset] _OverlayMaskMap0("Mask Map Top (R:Metallic G:AO B:Height A:Smoothness)", 2D) = "white" {}
        [NoScaleOffset] _OverlayMetallicMap0("Metallic Top", 2D) = "white" {}
        [NoScaleOffset] _OverlayOcclusionMap0("Occlusion Top", 2D) = "white" {}
        [NoScaleOffset] _OverlaySmoothnessMap0("Smoothness Top", 2D) = "white" {}
        // Дефолт — sRGB 235, потолок дисциплины альбедо (§8), не единица: белый снег
        // это верхняя граница диапазона, а не выход за него.
        _OverlayColor0("Color", Color) = (0.921, 0.921, 0.921, 1)
        [Normal] _OverlayNormalMap0("Normal Top", 2D) = "bump" {}
        _OverlayNormalScale0("Scale", Range(0.0, 4.0)) = 1.0
        // Тикет 2-03: своя карта высоты слоя, следует общему режиму _MaskMapSeparate,
        // как база (раздельный — свой слот, упакованный — канал B Mask Map Top).
        [NoScaleOffset] _OverlayHeightMap0("Height Top", 2D) = "white" {}
        _OverlayMetallic0("Overlay Metallic", Range(0.0, 1.0)) = 0.0
        _OverlaySmoothness0("Overlay Smoothness", Range(0.0, 1.0)) = 0.2
        // 0 — слоя не видно нигде: закрывает самый вероятный отказ тикета (материал
        // без единой карты не должен менять вид при включении наноса).
        _OverlayCoverage0("Coverage", Range(0.0, 1.0)) = 0.0
        _OverlayEdgeSoftness0("Edge Softness", Range(0.0, 1.0)) = 0.1
        // Питает два слагаемых сразу (тикет 2-03, решение владельца 15.09.2026): вклад
        // в порог покрытия — из высоты БАЗЫ (снег затекает в швы кладки), визуальный бамп —
        // из своей Height Top (толщина слоя). Дефолт 0, не 1 как у базы в 2-02: ручка
        // дополнительно двигает площадь покрытия, которая раньше была прибита к нулю
        // дефолтом _OverlayHeightDepth0. Дефолт 1 менял бы вид любого материала с уже
        // назначенной базовой картой высоты.
        _OverlayHeightStrength0("Height Strength", Range(0.0, 6.0)) = 0.0
        // Своя сила затенения слоя (тикет 2-03, разворот решения тикета 07) — база под
        // сплошным снегом не может остаться такой, как была.
        _OverlayOcclusionStrength0("Occlusion Strength", Range(0.0, 1.0)) = 1.0

        // Художественный узор (_PATTERN), тикет 06 — одна карта шума на материал (RGB),
        // но у каждого потребителя свой блок настроек: проекция/тайлинг/поворот у наноса
        // и у блока смешивания раздельные, канал/сила/байас/мягкость — у каждого слоя
        // смешивания свои. .scratch/env-lit-layers/issues/06-noise-per-consumer.md.
        [ToggleUI] _Pattern("RGB Noise", Float) = 0.0
        // Дефолт "gray", не "white": формула СМЕЩАЕТ источник, а не умножает его (тикет 06),
        // и серый при байасе 0.5 — нейтраль (сдвиг = 0 при любой силе).
        [NoScaleOffset] _PatternMap("RGB Noise Map", 2D) = "gray" {}

        [Header(Overlay RGB Noise Block)]
        [Enum(Planar XZ, 0, Mesh UV, 1, Triplanar, 2)] _PatternSpace0("Projection", Float) = 0.0
        // Vector, не Float (тикет 2-03) — тайлинг по двум осям раздельно, как штатный
        // Scale/Offset. Читаются только .xy.
        _PatternTiling0("Tiling", Vector) = (1,1,0,0)
        _PatternRotation0("Rotation", Range(0.0, 360.0)) = 0.0
        [Enum(R, 0, G, 1, B, 2)] _PatternChannel0("Channel", Float) = 0.0
        // 0 — источник не смещается, граница наноса гладкая аналитическая, как до тикета 06.
        _PatternStrength0("Strength", Range(0.0, 1.0)) = 0.0
        _PatternBias0("Bias", Range(0.0, 1.0)) = 0.5

        // Смешивание материалов мазком (_MATERIAL_MIX) — второй и третий материал по весу
        // из вершинного цвета или маски-текстуры (.scratch/env-lit-layers/issues/
        // 04-material-blending.md). Граница обоих слоёв рвётся общей картой шума этого блока,
        // но у каждого слоя свой канал/сила/байас/мягкость (тикет 06).
        //
        // Префикс _Mix, а не _Blend: _Blend, _SrcBlend, _DstBlend и _BlendModePreserveSpecular
        // ниже — имена URP, занятые блендстейтом, и пятое _Blend* рядом с ними читалось бы
        // как ещё одна настройка прозрачности. Индексы 1 и 2, ноль остаётся за наносом.
        [ToggleUI] _MaterialMix("Material Blending", Float) = 0.0
        [ToggleUI] _MaterialMixTwo("Second Blend Layer", Float) = 0.0

        _MixMap1("Layer 1 Albedo", 2D) = "white" {}
        // Как у наноса — sRGB 235, потолок дисциплины альбедо (§8), не белый.
        _MixColor1("Layer 1 Color", Color) = (0.921, 0.921, 0.921, 1)
        [Normal] _MixNormalMap1("Layer 1 Normal", 2D) = "bump" {}
        _MixNormalScale1("Scale", Range(0.0, 4.0)) = 1.0
        _MixMetallic1("Layer 1 Metallic", Range(0.0, 1.0)) = 0.0
        _MixSmoothness1("Layer 1 Smoothness", Range(0.0, 1.0)) = 0.5
        // Комплект материальных карт слоя (тикет 07) — "Mask Map" здесь значит то же, что у базы
        // и у наноса: упакованный R:Metallic G:AO A:Smoothness. Не путать с удалённым в тикете 06
        // _MixMaskMap — та была маской мазка, не PBR-комплектом. Keyword _MIX_MAPS_1 выводится
        // из наличия текстуры активного режима; все карты используют _MixMap1_ST.
        [NoScaleOffset] _MixMaskMap1("Layer 1 Mask Map (R:Metallic G:AO A:Smoothness)", 2D) = "white" {}
        [NoScaleOffset] _MixMetallicMap1("Layer 1 Metallic Map", 2D) = "white" {}
        [NoScaleOffset] _MixOcclusionMap1("Layer 1 Occlusion Map", 2D) = "white" {}
        [NoScaleOffset] _MixSmoothnessMap1("Layer 1 Smoothness Map", 2D) = "white" {}
        // Своя сила затенения слоя (тикет 2-03, тот же разворот решения тикета 07, что у наноса).
        _MixOcclusionStrength1("Layer 1 Occlusion Strength", Range(0.0, 1.0)) = 1.0
        [Enum(Vertex Color, 0, RGB Noise, 1)] _MixMaskFromTexture1("Mask Source", Float) = 0.0
        _MixCoverage1("Blend Coverage", Range(0.0, 1.0)) = 0.0
        [Enum(Planar XZ, 0, Mesh UV, 1, Triplanar, 2)] _MixPatternSpace1("Projection", Float) = 0.0
        _MixPatternTiling1("Tiling", Vector) = (1,1,0,0)
        _MixPatternRotation1("Rotation", Range(0.0, 360.0)) = 0.0
        // Раскладка кисти RealBlend по умолчанию: слой 1 на G — см. тикет 04.
        [Enum(R, 0, G, 1, B, 2)] _MixPatternChannel1("Channel", Float) = 1.0
        _MixPatternStrength1("Strength", Range(0.0, 1.0)) = 0.0
        _MixPatternBias1("Bias", Range(0.0, 1.0)) = 0.5
        _MixEdgeSoftness1("Edge Softness", Range(0.0, 1.0)) = 0.1

        _MixMap2("Layer 2 Albedo", 2D) = "white" {}
        _MixColor2("Layer 2 Color", Color) = (0.921, 0.921, 0.921, 1)
        [Normal] _MixNormalMap2("Layer 2 Normal", 2D) = "bump" {}
        _MixNormalScale2("Scale", Range(0.0, 4.0)) = 1.0
        _MixMetallic2("Layer 2 Metallic", Range(0.0, 1.0)) = 0.0
        _MixSmoothness2("Layer 2 Smoothness", Range(0.0, 1.0)) = 0.5
        // Комплект материальных карт слоя 2 — см. комментарий у слоя 1.
        [NoScaleOffset] _MixMaskMap2("Layer 2 Mask Map (R:Metallic G:AO A:Smoothness)", 2D) = "white" {}
        [NoScaleOffset] _MixMetallicMap2("Layer 2 Metallic Map", 2D) = "white" {}
        [NoScaleOffset] _MixOcclusionMap2("Layer 2 Occlusion Map", 2D) = "white" {}
        [NoScaleOffset] _MixSmoothnessMap2("Layer 2 Smoothness Map", 2D) = "white" {}
        // Своя сила затенения слоя (тикет 2-03, тот же разворот решения тикета 07, что у наноса).
        _MixOcclusionStrength2("Layer 2 Occlusion Strength", Range(0.0, 1.0)) = 1.0
        [Enum(Vertex Color, 0, RGB Noise, 1)] _MixMaskFromTexture2("Mask Source", Float) = 0.0
        _MixCoverage2("Blend Coverage", Range(0.0, 1.0)) = 0.0
        [Enum(Planar XZ, 0, Mesh UV, 1, Triplanar, 2)] _MixPatternSpace2("Projection", Float) = 0.0
        _MixPatternTiling2("Tiling", Vector) = (1,1,0,0)
        _MixPatternRotation2("Rotation", Range(0.0, 360.0)) = 0.0
        // Раскладка кисти RealBlend по умолчанию: слой 2 на B — см. тикет 04.
        [Enum(R, 0, G, 1, B, 2)] _MixPatternChannel2("Channel", Float) = 2.0
        _MixPatternStrength2("Strength", Range(0.0, 1.0)) = 0.0
        _MixPatternBias2("Bias", Range(0.0, 1.0)) = 0.5
        _MixEdgeSoftness2("Edge Softness", Range(0.0, 1.0)) = 0.1

        // Blending state — унаследовано от URP Lit один в один, переключатель Surface Type
        // достаётся бесплатно (§6 "База": прозрачность есть в базе).
        _Surface("__surface", Float) = 0.0
        _Blend("__blend", Float) = 0.0
        _Cull("__cull", Float) = 2.0
        [ToggleUI] _AlphaClip("__clip", Float) = 0.0
        [HideInInspector] _SrcBlend("__src", Float) = 1.0
        [HideInInspector] _DstBlend("__dst", Float) = 0.0
        [HideInInspector] _SrcBlendAlpha("__srcA", Float) = 1.0
        [HideInInspector] _DstBlendAlpha("__dstA", Float) = 0.0
        [HideInInspector] _ZWrite("__zw", Float) = 1.0
        [HideInInspector] _BlendModePreserveSpecular("_BlendModePreserveSpecular", Float) = 1.0
        [HideInInspector] _AlphaToMask("__alphaToMask", Float) = 0.0

        [ToggleUI] _ReceiveShadows("Receive Shadows", Float) = 1.0
        _QueueOffset("Queue offset", Float) = 0.0

        [HideInInspector][NoScaleOffset] unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset] unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset] unity_ShadowMasks("unity_ShadowMasks", 2DArray) = "" {}
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Lit"
            "IgnoreProjector" = "True"
        }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend[_SrcBlend][_DstBlend], [_SrcBlendAlpha][_DstBlendAlpha]
            ZWrite[_ZWrite]
            Cull[_Cull]
            AlphaToMask[_AlphaToMask]

            HLSLPROGRAM
            #pragma target 2.0

            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _RECEIVE_SHADOWS_OFF
            #pragma shader_feature_local_fragment _SURFACE_TYPE_TRANSPARENT
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON
            #pragma shader_feature_local_fragment _EMISSION
            // _PROJECTIONSPACE_WORLD остаётся здесь, не в ENV_LitKeywords.hlsl: в Meta тот
            // же keyword объявлен без суффикса _fragment (мета резолвит positionPS в вершине).
            #pragma shader_feature_local_fragment _PROJECTIONSPACE_WORLD
            // _HEIGHT_BUMP общий с Meta: базовая нормаль определяет маску наноса
            // и тем самым альбедо, которое попадает в запечку.
            // _OVERLAY_HEIGHT_0 (тикет 2-03) остаётся здесь: бамп слоя наноса участвует
            // только в мировой нормали ForwardLit, Meta её не читает.
            #pragma shader_feature_local_fragment _OVERLAY_HEIGHT_0
            // Остальные ENV-специфичные фрагментные keyword'ы — общий список для ForwardLit
            // и Meta, тикет 06 (.scratch/env-lit-layers/issues/06-noise-per-consumer.md).
            #include_with_pragmas "ENV_LitKeywords.hlsl"

            // -------------------------------------
            // Universal Pipeline keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_ATLAS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _SCREEN_SPACE_IRRADIANCE
            #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fragment _ LIGHTMAP_BICUBIC_SAMPLING
            #pragma multi_compile_fragment _ REFLECTION_PROBE_ROTATION
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile _ USE_LEGACY_LIGHTMAPS
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile_fragment _ DEBUG_DISPLAY
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            #include "ENV_LitInput.hlsl"
            #include "ENV_LitForwardPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull[_Cull]

            HLSLPROGRAM
            #pragma target 2.0

            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #pragma shader_feature_local _ALPHATEST_ON

            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "ENV_LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull[_Cull]

            HLSLPROGRAM
            #pragma target 2.0

            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            #pragma shader_feature_local _ALPHATEST_ON

            #pragma multi_compile _ LOD_FADE_CROSSFADE

            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            #include "ENV_LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }

        // Пишет _CameraNormalsTexture — нужен SSAO/SSGI (§6: оба унаследованы даром).
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On
            Cull[_Cull]

            HLSLPROGRAM
            #pragma target 2.0

            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment

            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _ALPHATEST_ON

            #pragma multi_compile _ LOD_FADE_CROSSFADE

            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"

            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            #include "ENV_LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitDepthNormalsPass.hlsl"
            ENDHLSL
        }

        // Не участвует в обычном рендере — только запечка лайтмап/APV. Свой форк
        // (ENV_LitMetaPass.hlsl), не пакетный LitMetaPass.hlsl — см. .scratch/env-lit-layers/
        // issues/01-projection-space-and-meta-pass.md. Причина форка: стоковый мета-пасс
        // несёт во фрагмент только positionCS и uv, а градиент по высоте (и будущие нанос
        // и узор) нуждаются в позиции — она есть на входе вершинника, просто не проброшена.
        Pass
        {
            Name "Meta"
            Tags { "LightMode" = "Meta" }

            Cull Off

            HLSLPROGRAM
            #pragma target 2.0

            #pragma vertex ENV_LitMetaVertex
            #pragma fragment ENV_LitMetaFragment

            // _NORMALMAP и _PROJECTIONSPACE_WORLD без _fragment: оба гейтят работу вершинника
            // (тангент — первый, резолв пространства — второй), а не только фрагмента.
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _PROJECTIONSPACE_WORLD
            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            // Остальные ENV-специфичные фрагментные keyword'ы — общий список с ForwardLit,
            // тикет 06 (.scratch/env-lit-layers/issues/06-noise-per-consumer.md). Мазок
            // и узор обязаны попасть в запечку — мета-пасс для того и форкался в 01.
            #include_with_pragmas "ENV_LitKeywords.hlsl"
            #pragma shader_feature EDITOR_VISUALIZATION

            #include "ENV_LitInput.hlsl"
            #include "ENV_LitMetaPass.hlsl"
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
    CustomEditor "SpiderRig.Editor.Shaders.ENV_LitShaderGUI"
}
