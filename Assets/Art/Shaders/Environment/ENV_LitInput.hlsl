#ifndef SPIDERRIG_ENV_LIT_INPUT_INCLUDED
#define SPIDERRIG_ENV_LIT_INPUT_INCLUDED

// ENV_Lit — не монолит: пасс ForwardLit (и переиспользуемые без правок ShadowCaster /
// DepthOnly / DepthNormals / Meta из пакета URP) держатся на контракте
// InitializeStandardLitSurfaceData(uv, out SurfaceData). Этот файл — наш код: что за
// поверхность. Спек: docs/lighting-and-shading.md §6.
//
// PBR, metallic workflow, BRDF Unity не форкнут (см. §6 "Модель освещения") —
// _Reflectance сознательно не реализован в этой версии: диэлектрический F0 = 0.04
// зашит в InitializeBRDFData (Lighting.hlsl/BRDF.hlsl), а SurfaceData.specular учитывается
// только под _SPECULAR_SETUP — другим воркфлоу. Подставить свой F0 без форка light loop
// нельзя; решение — отложить ручку, а не форкать самый тяжёлый в поддержке файл URP
// ради одного слайдера. Вернуться, когда конкретному материалу это будет нужно.

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"

// NOTE: как в стоковом LitInput.hlsl — ни одного #ifdef внутри CBUFFER. SRP Batcher не
// переваривает разную раскладку буфера между материалами одного шейдера (§6, правило 1).
CBUFFER_START(UnityPerMaterial)
float4 _BaseMap_ST;
half4 _BaseColor;
half4 _EmissionColor;
half _Cutoff;
half _Metallic;
half _Smoothness;
half _OcclusionStrength;
half _BumpScale;
// Бамп из высоты базы (_HEIGHT_BUMP): сила эффекта и texel size обоих режимов карты
// (раздельного/упакованного) — как _BaseMap_TexelSize в стоковом LitInput.hlsl, texel
// size обязан жить в CBUFFER, иначе он уедет в $Globals и уронит SRP Batcher.
half _HeightStrength;
float4 _HeightMap_TexelSize;
float4 _MaskMap_TexelSize;
half _Surface;
// Блендстейт читается не только блоком Blend[_SrcBlend][_DstBlend], но и кодом:
// ENV_LitForwardPass выбирает по нему нейтраль тумана для прозрачного режима.
// Keyword'ов на это не хватает — _ALPHAPREMULTIPLY_ON ставится только при Preserve
// Specular, а Premultiply и Additive своих не имеют. В CBUFFER они обязаны быть:
// свойство, прочитанное в HLSL, становится настоящим uniform'ом, и вне UnityPerMaterial
// уехало бы в $Globals, потеряв совместимость с SRP Batcher.
half _SrcBlend;
half _DstBlend;
float4 _EmissionMap_ST;
// Правка альбедо (_ALBEDO_ADJUST): сдвиг тона, затем контраст и яркость. До освещения —
// см. §6 "Граница законности эффекта" в спеке.
half _HueShift;
half _Contrast;
half _Brightness;
// Градиент по высоте (_HEIGHT_GRADIENT): подмешивание цвета к альбедо, не умножение —
// умножение умеет только темнить. Пространство — общее свойство материала _ProjectionSpace,
// см. GetProjectionPosition ниже.
float _GradientMinHeight;
float _GradientMaxHeight;
half4 _GradientColor01;
half4 _GradientColor02;
half _GradientStrength;
// Слой наноса (_OVERLAY_LAYER_0): второй материал поверх основного — снег/пыль/грязь.
// Индекс 0 в именах — с самого начала: материал хранит значения по имени свойства,
// переименование под второй слой позже молча обнулит настройки на всех материалах.
// Пространство проекции общее с градиентом — _ProjectionSpace, см. GetProjectionPosition.
half4 _OverlayColor0;
// Штатный Scale/Offset слота Albedo Top (тикет 2-03) — заменил самодельный _OverlayTiling0,
// одна координата на все карты слоя (ENV_OverlayUV0).
float4 _OverlayMap0_ST;
half _OverlayNormalScale0;
half _OverlayMetallic0;
half _OverlaySmoothness0;
half _OverlayCoverage0;
half _OverlayEdgeSoftness0;
// Тикет 2-03: одна ручка питает бамп своей Height Top и затекание по высоте БАЗЫ —
// см. ENV_SampleOverlayHeight0 / ENV_OverlayHeightBumpTS0 / ComputeOverlayMask0.
half _OverlayHeightStrength0;
half _OverlayOcclusionStrength0;
// Texel size обоих режимов карты высоты слоя — как _HeightMap_TexelSize у базы, обязаны
// жить в CBUFFER, иначе уедут в $Globals и уронят SRP Batcher.
float4 _OverlayHeightMap0_TexelSize;
float4 _OverlayMaskMap0_TexelSize;
// Художественный узор (_PATTERN), тикет 06: своя карта шума — общая для наноса и обоих
// слоёв смешивания, но у каждого потребителя свой блок настроек (тиккет 06,
// .scratch/env-lit-layers/issues/06-noise-per-consumer.md). Индекс 0 — по аналогии
// с _OverlayXxx0: второй слот наноса заложен именованием, не реализован.
// float4, не float (тикет 2-03): читаются только .xy, тайлинг раздельный по осям
// (Vector2Property в инспекторе). float, не half — множится на мировую/объектную координату.
float4 _PatternTiling0;
// Поворот карты шума, в градусах — до умножения на тайлинг, поэтому не зависит от масштаба.
half _PatternRotation0;
// Канал текстуры (R/G/B), не keyword: три сравнения с uniform дешевле, чем x4 варианта
// пасса ради выбора канала. См. ENV_NoiseSource.
half _PatternChannel0;
half _PatternStrength0;
// Нейтраль канала шума — дефолт 0.5 держит серую (нейтральную) заглушку карты без сдвига
// источника при любой силе. См. ENV_NoiseSource.
half _PatternBias0;

// Смешивание материалов мазком (_MATERIAL_MIX): второй и третий материал по весу из
// вершинного цвета или из маски-текстуры. Префикс _Mix, а не _Blend: _Blend, _SrcBlend,
// _DstBlend и _BlendModePreserveSpecular — имена URP, уже занятые блендстейтом, и пятое
// _Blend* рядом с ними читалось бы как ещё одна настройка прозрачности.
// Индексы 1 и 2 — «первый и второй подмешиваемый слой» по тексту тикета; ноль за наносом.
// Каждый слой самодостаточен: свои проекция, тайлинг, поворот, канал, сила, байас,
// мягкость края и покрытие. Общая только карта _PatternMap.
float4 _MixPatternTiling1;
half _MixPatternRotation1;
half _MixPatternChannel1;
half _MixPatternStrength1;
half _MixPatternBias1;
half _MixEdgeSoftness1;
half _MixCoverage1;
// Своя сила затенения слоя (тикет 2-03, тот же разворот решения тикета 07, что у наноса).
half _MixOcclusionStrength1;
float4 _MixPatternTiling2;
half _MixPatternRotation2;
half _MixPatternChannel2;
half _MixPatternStrength2;
half _MixPatternBias2;
half _MixEdgeSoftness2;
half _MixCoverage2;
half _MixOcclusionStrength2;
half4 _MixColor1;
float4 _MixMap1_ST;
half _MixNormalScale1;
half _MixMetallic1;
half _MixSmoothness1;
half4 _MixColor2;
float4 _MixMap2_ST;
half _MixNormalScale2;
half _MixMetallic2;
half _MixSmoothness2;
// Переменные отладочной текстуры Rendering Debugger (material override, mip streaming).
// Как в стоковом LitInput.hlsl: макрос фиксированного размера, внутрь CBUFFER, без ifdef.
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END

// NOTE: DOTS-инстансинг ортогонален CBUFFER выше — не ifdef'им свойства, ifdef'им только
// использование (§6, правило 2: без дублирования здесь GPU Resident Drawer и Entities
// Graphics не смогут переопределить скаляр по инстансу).
#ifdef UNITY_DOTS_INSTANCING_ENABLED

UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
    UNITY_DOTS_INSTANCED_PROP(float4, _BaseColor)
    UNITY_DOTS_INSTANCED_PROP(float4, _EmissionColor)
    UNITY_DOTS_INSTANCED_PROP(float , _Cutoff)
    UNITY_DOTS_INSTANCED_PROP(float , _Metallic)
    UNITY_DOTS_INSTANCED_PROP(float , _Smoothness)
    UNITY_DOTS_INSTANCED_PROP(float , _OcclusionStrength)
    UNITY_DOTS_INSTANCED_PROP(float , _BumpScale)
    UNITY_DOTS_INSTANCED_PROP(float , _HeightStrength)
    UNITY_DOTS_INSTANCED_PROP(float , _Surface)
    UNITY_DOTS_INSTANCED_PROP(float , _HueShift)
    UNITY_DOTS_INSTANCED_PROP(float , _Contrast)
    UNITY_DOTS_INSTANCED_PROP(float , _Brightness)
    UNITY_DOTS_INSTANCED_PROP(float , _GradientMinHeight)
    UNITY_DOTS_INSTANCED_PROP(float , _GradientMaxHeight)
    UNITY_DOTS_INSTANCED_PROP(float4, _GradientColor01)
    UNITY_DOTS_INSTANCED_PROP(float4, _GradientColor02)
    UNITY_DOTS_INSTANCED_PROP(float , _GradientStrength)
    UNITY_DOTS_INSTANCED_PROP(float4, _OverlayColor0)
    UNITY_DOTS_INSTANCED_PROP(float , _OverlayNormalScale0)
    UNITY_DOTS_INSTANCED_PROP(float , _OverlayMetallic0)
    UNITY_DOTS_INSTANCED_PROP(float , _OverlaySmoothness0)
    UNITY_DOTS_INSTANCED_PROP(float , _OverlayCoverage0)
    UNITY_DOTS_INSTANCED_PROP(float , _OverlayEdgeSoftness0)
    UNITY_DOTS_INSTANCED_PROP(float , _OverlayHeightStrength0)
    UNITY_DOTS_INSTANCED_PROP(float , _OverlayOcclusionStrength0)
    UNITY_DOTS_INSTANCED_PROP(float4, _PatternTiling0)
    UNITY_DOTS_INSTANCED_PROP(float , _PatternRotation0)
    UNITY_DOTS_INSTANCED_PROP(float , _PatternChannel0)
    UNITY_DOTS_INSTANCED_PROP(float , _PatternStrength0)
    UNITY_DOTS_INSTANCED_PROP(float , _PatternBias0)
    UNITY_DOTS_INSTANCED_PROP(float4, _MixPatternTiling1)
    UNITY_DOTS_INSTANCED_PROP(float , _MixPatternRotation1)
    UNITY_DOTS_INSTANCED_PROP(float , _MixPatternChannel1)
    UNITY_DOTS_INSTANCED_PROP(float , _MixPatternStrength1)
    UNITY_DOTS_INSTANCED_PROP(float , _MixPatternBias1)
    UNITY_DOTS_INSTANCED_PROP(float , _MixEdgeSoftness1)
    UNITY_DOTS_INSTANCED_PROP(float , _MixCoverage1)
    UNITY_DOTS_INSTANCED_PROP(float , _MixOcclusionStrength1)
    UNITY_DOTS_INSTANCED_PROP(float4, _MixPatternTiling2)
    UNITY_DOTS_INSTANCED_PROP(float , _MixPatternRotation2)
    UNITY_DOTS_INSTANCED_PROP(float , _MixPatternChannel2)
    UNITY_DOTS_INSTANCED_PROP(float , _MixPatternStrength2)
    UNITY_DOTS_INSTANCED_PROP(float , _MixPatternBias2)
    UNITY_DOTS_INSTANCED_PROP(float , _MixEdgeSoftness2)
    UNITY_DOTS_INSTANCED_PROP(float , _MixCoverage2)
    UNITY_DOTS_INSTANCED_PROP(float , _MixOcclusionStrength2)
    UNITY_DOTS_INSTANCED_PROP(float4, _MixColor1)
    UNITY_DOTS_INSTANCED_PROP(float4, _MixMap1_ST)
    UNITY_DOTS_INSTANCED_PROP(float , _MixNormalScale1)
    UNITY_DOTS_INSTANCED_PROP(float , _MixMetallic1)
    UNITY_DOTS_INSTANCED_PROP(float , _MixSmoothness1)
    UNITY_DOTS_INSTANCED_PROP(float4, _MixColor2)
    UNITY_DOTS_INSTANCED_PROP(float4, _MixMap2_ST)
    UNITY_DOTS_INSTANCED_PROP(float , _MixNormalScale2)
    UNITY_DOTS_INSTANCED_PROP(float , _MixMetallic2)
    UNITY_DOTS_INSTANCED_PROP(float , _MixSmoothness2)
UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)

static float4 unity_DOTS_Sampled_BaseColor;
static float4 unity_DOTS_Sampled_EmissionColor;
static float  unity_DOTS_Sampled_Cutoff;
static float  unity_DOTS_Sampled_Metallic;
static float  unity_DOTS_Sampled_Smoothness;
static float  unity_DOTS_Sampled_OcclusionStrength;
static float  unity_DOTS_Sampled_BumpScale;
static float  unity_DOTS_Sampled_HeightStrength;
static float  unity_DOTS_Sampled_Surface;
static float  unity_DOTS_Sampled_HueShift;
static float  unity_DOTS_Sampled_Contrast;
static float  unity_DOTS_Sampled_Brightness;
static float  unity_DOTS_Sampled_GradientMinHeight;
static float  unity_DOTS_Sampled_GradientMaxHeight;
static float4 unity_DOTS_Sampled_GradientColor01;
static float4 unity_DOTS_Sampled_GradientColor02;
static float  unity_DOTS_Sampled_GradientStrength;
static float4 unity_DOTS_Sampled_OverlayColor0;
static float  unity_DOTS_Sampled_OverlayNormalScale0;
static float  unity_DOTS_Sampled_OverlayMetallic0;
static float  unity_DOTS_Sampled_OverlaySmoothness0;
static float  unity_DOTS_Sampled_OverlayCoverage0;
static float  unity_DOTS_Sampled_OverlayEdgeSoftness0;
static float  unity_DOTS_Sampled_OverlayHeightStrength0;
static float  unity_DOTS_Sampled_OverlayOcclusionStrength0;
static float4 unity_DOTS_Sampled_PatternTiling0;
static float  unity_DOTS_Sampled_PatternRotation0;
static float  unity_DOTS_Sampled_PatternChannel0;
static float  unity_DOTS_Sampled_PatternStrength0;
static float  unity_DOTS_Sampled_PatternBias0;
static float4 unity_DOTS_Sampled_MixPatternTiling1;
static float  unity_DOTS_Sampled_MixPatternRotation1;
static float  unity_DOTS_Sampled_MixPatternChannel1;
static float  unity_DOTS_Sampled_MixPatternStrength1;
static float  unity_DOTS_Sampled_MixPatternBias1;
static float  unity_DOTS_Sampled_MixEdgeSoftness1;
static float  unity_DOTS_Sampled_MixCoverage1;
static float  unity_DOTS_Sampled_MixOcclusionStrength1;
static float4 unity_DOTS_Sampled_MixPatternTiling2;
static float  unity_DOTS_Sampled_MixPatternRotation2;
static float  unity_DOTS_Sampled_MixPatternChannel2;
static float  unity_DOTS_Sampled_MixPatternStrength2;
static float  unity_DOTS_Sampled_MixPatternBias2;
static float  unity_DOTS_Sampled_MixEdgeSoftness2;
static float  unity_DOTS_Sampled_MixCoverage2;
static float  unity_DOTS_Sampled_MixOcclusionStrength2;
static float4 unity_DOTS_Sampled_MixColor1;
static float4 unity_DOTS_Sampled_MixMap1_ST;
static float  unity_DOTS_Sampled_MixNormalScale1;
static float  unity_DOTS_Sampled_MixMetallic1;
static float  unity_DOTS_Sampled_MixSmoothness1;
static float4 unity_DOTS_Sampled_MixColor2;
static float4 unity_DOTS_Sampled_MixMap2_ST;
static float  unity_DOTS_Sampled_MixNormalScale2;
static float  unity_DOTS_Sampled_MixMetallic2;
static float  unity_DOTS_Sampled_MixSmoothness2;

