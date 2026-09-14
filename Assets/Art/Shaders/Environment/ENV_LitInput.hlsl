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
half _SmoothnessIsRoughness;
half _OcclusionStrength;
half _BumpScale;
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
// float, не half: множится на мировую/объектную координату при планарной проекции.
float _OverlayTiling0;
half _OverlayNormalScale0;
half _OverlayMetallic0;
half _OverlaySmoothness0;
half _OverlayCoverage0;
half _OverlayEdgeSoftness0;
half _OverlayHeightDepth0;
// Художественный узор (_PATTERN): один на материал, общий для слоя наноса и будущего
// смешивания (04) — индекса в имени нет сознательно, в отличие от _OverlayXxx0.
// float, не half: множится на мировую/объектную координату при планарной проекции.
float _PatternTiling;
half _PatternStrength;
// Канал текстуры (R/G/B), не keyword: три сравнения с uniform дешевле, чем x4 варианта
// пасса ради выбора канала. См. SamplePatternMultiplier.
half _PatternChannel;
// Смешивание материалов мазком (_MATERIAL_MIX): второй и третий материал по весу из
// вершинного цвета или из маски-текстуры. Префикс _Mix, а не _Blend: _Blend, _SrcBlend,
// _DstBlend и _BlendModePreserveSpecular — имена URP, уже занятые блендстейтом, и пятое
// _Blend* рядом с ними читалось бы как ещё одна настройка прозрачности.
// Индексы 1 и 2 — «первый и второй подмешиваемый слой» по тексту тикета; ноль за наносом.
// float у тайлингов: множатся на мировую/объектную координату либо на UV.
float _MixMaskTiling;
// Один на оба слоя: края обоих мазков должны рваться согласованно — тот же довод,
// что «один узор на материал».
half _MixEdgeSoftness;
// Множитель веса покраски. Дефолт 0 — и это не осторожность, а единственная защита,
// которая здесь работает: меш без вершинных цветов отдаёт в шейдер БЕЛЫЙ, то есть вес 1,
// и без этой ручки включение галочки утопило бы непокрашенный объект во втором материале
// целиком. Отличить «цветов нет» от «покрашено в белый» шейдер не может — это одно число.
// Верхняя граница 2, а не 1: полутона чёрно-белой маски (0.6-0.8) иначе не дотянуть
// до полного покрытия, и сквозь мазок просвечивает база.
half _MixCoverage;
half4 _MixColor1;
float _MixTiling1;
half _MixNormalScale1;
half _MixMetallic1;
half _MixSmoothness1;
half4 _MixColor2;
float _MixTiling2;
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
    UNITY_DOTS_INSTANCED_PROP(float , _SmoothnessIsRoughness)
    UNITY_DOTS_INSTANCED_PROP(float , _OcclusionStrength)
    UNITY_DOTS_INSTANCED_PROP(float , _BumpScale)
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
    UNITY_DOTS_INSTANCED_PROP(float , _OverlayTiling0)
    UNITY_DOTS_INSTANCED_PROP(float , _OverlayNormalScale0)
    UNITY_DOTS_INSTANCED_PROP(float , _OverlayMetallic0)
    UNITY_DOTS_INSTANCED_PROP(float , _OverlaySmoothness0)
    UNITY_DOTS_INSTANCED_PROP(float , _OverlayCoverage0)
    UNITY_DOTS_INSTANCED_PROP(float , _OverlayEdgeSoftness0)
    UNITY_DOTS_INSTANCED_PROP(float , _OverlayHeightDepth0)
    UNITY_DOTS_INSTANCED_PROP(float , _PatternTiling)
    UNITY_DOTS_INSTANCED_PROP(float , _PatternStrength)
    UNITY_DOTS_INSTANCED_PROP(float , _PatternChannel)
    UNITY_DOTS_INSTANCED_PROP(float , _MixMaskTiling)
    UNITY_DOTS_INSTANCED_PROP(float , _MixEdgeSoftness)
    UNITY_DOTS_INSTANCED_PROP(float , _MixCoverage)
    UNITY_DOTS_INSTANCED_PROP(float4, _MixColor1)
    UNITY_DOTS_INSTANCED_PROP(float , _MixTiling1)
    UNITY_DOTS_INSTANCED_PROP(float , _MixNormalScale1)
    UNITY_DOTS_INSTANCED_PROP(float , _MixMetallic1)
    UNITY_DOTS_INSTANCED_PROP(float , _MixSmoothness1)
    UNITY_DOTS_INSTANCED_PROP(float4, _MixColor2)
    UNITY_DOTS_INSTANCED_PROP(float , _MixTiling2)
    UNITY_DOTS_INSTANCED_PROP(float , _MixNormalScale2)
    UNITY_DOTS_INSTANCED_PROP(float , _MixMetallic2)
    UNITY_DOTS_INSTANCED_PROP(float , _MixSmoothness2)
UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)

static float4 unity_DOTS_Sampled_BaseColor;
static float4 unity_DOTS_Sampled_EmissionColor;
static float  unity_DOTS_Sampled_Cutoff;
static float  unity_DOTS_Sampled_Metallic;
static float  unity_DOTS_Sampled_Smoothness;
static float  unity_DOTS_Sampled_SmoothnessIsRoughness;
static float  unity_DOTS_Sampled_OcclusionStrength;
static float  unity_DOTS_Sampled_BumpScale;
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
static float  unity_DOTS_Sampled_OverlayTiling0;
static float  unity_DOTS_Sampled_OverlayNormalScale0;
static float  unity_DOTS_Sampled_OverlayMetallic0;
static float  unity_DOTS_Sampled_OverlaySmoothness0;
static float  unity_DOTS_Sampled_OverlayCoverage0;
static float  unity_DOTS_Sampled_OverlayEdgeSoftness0;
static float  unity_DOTS_Sampled_OverlayHeightDepth0;
static float  unity_DOTS_Sampled_PatternTiling;
static float  unity_DOTS_Sampled_PatternStrength;
static float  unity_DOTS_Sampled_PatternChannel;
static float  unity_DOTS_Sampled_MixMaskTiling;
static float  unity_DOTS_Sampled_MixEdgeSoftness;
static float  unity_DOTS_Sampled_MixCoverage;
static float4 unity_DOTS_Sampled_MixColor1;
static float  unity_DOTS_Sampled_MixTiling1;
static float  unity_DOTS_Sampled_MixNormalScale1;
static float  unity_DOTS_Sampled_MixMetallic1;
static float  unity_DOTS_Sampled_MixSmoothness1;
static float4 unity_DOTS_Sampled_MixColor2;
static float  unity_DOTS_Sampled_MixTiling2;
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
    unity_DOTS_Sampled_SmoothnessIsRoughness = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _SmoothnessIsRoughness);
    unity_DOTS_Sampled_OcclusionStrength     = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _OcclusionStrength);
    unity_DOTS_Sampled_BumpScale             = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _BumpScale);
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
    unity_DOTS_Sampled_OverlayTiling0        = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _OverlayTiling0);
    unity_DOTS_Sampled_OverlayNormalScale0   = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _OverlayNormalScale0);
    unity_DOTS_Sampled_OverlayMetallic0      = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _OverlayMetallic0);
    unity_DOTS_Sampled_OverlaySmoothness0    = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _OverlaySmoothness0);
    unity_DOTS_Sampled_OverlayCoverage0      = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _OverlayCoverage0);
    unity_DOTS_Sampled_OverlayEdgeSoftness0  = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _OverlayEdgeSoftness0);
    unity_DOTS_Sampled_OverlayHeightDepth0   = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _OverlayHeightDepth0);
    unity_DOTS_Sampled_PatternTiling         = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _PatternTiling);
    unity_DOTS_Sampled_PatternStrength       = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _PatternStrength);
    unity_DOTS_Sampled_PatternChannel        = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _PatternChannel);
    unity_DOTS_Sampled_MixMaskTiling         = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _MixMaskTiling);
    unity_DOTS_Sampled_MixEdgeSoftness       = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _MixEdgeSoftness);
    unity_DOTS_Sampled_MixCoverage           = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _MixCoverage);
    unity_DOTS_Sampled_MixColor1             = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _MixColor1);
    unity_DOTS_Sampled_MixTiling1            = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _MixTiling1);
    unity_DOTS_Sampled_MixNormalScale1       = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _MixNormalScale1);
    unity_DOTS_Sampled_MixMetallic1          = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _MixMetallic1);
    unity_DOTS_Sampled_MixSmoothness1        = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _MixSmoothness1);
    unity_DOTS_Sampled_MixColor2             = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _MixColor2);
    unity_DOTS_Sampled_MixTiling2            = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _MixTiling2);
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
#define _SmoothnessIsRoughness   unity_DOTS_Sampled_SmoothnessIsRoughness
#define _OcclusionStrength       unity_DOTS_Sampled_OcclusionStrength
#define _BumpScale               unity_DOTS_Sampled_BumpScale
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
#define _OverlayTiling0          unity_DOTS_Sampled_OverlayTiling0
#define _OverlayNormalScale0     unity_DOTS_Sampled_OverlayNormalScale0
#define _OverlayMetallic0        unity_DOTS_Sampled_OverlayMetallic0
#define _OverlaySmoothness0      unity_DOTS_Sampled_OverlaySmoothness0
#define _OverlayCoverage0        unity_DOTS_Sampled_OverlayCoverage0
#define _OverlayEdgeSoftness0    unity_DOTS_Sampled_OverlayEdgeSoftness0
#define _OverlayHeightDepth0     unity_DOTS_Sampled_OverlayHeightDepth0
#define _PatternTiling           unity_DOTS_Sampled_PatternTiling
#define _PatternStrength         unity_DOTS_Sampled_PatternStrength
#define _PatternChannel          unity_DOTS_Sampled_PatternChannel
#define _MixMaskTiling           unity_DOTS_Sampled_MixMaskTiling
#define _MixEdgeSoftness         unity_DOTS_Sampled_MixEdgeSoftness
#define _MixCoverage             unity_DOTS_Sampled_MixCoverage
#define _MixColor1               unity_DOTS_Sampled_MixColor1
#define _MixTiling1              unity_DOTS_Sampled_MixTiling1
#define _MixNormalScale1         unity_DOTS_Sampled_MixNormalScale1
#define _MixMetallic1            unity_DOTS_Sampled_MixMetallic1
#define _MixSmoothness1          unity_DOTS_Sampled_MixSmoothness1
#define _MixColor2               unity_DOTS_Sampled_MixColor2
#define _MixTiling2              unity_DOTS_Sampled_MixTiling2
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

