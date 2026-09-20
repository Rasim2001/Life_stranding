#ifndef SPIDERRIG_ENV_LIT_INPUT_INCLUDED
#define SPIDERRIG_ENV_LIT_INPUT_INCLUDED

// ENV_Lit — не монолит: наши пассы ForwardLit и Meta держатся на контракте
// InitializeStandardLitSurfaceData(geo, out SurfaceData), а ShadowCaster / DepthOnly /
// DepthNormals — пакетные пассы URP без правок, они эту функцию не зовут и читают только
// _BaseMap по Mesh UV. Этот файл — наш код: что за поверхность. Спек:
// docs/lighting-and-shading.md §6.
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
// Поворот рисунка карт Base, градусы — одна ручка на все карты (см. ENV_BuildBaseProjection).
half _BaseRotation;
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
// Правка альбедо (_ALBEDO_ADJUST): сдвиг тона, насыщенность, затем контраст и яркость.
// До освещения — см. §6 "Граница законности эффекта" в спеке.
half _HueShift;
half _Saturation;
half _Contrast;
half _Brightness;
// Градиент по высоте (_HEIGHT_GRADIENT): подмешивание цвета к альбедо, не умножение —
// умножение умеет только темнить. Пространство — своё, _GradientSpace, см. ENV_GradientHeight ниже.
float _GradientMinHeight;
float _GradientMaxHeight;
half4 _GradientColor01;
half4 _GradientColor02;
half _GradientStrength;
// Слой наноса (_OVERLAY_LAYER_0): второй материал поверх основного — снег/пыль/грязь.
// Индекс 0 в именах — с самого начала: материал хранит значения по имени свойства,
// переименование под второй слой позже молча обнулит настройки на всех материалах.
// Пространство слоя — своё, _OverlaySpace0 (карты, наклон и шум Top), см. ENV_OverlayPosition.
half4 _OverlayColor0;
// Штатный Scale/Offset слота Albedo Top (тикет 2-03) — заменил самодельный _OverlayTiling0,
// одна координата на все карты слоя (ENV_OverlayUV0).
float4 _OverlayMap0_ST;
half _OverlayNormalScale0;
half _OverlayMetallic0;
half _OverlaySmoothness0;
half _OverlayCoverage0;
half _OverlayEdgeSoftness0;
half _OverlayThickness0;
half _OverlayEdgeThickness0;
half _OverlayInheritRelief0;
// Height Strength управляет только собственным бампом Top; маска и наследование рельефа
// используют независимые свойства.
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
// Поворот рисунка карт слоя, градусы — одна ручка на все карты слоя (ENV_BuildProjection).
half _MixRotation1;
half _MixNormalScale1;
half _MixMetallic1;
half _MixSmoothness1;
half4 _MixColor2;
float4 _MixMap2_ST;
half _MixRotation2;
half _MixNormalScale2;
half _MixMetallic2;
half _MixSmoothness2;
// Рельеф слоя 1 (тикет 05): сила бампа Height, подавление унаследованного рельефа Base, подписанная
// кромка (-1 углубление .. +1 выступ) и наследование. Texel size обоих режимов карты высоты слоя —
// как у Base, обязан жить в CBUFFER, иначе уедет в $Globals и уронит SRP Batcher.
half _MixHeightStrength1;
half _MixReliefSmoothing1;
half _MixEdgeThickness1;
half _MixInheritRelief1;
float4 _MixHeightMap1_TexelSize;
float4 _MixMaskMap1_TexelSize;
// Рельеф слоя 2 (тикет 06) — те же свойства, что у слоя 1.
half _MixHeightStrength2;
half _MixReliefSmoothing2;
half _MixEdgeThickness2;
half _MixInheritRelief2;
float4 _MixHeightMap2_TexelSize;
float4 _MixMaskMap2_TexelSize;
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
    UNITY_DOTS_INSTANCED_PROP(float , _BaseRotation)
    UNITY_DOTS_INSTANCED_PROP(float4, _EmissionColor)
    UNITY_DOTS_INSTANCED_PROP(float , _Cutoff)
    UNITY_DOTS_INSTANCED_PROP(float , _Metallic)
    UNITY_DOTS_INSTANCED_PROP(float , _Smoothness)
    UNITY_DOTS_INSTANCED_PROP(float , _OcclusionStrength)
    UNITY_DOTS_INSTANCED_PROP(float , _BumpScale)
    UNITY_DOTS_INSTANCED_PROP(float , _HeightStrength)
    UNITY_DOTS_INSTANCED_PROP(float , _Surface)
    UNITY_DOTS_INSTANCED_PROP(float , _HueShift)
    UNITY_DOTS_INSTANCED_PROP(float , _Saturation)
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
    UNITY_DOTS_INSTANCED_PROP(float , _OverlayThickness0)
    UNITY_DOTS_INSTANCED_PROP(float , _OverlayEdgeThickness0)
    UNITY_DOTS_INSTANCED_PROP(float , _OverlayInheritRelief0)
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
    UNITY_DOTS_INSTANCED_PROP(float , _MixRotation1)
    UNITY_DOTS_INSTANCED_PROP(float , _MixNormalScale1)
    UNITY_DOTS_INSTANCED_PROP(float , _MixMetallic1)
    UNITY_DOTS_INSTANCED_PROP(float , _MixSmoothness1)
    UNITY_DOTS_INSTANCED_PROP(float4, _MixColor2)
    UNITY_DOTS_INSTANCED_PROP(float4, _MixMap2_ST)
    UNITY_DOTS_INSTANCED_PROP(float , _MixRotation2)
    UNITY_DOTS_INSTANCED_PROP(float , _MixNormalScale2)
    UNITY_DOTS_INSTANCED_PROP(float , _MixMetallic2)
    UNITY_DOTS_INSTANCED_PROP(float , _MixSmoothness2)
    UNITY_DOTS_INSTANCED_PROP(float , _MixHeightStrength1)
    UNITY_DOTS_INSTANCED_PROP(float , _MixReliefSmoothing1)
    UNITY_DOTS_INSTANCED_PROP(float , _MixEdgeThickness1)
    UNITY_DOTS_INSTANCED_PROP(float , _MixInheritRelief1)
    UNITY_DOTS_INSTANCED_PROP(float , _MixHeightStrength2)
    UNITY_DOTS_INSTANCED_PROP(float , _MixReliefSmoothing2)
    UNITY_DOTS_INSTANCED_PROP(float , _MixEdgeThickness2)
    UNITY_DOTS_INSTANCED_PROP(float , _MixInheritRelief2)
UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)

static float4 unity_DOTS_Sampled_BaseColor;
static float  unity_DOTS_Sampled_BaseRotation;
static float4 unity_DOTS_Sampled_EmissionColor;
static float  unity_DOTS_Sampled_Cutoff;
static float  unity_DOTS_Sampled_Metallic;
static float  unity_DOTS_Sampled_Smoothness;
static float  unity_DOTS_Sampled_OcclusionStrength;
static float  unity_DOTS_Sampled_BumpScale;
static float  unity_DOTS_Sampled_HeightStrength;
static float  unity_DOTS_Sampled_Surface;
static float  unity_DOTS_Sampled_HueShift;
static float  unity_DOTS_Sampled_Saturation;
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
static float  unity_DOTS_Sampled_OverlayThickness0;
static float  unity_DOTS_Sampled_OverlayEdgeThickness0;
static float  unity_DOTS_Sampled_OverlayInheritRelief0;
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
static float  unity_DOTS_Sampled_MixRotation1;
static float  unity_DOTS_Sampled_MixNormalScale1;
static float  unity_DOTS_Sampled_MixMetallic1;
static float  unity_DOTS_Sampled_MixSmoothness1;
static float4 unity_DOTS_Sampled_MixColor2;
static float4 unity_DOTS_Sampled_MixMap2_ST;
static float  unity_DOTS_Sampled_MixRotation2;
static float  unity_DOTS_Sampled_MixNormalScale2;
static float  unity_DOTS_Sampled_MixMetallic2;
static float  unity_DOTS_Sampled_MixSmoothness2;
static float  unity_DOTS_Sampled_MixHeightStrength1;
static float  unity_DOTS_Sampled_MixReliefSmoothing1;
static float  unity_DOTS_Sampled_MixEdgeThickness1;
static float  unity_DOTS_Sampled_MixInheritRelief1;
static float  unity_DOTS_Sampled_MixHeightStrength2;
static float  unity_DOTS_Sampled_MixReliefSmoothing2;
static float  unity_DOTS_Sampled_MixEdgeThickness2;
static float  unity_DOTS_Sampled_MixInheritRelief2;