void SetupDOTSENVLitMaterialPropertyCaches()
{
    unity_DOTS_Sampled_BaseColor             = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _BaseColor);
    unity_DOTS_Sampled_EmissionColor         = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _EmissionColor);
    unity_DOTS_Sampled_Cutoff                = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _Cutoff);
    unity_DOTS_Sampled_Metallic              = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _Metallic);
    unity_DOTS_Sampled_Smoothness            = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _Smoothness);
    unity_DOTS_Sampled_OcclusionStrength     = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _OcclusionStrength);
    unity_DOTS_Sampled_BumpScale             = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _BumpScale);
    unity_DOTS_Sampled_HeightStrength        = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _HeightStrength);
    unity_DOTS_Sampled_Surface               = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _Surface);
    unity_DOTS_Sampled_HueShift              = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _HueShift);
    unity_DOTS_Sampled_Contrast              = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _Contrast);
    unity_DOTS_Sampled_Brightness            = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _Brightness);
    unity_DOTS_Sampled_GradientMinHeight     = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _GradientMinHeight);
    unity_DOTS_Sampled_GradientMaxHeight     = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _GradientMaxHeight);
    unity_DOTS_Sampled_GradientColor01       = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _GradientColor01);
    unity_DOTS_Sampled_GradientColor02       = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _GradientColor02);
    unity_DOTS_Sampled_GradientStrength      = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _GradientStrength);
    unity_DOTS_Sampled_OverlayColor0         = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _OverlayColor0);
    unity_DOTS_Sampled_OverlayNormalScale0   = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _OverlayNormalScale0);
    unity_DOTS_Sampled_OverlayMetallic0      = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _OverlayMetallic0);
    unity_DOTS_Sampled_OverlaySmoothness0    = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _OverlaySmoothness0);
    unity_DOTS_Sampled_OverlayCoverage0      = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _OverlayCoverage0);
    unity_DOTS_Sampled_OverlayEdgeSoftness0  = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _OverlayEdgeSoftness0);
    unity_DOTS_Sampled_OverlayHeightStrength0 = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _OverlayHeightStrength0);
    unity_DOTS_Sampled_OverlayOcclusionStrength0 = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _OverlayOcclusionStrength0);
    unity_DOTS_Sampled_PatternTiling0        = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _PatternTiling0);
    unity_DOTS_Sampled_PatternRotation0      = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _PatternRotation0);
    unity_DOTS_Sampled_PatternChannel0       = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _PatternChannel0);
    unity_DOTS_Sampled_PatternStrength0      = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _PatternStrength0);
    unity_DOTS_Sampled_PatternBias0          = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _PatternBias0);
    unity_DOTS_Sampled_MixPatternTiling1     = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _MixPatternTiling1);
    unity_DOTS_Sampled_MixPatternRotation1   = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _MixPatternRotation1);
    unity_DOTS_Sampled_MixPatternChannel1    = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _MixPatternChannel1);
    unity_DOTS_Sampled_MixPatternStrength1   = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _MixPatternStrength1);
    unity_DOTS_Sampled_MixPatternBias1       = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _MixPatternBias1);
    unity_DOTS_Sampled_MixEdgeSoftness1      = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _MixEdgeSoftness1);
    unity_DOTS_Sampled_MixCoverage1          = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _MixCoverage1);
    unity_DOTS_Sampled_MixOcclusionStrength1 = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _MixOcclusionStrength1);
    unity_DOTS_Sampled_MixPatternTiling2     = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _MixPatternTiling2);
    unity_DOTS_Sampled_MixPatternRotation2   = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _MixPatternRotation2);
    unity_DOTS_Sampled_MixPatternChannel2    = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _MixPatternChannel2);
    unity_DOTS_Sampled_MixPatternStrength2   = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _MixPatternStrength2);
    unity_DOTS_Sampled_MixPatternBias2       = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _MixPatternBias2);
    unity_DOTS_Sampled_MixEdgeSoftness2      = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _MixEdgeSoftness2);
    unity_DOTS_Sampled_MixCoverage2          = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _MixCoverage2);
    unity_DOTS_Sampled_MixOcclusionStrength2 = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _MixOcclusionStrength2);
    unity_DOTS_Sampled_MixColor1             = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _MixColor1);
    unity_DOTS_Sampled_MixMap1_ST            = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _MixMap1_ST);
    unity_DOTS_Sampled_MixNormalScale1       = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _MixNormalScale1);
    unity_DOTS_Sampled_MixMetallic1          = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _MixMetallic1);
    unity_DOTS_Sampled_MixSmoothness1        = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _MixSmoothness1);
    unity_DOTS_Sampled_MixColor2             = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _MixColor2);
    unity_DOTS_Sampled_MixMap2_ST            = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _MixMap2_ST);
    unity_DOTS_Sampled_MixNormalScale2       = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _MixNormalScale2);
    unity_DOTS_Sampled_MixMetallic2          = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _MixMetallic2);
    unity_DOTS_Sampled_MixSmoothness2        = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _MixSmoothness2);
}

#undef UNITY_SETUP_DOTS_MATERIAL_PROPERTY_CACHES
#define UNITY_SETUP_DOTS_MATERIAL_PROPERTY_CACHES() SetupDOTSENVLitMaterialPropertyCaches()

#define _BaseColor               unity_DOTS_Sampled_BaseColor
#define _EmissionColor           unity_DOTS_Sampled_EmissionColor
#define _Cutoff                  unity_DOTS_Sampled_Cutoff
#define _Metallic                unity_DOTS_Sampled_Metallic
#define _Smoothness              unity_DOTS_Sampled_Smoothness
#define _OcclusionStrength       unity_DOTS_Sampled_OcclusionStrength
#define _BumpScale               unity_DOTS_Sampled_BumpScale
#define _HeightStrength          unity_DOTS_Sampled_HeightStrength
#define _Surface                 unity_DOTS_Sampled_Surface
#define _HueShift                unity_DOTS_Sampled_HueShift
#define _Contrast                unity_DOTS_Sampled_Contrast
#define _Brightness              unity_DOTS_Sampled_Brightness
#define _GradientMinHeight       unity_DOTS_Sampled_GradientMinHeight
#define _GradientMaxHeight       unity_DOTS_Sampled_GradientMaxHeight
#define _GradientColor01         unity_DOTS_Sampled_GradientColor01
#define _GradientColor02         unity_DOTS_Sampled_GradientColor02
#define _GradientStrength        unity_DOTS_Sampled_GradientStrength
#define _OverlayColor0           unity_DOTS_Sampled_OverlayColor0
#define _OverlayNormalScale0     unity_DOTS_Sampled_OverlayNormalScale0
#define _OverlayMetallic0        unity_DOTS_Sampled_OverlayMetallic0
#define _OverlaySmoothness0      unity_DOTS_Sampled_OverlaySmoothness0
#define _OverlayCoverage0        unity_DOTS_Sampled_OverlayCoverage0
#define _OverlayEdgeSoftness0    unity_DOTS_Sampled_OverlayEdgeSoftness0
#define _OverlayHeightStrength0  unity_DOTS_Sampled_OverlayHeightStrength0
#define _OverlayOcclusionStrength0 unity_DOTS_Sampled_OverlayOcclusionStrength0
#define _PatternTiling0          unity_DOTS_Sampled_PatternTiling0
#define _PatternRotation0        unity_DOTS_Sampled_PatternRotation0
#define _PatternChannel0         unity_DOTS_Sampled_PatternChannel0
#define _PatternStrength0        unity_DOTS_Sampled_PatternStrength0
#define _PatternBias0            unity_DOTS_Sampled_PatternBias0
#define _MixPatternTiling1       unity_DOTS_Sampled_MixPatternTiling1
#define _MixPatternRotation1     unity_DOTS_Sampled_MixPatternRotation1
#define _MixPatternChannel1      unity_DOTS_Sampled_MixPatternChannel1
#define _MixPatternStrength1     unity_DOTS_Sampled_MixPatternStrength1
#define _MixPatternBias1         unity_DOTS_Sampled_MixPatternBias1
#define _MixEdgeSoftness1        unity_DOTS_Sampled_MixEdgeSoftness1
#define _MixCoverage1            unity_DOTS_Sampled_MixCoverage1
#define _MixOcclusionStrength1   unity_DOTS_Sampled_MixOcclusionStrength1
#define _MixPatternTiling2       unity_DOTS_Sampled_MixPatternTiling2
#define _MixPatternRotation2     unity_DOTS_Sampled_MixPatternRotation2
#define _MixPatternChannel2      unity_DOTS_Sampled_MixPatternChannel2
#define _MixPatternStrength2     unity_DOTS_Sampled_MixPatternStrength2
#define _MixPatternBias2         unity_DOTS_Sampled_MixPatternBias2
#define _MixEdgeSoftness2        unity_DOTS_Sampled_MixEdgeSoftness2
#define _MixCoverage2            unity_DOTS_Sampled_MixCoverage2
#define _MixOcclusionStrength2   unity_DOTS_Sampled_MixOcclusionStrength2
#define _MixColor1               unity_DOTS_Sampled_MixColor1
#define _MixMap1_ST              unity_DOTS_Sampled_MixMap1_ST
#define _MixNormalScale1         unity_DOTS_Sampled_MixNormalScale1
#define _MixMetallic1            unity_DOTS_Sampled_MixMetallic1
#define _MixSmoothness1          unity_DOTS_Sampled_MixSmoothness1
#define _MixColor2               unity_DOTS_Sampled_MixColor2
#define _MixMap2_ST              unity_DOTS_Sampled_MixMap2_ST
#define _MixNormalScale2         unity_DOTS_Sampled_MixNormalScale2
#define _MixMetallic2            unity_DOTS_Sampled_MixMetallic2
#define _MixSmoothness2          unity_DOTS_Sampled_MixSmoothness2

#endif

TEXTURE2D(_MaskMap); SAMPLER(sampler_MaskMap);

// Раздельный режим (_MASKMAP_SEPARATE): четыре карты вместо одной упакованной — тайлящаяся
// стена, которой нужна только гладкость, обходится одной одноканальной картой вдвое дешевле
// упакованной BC7. Все четыре делят один SAMPLER — настройки фильтрации у них одинаковые,
// четыре состояния сэмплера тратить незачем. Дефолт "white" = 1.0, пустой слот падает
// на скаляр (_Metallic / _OcclusionStrength / _Smoothness) сам, без отдельного keyword'а.
// _HeightMap — четвёртый член тройки: высота микрорельефа под слоем наноса следует
// существующему переключателю режима, своего не заводит (см. спек тикета 02).
TEXTURE2D(_MetallicMap);
TEXTURE2D(_OcclusionMap);
TEXTURE2D(_SmoothnessMap);
TEXTURE2D(_HeightMap);
SAMPLER(sampler_MetallicMap);

// Раскладка HDRP Mask Map с заменой канала B (detail mask там не нужен) на высоту
// микрорельефа под слоем наноса — см. §6 "База". Канал был бесплатен и остался: альфа
// под smoothness уже требует BC7, а BC7 несёт все четыре канала независимо от того,
// пишем мы в B или нет.
half4 SampleMaskMap(float2 uv)
{
    return SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, uv);
}