// Высота микрорельефа для слоя наноса (_OVERLAY_LAYER_0) — следует существующему режиму
// маски, своего переключателя не заводит: владелец объяснил зачем тот режим существует
// (раздельные карты со стора подключаются быстро, проверяются в движке, потом пакуются
// в Substance — высота обязана себя вести как остальные каналы). Дефолт "white" = 1.0 —
// нейтраль: «нет карты» и «выступ» это одно и то же, никакого сдвига маски.
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

// Слой наноса (_OVERLAY_LAYER_0): своя пара альбедо/нормаль, один сэмплер на двоих —
// фильтрация у пары одна, как у тройки masks выше.
TEXTURE2D(_OverlayMap0);
TEXTURE2D(_OverlayNormalMap0);
SAMPLER(sampler_OverlayMap0);

// Художественный узор (_PATTERN): свой сэмплер, не общий с парой наноса выше — в 04 узор
// работает и при выключенном слое наноса, и его фильтрация не должна молча следовать
// за настройками импорта чужой текстуры.
TEXTURE2D(_PatternMap);
SAMPLER(sampler_PatternMap);

// Смешивание материалов (_MATERIAL_MIX): четыре карты двух слоёв делят один сэмплер —
// фильтрация у них одна, как у тройки масок и пары наноса. У маски-мазка свой: у неё
// другая роль (данные, не цвет) и свои настройки импорта. Девять сэмплеров из шестнадцати.
TEXTURE2D(_MixMaskMap);
SAMPLER(sampler_MixMaskMap);
TEXTURE2D(_MixMap1);
TEXTURE2D(_MixNormalMap1);
TEXTURE2D(_MixMap2);
TEXTURE2D(_MixNormalMap2);
SAMPLER(sampler_MixMap1);