void SetupDOTSENVLitMaterialPropertyCaches()
{
    unity_DOTS_Sampled_BaseColor             = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _BaseColor);
    unity_DOTS_Sampled_BaseRotation          = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _BaseRotation);
    unity_DOTS_Sampled_EmissionColor         = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _EmissionColor);
    unity_DOTS_Sampled_Cutoff                = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _Cutoff);
    unity_DOTS_Sampled_Metallic              = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _Metallic);
    unity_DOTS_Sampled_Smoothness            = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _Smoothness);
    unity_DOTS_Sampled_OcclusionStrength     = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _OcclusionStrength);
    unity_DOTS_Sampled_BumpScale             = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _BumpScale);
    unity_DOTS_Sampled_HeightStrength        = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _HeightStrength);
    unity_DOTS_Sampled_Surface               = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _Surface);
    unity_DOTS_Sampled_HueShift              = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _HueShift);
    unity_DOTS_Sampled_Saturation            = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _Saturation);
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
    unity_DOTS_Sampled_OverlayThickness0     = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _OverlayThickness0);
    unity_DOTS_Sampled_OverlayEdgeThickness0 = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _OverlayEdgeThickness0);
    unity_DOTS_Sampled_OverlayInheritRelief0 = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _OverlayInheritRelief0);
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
    unity_DOTS_Sampled_MixRotation1          = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _MixRotation1);
    unity_DOTS_Sampled_MixNormalScale1       = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _MixNormalScale1);
    unity_DOTS_Sampled_MixMetallic1          = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _MixMetallic1);
    unity_DOTS_Sampled_MixSmoothness1        = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _MixSmoothness1);
    unity_DOTS_Sampled_MixColor2             = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _MixColor2);
    unity_DOTS_Sampled_MixMap2_ST            = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _MixMap2_ST);
    unity_DOTS_Sampled_MixRotation2          = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _MixRotation2);
    unity_DOTS_Sampled_MixNormalScale2       = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _MixNormalScale2);
    unity_DOTS_Sampled_MixMetallic2          = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _MixMetallic2);
    unity_DOTS_Sampled_MixSmoothness2        = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _MixSmoothness2);
    unity_DOTS_Sampled_MixHeightStrength1    = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _MixHeightStrength1);
    unity_DOTS_Sampled_MixReliefSmoothing1   = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _MixReliefSmoothing1);
    unity_DOTS_Sampled_MixEdgeThickness1     = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _MixEdgeThickness1);
    unity_DOTS_Sampled_MixInheritRelief1     = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _MixInheritRelief1);
    unity_DOTS_Sampled_MixHeightStrength2    = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _MixHeightStrength2);
    unity_DOTS_Sampled_MixReliefSmoothing2   = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _MixReliefSmoothing2);
    unity_DOTS_Sampled_MixEdgeThickness2     = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _MixEdgeThickness2);
    unity_DOTS_Sampled_MixInheritRelief2     = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _MixInheritRelief2);
}

#undef UNITY_SETUP_DOTS_MATERIAL_PROPERTY_CACHES
#define UNITY_SETUP_DOTS_MATERIAL_PROPERTY_CACHES() SetupDOTSENVLitMaterialPropertyCaches()

#define _BaseColor               unity_DOTS_Sampled_BaseColor
#define _BaseRotation            unity_DOTS_Sampled_BaseRotation
#define _EmissionColor           unity_DOTS_Sampled_EmissionColor
#define _Cutoff                  unity_DOTS_Sampled_Cutoff
#define _Metallic                unity_DOTS_Sampled_Metallic
#define _Smoothness              unity_DOTS_Sampled_Smoothness
#define _OcclusionStrength       unity_DOTS_Sampled_OcclusionStrength
#define _BumpScale               unity_DOTS_Sampled_BumpScale
#define _HeightStrength          unity_DOTS_Sampled_HeightStrength
#define _Surface                 unity_DOTS_Sampled_Surface
#define _HueShift                unity_DOTS_Sampled_HueShift
#define _Saturation              unity_DOTS_Sampled_Saturation
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
#define _OverlayThickness0       unity_DOTS_Sampled_OverlayThickness0
#define _OverlayEdgeThickness0   unity_DOTS_Sampled_OverlayEdgeThickness0
#define _OverlayInheritRelief0   unity_DOTS_Sampled_OverlayInheritRelief0
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
#define _MixRotation1            unity_DOTS_Sampled_MixRotation1
#define _MixNormalScale1         unity_DOTS_Sampled_MixNormalScale1
#define _MixMetallic1            unity_DOTS_Sampled_MixMetallic1
#define _MixSmoothness1          unity_DOTS_Sampled_MixSmoothness1
#define _MixColor2               unity_DOTS_Sampled_MixColor2
#define _MixMap2_ST              unity_DOTS_Sampled_MixMap2_ST
#define _MixRotation2            unity_DOTS_Sampled_MixRotation2
#define _MixNormalScale2         unity_DOTS_Sampled_MixNormalScale2
#define _MixMetallic2            unity_DOTS_Sampled_MixMetallic2
#define _MixSmoothness2          unity_DOTS_Sampled_MixSmoothness2
#define _MixHeightStrength1      unity_DOTS_Sampled_MixHeightStrength1
#define _MixReliefSmoothing1     unity_DOTS_Sampled_MixReliefSmoothing1
#define _MixEdgeThickness1       unity_DOTS_Sampled_MixEdgeThickness1
#define _MixInheritRelief1       unity_DOTS_Sampled_MixInheritRelief1
#define _MixHeightStrength2      unity_DOTS_Sampled_MixHeightStrength2
#define _MixReliefSmoothing2     unity_DOTS_Sampled_MixReliefSmoothing2
#define _MixEdgeThickness2       unity_DOTS_Sampled_MixEdgeThickness2
#define _MixInheritRelief2       unity_DOTS_Sampled_MixInheritRelief2

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