half SampleMaskMapOcclusion(half occlusionChannel)
{
    return LerpWhiteTo(occlusionChannel, _OcclusionStrength);
}

// Высота микрорельефа — следует существующему режиму маски, своего переключателя не
// заводит: владелец объяснил зачем тот режим существует (раздельные карты со стора
// подключаются быстро, проверяются в движке, потом пакуются в Substance — высота обязана
// себя вести как остальные каналы). Дефолт "white" = 1.0 — нейтраль: «нет карты» и
// «выступ» это одно и то же, никакого сдвига маски. Два потребителя: маска слоя наноса
// (ComputeOverlayMask0) и бамп базовой поверхности (ENV_HeightBumpTS, тикет 02).
half SampleLayerHeight(float2 uv)
{
#if defined(_MASKMAP_SEPARATE)
    return SAMPLE_TEXTURE2D(_HeightMap, sampler_MetallicMap, uv).r;
#else
    // Тот же вызов с теми же аргументами уже стоит в InitializeStandardLitSurfaceData —
    // компилятор складывает идентичные текстурные выборки. Протащить результат наружу
    // нельзя: сигнатуру InitializeStandardLitSurfaceData(uv, out SurfaceData) держат
    // пакетные пассы ShadowCaster/DepthOnly/DepthNormals, которые её тоже вызывают.
    return SampleMaskMap(uv).b;
#endif
}

// Бамп из высоты базы (_HEIGHT_BUMP, тикет 02) — псевдо-нормаль конечной разностью карты
// высоты по u и по v, шаг один тексель, без смещения геометрии (parallax отклонён на
// грилле). Разность вперёд (h - h_u), не центральная: две лишние выборки вместо четырёх,
// ценой полутекселя сдвига рельефа — на бампе не читается. Знак: h - h_u это -dh/du
// с точностью до положительного множителя, то есть выпуклость карты остаётся выпуклостью.
//
// GAIN — именованная константа, подобранная на глаз: сырая разность по текселю на типовой
// карте даёт 0.01-0.05, без множителя рельеф не виден ни на каком положении ползунка.
// При GAIN 8 ползунок 1 читается как обычная normal map, 6 — как контрастный рельеф.
// Если на реальной карте владелец скажет «слабо/сильно» — правится это число, а не
// диапазон ползунка _HeightStrength.
half3 ENV_HeightBumpTS(float2 uv)
{
    static const half GAIN = half(8.0);

#if defined(_MASKMAP_SEPARATE)
    float2 texel = _HeightMap_TexelSize.xy;
#else
    float2 texel = _MaskMap_TexelSize.xy;
#endif

    half h = SampleLayerHeight(uv);
    half hU = SampleLayerHeight(uv + float2(texel.x, 0.0));
    half hV = SampleLayerHeight(uv + float2(0.0, texel.y));

    half2 slope = half2(h - hU, h - hV) * _HeightStrength * GAIN;
    return normalize(half3(slope.x, slope.y, half(1.0)));
}

// Слой наноса (_OVERLAY_LAYER_0): своя пара альбедо/нормаль, один сэмплер на двоих —
// фильтрация у пары одна, как у тройки masks выше.
TEXTURE2D(_OverlayMap0);
TEXTURE2D(_OverlayNormalMap0);
SAMPLER(sampler_OverlayMap0);

// Комплект материальных карт наноса (тикет 07): та же раскладка режима _MASKMAP_SEPARATE,
// что у базы. Делит sampler_OverlayMap0 с альбедо/нормалью — та же фильтрация. Гейт —
// keyword _OVERLAY_MAPS_0, см. ENV_LitKeywords.hlsl.
TEXTURE2D(_OverlayMaskMap0);
TEXTURE2D(_OverlayMetallicMap0);
TEXTURE2D(_OverlayOcclusionMap0);
TEXTURE2D(_OverlaySmoothnessMap0);
// Высота слоя (тикет 2-03) — раздельный режим, на общем sampler_OverlayMap0.
TEXTURE2D(_OverlayHeightMap0);

// Координата слоя: планарная проекция XZ со штатным Scale/Offset слота Albedo Top.
// Одна точка на все карты слоя — принцип тикета 07 «карты одного материала лежат
// друг на друге» (тикет 2-03: меняется только то, чем задаются координаты — Vector2+Vector2
// вместо скалярного множителя _OverlayTiling0).
float2 ENV_OverlayUV0(float3 positionPS)
{
    return positionPS.xz * _OverlayMap0_ST.xy + _OverlayMap0_ST.zw;
}

// Высота слоя наноса (тикет 2-03). Следует общему режиму _MASKMAP_SEPARATE, как база:
// раздельный — свой слот, упакованный — канал B комплекта слоя. Правило владельца:
// раскладка упакованной карты одинакова у базы и у всех слоёв, чтобы карты можно было
// менять местами между слоями. Дефолт "white" = 1 — нейтраль, бамп нулевой.
half ENV_SampleOverlayHeight0(float2 uv)
{
#if defined(_MASKMAP_SEPARATE)
    return SAMPLE_TEXTURE2D(_OverlayHeightMap0, sampler_OverlayMap0, uv).r;
#else
    return SAMPLE_TEXTURE2D(_OverlayMaskMap0, sampler_OverlayMap0, uv).b;
#endif
}

// Бамп из высоты слоя (тикет 2-03) — та же техника и та же константа GAIN, что у базы
// (ENV_HeightBumpTS): две ручки силы высоты в одном материале обязаны ощущаться одинаково.
half3 ENV_OverlayHeightBumpTS0(float2 uv)
{
    static const half GAIN = half(8.0);

#if defined(_MASKMAP_SEPARATE)
    float2 texel = _OverlayHeightMap0_TexelSize.xy;
#else
    float2 texel = _OverlayMaskMap0_TexelSize.xy;
#endif

    half h = ENV_SampleOverlayHeight0(uv);
    half hU = ENV_SampleOverlayHeight0(uv + float2(texel.x, 0.0));
    half hV = ENV_SampleOverlayHeight0(uv + float2(0.0, texel.y));

    half2 slope = half2(h - hU, h - hV) * _OverlayHeightStrength0 * GAIN;
    return normalize(half3(slope.x, slope.y, half(1.0)));
}

// Художественный узор (_PATTERN), тикет 06: одна карта на три потребителя (нанос,
// слой смешивания 1, слой смешивания 2). Каждый потребитель читает её своими
// координатами, но фильтрация и импорт текстуры остаются общими.
TEXTURE2D(_PatternMap);
SAMPLER(sampler_PatternMap);

// Смешивание материалов (_MATERIAL_MIX): четыре карты двух слоёв делят один сэмплер —
// фильтрация у них одна, как у тройки масок и пары наноса. Восемь сэмплеров из шестнадцати
// (_MixMaskMap ушёл в тикете 06 — маска мазка не своя текстура, а роль общей карты шума).
TEXTURE2D(_MixMap1);
TEXTURE2D(_MixNormalMap1);
TEXTURE2D(_MixMap2);
TEXTURE2D(_MixNormalMap2);
SAMPLER(sampler_MixMap1);

// Комплекты материальных карт слоёв смешивания (тикет 07) — та же раскладка режима
// _MASKMAP_SEPARATE, что у базы и у наноса. Делят sampler_MixMap1 с альбедо/нормалью
// своего слоя. "Mask Map" здесь — упакованный R:Metallic G:AO A:Smoothness, НЕ маска мазка:
// та (_MixMaskMap) удалена в тикете 06, имя занято заново под другой смысл, коллизии
// в сериализации нет. Гейт — свой keyword на слой (_MIX_MAPS_1 / _MIX_MAPS_2).
TEXTURE2D(_MixMaskMap1);
TEXTURE2D(_MixMetallicMap1);
TEXTURE2D(_MixOcclusionMap1);
TEXTURE2D(_MixSmoothnessMap1);
TEXTURE2D(_MixMaskMap2);
TEXTURE2D(_MixMetallicMap2);
TEXTURE2D(_MixOcclusionMap2);
TEXTURE2D(_MixSmoothnessMap2);

// Художественный узор (_PATTERN), тикет 06 — .scratch/env-lit-layers/issues/
// 06-noise-per-consumer.md. Одна карта шума на материал, но у каждого потребителя (нанос,
// слой смешивания 1, слой смешивания 2) свой блок настроек: проекция, тайлинг, поворот
// у наноса и у блока смешивания раздельные, канал/сила/байас/мягкость — у каждого слоя
// смешивания свои. Проекция и тайлинг неразделимы (тайлинг planar — «тайлов на метр»,
// тайлинг UV — «тайлов на UV-шелл»), поэтому идут парой в один keyword-набор.

// Шкала мягкости края — ползунок в квадрате, а не напрямую. Полезный диапазон для наклона
// источника примерно 0-0.3, ползолнок отдан под 0-1: линейная шкала топит слой в равномерную
// полупрозрачную плёнку уже на 0.2 (грилл 15.09.2026, пункт 7 — арифметически верное, но
// нечитаемое поведение). Квадрат отдаёт разрешение низу шкалы.
half ENV_EdgeFromSlider(half slider)
{
    return slider * slider * half(0.5);
}