// Проекция текстур границы — узора и маски-мазка. Своя, отдельная от _ProjectionSpace:
// та выбирает мировые или объектные координаты, эта — планарную развёртку или собственную
// UV меша, и смешивать два смысла в одну ручку было бы ловушкой.
//
// Planar (по умолчанию) — XZ в пространстве проекции: рисунок непрерывен через стыки
// модулей, и это единственный способ провести мазок через шов, где вершины принадлежат
// разным объектам. Но «развёртка» у планарной проекции есть только сверху: на вертикальных
// и промежуточных гранях она размазывается в полосы.
//
// UV — по развёртке меша, со своим множителем тайлинга. Работает одинаково на любой
// ориентации, поэтому годится для мазка, который рисуют и на стенах: сколы краски, грязь.
// Цена — рисунок повторяется на каждом экземпляре модуля.
//
// Одна ручка на узор и маску сразу, а не две: материал в работе бывает либо модульным,
// либо уникальным объектом, и выбор ложится на материал целиком.
float2 GetPatternUV(float2 uv, float3 positionPS, float tiling)
{
#if defined(_PATTERNSPACE_UV)
    return uv * tiling;
#else
    return positionPS.xz * tiling;
#endif
}

// Узор нужен обоим потребителям — слою наноса и мазку. Гейт выборки один на оба, чтобы
// при выключенных обоих выборка текстуры не осталась мёртвой: полагаться на то, что её
// уберёт компилятор, здесь не будем — выборка текстуры не та вещь, которую оставляют
// на усмотрение.
#if defined(_OVERLAY_LAYER_0) || defined(_MATERIAL_MIX)
#define ENV_NEEDS_PATTERN
#endif

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

// Художественный узор (_PATTERN): множитель источника маски ДО порога, не добавка к готовой
// маске ПОСЛЕ него — разница решающая (тикет 03, спек "Слой наноса"). После порога узор дал
// бы полупрозрачные пятна по всей площади снега, включая середину сугроба; до порога он рвёт
// именно границу, оставляя внутренность покрытия сплошной. ComputeOverlayMask0 ниже умножает
// им saturate(slope + relief) перед smoothstep — здесь только сама выборка.
//
// Одна выборка на материал: тикет требует общий узор для наноса (03) и будущего смешивания
// (04), и вызывающий код (ForwardLit/Meta) берёт число один раз за фрагмент.
//
// Канал выбирается свойством _PatternChannel, не keyword'ом — три сравнения с uniform
// дешевле, чем x4 варианта пасса ради выбора одного из трёх каналов. Маска канала building
// исчерпывающе (третья ветка — "иначе", не "== 2"): сумма весов равна 1 при любом значении
// свойства, включая испорченное — так испорченный _PatternChannel не обнуляет узор молча.
//
// Пустой слот "white" даёт 1 в любом канале, поэтому lerp(1, 1, сила) = 1 при любой силе —
// доказательство, что незаполненный Pattern не меняет вид (пункт 1 верификации).
half SamplePatternMultiplier(float2 uv, float3 positionPS)
{
#if defined(_PATTERN)
    float2 patternUV = GetPatternUV(uv, positionPS, _PatternTiling);
    half3 pattern = SAMPLE_TEXTURE2D(_PatternMap, sampler_PatternMap, patternUV).rgb;

    half3 channelMask = _PatternChannel < half(0.5) ? half3(1.0, 0.0, 0.0)
                       : _PatternChannel < half(1.5) ? half3(0.0, 1.0, 0.0)
                       :                               half3(0.0, 0.0, 1.0);
    half channel = dot(pattern, channelMask);

    return lerp(half(1.0), channel, _PatternStrength);
#else
    return half(1.0);
#endif
}

