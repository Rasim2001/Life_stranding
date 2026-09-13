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
// умножение умеет только темнить. Пространство — Local/World, см. ENV_LitForwardPass.hlsl.
float _GradientMinHeight;
float _GradientMaxHeight;
half4 _GradientColor01;
half4 _GradientColor02;
half _GradientStrength;
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

#endif

TEXTURE2D(_MaskMap); SAMPLER(sampler_MaskMap);

// Раздельный режим (_MASKMAP_SEPARATE): три карты вместо одной упакованной — тайлящаяся
// стена, которой нужна только гладкость, обходится одной одноканальной картой вдвое дешевле
// упакованной BC7. Все три делят один SAMPLER — настройки фильтрации у них одинаковые,
// три состояния сэмплера тратить незачем. Дефолт "white" = 1.0, пустой слот падает
// на скаляр (_Metallic / _OcclusionStrength / _Smoothness) сам, без отдельного keyword'а.
// Height-слота нет — у height в этой версии нет потребителя (см. спек, "Не делаем").
TEXTURE2D(_MetallicMap);
TEXTURE2D(_OcclusionMap);
TEXTURE2D(_SmoothnessMap);
SAMPLER(sampler_MetallicMap);

// Раскладка HDRP Mask Map с заменой канала B (detail mask там не нужен) на резерв под
// height — см. §6 "База". Канал бесплатен: альфа под smoothness уже требует BC7, а BC7
// несёт все четыре канала независимо от того, пишем мы в B или нет.
half4 SampleMaskMap(float2 uv)
{
    return SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, uv);
}

half SampleMaskMapOcclusion(half occlusionChannel)
{
    return LerpWhiteTo(occlusionChannel, _OcclusionStrength);
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