// Источник границы: карта шума СМЕЩАЕТ базовое значение, а не умножает его. Умножение
// на канал со средней яркостью ~0.5 опускало средний источник и честно уменьшало площадь
// покрытия при росте силы — старый дефект (грилл 15.09.2026, причина 3). Смещение держит
// среднее источника на месте: сила отвечает за рванину края, не за его положение.
//
// saturate обязателен, не для порядка: после сложения источник вылезает за [0,1] на силу/2
// в обе стороны, а жёсткость концов ENV_ThresholdMask держится на том, что источник её
// не превышает.
//
// bias — нейтраль канала карты шума. Дефолт свойства 0.5 предполагает карту со средней
// яркостью канала около 0.5; у карты с другим средним лечится этой же ручкой на месте,
// без похода в Photoshop.
half ENV_NoiseSource(half base, half3 noiseRGB, half channel, half strength, half bias)
{
    half3 channelMask = channel < half(0.5) ? half3(1.0, 0.0, 0.0)
                       : channel < half(1.5) ? half3(0.0, 1.0, 0.0)
                       :                       half3(0.0, 0.0, 1.0);
    half sampleValue = dot(noiseRGB, channelMask);
    return saturate(base + (sampleValue - bias) * strength);
}

// Тело выборки карты шума — planar XZ (mode 0, дефолт) / UV меша (mode 1) / трипланар
// (mode 2). И position, и normal приходят уже В ПРОСТРАНСТВЕ ПРОЕКЦИИ (GetProjectionPosition /
// ENV_GetProjectionNormalFromWorld на стороне вызова) — трипланар следует общему
// _ProjectionSpace материала, как и всё остальное в этом шейдере, а не форсирует world.
//
// mode — компайл-тайм литерал, приходящий с каждого сайта вызова уже вычисленным из
// соответствующего keyword'а (ENV_SampleNoiseOverlay0 / ENV_SampleNoiseMix ниже). При
// инлайне ветки по константе сворачиваются, мёртвого кода и лишнего ветвления вокруг
// выборки текстуры не остаётся.
//
// Поворот — матрица 2×2, применяется ДО умножения на тайлинг, поэтому не зависит
// от масштаба. В трипланаре тот же поворот действует внутри каждой из трёх плоскостей.
half3 ENV_SampleNoiseCore(TEXTURE2D_PARAM(map, samp), float2 uv, float3 positionPS,
                          half3 normalPS, float2 tiling, half rotationDeg, uint mode)
{
    half s, c;
    sincos(radians(rotationDeg), s, c);
    float2x2 rotation = float2x2(c, -s, s, c);

    if (mode == 1u) // UV меша
    {
        return SAMPLE_TEXTURE2D(map, samp, mul(rotation, uv) * tiling).rgb;
    }
    else if (mode == 2u) // Триплanar, три выборки, смешивание по весам нормали
    {
        float2 uvXZ, uvXY, uvZY;
        GetTriplanarCoordinate(positionPS, uvXZ, uvXY, uvZY);
        half3 weights = half3(ComputeTriplanarWeights(real3(normalPS)));

        half3 sampleZY = SAMPLE_TEXTURE2D(map, samp, mul(rotation, uvZY) * tiling).rgb;
        half3 sampleXZ = SAMPLE_TEXTURE2D(map, samp, mul(rotation, uvXZ) * tiling).rgb;
        half3 sampleXY = SAMPLE_TEXTURE2D(map, samp, mul(rotation, uvXY) * tiling).rgb;

        // Раскладка осей — как в CommonMaterial.hlsl: weights.x относится к uvZY,
        // weights.y — к uvXZ, weights.z — к uvXY (сверено по исходнику пакета).
        return sampleZY * weights.x + sampleXZ * weights.y + sampleXY * weights.z;
    }
    else // Planar XZ, дефолт
    {
        return SAMPLE_TEXTURE2D(map, samp, mul(rotation, positionPS.xz) * tiling).rgb;
    }
}

// Обёртка для блока наноса — mode выбирается keyword'ами _PATTERNSPACE0_UV /
// _PATTERNSPACE0_TRIPLANAR (ENV_LitKeywords.hlsl). Свой тайлинг и поворот, общая карта.
half3 ENV_SampleNoiseOverlay0(float2 uv, float3 positionPS, half3 normalPS)
{
#if defined(_PATTERNSPACE0_TRIPLANAR)
    uint mode = 2u;
#elif defined(_PATTERNSPACE0_UV)
    uint mode = 1u;
#else
    uint mode = 0u;
#endif
    return ENV_SampleNoiseCore(TEXTURE2D_ARGS(_PatternMap, sampler_PatternMap),
        uv, positionPS, normalPS, _PatternTiling0.xy, _PatternRotation0, mode);
}

half3 ENV_SampleNoiseMix1(float2 uv, float3 positionPS, half3 normalPS)
{
#if defined(_MIXPATTERNSPACE1_TRIPLANAR)
    uint mode = 2u;
#elif defined(_MIXPATTERNSPACE1_UV)
    uint mode = 1u;
#else
    uint mode = 0u;
#endif
    return ENV_SampleNoiseCore(TEXTURE2D_ARGS(_PatternMap, sampler_PatternMap),
        uv, positionPS, normalPS, _MixPatternTiling1.xy, _MixPatternRotation1, mode);
}

half3 ENV_SampleNoiseMix2(float2 uv, float3 positionPS, half3 normalPS)
{
#if defined(_MIXPATTERNSPACE2_TRIPLANAR)
    uint mode = 2u;
#elif defined(_MIXPATTERNSPACE2_UV)
    uint mode = 1u;
#else
    uint mode = 0u;
#endif
    return ENV_SampleNoiseCore(TEXTURE2D_ARGS(_PatternMap, sampler_PatternMap),
        uv, positionPS, normalPS, _MixPatternTiling2.xy, _MixPatternRotation2, mode);
}

// Правка альбедо (_ALBEDO_ADJUST), до освещения — см. спек, "Граница законности эффекта".
// Порядок фиксирован: сдвиг тона, потом контраст/яркость. Обе часто нужны вместе на одной
// текстуре («подкрутить тон купленного кирпича»), поэтому один keyword на обе — иначе три
// отдельных keyword'а дали бы восемь комбинаций варианта шейдера вместо четырёх.
half3 ApplyAlbedoAdjust(half3 albedo)
{
#if defined(_ALBEDO_ADJUST)
    half3 hsv = RgbToHsv(albedo);
    hsv.x = frac(hsv.x + _HueShift);
    albedo = HsvToRgb(hsv);

    // saturate обязателен: контраст 2.0 на тёмном текселе даёт (0 - 0.5) * 2 + 0.5 = -0.5,
    // а отрицательное альбедо — это отрицательный diffuse в HDR-таргете (тонмаппер и блум
    // на таком входе не определены) и отрицательное альбедо в запечке через Meta-пасс.
    albedo = saturate((albedo - half(0.5)) * _Contrast + half(0.5) + _Brightness);
#endif
    return albedo;
}

// Пространство проекции — общее для градиента по высоте и для будущих слоёв наноса/узора
// (.scratch/env-lit-layers/spec.md). Единственная точка выбора: ForwardLit и мета-пасс
// добывают обе позиции своим способом (первый — из positionWS, второй — прямо в вершине
// из positionOS) и зовут одну и ту же функцию, поэтому оба пасса считают эффект одинаково.
// Неактивная ветка мертва и складывается компилятором — это keyword времени компиляции,
// не рантайм-ветвление.
float3 GetProjectionPosition(float3 positionWS, float3 positionOS)
{
#if defined(_PROJECTIONSPACE_WORLD)
    return GetAbsolutePositionWS(positionWS);
#else
    return positionOS;
#endif
}

// Пара к GetProjectionPosition — тот же keyword, для направлений вместо точек. Принимает
// вектор, уже выраженный в осях пространства проекции (мировых или объектных), и переводит
// его в мировое пространство. Потребитель — ось «верха» для наклона поверхности
// (ComputeOverlayMask0). Для нормалей эта функция не годится, см. GetProjectionNormal ниже.
float3 GetProjectionDirection(float3 dirPS)
{
#if defined(_PROJECTIONSPACE_WORLD)
    return dirPS;
#else
    return TransformObjectToWorldDir(dirPS);
#endif
}

// То же для нормалей, и это не одно и то же: нормаль переводится обратной транспонированной
// матрицей, направление — обычной. При неравномерном масштабе разница не косметическая, а
// в противоположную сторону: куб, растянутый (5,1,1), растягивает и планарную проекцию,
// значит тот же бугор нормали занимает в мире впятеро больше — наклон обязан стать положе.
// Обычная матрица делает ровно наоборот и задирает его (наклон 45° по u → 79° вместо 11°).
// Модульная геометрия живёт на неравномерном масштабе, так что это обычный случай, не край.
float3 GetProjectionNormal(float3 normalPS)
{
#if defined(_PROJECTIONSPACE_WORLD)
    return normalPS;
#else
    return TransformObjectToWorldNormal(normalPS);
#endif
}