// Общий порог всех масок этого шейдера: слоя наноса (02) и мазка смешивания (04). Спек
// требует, чтобы все три механизма пользовались одним механизмом маски, а четыре строки
// порога в двух копиях — ровно то, что разъезжается молча через год.
//
// Роли параметров у двух потребителей разные, и это осознанно:
//   нанос — source = saturate(наклон + рельеф) * узор,  coverage = ползунок _OverlayCoverage0;
//   мазок — source = узор,                              coverage = вес из канала попиксельно.
// То есть у мазка процедурного источника нет вовсе, его место занимает сам узор, а покрытие
// приходит из покраски. Оба конца при этом доказуемо жёсткие: coverage = 0 даёт порог 1+edge
// при source <= 1 (маска ровно 0), coverage = 1 даёт порог -edge при source >= 0 (ровно 1).
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
// SampleLayerHeight — общий канал с раздельным/упакованным режимом маски), художественный
// узор (SamplePatternMultiplier — 1.0, если _PATTERN выключен) и порог покрытия.
//
// Сам порог — в ENV_ThresholdMask выше, общий с мазком смешивания (04). Узор эту формулу
// не трогает: он умножает источник ДО smoothstep, поэтому при Coverage = 0 источник
// остаётся <= 1 и маска = 0 независимо от узора (пункт 1 верификации 02 остаётся в силе).
half ComputeOverlayMask0(float2 uv, half3 normalWS, half patternMultiplier)
{
#if defined(_OVERLAY_LAYER_0)
    half3 upPS = half3(GetProjectionDirection(float3(0.0, 1.0, 0.0)));
    half slope = saturate(dot(normalWS, upPS));

    half height = SampleLayerHeight(uv);
    half relief = (half(1.0) - height) * _OverlayHeightDepth0;

    float source = saturate(float(slope) + float(relief)) * float(patternMultiplier);
    return ENV_ThresholdMask(source, _OverlayCoverage0, _OverlayEdgeSoftness0);
#else
    return half(0.0);
#endif
}

// Подмешивание слоя в поверхность — вклад в альбедо/металличность/гладкость/затенение
// одинаков для ForwardLit и Meta-пасса, оба зовут эту функцию с одной и той же маской.
// alpha — по той же причине, что описана в шапке ApplyHeightGradient ниже: подмешивать
// после AlphaModulate надо тем же способом, иначе прозрачная поверхность красит фон.
//
// Затенение (occlusion) тикет прямо не называет — только альбедо, нормаль и блеск, — но
// база под сплошным снегом не может остаться такой, как была: тёмный шов кладки просвечивал
// бы сквозь белое. lerp к 1.0 — снег не затеняет сам себя.
void ApplyOverlayLayer0(float3 positionPS, half mask, half alpha, inout SurfaceData surfaceData)
{
#if defined(_OVERLAY_LAYER_0)
    float2 overlayUV = positionPS.xz * _OverlayTiling0;
    half3 overlayAlbedo = SAMPLE_TEXTURE2D(_OverlayMap0, sampler_OverlayMap0, overlayUV).rgb * _OverlayColor0.rgb;

    surfaceData.albedo = lerp(surfaceData.albedo, AlphaModulate(overlayAlbedo, alpha), mask);
    surfaceData.metallic = lerp(surfaceData.metallic, _OverlayMetallic0, mask);
    surfaceData.smoothness = lerp(surfaceData.smoothness, _OverlaySmoothness0, mask);
    surfaceData.occlusion = lerp(surfaceData.occlusion, half(1.0), mask);
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
    float2 overlayUV = positionPS.xz * _OverlayTiling0;
    half4 packedNormal = SAMPLE_TEXTURE2D(_OverlayNormalMap0, sampler_OverlayMap0, overlayUV);
#if BUMP_SCALE_NOT_SUPPORTED
    half3 normalTS = UnpackNormal(packedNormal);
#else
    half3 normalTS = UnpackNormalScale(packedNormal, _OverlayNormalScale0);
#endif
    float3 normalPS = float3(normalTS.x, normalTS.z, normalTS.y);
    return half3(normalize(GetProjectionNormal(normalPS)));
}