half SampleMaskMapOcclusion(half occlusionChannel)
{
    return LerpWhiteTo(occlusionChannel, _OcclusionStrength);
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

// Координата слоя: планарная проекция XZ в пространстве слоя (_OverlaySpace0) со штатным
// Scale/Offset слота Albedo Top.
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
// (ENV_TopReliefBaseHeightNormalTS): две ручки силы высоты в одном материале обязаны ощущаться одинаково.
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
// Высота слоя 1 (тикет 05) — раздельный режим, на общем sampler_MixMap1. В упакованном — канал B _MixMaskMap1.
TEXTURE2D(_MixHeightMap1);
// Высота слоя 2 (тикет 06) — так же: раздельный режим на sampler_MixMap1, в упакованном — канал B _MixMaskMap2.
TEXTURE2D(_MixHeightMap2);

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
// в обе стороны, а жёсткость концов маски держится на том, что источник её
// не превышает.
//
// Bias в инспекторе центрирован вокруг нуля. Нулю соответствует прежняя нейтраль 0.5;
// -1 эквивалентен прежнему Bias 1 и уменьшает покрытие, +1 эквивалентен Bias 0
// и наращивает покрытие. Множитель 0.5 сохраняет прежний диапазон смещения.
half ENV_NoiseSource(half base, half3 noiseRGB, half channel, half strength, half bias)
{
    half3 channelMask = channel < half(0.5) ? half3(1.0, 0.0, 0.0)
                       : channel < half(1.5) ? half3(0.0, 1.0, 0.0)
                       :                       half3(0.0, 0.0, 1.0);
    half sampleValue = dot(noiseRGB, channelMask);
    return saturate(base + (sampleValue - half(0.5) + bias * half(0.5)) * strength);
}

// Тело выборки карты шума слоя Top — planar XZ (mode 0, дефолт) / UV меша (mode 1) / трипланар
// (mode 2). Позиция и нормаль приходят уже В ПРОСТРАНСТВЕ СЛОЯ TOP (ENV_OverlayPosition /
// ENV_OverlayNormalFromWorld на стороне вызова) — трипланар следует _OverlaySpace0.
// UV — сырой uv0 меша: Tiling и Offset карт Base шум Top не двигают. Шум слоёв Blend сюда
// не входит — у него своё ядро проекций (ENV_MixLayerMask).
//
// mode — компайл-тайм литерал, приходящий с сайта вызова уже вычисленным из keyword'а
// (ENV_SampleNoiseOverlay0 ниже). При инлайне ветки по константе сворачиваются, мёртвого
// кода и лишнего ветвления вокруг выборки текстуры не остаётся.
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

// Правка альбедо (_ALBEDO_ADJUST), до освещения — см. спек, "Граница законности эффекта".
// Порядок фиксирован: сдвиг тона, насыщенность, потом контраст/яркость. Они часто нужны
// вместе на одной текстуре («подкрутить тон купленного кирпича»), поэтому один keyword на
// все — иначе отдельные keyword'ы дали бы кратно больше комбинаций варианта шейдера.
// Зовётся только для Base (см. InitializeStandardLitSurfaceData): цвета Blend/Top эта правка
// не видит, а Meta-пасс получает её той же функцией.
half3 ApplyAlbedoAdjust(half3 albedo)
{
#if defined(_ALBEDO_ADJUST)
    half3 hsv = RgbToHsv(albedo);
    hsv.x = frac(hsv.x + _HueShift);
    albedo = HsvToRgb(hsv);

    // Насыщенность: лерп от серого той же яркости (Rec.709, линейное пространство). -1 — ЧБ
    // с сохранением яркостных деталей, 0 — тождество, +1 — цветность вдвое. Серый остаётся
    // серым: веса Luminance в сумме дают 1. Свой saturate не нужен — на насыщенных цветах
    // результат может выйти за [0,1], но его срезает saturate ниже.
    half luma = Luminance(albedo);
    albedo = lerp(half3(luma, luma, luma), albedo, half(1.0) + _Saturation);

    // saturate обязателен: контраст 2.0 на тёмном текселе даёт (0 - 0.5) * 2 + 0.5 = -0.5,
    // а отрицательное альбедо — это отрицательный diffuse в HDR-таргете (тонмаппер и блум
    // на таком входе не определены) и отрицательное альбедо в запечке через Meta-пасс.
    albedo = saturate((albedo - half(0.5)) * _Contrast + half(0.5) + _Brightness);
#endif
    return albedo;
}

// Пространство эффектов. Общего переключателя нет: градиент по высоте (_GradientSpace) и слой
// Top (_OverlaySpace0) выбирают Local/World каждый сам; карты Base и Blend и маски Blend —
// свои режимы в ядре проекций (ENV_BuildProjection). Оба пасса, ForwardLit и Meta, зовут одни
// и те же функции, поэтому считают эффект одинаково. Неактивная ветка мертва и складывается
// компилятором — это keyword времени компиляции, не рантайм-ветвление.

// Высота для градиента: мировая (с учётом camera-relative) или объектная Y.
float ENV_GradientHeight(float3 positionWS, float3 positionOS)
{
#if defined(_GRADIENTSPACE_WORLD)
    return GetAbsolutePositionWS(positionWS).y;
#else
    return positionOS.y;
#endif
}

// Позиция в пространстве слоя Top — координата его карт (XZ) и Mesh/планарного шума.
float3 ENV_OverlayPosition(float3 positionWS, float3 positionOS)
{
#if defined(_OVERLAYSPACE0_WORLD)
    return GetAbsolutePositionWS(positionWS);
#else
    return positionOS;
#endif
}

// Пара к ENV_OverlayPosition — для направлений вместо точек. Принимает вектор, уже выраженный
// в осях пространства слоя (мировых или объектных), и переводит его в мировое пространство.
// Потребитель — ось «верха» для наклона поверхности (ComputeOverlayMask0). Для нормалей эта
// функция не годится, см. ENV_OverlayNormal ниже.
float3 ENV_OverlayDirection(float3 dirPS)
{
#if defined(_OVERLAYSPACE0_WORLD)
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
float3 ENV_OverlayNormal(float3 normalPS)
{
#if defined(_OVERLAYSPACE0_WORLD)
    return normalPS;
#else
    return TransformObjectToWorldNormal(normalPS);
#endif
}

// Обратная пара к ENV_OverlayNormal — переводит МИРОВУЮ нормаль в пространство слоя Top,
// а не наоборот. Нужна трипланару шума Top (тикет 06): веса ComputeTriplanarWeights обязаны
// смотреть на нормаль в том же пространстве, что и позиция, иначе оси весов и оси координат
// разъедутся. Под World — тождество, под Local — TransformWorldToObjectNormal (обратная
// транспонированная матрица; та же оговорка о неравномерном масштабе, что у ENV_OverlayNormal
// выше, здесь действует в обратную сторону).
half3 ENV_OverlayNormalFromWorld(half3 normalWS)
{
#if defined(_OVERLAYSPACE0_WORLD)
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

// Слой наноса (_OVERLAY_LAYER_0): маска зависит от сглаженной нормали меша до Normal/Height
// Base, Blend Layers и Top. Та же нормаль задаёт веса трипланарного RGB Noise. Coverage,
// Edge Softness и настройки Noise — единственные владельцы формы покрытия; рельеф,
// Relief Smoothing, Edge Thickness и наследование её не двигают.
// Порог Top в виде поля: d = source - (1 - coverage) — знаковая «глубина» относительно контура,
// mask = smoothstep(-hw, hw, d). Поле и полуширина нужны кромке слоя 1: градиент от гладкого d
// (а не от узкой полосы самой маски) не рвётся на блоки 2x2 пикселя. Концы покрытия 0/1 — жёсткие,
// и поле там уведено далеко от контура (±1), чтобы кромка не появлялась там, где перехода нет.
// fwidth — до всех выходов: покрытие слоя 1 может идти из вершинного цвета и меняться по пикселям,
// а производная внутри расходящейся ветки не определена.
struct ENV_MaskField
{
    half mask;
    float d;
    float hw;
};

ENV_MaskField ENV_TopReliefMaskField(float source, half coverage, half softness)
{
    float d = source - (1.0 - float(coverage));
    float pixelWidth = max(fwidth(d), 1e-4);
    ENV_MaskField f;
    f.hw = max(pixelWidth * 0.5, float(softness) * 0.25);
    f.d = d;
    f.mask = half(smoothstep(-f.hw, f.hw, d));
    if (coverage <= half(0.0))
    {
        f.mask = half(0.0);
        f.d = -1.0;
    }
    else if (coverage >= half(1.0))
    {
        f.mask = half(1.0);
        f.d = 1.0;
    }
    return f;
}

half ENV_TopReliefOverlayMask(float source, half coverage, half softness)
{
    return ENV_TopReliefMaskField(source, coverage, softness).mask;
}

// uv — сырой uv0 меша (шум Top в режиме Mesh UV); positionPS — ENV_OverlayPosition.
half ComputeOverlayMask0(float2 uv, float3 positionPS, half3 normalWS)
{
#if defined(_OVERLAY_LAYER_0)
    half3 upPS = half3(ENV_OverlayDirection(float3(0.0, 1.0, 0.0)));
    half slope = saturate(dot(normalWS, upPS));

    // ENV_Lit Top Relief contract: mask location depends on mesh slope and optional RGB noise only.
    // Base/Top relief, Thickness and inheritance cannot move this source.
    half baseSource = slope;

#if defined(_PATTERN)
    half3 normalPS = ENV_OverlayNormalFromWorld(normalWS);
    half3 noise = ENV_SampleNoiseOverlay0(uv, positionPS, normalPS);
    half source = ENV_NoiseSource(baseSource, noise, _PatternChannel0, _PatternStrength0, _PatternBias0);
#else
    half source = baseSource;
#endif

    return ENV_TopReliefOverlayMask(float(source), _OverlayCoverage0, _OverlayEdgeSoftness0);
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
    return half3(normalize(ENV_OverlayNormal(normalPS)));
}

// ENV_Lit Top Relief: frequency-selective inheritance accepted by the visual and GPU-cost gate.
half ENV_TopReliefReliefMipBias()
{
    half thickness = saturate(_OverlayThickness0);
    return thickness * thickness * half(5.0);
}

half3 ENV_TopReliefSampleNormalBias(TEXTURE2D_PARAM(map, samp), float2 uv, half scale, half bias)
{
    half4 packedNormal = SAMPLE_TEXTURE2D_BIAS(map, samp, uv, bias);
#if BUMP_SCALE_NOT_SUPPORTED
    return UnpackNormal(packedNormal);
#else
    return UnpackNormalScale(packedNormal, scale);
#endif
}

half ENV_TopReliefSampleBaseHeightBias(float2 uv, half bias)
{
#if defined(_MASKMAP_SEPARATE)
    return SAMPLE_TEXTURE2D_BIAS(_HeightMap, sampler_MetallicMap, uv, bias).r;
#else
    return SAMPLE_TEXTURE2D_BIAS(_MaskMap, sampler_MaskMap, uv, bias).b;
#endif
}

// Бамп из высоты базы (_HEIGHT_BUMP) — псевдо-нормаль конечной разностью карты высоты по u и v,
// шаг один тексель, без смещения геометрии. Разность вперёд (h - h_u), не центральная: две лишние
// выборки вместо четырёх, ценой полутекселя сдвига. Знак: h - h_u это -dh/du с точностью
// до положительного множителя, то есть выпуклость карты остаётся выпуклостью.
//
// GAIN подобран на глаз: сырая разность по текселю на типовой карте даёт 0.01-0.05, без
// множителя рельеф не виден ни на каком положении ползунка. При GAIN 8 ползунок 1 читается
// как обычная normal map, 6 — как контрастный рельеф. «Слабо/сильно» правится этим числом,
// а не диапазоном _HeightStrength. Тот же GAIN у бампа Top (ENV_OverlayHeightBumpTS0).
half3 ENV_TopReliefBaseHeightNormalTS(float2 uv, half bias)
{
    static const half GAIN = half(8.0);
#if defined(_MASKMAP_SEPARATE)
    float2 texel = _HeightMap_TexelSize.xy;
#else
    float2 texel = _MaskMap_TexelSize.xy;
#endif
    texel *= exp2(float(bias));
    half h = ENV_TopReliefSampleBaseHeightBias(uv, bias);
    half hU = ENV_TopReliefSampleBaseHeightBias(uv + float2(texel.x, 0.0), bias);
    half hV = ENV_TopReliefSampleBaseHeightBias(uv + float2(0.0, texel.y), bias);
    half2 slope = half2(h - hU, h - hV) * _HeightStrength * GAIN;
    return normalize(half3(slope.x, slope.y, half(1.0)));
}

half3 ENV_TopReliefFilteredBaseNormalTS(float2 uv, half bias)
{
#if defined(_NORMALMAP)
    half3 normalTS = ENV_TopReliefSampleNormalBias(
        TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap), uv, _BumpScale, bias);
#else
    half3 normalTS = half3(0.0, 0.0, 1.0);
#endif
#if defined(_HEIGHT_BUMP)
    normalTS = BlendNormal(normalTS, ENV_TopReliefBaseHeightNormalTS(uv, bias));
#endif
    return normalTS;
}

// ---------------------------------------------------------------------------------------------
// Проекция карт: Mesh UV / Local Triplanar / World Triplanar. Один механизм на Base и оба слоя
// Blend: одинаковые настройки дают одну и ту же координату и фазу, поэтому песок Base на груде
// и песок Blend на полу совпадают без подгонки чисел.
//
// Координата карты: uvT = T · R · S · p + o, где p — координата в плоскости проекции (в UV-режиме
// сырой uv0), S — знак плоскости (только трипланар), R — одна на все карты комплекта Rotation,
// T и o — Tiling и Offset комплекта (_BaseMap_ST / _MixMapN_ST). Tiling 1 — повтор на метр
// в World, на локальную единицу в Local, на UV-остров в Mesh UV. Слои независимы: Blend в Mesh UV
// берёт сырой uv0 и свой ST, Tiling и Offset Base на него не действуют.
//
// Вырез и Emission сюда не входят: они всегда по Mesh UV (geo.uv / geo.uv0), потому что тень
// и глубина — пакетные пассы URP, читающие только UV меша.
struct ENV_BaseGeometry
{
    float2 uv;         // TRANSFORM_TEX(uv0, _BaseMap): вырез и отладка, как раньше
    float2 uv0;        // сырой UV меша: обратное деление на Tiling теряет данные при Tiling 0
    float3 positionWS;
    float3 positionOS;
    half3 normalWS;    // нормализованная нормаль меша, до карт
    half4 tangentWS;   // xyz + знак; нулевой без _NORMALMAP — трипланарная нормаль его требует
};

ENV_BaseGeometry ENV_MakeBaseGeometry(float4 uv, float3 positionWS, half3 normalWS, half4 tangentWS)
{
    ENV_BaseGeometry geo;
    geo.uv = uv.xy;
    geo.uv0 = uv.zw;
    geo.positionWS = positionWS;
    geo.positionOS = TransformWorldToObject(positionWS);
    geo.normalWS = NormalizeNormalPerPixel(normalWS);
    geo.tangentWS = tangentWS;
    return geo;
}

// Режим проекции комплекта карт: 0 — Mesh UV, 1 — Local, 2 — World. Компайл-тайм литерал
// из keyword'ов (ENV_LitKeywords.hlsl): при инлайне ветки по константе сворачиваются, а лишние
// выборки трипланара в UV-режиме исчезают.
#if defined(_BASEPROJECTION_WORLD)
#define ENV_BASE_MODE 2u
#elif defined(_BASEPROJECTION_LOCAL)
#define ENV_BASE_MODE 1u
#else
#define ENV_BASE_MODE 0u
#endif

#if defined(_MIXPROJECTION1_WORLD)
#define ENV_MIX1_MODE 2u
#elif defined(_MIXPROJECTION1_LOCAL)
#define ENV_MIX1_MODE 1u
#else
#define ENV_MIX1_MODE 0u
#endif

#if defined(_MIXPROJECTION2_WORLD)
#define ENV_MIX2_MODE 2u
#elif defined(_MIXPROJECTION2_LOCAL)
#define ENV_MIX2_MODE 1u
#else
#define ENV_MIX2_MODE 0u
#endif

// Проекция RGB Noise (маски) слоёв Blend — отдельный режим, независимый от карт слоя.
#if defined(_MIXPATTERNPROJECTION1_WORLD)
#define ENV_MIXPAT1_MODE 2u
#elif defined(_MIXPATTERNPROJECTION1_LOCAL)
#define ENV_MIXPAT1_MODE 1u
#else
#define ENV_MIXPAT1_MODE 0u
#endif

#if defined(_MIXPATTERNPROJECTION2_WORLD)
#define ENV_MIXPAT2_MODE 2u
#elif defined(_MIXPATTERNPROJECTION2_LOCAL)
#define ENV_MIXPAT2_MODE 1u
#else
#define ENV_MIXPAT2_MODE 0u
#endif

struct ENV_Projection
{
    float2 uvA;        // Mesh UV: единственная координата. Трипланар: плоскость ZY (вес x)
    float2 uvB;        // плоскость XZ (вес y)
    float2 uvC;        // плоскость XY (вес z)
    half3 weights;
    half3 axisSign;    // знак нормали по осям: >= 0 даёт +1, а не 0 как sign()
    half3 normalPS;    // нормаль меша в пространстве проекции
};

float2x2 ENV_RotationMatrix(half rotationDeg)
{
    half s, c;
    sincos(radians(rotationDeg), s, c);
    return float2x2(c, -s, s, c);
}

ENV_Projection ENV_BuildProjection(ENV_BaseGeometry geo, float4 st, half rotationDeg, uint mode)
{
    ENV_Projection p = (ENV_Projection)0;
    float2x2 rotation = ENV_RotationMatrix(rotationDeg);
    float2 tiling = st.xy;
    float2 offset = st.zw;

    if (mode != 0u)
    {
        float3 positionPS;
        half3 normalPS;
        if (mode == 2u)
        {
            positionPS = GetAbsolutePositionWS(geo.positionWS);
            normalPS = geo.normalWS;
        }
        else
        {
            positionPS = geo.positionOS;
            // Обратно-транспонированная матрица — та же оговорка о неравномерном масштабе,
            // что у ENV_OverlayNormalFromWorld.
            normalPS = half3(TransformWorldToObjectNormal(float3(geo.normalWS)));
        }
        float2 uvXZ, uvXY, uvZY;
        GetTriplanarCoordinate(positionPS, uvXZ, uvXY, uvZY);

        // Обратная сторона плоскости зеркалит рисунок; переворот u по знаку нормали снимает это.
        half3 axisSign = half3(normalPS.x < 0.0 ? -1.0 : 1.0,
                               normalPS.y < 0.0 ? -1.0 : 1.0,
                               normalPS.z < 0.0 ? -1.0 : 1.0);
        uvZY.x *= axisSign.x;
        uvXZ.x *= axisSign.y;
        uvXY.x *= -axisSign.z;

        p.uvA = mul(rotation, uvZY) * tiling + offset;
        p.uvB = mul(rotation, uvXZ) * tiling + offset;
        p.uvC = mul(rotation, uvXY) * tiling + offset;
        p.weights = half3(ComputeTriplanarWeights(real3(normalPS)));
        p.axisSign = axisSign;
        p.normalPS = normalPS;
    }
    else
    {
        p.uvA = mul(rotation, geo.uv0) * tiling + offset;
        p.uvB = p.uvA;
        p.uvC = p.uvA;
        p.weights = half3(1.0, 0.0, 0.0);
        p.axisSign = half3(1.0, 1.0, 1.0);
        p.normalPS = geo.normalWS;
    }
    return p;
}

ENV_Projection ENV_BuildBaseProjection(ENV_BaseGeometry geo)
{
    return ENV_BuildProjection(geo, _BaseMap_ST, _BaseRotation, ENV_BASE_MODE);
}

// Выборка карты по проекции. Каналы смешиваются по весам целиком — вызывающий берёт нужный.
half4 ENV_SampleProjected(TEXTURE2D_PARAM(tex, samp), ENV_Projection p, uint mode)
{
    if (mode != 0u)
    {
        return SAMPLE_TEXTURE2D(tex, samp, p.uvA) * p.weights.x
             + SAMPLE_TEXTURE2D(tex, samp, p.uvB) * p.weights.y
             + SAMPLE_TEXTURE2D(tex, samp, p.uvC) * p.weights.z;
    }
    return SAMPLE_TEXTURE2D(tex, samp, p.uvA);
}

half4 ENV_SampleBase(TEXTURE2D_PARAM(tex, samp), ENV_Projection p)
{
    return ENV_SampleProjected(TEXTURE2D_ARGS(tex, samp), p, ENV_BASE_MODE);
}

// Наклон карты нормалей/высоты переводится из осей текстуры в оси плоскости проекции:
// dh/dp = S · Rᵀ · T · ∇h. Знак Tiling зеркалит и рисунок, и наклон вместе; неравномерный
// Tiling поворачивает наклон вслед за растяжением. Длина сохраняется: Tiling задаёт
// направление, а силу бампа держат Normal/Height Strength.
half2 ENV_SlopeToPlane(half2 slope, half flipU, float4 st, half rotationDeg)
{
    float2 t = float2(slope) * st.xy;
    float2 g = mul(t, ENV_RotationMatrix(rotationDeg)); // строка × R = Rᵀ · t
    g.x *= flipU;
    float len = length(float2(slope));
    float glen = length(g);
    return glen > 1e-5 ? half2(g * (len / glen)) : slope;
}

// Нормали комплекта в касательном пространстве меша. nA/nB/nC — нормали, выбранные по uvA/uvB/uvC
// (в Mesh UV нужна только nA). Трипланар: перевод наклона в оси плоскостей, whiteout-смешивание
// (Golus) вокруг нормали меша, затем мир и касательное пространство меша тем же базисом,
// что собирает InitializeInputData.
half3 ENV_ProjectedNormalToTS(half3 nA, half3 nB, half3 nC, ENV_BaseGeometry geo,
                              ENV_Projection p, float4 st, half rotationDeg, uint mode)
{
    if (mode != 0u)
    {
        nA.xy = ENV_SlopeToPlane(nA.xy, p.axisSign.x, st, rotationDeg);
        nB.xy = ENV_SlopeToPlane(nB.xy, p.axisSign.y, st, rotationDeg);
        nC.xy = ENV_SlopeToPlane(nC.xy, -p.axisSign.z, st, rotationDeg);

        // Оси — как в GetTriplanarCoordinate.
        half3 n = p.normalPS;
        half3 pX = half3(nA.xy + n.zy, abs(nA.z) * n.x);
        half3 pY = half3(nB.xy + n.xz, abs(nB.z) * n.y);
        half3 pZ = half3(nC.xy + n.xy, abs(nC.z) * n.z);
        half3 perturbedPS = normalize(pX.zyx * p.weights.x + pY.xzy * p.weights.y + pZ.xyz * p.weights.z);

        half3 perturbedWS;
        if (mode == 2u)
            perturbedWS = perturbedPS;
        else
            perturbedWS = half3(TransformObjectToWorldNormal(float3(perturbedPS)));

        half3 bitangent = geo.tangentWS.w * cross(geo.normalWS, geo.tangentWS.xyz);
        half3x3 tangentToWorld = half3x3(geo.tangentWS.xyz, bitangent, geo.normalWS);
        return TransformWorldToTangent(perturbedWS, tangentToWorld);
    }
    nA.xy = ENV_SlopeToPlane(nA.xy, half(1.0), st, rotationDeg);
    return nA;
}

// Нормаль Base в касательном пространстве меша: карта нормалей + бамп из высоты по проекции.
// bias — mip-смещение фильтрованного пути Top; основной путь зовёт с нулём и получает
// то же, что давало отдельное ядро до тикета 02.
half3 ENV_BaseSurfaceNormalTS(ENV_BaseGeometry geo, ENV_Projection p, half bias)
{
    half3 nA = ENV_TopReliefFilteredBaseNormalTS(p.uvA, bias);
    half3 nB = nA;
    half3 nC = nA;
    if (ENV_BASE_MODE != 0u)
    {
        nB = ENV_TopReliefFilteredBaseNormalTS(p.uvB, bias);
        nC = ENV_TopReliefFilteredBaseNormalTS(p.uvC, bias);
    }
    return ENV_ProjectedNormalToTS(nA, nB, nC, geo, p, _BaseMap_ST, _BaseRotation, ENV_BASE_MODE);
}

// ---------------------------------------------------------------------------------------------
// Рельеф слоёв Blend (тикеты 05, 06) — перенос принятого рельефа Top в касательное пространство меша,
// где нормаль меша = (0,0,1). Порядок: Base -> Layer 1 -> Layer 2 -> Top; каждый следующий слой наследует итог
// всех нижних. Общие части (бамп высоты, кромка, размытие) — одни функции на оба слоя: одинаковое поведение
// обеспечивает код, а не копия.

// Бамп из высоты слоя — та же техника и тот же GAIN, что у Base и Top: три ручки силы высоты
// в одном материале обязаны ощущаться одинаково. Упакованный режим — канал B карты материала слоя
// (_MixMaskMapN), раздельный — R отдельной карты Height; какую текстуру подать, решает вызывающий.
half ENV_SampleLayerHeight(TEXTURE2D_PARAM(heightMap, samp), float2 uv, half bias)
{
#if defined(_MASKMAP_SEPARATE)
    return SAMPLE_TEXTURE2D_BIAS(heightMap, samp, uv, bias).r;
#else
    return SAMPLE_TEXTURE2D_BIAS(heightMap, samp, uv, bias).b;
#endif
}

half3 ENV_LayerHeightNormalTS(TEXTURE2D_PARAM(heightMap, samp), float2 texelSize, half strength,
                              float2 uv, half bias)
{
    static const half GAIN = half(8.0);
    float2 texel = texelSize * exp2(float(bias));
    half h = ENV_SampleLayerHeight(TEXTURE2D_ARGS(heightMap, samp), uv, bias);
    half hU = ENV_SampleLayerHeight(TEXTURE2D_ARGS(heightMap, samp), uv + float2(texel.x, 0.0), bias);
    half hV = ENV_SampleLayerHeight(TEXTURE2D_ARGS(heightMap, samp), uv + float2(0.0, texel.y), bias);
    half2 slope = half2(h - hU, h - hV) * strength * GAIN;
    return normalize(half3(slope.x, slope.y, half(1.0)));
}

// Нормаль одной плоскости проекции слоя: карта нормалей + бамп из высоты (по наличию текстуры).
half3 ENV_Mix1PlaneNormalTS(float2 uv, half bias)
{
    half3 n = ENV_TopReliefSampleNormalBias(
        TEXTURE2D_ARGS(_MixNormalMap1, sampler_MixMap1), uv, _MixNormalScale1, bias);
#if defined(_MIX_HEIGHT_1)
#if defined(_MASKMAP_SEPARATE)
    n = BlendNormal(n, ENV_LayerHeightNormalTS(TEXTURE2D_ARGS(_MixHeightMap1, sampler_MixMap1),
        _MixHeightMap1_TexelSize.xy, _MixHeightStrength1, uv, bias));
#else
    n = BlendNormal(n, ENV_LayerHeightNormalTS(TEXTURE2D_ARGS(_MixMaskMap1, sampler_MixMap1),
        _MixMaskMap1_TexelSize.xy, _MixHeightStrength1, uv, bias));
#endif
#endif
    return n;
}

half3 ENV_Mix2PlaneNormalTS(float2 uv, half bias)
{
    half3 n = ENV_TopReliefSampleNormalBias(
        TEXTURE2D_ARGS(_MixNormalMap2, sampler_MixMap1), uv, _MixNormalScale2, bias);
#if defined(_MIX_HEIGHT_2)
#if defined(_MASKMAP_SEPARATE)
    n = BlendNormal(n, ENV_LayerHeightNormalTS(TEXTURE2D_ARGS(_MixHeightMap2, sampler_MixMap1),
        _MixHeightMap2_TexelSize.xy, _MixHeightStrength2, uv, bias));
#else
    n = BlendNormal(n, ENV_LayerHeightNormalTS(TEXTURE2D_ARGS(_MixMaskMap2, sampler_MixMap1),
        _MixMaskMap2_TexelSize.xy, _MixHeightStrength2, uv, bias));
#endif
#endif
    return n;
}

// Собственная фактура слоя (Normal + Height) в касательном пространстве меша, по его проекции.
half3 ENV_Mix1SurfaceNormalTS(ENV_BaseGeometry geo, ENV_Projection p, half bias)
{
    half3 nA = ENV_Mix1PlaneNormalTS(p.uvA, bias);
    half3 nB = nA;
    half3 nC = nA;
    if (ENV_MIX1_MODE != 0u)
    {
        nB = ENV_Mix1PlaneNormalTS(p.uvB, bias);
        nC = ENV_Mix1PlaneNormalTS(p.uvC, bias);
    }
    return ENV_ProjectedNormalToTS(nA, nB, nC, geo, p, _MixMap1_ST, _MixRotation1, ENV_MIX1_MODE);
}

half3 ENV_Mix2SurfaceNormalTS(ENV_BaseGeometry geo, ENV_Projection p, half bias)
{
    half3 nA = ENV_Mix2PlaneNormalTS(p.uvA, bias);
    half3 nB = nA;
    half3 nC = nA;
    if (ENV_MIX2_MODE != 0u)
    {
        nB = ENV_Mix2PlaneNormalTS(p.uvB, bias);
        nC = ENV_Mix2PlaneNormalTS(p.uvC, bias);
    }
    return ENV_ProjectedNormalToTS(nA, nB, nC, geo, p, _MixMap2_ST, _MixRotation2, ENV_MIX2_MODE);
}

// Mip-смещение размытия подложки слоя: два размытия (своё 5·S² и внешнее — Top) складываются по дисперсии;
// единица вычитается, потому что ширина без смещения — уже 1 тексель.
half ENV_CombinedReliefBias(half layerBias, half outerBias)
{
    return outerBias > half(0.0)
        ? half(0.5) * log2(exp2(layerBias * half(2.0)) + exp2(outerBias * half(2.0)) - half(1.0))
        : layerBias;
}

// Виртуальная кромка. Градиент берётся от гладкого поля d (знаковая глубина относительно контура), а не от
// самой маски: маска меняется на 1-3 пикселях, и ddx/ddy по ней считаются блоками 2x2 — по контуру шла
// пиксельная лесенка. Вес скоса — производная smoothstep по d, посчитанная по пикселю точно; полуширина
// скоса не меньше 1.5 пикселя, чтобы жёсткий край (Softness 0) давал сглаженную линию, а не аляс.
// Коэффициенты 0.16 (жёсткий край) .. 0.06 (самый мягкий) — как у Top: шире переход — положе скос.
// Минус — углубление, плюс — выступ; маску не двигает (mask сюда только приходит).
half3 ENV_ApplyLayerEdgeTS(half3 coveredTS, ENV_BaseGeometry geo, ENV_MaskField mask,
                           half edgeThickness, half softness)
{
#if defined(_NORMALMAP)
    float3 dpdx = ddx(geo.positionWS);
    float3 dpdy = ddy(geo.positionWS);
    float fieldDx = ddx(mask.d);
    float fieldDy = ddy(mask.d);
    float3 fieldGradientWS = dpdx * (fieldDx / max(dot(dpdx, dpdx), 1e-5))
                           + dpdy * (fieldDy / max(dot(dpdy, dpdy), 1e-5));
    float capHalfWidth = max(mask.hw, 1.5 * max(fwidth(mask.d), 1e-4));
    float ramp = saturate((mask.d + capHalfWidth) / (2.0 * capHalfWidth));
    float bevelWeight = 6.0 * ramp * (1.0 - ramp) / (2.0 * capHalfWidth);
    half3 tangent = normalize(geo.tangentWS.xyz);
    half3 bitangent = geo.tangentWS.w * cross(geo.normalWS, tangent);
    half2 gradientTS = half2(dot(half3(fieldGradientWS), tangent), dot(half3(fieldGradientWS), bitangent))
                     * half(bevelWeight);
    half capStrength = clamp(edgeThickness, half(-1.0), half(1.0))
        * lerp(half(0.16), half(0.06), saturate(softness));
    coveredTS = normalize(half3(coveredTS.xy - gradientTS * capStrength, coveredTS.z));
#endif
    return coveredTS;
}

// Итоговая нормаль слоя 1 внутри его покрытия. topBias — mip-смещение фильтрованного пути Top
// (основной путь зовёт с нулём): подавление Base под слоем и Top накладываются как два последовательных
// размытия, свою фактуру слоя размывает только Top. Кромка от mip не зависит.
//  * Inherit on, Relief Smoothing 0, силы 0, кромка 0 — ровно нормаль Base.
//  * Inherit off — форма меша + своя фактура и кромка.
//  * Кромка: минус — углубление, плюс — выступ; маску не двигает, ведь mask1 сюда только приходит.
half3 ENV_Layer1CoveredNormalTS(ENV_BaseGeometry geo, ENV_Projection proj1, ENV_MaskField mask1, half topBias)
{
    half smoothing = saturate(_MixReliefSmoothing1);
    half smoothing2 = smoothing * smoothing;
    half ownBias = max(topBias, half(0.0));
    half combinedBias = ENV_CombinedReliefBias(smoothing2 * half(5.0), ownBias);
    half amplitude = (half(1.0) - smoothing2 * smoothing2) * step(half(0.5), _MixInheritRelief1);

    half3 inheritedTS = half3(0.0, 0.0, 1.0);
    if (amplitude > half(0.0))
    {
        half3 baseTS = ENV_BaseSurfaceNormalTS(geo, ENV_BuildBaseProjection(geo), combinedBias);
        inheritedTS = normalize(lerp(half3(0.0, 0.0, 1.0), baseTS, amplitude));
    }

    // Своя фактура добавляется вокруг формы меша: нейтральная карта оставляет наклоны нетронутыми.
    half3 ownTS = ENV_Mix1SurfaceNormalTS(geo, proj1, ownBias);
    half3 coveredTS = normalize(inheritedTS + half3(ownTS.xy, half(0.0)));
    return ENV_ApplyLayerEdgeTS(coveredTS, geo, mask1, _MixEdgeThickness1, _MixEdgeSoftness1);
}

// Поверхность под слоем 2 на mip-смещении bias: Base, а внутри покрытия слоя 1 — итог слоя 1 (с его кромкой).
// Сложение размытий ассоциативно, поэтому слой 1 сам добавляет своё сглаживание к внешнему bias.
half3 ENV_SurfaceUnderLayer2TS(ENV_BaseGeometry geo, ENV_Projection proj1, ENV_MaskField mask1, half bias)
{
    half3 baseTS = ENV_BaseSurfaceNormalTS(geo, ENV_BuildBaseProjection(geo), bias);
    half3 layer1TS = ENV_Layer1CoveredNormalTS(geo, proj1, mask1, bias);
    return lerp(baseTS, layer1TS, mask1.mask);
}

// Итоговая нормаль слоя 2 внутри его покрытия (тикет 06) — та же схема, что у слоя 1, но подложка —
// итог Base и слоя 1, а не один Base. topBias — mip-смещение пути Top (основной путь зовёт с нулём).
// underAtOuterTS — подложка, уже собранная вызывающим на смещении topBias (lerp Base/слой 1 по mask1):
// при Relief Smoothing 0 собственное размытие слоя нулевое и пересобирать её незачем. Ветвление —
// по материальной константе, а не по маске: внутри ddx/ddy и выборки с неявными производными.
half3 ENV_Layer2CoveredNormalTS(ENV_BaseGeometry geo, ENV_Projection proj1, ENV_MaskField mask1,
                                ENV_Projection proj2, ENV_MaskField mask2,
                                half topBias, half3 underAtOuterTS)
{
    half smoothing = saturate(_MixReliefSmoothing2);
    half smoothing2 = smoothing * smoothing;
    half ownBias = max(topBias, half(0.0));
    half combinedBias = ENV_CombinedReliefBias(smoothing2 * half(5.0), ownBias);
    half amplitude = (half(1.0) - smoothing2 * smoothing2) * step(half(0.5), _MixInheritRelief2);

    half3 inheritedTS = half3(0.0, 0.0, 1.0);
    if (amplitude > half(0.0))
    {
        half3 underTS = underAtOuterTS;
        if (smoothing > half(0.0))
            underTS = ENV_SurfaceUnderLayer2TS(geo, proj1, mask1, combinedBias);
        inheritedTS = normalize(lerp(half3(0.0, 0.0, 1.0), underTS, amplitude));
    }

    half3 ownTS = ENV_Mix2SurfaceNormalTS(geo, proj2, ownBias);
    half3 coveredTS = normalize(inheritedTS + half3(ownTS.xy, half(0.0)));
    return ENV_ApplyLayerEdgeTS(coveredTS, geo, mask2, _MixEdgeThickness2, _MixEdgeSoftness2);
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

// Каналы комплекта материальных карт слоя по проекции — раскладка та же, что у
// ENV_SampleLayerMaterialChannels (R metallic, G occlusion, A smoothness; раздельный режим — R
// каждой карты). Возвращает каналы как есть, до множителей слоя.
half3 ENV_SampleLayerMaterialChannelsProjected(ENV_Projection p, uint mode,
    TEXTURE2D_PARAM(maskMap, samp),
    TEXTURE2D(metallicMap), TEXTURE2D(occlusionMap), TEXTURE2D(smoothnessMap))
{
#if defined(_MASKMAP_SEPARATE)
    return half3(ENV_SampleProjected(TEXTURE2D_ARGS(metallicMap, samp), p, mode).r,
                 ENV_SampleProjected(TEXTURE2D_ARGS(occlusionMap, samp), p, mode).r,
                 ENV_SampleProjected(TEXTURE2D_ARGS(smoothnessMap, samp), p, mode).r);
#else
    half4 packed = ENV_SampleProjected(TEXTURE2D_ARGS(maskMap, samp), p, mode);
    return half3(packed.r, packed.g, packed.a);
#endif
}

// Один подмешиваемый слой. Именно функция, а не два развёрнутых блока на два слоя: тикет
// требует, чтобы оба слоя вели себя одинаково, и общая функция это гарантирует, а не обещает.
//
// Карты слоя идут по СВОЕЙ проекции (Mesh UV / Local / World, свой ST и Rotation), не по UV
// Base — сэмплируются на стороне вызова. Нормаль слоя по-прежнему смешивается прямо
// в surfaceData.normalTS, то есть ДО сборки мировой нормали, — маска наноса ниже по коду видит
// уже смешанную нормаль сама, без единой правки.
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
// "канал × ползунок".
//
// alpha — по той же причине, что у наноса и градиента: подмешивать после AlphaModulate надо
// тем же способом, иначе прозрачная поверхность красит фон.
void ENV_ApplyMixLayer(
    half3 layerAlbedo, half3 layerNormalTS, half mask, half alpha,
    half metallic, half smoothness, half3 materialChannels, half occlusionStrength,
    inout SurfaceData surfaceData)
{
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

// Маска слоя Blend: покрытие + RGB Noise. Шум читается тем же ядром проекций, что и карты
// (ENV_BuildProjection), но в своём режиме: Mesh UV / Local / World Triplanar — независимо
// от карт слоя, другого слоя, Top и градиента. Tiling — повторов на метр (World), на локальную
// единицу (Local), на UV-остров (Mesh UV, сырой uv0 — Tiling/Offset Base маску не двигают).
// Веса трипланара — по нормали меша (geo.normalWS), не по рельефу: Normal/Height маску не двигают.
// Offset у маски нет.
//
// При выключенном _PATTERN источник = 0.5 постоянно — без узора детализацию брать неоткуда,
// граница идёт жёстко по покрытию, мягчит её только Edge Softness слоя.
half ENV_MixLayerSource(ENV_BaseGeometry geo, float4 tiling, half rotationDeg,
                        half channel, half strength, half bias, uint mode)
{
    half source = half(0.5);
#if defined(_PATTERN)
    ENV_Projection p = ENV_BuildProjection(geo, float4(tiling.xy, 0.0, 0.0), rotationDeg, mode);
    half3 noise = ENV_SampleProjected(TEXTURE2D_ARGS(_PatternMap, sampler_PatternMap), p, mode).rgb;
    source = ENV_NoiseSource(half(0.5), noise, channel, strength, bias);
#endif
    return source;
}

// Оба слоя — порог Top: середина перехода неподвижна при любой мягкости, концы 0/1 жёсткие.
// Поле d и полуширина нужны кромке слоя (ENV_ApplyLayerEdgeTS).
ENV_MaskField ENV_MixLayerMask(ENV_BaseGeometry geo, half coverage, float4 tiling, half rotationDeg,
                               half channel, half strength, half bias, half softness, uint mode)
{
    half source = ENV_MixLayerSource(geo, tiling, rotationDeg, channel, strength, bias, mode);
    return ENV_TopReliefMaskField(float(source), coverage, softness);
}

// Смешивание материалов мазком целиком, тикет 06. Зовётся ДО слоя наноса: снег падает
// на то, что под ним уже сложилось, и маска наноса считается по смешанной нормали.
//
// Одна общая карта шума, но каждый слой читает её своей проекцией, тайлингом и поворотом
// (ENV_MixLayerMask); карты слоя (Albedo/Normal/PBR) имеют свою, независимую (ENV_MIXn_MODE).
//
// Слой 2 ложится ПОСЛЕ слоя 1 — порядок фиксирован тикетом 04. Кисть держит сумму весов <= 1
// («база есть остаток»), так что на практике перекрытия почти нет; при обоих каналах
// на максимуме выигрывает второй, и это названное решение, а не побочный эффект.
void ApplyMaterialMix(ENV_BaseGeometry geo, half4 vertexColor, half alpha, inout SurfaceData surfaceData)
{
#if defined(_MATERIAL_MIX)
    half2 coverage = SampleMixCoverage(vertexColor);

    ENV_Projection proj1 = ENV_BuildProjection(geo, _MixMap1_ST, _MixRotation1, ENV_MIX1_MODE);
    half3 layerAlbedo1 = ENV_SampleProjected(
        TEXTURE2D_ARGS(_MixMap1, sampler_MixMap1), proj1, ENV_MIX1_MODE).rgb * _MixColor1.rgb;
    half3 materialChannels1 = half3(1.0, 1.0, 1.0);
#if defined(_MIX_MAPS_1)
    materialChannels1 = ENV_SampleLayerMaterialChannelsProjected(proj1, ENV_MIX1_MODE,
        TEXTURE2D_ARGS(_MixMaskMap1, sampler_MixMap1),
        _MixMetallicMap1, _MixOcclusionMap1, _MixSmoothnessMap1);
#endif

    ENV_MaskField mask1 = ENV_MixLayerMask(geo, coverage.x, _MixPatternTiling1, _MixPatternRotation1,
        _MixPatternChannel1, _MixPatternStrength1, _MixPatternBias1, _MixEdgeSoftness1, ENV_MIXPAT1_MODE);
    // Рельеф слоя 1 (тикет 05): наследование Base, своя фактура и кромка — ENV_Layer1CoveredNormalTS.
    // normalTS читают только ветки под _NORMALMAP; инспектор включает его вместе с Material Blending.
    half3 layerNormalTS1 = half3(0.0, 0.0, 1.0);
#if defined(_NORMALMAP)
    layerNormalTS1 = ENV_Layer1CoveredNormalTS(geo, proj1, mask1, half(0.0));
#endif
    ENV_ApplyMixLayer(layerAlbedo1, layerNormalTS1, mask1.mask, alpha,
        _MixMetallic1, _MixSmoothness1, materialChannels1, _MixOcclusionStrength1,
        surfaceData);

#if defined(_MATERIAL_MIX_2)
    ENV_Projection proj2 = ENV_BuildProjection(geo, _MixMap2_ST, _MixRotation2, ENV_MIX2_MODE);
    half3 layerAlbedo2 = ENV_SampleProjected(
        TEXTURE2D_ARGS(_MixMap2, sampler_MixMap1), proj2, ENV_MIX2_MODE).rgb * _MixColor2.rgb;
    half3 materialChannels2 = half3(1.0, 1.0, 1.0);
#if defined(_MIX_MAPS_2)
    materialChannels2 = ENV_SampleLayerMaterialChannelsProjected(proj2, ENV_MIX2_MODE,
        TEXTURE2D_ARGS(_MixMaskMap2, sampler_MixMap1),
        _MixMetallicMap2, _MixOcclusionMap2, _MixSmoothnessMap2);
#endif

    ENV_MaskField mask2 = ENV_MixLayerMask(geo, coverage.y, _MixPatternTiling2, _MixPatternRotation2,
        _MixPatternChannel2, _MixPatternStrength2, _MixPatternBias2, _MixEdgeSoftness2, ENV_MIXPAT2_MODE);
    // Рельеф слоя 2 (тикет 06): подложка — итог Base и слоя 1, уже лежащий в surfaceData.normalTS.
    half3 layerNormalTS2 = half3(0.0, 0.0, 1.0);
#if defined(_NORMALMAP)
    layerNormalTS2 = ENV_Layer2CoveredNormalTS(geo, proj1, mask1, proj2, mask2, half(0.0), surfaceData.normalTS);
#endif
    ENV_ApplyMixLayer(layerAlbedo2, layerNormalTS2, mask2.mask, alpha,
        _MixMetallic2, _MixSmoothness2, materialChannels2, _MixOcclusionStrength2,
        surfaceData);
#endif
#endif
}

half3 ENV_TopReliefFilteredSurfaceNormalTS(ENV_BaseGeometry geo, half4 vertexColor)
{
    // Все три комплекта идут по своим проекциям: иначе наследование Top тянуло бы под себя
    // UV-рельеф, которого у слоя уже нет.
    half bias = ENV_TopReliefReliefMipBias();
    half3 normalTS = ENV_BaseSurfaceNormalTS(geo, ENV_BuildBaseProjection(geo), bias);

#if defined(_MATERIAL_MIX)
    half2 coverage = SampleMixCoverage(vertexColor);
    ENV_MaskField mask1 = ENV_MixLayerMask(geo, coverage.x, _MixPatternTiling1, _MixPatternRotation1,
        _MixPatternChannel1, _MixPatternStrength1, _MixPatternBias1, _MixEdgeSoftness1, ENV_MIXPAT1_MODE);
    ENV_Projection proj1 = ENV_BuildProjection(geo, _MixMap1_ST, _MixRotation1, ENV_MIX1_MODE);
    // Внутри покрытия слоя 1 Top наследует его итог (Base под ним, своя фактура, кромка), а не голый Base.
    half3 layerNormal1 = ENV_Layer1CoveredNormalTS(geo, proj1, mask1, bias);
    normalTS = lerp(normalTS, layerNormal1, mask1.mask);

#if defined(_MATERIAL_MIX_2)
    ENV_MaskField mask2 = ENV_MixLayerMask(geo, coverage.y, _MixPatternTiling2, _MixPatternRotation2,
        _MixPatternChannel2, _MixPatternStrength2, _MixPatternBias2, _MixEdgeSoftness2, ENV_MIXPAT2_MODE);
    ENV_Projection proj2 = ENV_BuildProjection(geo, _MixMap2_ST, _MixRotation2, ENV_MIX2_MODE);
    // Внутри покрытия слоя 2 Top наследует его итог (Base и слой 1 под ним, своя фактура, кромка):
    // normalTS сейчас — подложка Base/слой 1 на смещении Top.
    half3 layerNormal2 = ENV_Layer2CoveredNormalTS(geo, proj1, mask1, proj2, mask2, bias, normalTS);
    normalTS = lerp(normalTS, layerNormal2, mask2.mask);
#endif
#endif
    return normalize(normalTS);
}

// ENV_Lit: градиент по высоте (_HEIGHT_GRADIENT). Раньше жил в ENV_LitForwardPass.hlsl —
// переехал сюда вместе с собственным мета-пассом (ENV_LitMetaPass.hlsl), который теперь
// тоже его вызывает. Принимает готовую высоту, а не позицию: в мета-пассе обратного
// преобразования из мировой позиции нет, каждый пасс подаёт число своим способом через
// ENV_GradientHeight выше.
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

inline void InitializeStandardLitSurfaceData(ENV_BaseGeometry geo, out SurfaceData outSurfaceData)
{
    ENV_Projection proj = ENV_BuildBaseProjection(geo);

    half4 albedoAlpha = ENV_SampleBase(TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap), proj);
#if defined(_ALPHATEST_ON) || defined(_SURFACE_TYPE_TRANSPARENT)
    // Вырез и прозрачность — всегда по Mesh UV без Rotation: те же координаты читают пакетные
    // ShadowCaster/DepthOnly/DepthNormals, и тень с глубиной совпадают с видимым вырезом.
    albedoAlpha.a = SampleAlbedoAlpha(geo.uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap)).a;
#endif
    outSurfaceData.alpha = Alpha(albedoAlpha.a, _BaseColor, _Cutoff);

    outSurfaceData.albedo = albedoAlpha.rgb * _BaseColor.rgb;
    outSurfaceData.albedo = ApplyAlbedoAdjust(outSurfaceData.albedo);
    outSurfaceData.albedo = AlphaModulate(outSurfaceData.albedo, outSurfaceData.alpha);

    half metallicChannel;
    half occlusionChannel;
    half smoothnessChannel;

#if defined(_MASKMAP_SEPARATE)
    metallicChannel = ENV_SampleBase(TEXTURE2D_ARGS(_MetallicMap, sampler_MetallicMap), proj).r;
    occlusionChannel = ENV_SampleBase(TEXTURE2D_ARGS(_OcclusionMap, sampler_MetallicMap), proj).r;
    smoothnessChannel = ENV_SampleBase(TEXTURE2D_ARGS(_SmoothnessMap, sampler_MetallicMap), proj).r;
#else
    half4 mask = ENV_SampleBase(TEXTURE2D_ARGS(_MaskMap, sampler_MaskMap), proj);
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
    // Карта нормалей и бамп из высоты базы (_HEIGHT_BUMP) — до слоёв смешивания (они
    // перезаписывают normalTS по маске, рельеф базы под заменённым материалом оставаться
    // не должен) и до сборки нормали для маски наноса (ENV_ResolveNormalWS), то есть нанос
    // видит рельеф базы. Читают normalTS только ветки под _NORMALMAP — без него плоская.
#if defined(_NORMALMAP)
    outSurfaceData.normalTS = ENV_BaseSurfaceNormalTS(geo, proj, half(0.0));
#else
    outSurfaceData.normalTS = half3(0.0, 0.0, 1.0);
#endif

    outSurfaceData.occlusion = SampleMaskMapOcclusion(occlusionChannel);

    // Эмиссия — по Mesh UV со своим тайлингом, проекция и Rotation Base на неё не действуют.
    // Берётся сырой uv0 из вершины: прежнее обратное деление на _BaseMap_ST.xy теряло данные
    // при Tiling 0 (inf, а с ним NaN, который блум размазывает по кадру).
    float2 emissionUV = TRANSFORM_TEX(geo.uv0, _EmissionMap);
    outSurfaceData.emission = SampleEmission(emissionUV, _EmissionColor.rgb, TEXTURE2D_ARGS(_EmissionMap, sampler_EmissionMap));

    outSurfaceData.clearCoatMask = half(0.0);
    outSurfaceData.clearCoatSmoothness = half(0.0);
}

#endif // SPIDERRIG_ENV_LIT_INPUT_INCLUDED