// Обратная пара к GetProjectionNormal — переводит МИРОВУЮ нормаль в пространство проекции,
// а не наоборот. Нужна трипланару (тикет 06): веса ComputeTriplanarWeights обязаны смотреть
// на нормаль в том же пространстве, что и positionPS, иначе оси весов и оси координат
// разъедутся. Под World — тождество, под Local — TransformWorldToObjectNormal (обратная
// транспонированная матрица; та же оговорка о неравномерном масштабе, что у GetProjectionNormal
// выше, здесь действует в обратную сторону).
half3 ENV_GetProjectionNormalFromWorld(half3 normalWS)
{
#if defined(_PROJECTIONSPACE_WORLD)
    return normalWS;
#else
    return half3(TransformWorldToObjectNormal(float3(normalWS)));
#endif
}

// Сборка мировой нормали после карты нормалей — то же, что делает InitializeInputData
// в ENV_LitForwardPass.hlsl, но вызывается раньше её: маска слоя наноса зависит от готовой
// нормали, а слой её же и правит перед тем, как InitializeInputData её примет.
//
// Нормализация на выходе обязательна, а не для порядка: у TransformTangentToWorld третий
// аргумент doNormalize по умолчанию false (core/SpaceTransforms.hlsl), и базис после
// интерполяции неединичной длины. Потребитель здесь один — dot в ComputeOverlayMask0, —
// и от длины вектора он поехал бы: граница покрытия ползёт по мешу, а при выключении
// _NORMALMAP скачет, потому что вторая ветка длину чинит. InitializeInputData делает
// то же самое строкой ниже сборки той же матрицы.
half3 ENV_ResolveNormalWS(half3 normalTS, half3 vertexNormalWS, half4 tangentWS)
{
#if defined(_NORMALMAP)
    half sgn = tangentWS.w;
    half3 bitangent = sgn * cross(vertexNormalWS, tangentWS.xyz);
    half3x3 tangentToWorld = half3x3(tangentWS.xyz, bitangent, vertexNormalWS);
    return NormalizeNormalPerPixel(TransformTangentToWorld(normalTS, tangentToWorld));
#else
    return NormalizeNormalPerPixel(vertexNormalWS);
#endif
}

// Общий порог всех масок этого шейдера: слоя наноса (02) и обоих слоёв смешивания (04, 06).
// Спек требует, чтобы все три механизма пользовались одним механизмом маски, а четыре
// строки порога в трёх копиях — ровно то, что разъезжается молча через год.
//
// Роли параметров у потребителей разные, и это осознанно (тикет 06):
//   нанос — source = ENV_NoiseSource(saturate(наклон + рельеф), ...), coverage = _OverlayCoverage0;
//   мазок — source = ENV_NoiseSource(0.5, ...) при включённом _PATTERN, иначе константа 0.5,
//           coverage = вес из канала вершинного цвета либо собственный _MixCoverageN.
// Оба конца при этом доказуемо жёсткие: coverage = 0 даёт порог 1+edge при source <= 1
// (маска ровно 0), coverage = 1 даёт порог -edge при source >= 0 (ровно 1).
//
// Порог сдвинут на ширину края: lerp(1 + edge, -edge, coverage). Без сдвига coverage = 0
// давал бы половину интенсивности на идеально горизонтальной поверхности — smoothstep
// на границе диапазона возвращает 0.5, а не 0. Со сдвигом концы ползунка настоящие:
// 0 — пусто везде, 1 — покрыто всюду, включая стены.
//
// Порог и края считаются в float, хотя всё вокруг — half. Не из осторожности: шаг
// binary16 около 1.0 равен 2^-10 ~ 9.8e-4, то есть крупнее самой страховки 1e-4.
// В half при Edge Softness 0 и Coverage 0 обе границы smoothstep округлились бы
// в одну и ту же 1.0, а на идеально горизонтальной грани source равен ровно 1.0 —
// вырожденный интервал и деление 0/0 в маске. На D3D11 half это float и случай
// не наступает; на Metal/Vulkan, где half настоящий 16-битный, наступает.
// Художественный диапазон не меняется: меняется точность, которой считается край.
half ENV_ThresholdMask(float source, half coverage, half edgeSoftness)
{
    float edge = max(float(edgeSoftness), 1e-4);
    float threshold = lerp(1.0 + edge, -edge, float(coverage));
    return half(smoothstep(threshold - edge, threshold + edge, source));
}

// Слой наноса (_OVERLAY_LAYER_0): маска считается по нормали ПОСЛЕ карты нормалей, не по
// геометрической — разница бесплатная и решающая, иначе слой ляжет ровной плёнкой поверх
// кладки и проигнорирует рельеф. Входы: наклон поверхности, микрорельеф (высота из
// SampleLayerHeight — общий канал с раздельным/упакованным режимом маски), свой блок узора
// (тикет 06 — ENV_SampleNoiseOverlay0/ENV_NoiseSource, смещает базу, если _PATTERN включён)
// и порог покрытия.
//
// normalWS приходит уже ПОСЛЕ карты нормалей — тот же вектор используется и для наклона,
// и как normalPS для весов трипланара узора наноса (после перевода в пространство проекции).
half ComputeOverlayMask0(float2 uv, float3 positionPS, half3 normalWS)
{
#if defined(_OVERLAY_LAYER_0)
    half3 upPS = half3(GetProjectionDirection(float3(0.0, 1.0, 0.0)));
    half slope = saturate(dot(normalWS, upPS));

    // Тикет 2-03: relief считается из высоты БАЗЫ (не своей Height Top) — снег затекает
    // в рельеф кладки, а не в собственный микрорельеф. saturate, а не деление на границу
    // ползунка: до 1 ручка набирает затекание в рельеф основания, выше растёт только
    // толщина слоя (бамп в ENV_OverlayHeightBumpTS0).
    half height = SampleLayerHeight(uv);
    half relief = (half(1.0) - height) * saturate(_OverlayHeightStrength0);

    half baseSource = half(saturate(float(slope) + float(relief)));

#if defined(_PATTERN)
    half3 normalPS = ENV_GetProjectionNormalFromWorld(normalWS);
    half3 noise = ENV_SampleNoiseOverlay0(uv, positionPS, normalPS);
    half source = ENV_NoiseSource(baseSource, noise, _PatternChannel0, _PatternStrength0, _PatternBias0);
#else
    half source = baseSource;
#endif

    return ENV_ThresholdMask(float(source), _OverlayCoverage0, _OverlayEdgeSoftness0);
#else
    return half(0.0);
#endif
}

// Комплект материальных карт одного слоя (тикет 07, .scratch/env-lit-layers/issues/
// 07-layer-maps.md) — металличность/затенение/гладкость картой вместо одного числа на слой.
// Режим общий на материал (_MASKMAP_SEPARATE), как у базы: SampleMaskMapOcclusion выше.
// Возвращает каналы КАК ЕСТЬ, до умножения на ползунки слоя — та же формула "карта × ползунок",
// что уже применяет InitializeStandardLitSurfaceData, читателю остаётся её повторить с своими
// множителями. Раскладка packed — как у _MaskMap: R metallic, G occlusion, A smoothness;
// канал B (высота) не читается — потребителя у слоя нет.
half3 ENV_SampleLayerMaterialChannels(float2 uv, TEXTURE2D_PARAM(maskMap, samp),
    TEXTURE2D(metallicMap), TEXTURE2D(occlusionMap), TEXTURE2D(smoothnessMap))
{
#if defined(_MASKMAP_SEPARATE)
    return half3(SAMPLE_TEXTURE2D(metallicMap, samp, uv).r,
                 SAMPLE_TEXTURE2D(occlusionMap, samp, uv).r,
                 SAMPLE_TEXTURE2D(smoothnessMap, samp, uv).r);
#else
    half4 packed = SAMPLE_TEXTURE2D(maskMap, samp, uv);
    return half3(packed.r, packed.g, packed.a);
#endif
}

// Подмешивание слоя в поверхность — вклад в альбедо/металличность/гладкость/затенение
// одинаков для ForwardLit и Meta-пасса, оба зовут эту функцию с одной и той же маской.
// alpha — по той же причине, что описана в шапке ApplyHeightGradient ниже: подмешивать
// после AlphaModulate надо тем же способом, иначе прозрачная поверхность красит фон.
//
// Металличность/гладкость/затенение — число на весь слой либо, под _OVERLAY_MAPS_0 (тикет 07),
// своя карта комплекта: та же формула "карта × ползунок", что у базы. Без карты (keyword выключен)
// occlusion = 1.0 — база под сплошным снегом не может остаться такой, как была (тёмный шов кладки
// просвечивал бы сквозь белое), а своего AO у слоя без карты нет. С картой слой затеняет себя сам.
void ApplyOverlayLayer0(float3 positionPS, half mask, half alpha, inout SurfaceData surfaceData)
{
#if defined(_OVERLAY_LAYER_0)
    float2 overlayUV = ENV_OverlayUV0(positionPS);
    half3 overlayAlbedo = SAMPLE_TEXTURE2D(_OverlayMap0, sampler_OverlayMap0, overlayUV).rgb * _OverlayColor0.rgb;

    half overlayMetallic = _OverlayMetallic0;
    half overlaySmoothness = _OverlaySmoothness0;
    half overlayOcclusion = half(1.0);
#if defined(_OVERLAY_MAPS_0)
    half3 overlayChannels = ENV_SampleLayerMaterialChannels(overlayUV,
        TEXTURE2D_ARGS(_OverlayMaskMap0, sampler_OverlayMap0),
        _OverlayMetallicMap0, _OverlayOcclusionMap0, _OverlaySmoothnessMap0);
    overlayMetallic *= overlayChannels.r;
    overlaySmoothness *= overlayChannels.b;
    // Тикет 2-03: своя сила затенения слоя, не общая _OcclusionStrength базы.
    overlayOcclusion = LerpWhiteTo(overlayChannels.g, _OverlayOcclusionStrength0);
#endif

    surfaceData.albedo = lerp(surfaceData.albedo, AlphaModulate(overlayAlbedo, alpha), mask);
    surfaceData.metallic = lerp(surfaceData.metallic, overlayMetallic, mask);
    surfaceData.smoothness = lerp(surfaceData.smoothness, overlaySmoothness, mask);
    surfaceData.occlusion = lerp(surfaceData.occlusion, overlayOcclusion, mask);
#endif
}