// Смешивание материалов мазком (_MATERIAL_MIX): вес подмешивания одного слоя.
//
// Контракт каналов один на оба источника: R — резерв, G — слой 1, B — слой 2, A — резерв.
// Резерв не читается вообще, поэтому «покраска в зарезервированные каналы не влияет на вид»
// выполняется по построению, а не проверкой. Раскладка совпала с дефолтной раскладкой кисти
// RealBlend (None, G, B, A, R) независимо — кисть даёт выбор канала на слой, менять там
// ничего не нужно.
//
// Источник переключается keyword'ом, а не ползунком: при вершинном режиме выборки маски
// быть не должно вовсе.
//
// Проекция маски — общая с узором, через GetPatternUV: Planar для модулей (мазок течёт
// через шов), UV для уникальных объектов и вертикальных граней. Отдельной ручки смещения
// в планарном режиме нет — место мазка задаётся тайлингом.
half2 SampleMixWeights(float2 uv, float3 positionPS, half4 vertexColor)
{
#if defined(_MIX_MASK_TEXTURE)
    float2 maskUV = GetPatternUV(uv, positionPS, _MixMaskTiling);
    half4 mask = SAMPLE_TEXTURE2D(_MixMaskMap, sampler_MixMaskMap, maskUV);
    return half2(mask.g, mask.b);
#else
    return half2(vertexColor.g, vertexColor.b);
#endif
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
// Затенение к единице — тот же приём и тот же довод, что в слое наноса: AO базы описывает
// швы базы, под заменённым материалом он не к месту, а своего AO у слоя нет.
//
// alpha — по той же причине, что у наноса и градиента: подмешивать после AlphaModulate надо
// тем же способом, иначе прозрачная поверхность красит фон.
void ENV_ApplyMixLayer(
    float2 uv, half mask, half alpha,
    TEXTURE2D_PARAM(albedoMap, albedoSampler),
    TEXTURE2D_PARAM(normalMap, normalSampler),
    half3 tint, float tiling, half normalScale, half metallic, half smoothness,
    inout SurfaceData surfaceData)
{
    float2 layerUV = uv * tiling;

    half3 layerAlbedo = SAMPLE_TEXTURE2D(albedoMap, albedoSampler, layerUV).rgb * tint;

    half4 packedNormal = SAMPLE_TEXTURE2D(normalMap, normalSampler, layerUV);
#if BUMP_SCALE_NOT_SUPPORTED
    half3 layerNormalTS = UnpackNormal(packedNormal);
#else
    half3 layerNormalTS = UnpackNormalScale(packedNormal, normalScale);
#endif

    surfaceData.albedo     = lerp(surfaceData.albedo, AlphaModulate(layerAlbedo, alpha), mask);
    surfaceData.normalTS   = lerp(surfaceData.normalTS, layerNormalTS, mask);
    surfaceData.metallic   = lerp(surfaceData.metallic, metallic, mask);
    surfaceData.smoothness = lerp(surfaceData.smoothness, smoothness, mask);
    surfaceData.occlusion  = lerp(surfaceData.occlusion, half(1.0), mask);
}

// Смешивание материалов мазком целиком. Зовётся ДО слоя наноса: снег падает на то, что
// под ним уже сложилось, и маска наноса считается по смешанной нормали.
//
// Маска мазка идёт через тот же ENV_ThresholdMask, что и нанос, но роли параметров у них
// разные: процедурного источника у мазка нет вовсе — его место занимает сам узор, — а
// покрытие приходит попиксельно из веса покраски, домноженного на _MixCoverage.
// Отсюда оба конца жёсткие: покрытие 0 даёт маску ровно 0 при любом узоре, 1 — ровно 1.
// При выключенном _PATTERN источник равен единице, и граница становится жёсткой: без узора
// детализацию брать неоткуда, мягчит её только _MixEdgeSoftness.
//
// _MixCoverage на нуле выключает мазок целиком — это и есть защита от белого вершинного
// цвета непокрашенного меша (см. комментарий к свойству в CBUFFER).
//
// Слой 2 ложится ПОСЛЕ слоя 1 — порядок фиксирован тикетом. Кисть держит сумму весов <= 1
// («база есть остаток»), так что на практике перекрытия почти нет; при обоих каналах
// на максимуме выигрывает второй, и это названное решение, а не побочный эффект.
void ApplyMaterialMix(float2 uv, float3 positionPS, half4 vertexColor, half patternMultiplier,
                      half alpha, inout SurfaceData surfaceData)
{
#if defined(_MATERIAL_MIX)
    half2 weights = saturate(SampleMixWeights(uv, positionPS, vertexColor) * _MixCoverage);

    half mask1 = ENV_ThresholdMask(float(patternMultiplier), weights.x, _MixEdgeSoftness);
    ENV_ApplyMixLayer(uv, mask1, alpha,
        TEXTURE2D_ARGS(_MixMap1, sampler_MixMap1),
        TEXTURE2D_ARGS(_MixNormalMap1, sampler_MixMap1),
        _MixColor1.rgb, _MixTiling1, _MixNormalScale1, _MixMetallic1, _MixSmoothness1,
        surfaceData);

#if defined(_MATERIAL_MIX_2)
    half mask2 = ENV_ThresholdMask(float(patternMultiplier), weights.y, _MixEdgeSoftness);
    ENV_ApplyMixLayer(uv, mask2, alpha,
        TEXTURE2D_ARGS(_MixMap2, sampler_MixMap1),
        TEXTURE2D_ARGS(_MixNormalMap2, sampler_MixMap1),
        _MixColor2.rgb, _MixTiling2, _MixNormalScale2, _MixMetallic2, _MixSmoothness2,
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

    // _SmoothnessIsRoughness: инверсия под Substance/glTF, где канал приходит как roughness,
    // а не smoothness. Одна галочка на оба режима — источник тот же вопрос независимо
    // от того, альфа это упакованной карты или отдельный файл.
    //
    // Скаляр применяется В ПРОСТРАНСТВЕ КАНАЛА, а не поверх инверсии. Иначе пустой слот
    // ломает правило «нет карты — падаем на скаляр»: заглушка "white" даёт канал 1,
    // инверсия превращает её в 0, и гладкость становится нулём при любом положении
    // слайдера. Здесь в режиме roughness скаляр масштабирует шероховатость, и пустая
    // карта в обоих режимах даёт ровно _Smoothness.
    outSurfaceData.smoothness = (_SmoothnessIsRoughness > half(0.5))
        ? (half(1.0) - smoothnessChannel * (half(1.0) - _Smoothness))
        : (smoothnessChannel * _Smoothness);

    // Без инверсии зелёного канала: она применилась бы только здесь, а пакетные пассы
    // DepthNormals / ShadowCaster сэмплят нормаль сами — см. комментарий в ENV_Lit.shader.
    outSurfaceData.normalTS = SampleNormal(uv, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap), _BumpScale);

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