// Мировая нормаль слоя — считается только в ForwardLit: UnityMetaFragment (Meta-пасс)
// нормаль не читает вовсе, писать её там — мёртвый код.
//
// Планарный фрейм проекции: T=+X, B=+Z, N=+Y в пространстве проекции — тот же порядок,
// что и у positionPS.xz выше. Распаковка нормали — как в SampleNormal (SurfaceInput.hlsl),
// включая ветку BUMP_SCALE_NOT_SUPPORTED.
//
// Отображение компонент (x, y, z) → (x, z, y), БЕЗ смены знака: разобрано при планировании,
// что фрейм левый, но выпуклость текстуры остаётся выпуклостью именно потому, что u→X
// и v→Z идут напрямую. Перестановка знака у любой из компонент — то, что это сломает.
half3 GetOverlayNormalWS0(float3 positionPS)
{
    float2 overlayUV = ENV_OverlayUV0(positionPS);
    half4 packedNormal = SAMPLE_TEXTURE2D(_OverlayNormalMap0, sampler_OverlayMap0, overlayUV);
#if BUMP_SCALE_NOT_SUPPORTED
    half3 normalTS = UnpackNormal(packedNormal);
#else
    half3 normalTS = UnpackNormalScale(packedNormal, _OverlayNormalScale0);
#endif
    // Тикет 2-03: бамп из Height Top складывается с картой нормалей слоя в её же
    // тангенциальном фрейме, до перестановки компонент (x,z,y) в мировой/объектный фрейм
    // проекции. Принудительный _NORMALMAP здесь не нужен (в отличие от базы в 2-02):
    // эта функция вызывается безусловно и пишет прямо в inputData.normalWS.
#if defined(_OVERLAY_HEIGHT_0)
    normalTS = BlendNormal(normalTS, ENV_OverlayHeightBumpTS0(overlayUV));
#endif
    float3 normalPS = float3(normalTS.x, normalTS.z, normalTS.y);
    return half3(normalize(GetProjectionNormal(normalPS)));
}

// Смешивание материалов мазком (_MATERIAL_MIX), тикет 06: покрытие обоих слоёв.
//
// Контракт каналов вершинного цвета один на оба слоя: R — резерв, G — слой 1, B — слой 2,
// A — резерв. Резерв не читается вообще, поэтому «покраска в зарезервированные каналы
// не влияет на вид» выполняется по построению, а не проверкой. Раскладка совпала
// с дефолтной раскладкой кисти RealBlend (None, G, B, A, R) независимо.
//
// В режиме RGB Noise у каждого слоя своё покрытие. В режиме Vertex Color слой 1 читает G,
// слой 2 — B. Ручка неактивного режима сохраняется в материале, но в расчёт не входит.
half2 SampleMixCoverage(half4 vertexColor)
{
#if defined(_MIX_MASK_TEXTURE_1)
    half coverage1 = _MixCoverage1;
#else
    half coverage1 = vertexColor.g;
#endif
#if defined(_MIX_MASK_TEXTURE_2)
    half coverage2 = _MixCoverage2;
#else
    half coverage2 = vertexColor.b;
#endif
    return half2(coverage1, coverage2);
}

// Один подмешиваемый слой. Именно функция, а не два развёрнутых блока на два слоя: тикет
// требует, чтобы оба слоя вели себя одинаково, и общая функция это гарантирует, а не обещает.
//
// Текстуры слоёв идут по UV БАЗЫ (со своим множителем тайлинга), а не планарно. Довод
// решающий и технический: нормаль слоя тогда смешивается прямо в surfaceData.normalTS,
// то есть ДО сборки мировой нормали, — и маска наноса ниже по коду видит уже смешанную
// нормаль сама, без единой правки. Планарный вариант потребовал бы дубля GetOverlayNormalWS0
// на слой и смешивания нормалей в мировом пространстве после InitializeInputData.
//
// Нормали лерпятся без нормализации здесь: оба потребителя ниже (ENV_ResolveNormalWS
// и InitializeInputData) зовут NormalizeNormalPerPixel сами.
//
// Затенение — тот же приём, что в слое наноса: AO базы описывает швы базы, под заменённым
// материалом он не к месту. Без карты комплекта (тикет 07) — lerp к единице, своего AO нет;
// с картой — LerpWhiteTo(channels.g, occlusionStrength) своей силой слоя (тикет 2-03,
// _MixOcclusionStrength1/2), не общей _OcclusionStrength базы.
//
// materialChannels приходит параметром, а не читается здесь: keyword комплекта свой у каждого
// слоя (_MIX_MAPS_1 / _MIX_MAPS_2), а функция одна на оба — ветвление на стороне вызова,
// в ApplyMaterialMix. Без карты слоя вызывающий передаёт half3(1,1,1) — нейтраль формулы
// "канал × ползунок", арифметически равная сегодняшнему поведению без комплекта.
//
// layerUV приходит готовым, не (uv, tiling): та же координата нужна и альбедо/нормали слоя,
// и его комплекту карт на стороне вызова — вычислять её дважды было бы тем расхождением,
// которое разъезжается молча через год.
//
// alpha — по той же причине, что у наноса и градиента: подмешивать после AlphaModulate надо
// тем же способом, иначе прозрачная поверхность красит фон.
void ENV_ApplyMixLayer(
    float2 layerUV, half mask, half alpha,
    TEXTURE2D_PARAM(albedoMap, albedoSampler),
    TEXTURE2D_PARAM(normalMap, normalSampler),
    half3 tint, half normalScale, half metallic, half smoothness, half3 materialChannels,
    half occlusionStrength,
    inout SurfaceData surfaceData)
{
    half3 layerAlbedo = SAMPLE_TEXTURE2D(albedoMap, albedoSampler, layerUV).rgb * tint;

    half4 packedNormal = SAMPLE_TEXTURE2D(normalMap, normalSampler, layerUV);
#if BUMP_SCALE_NOT_SUPPORTED
    half3 layerNormalTS = UnpackNormal(packedNormal);
#else
    half3 layerNormalTS = UnpackNormalScale(packedNormal, normalScale);
#endif

    half layerMetallic = metallic * materialChannels.r;
    half layerSmoothness = smoothness * materialChannels.b;
    // Тикет 2-03: своя сила затенения слоя, не общая _OcclusionStrength базы.
    half layerOcclusion = LerpWhiteTo(materialChannels.g, occlusionStrength);

    surfaceData.albedo     = lerp(surfaceData.albedo, AlphaModulate(layerAlbedo, alpha), mask);
    surfaceData.normalTS   = lerp(surfaceData.normalTS, layerNormalTS, mask);
    surfaceData.metallic   = lerp(surfaceData.metallic, layerMetallic, mask);
    surfaceData.smoothness = lerp(surfaceData.smoothness, layerSmoothness, mask);
    surfaceData.occlusion  = lerp(surfaceData.occlusion, layerOcclusion, mask);
}

// Смешивание материалов мазком целиком, тикет 06. Зовётся ДО слоя наноса: снег падает
// на то, что под ним уже сложилось, и маска наноса считается по смешанной нормали.
//
// Одна общая карта шума, но каждый слой читает её своей проекцией, тайлингом и поворотом.
//
// При выключенном _PATTERN источник = 0.5 у обоих слоёв постоянно — без узора детализацию
// брать неоткуда, граница идёт жёстко по покрытию, мягчит её только Edge Softness слоя.
//
// normalWS — геометрическая мировая нормаль (до карты нормалей, эта функция зовётся раньше
// её сборки): нужна только как normalPS для весов трипланара узора смешивания.
//
// Слой 2 ложится ПОСЛЕ слоя 1 — порядок фиксирован тикетом 04. Кисть держит сумму весов <= 1
// («база есть остаток»), так что на практике перекрытия почти нет; при обоих каналах
// на максимуме выигрывает второй, и это названное решение, а не побочный эффект.
void ApplyMaterialMix(float2 uv, float3 positionPS, half3 normalWS, half4 vertexColor,
                      half alpha, inout SurfaceData surfaceData)
{
#if defined(_MATERIAL_MIX)
    half2 coverage = SampleMixCoverage(vertexColor);

    half source1 = half(0.5);
#if defined(_PATTERN)
    half3 normalPS = ENV_GetProjectionNormalFromWorld(normalWS);
    half3 noise1 = ENV_SampleNoiseMix1(uv, positionPS, normalPS);
    source1 = ENV_NoiseSource(half(0.5), noise1, _MixPatternChannel1, _MixPatternStrength1, _MixPatternBias1);
#endif

    float2 layerUV1 = uv * _MixMap1_ST.xy + _MixMap1_ST.zw;
    half3 materialChannels1 = half3(1.0, 1.0, 1.0);
#if defined(_MIX_MAPS_1)
    materialChannels1 = ENV_SampleLayerMaterialChannels(layerUV1,
        TEXTURE2D_ARGS(_MixMaskMap1, sampler_MixMap1),
        _MixMetallicMap1, _MixOcclusionMap1, _MixSmoothnessMap1);
#endif

    half mask1 = ENV_ThresholdMask(float(source1), coverage.x, _MixEdgeSoftness1);
    ENV_ApplyMixLayer(layerUV1, mask1, alpha,
        TEXTURE2D_ARGS(_MixMap1, sampler_MixMap1),
        TEXTURE2D_ARGS(_MixNormalMap1, sampler_MixMap1),
        _MixColor1.rgb, _MixNormalScale1, _MixMetallic1, _MixSmoothness1, materialChannels1,
        _MixOcclusionStrength1,
        surfaceData);

#if defined(_MATERIAL_MIX_2)
    half source2 = half(0.5);
#if defined(_PATTERN)
    half3 noise2 = ENV_SampleNoiseMix2(uv, positionPS, normalPS);
    source2 = ENV_NoiseSource(half(0.5), noise2, _MixPatternChannel2, _MixPatternStrength2, _MixPatternBias2);
#endif

    float2 layerUV2 = uv * _MixMap2_ST.xy + _MixMap2_ST.zw;
    half3 materialChannels2 = half3(1.0, 1.0, 1.0);
#if defined(_MIX_MAPS_2)
    materialChannels2 = ENV_SampleLayerMaterialChannels(layerUV2,
        TEXTURE2D_ARGS(_MixMaskMap2, sampler_MixMap1),
        _MixMetallicMap2, _MixOcclusionMap2, _MixSmoothnessMap2);
#endif

    half mask2 = ENV_ThresholdMask(float(source2), coverage.y, _MixEdgeSoftness2);
    ENV_ApplyMixLayer(layerUV2, mask2, alpha,
        TEXTURE2D_ARGS(_MixMap2, sampler_MixMap1),
        TEXTURE2D_ARGS(_MixNormalMap2, sampler_MixMap1),
        _MixColor2.rgb, _MixNormalScale2, _MixMetallic2, _MixSmoothness2, materialChannels2,
        _MixOcclusionStrength2,
        surfaceData);
#endif
#endif
}

// ENV_Lit: градиент по высоте (_HEIGHT_GRADIENT). Раньше жил в ENV_LitForwardPass.hlsl —
// переехал сюда вместе с собственным мета-пассом (ENV_LitMetaPass.hlsl), который теперь
// тоже его вызывает. Принимает готовую высоту, а не позицию: в мета-пассе обратного
// преобразования из мировой позиции нет, каждый пасс подаёт число своим способом через
// GetProjectionPosition выше.
//
// alpha приезжает параметром из-за порядка: InitializeStandardLitSurfaceData уже прогнала
// альбедо через AlphaModulate (на Multiply-блендинге это lerp к белому по альфе), а мы
// подмешиваем цвет после неё. Подмешать сырой gradientColor — значит вернуть тонировку
// в полную силу мимо этого затухания, и почти прозрачная Multiply-поверхность начнёт
// красить фон. Модулируем цвет градиента тем же способом: lerp(AM(a), AM(g), s) тождественно
// равно AM(lerp(a, g, s)), то есть результат тот же, как если бы градиент шёл до модуляции.
// На непрозрачном пути AlphaModulate — тождество, там ничего не меняется.
half3 ApplyHeightGradient(half3 albedo, float height, half alpha)
{
#if defined(_HEIGHT_GRADIENT)
    float range = max(_GradientMaxHeight - _GradientMinHeight, 1e-5);
    float t = saturate((height - _GradientMinHeight) / range);
    half4 gradientColor = lerp(_GradientColor01, _GradientColor02, t);
    // Альфа цвета — локальная сила подмеса поверх общего _GradientStrength. Лерп идёт по
    // half4, поэтому альфа тоже интерполируется по высоте: цвет с нулевой альфой на одном
    // конце даёт градиент, затухающий в исходное альбедо, а не в другой цвет. Без этого
    // альфа в пикере цвета редактировалась бы, но ни на что не влияла.
    half gradientWeight = _GradientStrength * gradientColor.a;
    albedo = lerp(albedo, AlphaModulate(gradientColor.rgb, alpha), gradientWeight);
#endif
    return albedo;
}

inline void InitializeStandardLitSurfaceData(float2 uv, out SurfaceData outSurfaceData)
{
    half4 albedoAlpha = SampleAlbedoAlpha(uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap));
    outSurfaceData.alpha = Alpha(albedoAlpha.a, _BaseColor, _Cutoff);

    outSurfaceData.albedo = albedoAlpha.rgb * _BaseColor.rgb;
    outSurfaceData.albedo = ApplyAlbedoAdjust(outSurfaceData.albedo);
    outSurfaceData.albedo = AlphaModulate(outSurfaceData.albedo, outSurfaceData.alpha);

    half metallicChannel;
    half occlusionChannel;
    half smoothnessChannel;

#if defined(_MASKMAP_SEPARATE)
    metallicChannel = SAMPLE_TEXTURE2D(_MetallicMap, sampler_MetallicMap, uv).r;
    occlusionChannel = SAMPLE_TEXTURE2D(_OcclusionMap, sampler_MetallicMap, uv).r;
    smoothnessChannel = SAMPLE_TEXTURE2D(_SmoothnessMap, sampler_MetallicMap, uv).r;
#else
    half4 mask = SampleMaskMap(uv);
    metallicChannel = mask.r;
    occlusionChannel = mask.g;
    smoothnessChannel = mask.a;
#endif

    outSurfaceData.metallic = metallicChannel * _Metallic;
    outSurfaceData.specular = half3(0.0, 0.0, 0.0);

    // Инверсия roughness→smoothness убрана (тикет 07, грилл 15.09.2026): конвенция канала —
    // ответственность художника за файл, не ручка материала. Канал читается как smoothness
    // всегда; пустой слот даёт "white" = 1, и формула падает на чистый _Smoothness.
    outSurfaceData.smoothness = smoothnessChannel * _Smoothness;

    // Без инверсии зелёного канала: она применилась бы только здесь, а пакетные пассы
    // DepthNormals / ShadowCaster сэмплят нормаль сами — см. комментарий в ENV_Lit.shader.
    outSurfaceData.normalTS = SampleNormal(uv, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap), _BumpScale);
    // Бамп из высоты базы (_HEIGHT_BUMP, тикет 02) — до слоёв смешивания (они перезаписывают
    // normalTS по маске, рельеф базы под заменённым материалом оставаться не должен) и до
    // сборки нормали для маски наноса (ENV_ResolveNormalWS), то есть нанос видит рельеф базы.
#if defined(_HEIGHT_BUMP)
    outSurfaceData.normalTS = BlendNormal(outSurfaceData.normalTS, ENV_HeightBumpTS(uv));
#endif

    outSurfaceData.occlusion = SampleMaskMapOcclusion(occlusionChannel);

    // Свой тайлинг у эмиссии: uv приезжает уже трансформированным по _BaseMap_ST (и в
    // ForwardLit, и в Meta-пассе). URP-шный UNDO_TRANSFORM_TEX сюда не годится — вне
    // DEBUG_DISPLAY это no-op (see Debug/DebuggingCommon.hlsl), задуманный как быстрый путь
    // для отладочного оверлея, а не как всегда работающая функция. Разворачиваем сами.
    // Тайлинг 0 инспектор принимает молча — без защиты деление даёт inf, TRANSFORM_TEX
    // тащит его дальше, и эмиссия возвращает NaN, который блум размазывает по кадру.
    // Знак сохраняем: отрицательный тайлинг — это законное зеркалирование.
    float2 safeTiling = max(abs(_BaseMap_ST.xy), 1e-5);
    float2 baseTiling = _BaseMap_ST.xy < 0.0 ? -safeTiling : safeTiling;
    float2 uv0 = (uv - _BaseMap_ST.zw) / baseTiling;
    float2 emissionUV = TRANSFORM_TEX(uv0, _EmissionMap);
    outSurfaceData.emission = SampleEmission(emissionUV, _EmissionColor.rgb, TEXTURE2D_ARGS(_EmissionMap, sampler_EmissionMap));

    outSurfaceData.clearCoatMask = half(0.0);
    outSurfaceData.clearCoatSmoothness = half(0.0);
}

#endif // SPIDERRIG_ENV_LIT_INPUT_INCLUDED
