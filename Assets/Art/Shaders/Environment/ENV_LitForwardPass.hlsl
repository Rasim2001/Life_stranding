#ifndef SPIDERRIG_ENV_LIT_FORWARD_PASS_INCLUDED
#define SPIDERRIG_ENV_LIT_FORWARD_PASS_INCLUDED

// Копия Packages/com.unity.render-pipelines.universal/Shaders/LitForwardPass.hlsl (URP
// 17.3.0, com.unity.render-pipelines.universal@37e06a5b08b3) с правками, помеченными
// "ENV_Lit:" ниже. На мажорном апгрейде URP — сверить с новой версией файла.
// Спек: docs/lighting-and-shading.md §6.

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
// ENV_Lit: §6 правило 7 — прозрачность не пишет глубину, полноэкранный пасс тумана её не
// красит, поэтому прозрачный режим обязан сам применить туман через глобалы WeatherFog.
#include "../WeatherFog.hlsl"

#if defined(LOD_FADE_CROSSFADE)
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
#endif

#if defined(_PARALLAXMAP)
#define REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR
#endif

#if (defined(_NORMALMAP) || (defined(_PARALLAXMAP) && !defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR))) || defined(_DETAIL)
#define REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR
#endif

// ENV_Lit: positionWS у стока объявлен под #if REQUIRES_WORLD_SPACE_POS_INTERPOLATOR —
// этот макро включается светом/APV не всегда. Правилу 7 он нужен безусловно (прозрачный
// туман считается от positionWS в любой конфигурации), поэтому форсируем его здесь.
#if !defined(REQUIRES_WORLD_SPACE_POS_INTERPOLATOR)
#define REQUIRES_WORLD_SPACE_POS_INTERPOLATOR
#endif

// keep this file in sync with LitGBufferPass.hlsl (GBuffer-пасс в ENV_Lit не используется,
// см. §6 в ENV_Lit.shader — Forward+, не Deferred)

struct Attributes
{
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float4 tangentOS    : TANGENT;
    float2 texcoord     : TEXCOORD0;
    float2 staticLightmapUV   : TEXCOORD1;
    float2 dynamicLightmapUV  : TEXCOORD2;
    // ENV_Lit: сток вообще не читает vertex color. Заводим канал про запас — потребитель
    // не назначен (см. docs/lighting-and-shading.md §9 "Отложено": маска-мазок и тонировка
    // альбедо из vertex color обе пока закрыты — см. AllIn13DShader как референс метода,
    // который туда же и упирается). Ничего в этом файле его не читает.
    float4 color        : COLOR;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float2 uv                       : TEXCOORD0;

#if defined(REQUIRES_WORLD_SPACE_POS_INTERPOLATOR)
    float3 positionWS               : TEXCOORD1;
#endif

    float3 normalWS                 : TEXCOORD2;
#if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR)
    half4 tangentWS                : TEXCOORD3;    // xyz: tangent, w: sign
#endif

#ifdef _ADDITIONAL_LIGHTS_VERTEX
    half4 fogFactorAndVertexLight   : TEXCOORD5; // x: fogFactor, yzw: vertex light
#else
    half  fogFactor                 : TEXCOORD5;
#endif

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    float4 shadowCoord              : TEXCOORD6;
#endif

#if defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR)
    half3 viewDirTS                : TEXCOORD7;
#endif

    DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 8);
#ifdef DYNAMICLIGHTMAP_ON
    float2  dynamicLightmapUV : TEXCOORD9; // Dynamic lightmap UVs
#endif

#ifdef USE_APV_PROBE_OCCLUSION
    float4 probeOcclusion : TEXCOORD10;
#endif

    // ENV_Lit: непотреблённый vertex color, см. правку Attributes выше.
    half4 vertexColor               : TEXCOORD11;

    float4 positionCS               : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

void InitializeInputData(Varyings input, half3 normalTS, out InputData inputData)
{
    inputData = (InputData)0;

#if defined(REQUIRES_WORLD_SPACE_POS_INTERPOLATOR)
    inputData.positionWS = input.positionWS;
#endif

#if defined(DEBUG_DISPLAY)
    inputData.positionCS = input.positionCS;
#endif

    half3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
#if defined(_NORMALMAP) || defined(_DETAIL)
    float sgn = input.tangentWS.w;      // should be either +1 or -1
    float3 bitangent = sgn * cross(input.normalWS.xyz, input.tangentWS.xyz);
    half3x3 tangentToWorld = half3x3(input.tangentWS.xyz, bitangent.xyz, input.normalWS.xyz);

    #if defined(_NORMALMAP)
    inputData.tangentToWorld = tangentToWorld;
    #endif
    inputData.normalWS = TransformTangentToWorld(normalTS, tangentToWorld);
#else
    inputData.normalWS = input.normalWS;
#endif

    inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);
    inputData.viewDirectionWS = viewDirWS;

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    inputData.shadowCoord = input.shadowCoord;
#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
    inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
#else
    inputData.shadowCoord = float4(0, 0, 0, 0);
#endif
#ifdef _ADDITIONAL_LIGHTS_VERTEX
    inputData.fogCoord = InitializeInputDataFog(float4(input.positionWS, 1.0), input.fogFactorAndVertexLight.x);
    inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;
#else
    inputData.fogCoord = InitializeInputDataFog(float4(input.positionWS, 1.0), input.fogFactor);
#endif

    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);

    #if defined(DEBUG_DISPLAY)
    #if defined(DYNAMICLIGHTMAP_ON)
    inputData.dynamicLightmapUV = input.dynamicLightmapUV;
    #endif
    #if defined(LIGHTMAP_ON)
    inputData.staticLightmapUV = input.staticLightmapUV;
    #else
    inputData.vertexSH = input.vertexSH;
    #endif
    #if defined(USE_APV_PROBE_OCCLUSION)
    inputData.probeOcclusion = input.probeOcclusion;
    #endif
    #endif
}

void InitializeBakedGIData(Varyings input, inout InputData inputData)
{
    #if defined(_SCREEN_SPACE_IRRADIANCE)
    inputData.bakedGI = SAMPLE_GI(_ScreenSpaceIrradiance, input.positionCS.xy);
    #elif defined(DYNAMICLIGHTMAP_ON)
    inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.dynamicLightmapUV, input.vertexSH, inputData.normalWS);
    inputData.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);
    #elif !defined(LIGHTMAP_ON) && (defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2))
    inputData.bakedGI = SAMPLE_GI(input.vertexSH,
        GetAbsolutePositionWS(inputData.positionWS),
        inputData.normalWS,
        inputData.viewDirectionWS,
        input.positionCS.xy,
        input.probeOcclusion,
        inputData.shadowMask);
    #else
    inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.vertexSH, inputData.normalWS);
    inputData.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);
    #endif
}

///////////////////////////////////////////////////////////////////////////////
//                  Vertex and Fragment functions                            //
///////////////////////////////////////////////////////////////////////////////

// Used in Standard (Physically Based) shader
Varyings LitPassVertex(Attributes input)
{
    Varyings output = (Varyings)0;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);

    // normalWS and tangentWS already normalize.
    // this is required to avoid skewing the direction during interpolation
    // also required for per-vertex lighting and SH evaluation
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

    half3 vertexLight = VertexLighting(vertexInput.positionWS, normalInput.normalWS);

    half fogFactor = 0;
    #if !defined(_FOG_FRAGMENT)
        fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
    #endif

    output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);

    // ENV_Lit: непотреблённый vertex color, см. правку Attributes выше.
    output.vertexColor = half4(input.color);

    // already normalized from normal transform to WS.
    output.normalWS = normalInput.normalWS;
#if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR) || defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR)
    real sign = input.tangentOS.w * GetOddNegativeScale();
    half4 tangentWS = half4(normalInput.tangentWS.xyz, sign);
#endif
#if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR)
    output.tangentWS = tangentWS;
#endif

#if defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR)
    half3 viewDirWS = GetWorldSpaceNormalizeViewDir(vertexInput.positionWS);
    half3 viewDirTS = GetViewDirectionTangentSpace(tangentWS, output.normalWS, viewDirWS);
    output.viewDirTS = viewDirTS;
#endif

    OUTPUT_LIGHTMAP_UV(input.staticLightmapUV, unity_LightmapST, output.staticLightmapUV);
#ifdef DYNAMICLIGHTMAP_ON
    output.dynamicLightmapUV = input.dynamicLightmapUV.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
#endif
    OUTPUT_SH4(vertexInput.positionWS, output.normalWS.xyz, GetWorldSpaceNormalizeViewDir(vertexInput.positionWS), output.vertexSH, output.probeOcclusion);
#ifdef _ADDITIONAL_LIGHTS_VERTEX
    output.fogFactorAndVertexLight = half4(fogFactor, vertexLight);
#else
    output.fogFactor = fogFactor;
#endif

#if defined(REQUIRES_WORLD_SPACE_POS_INTERPOLATOR)
    output.positionWS = vertexInput.positionWS;
#endif

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    output.shadowCoord = GetShadowCoord(vertexInput);
#endif

    output.positionCS = vertexInput.positionCS;

    return output;
}

// ENV_Lit: градиент по высоте (_HEIGHT_GRADIENT) — только здесь, не в ENV_LitInput.hlsl.
// Причина: набор интерполяторов Meta-пасса URP несёт только positionCS и uv, мировой
// позиции там нет, а форкать Meta-пасс ради отскока от десятисантиметровой полоски
// отвергнуто в спеке (.scratch/env-lit-shader/spec.md). Эффект в запечку сознательно
// не едет. lerp, не умножение — умножение умеет только темнить, см. тот же спек.
//
// alpha приезжает параметром из-за порядка: InitializeStandardLitSurfaceData уже прогнала
// альбедо через AlphaModulate (на Multiply-блендинге это lerp к белому по альфе), а мы
// подмешиваем цвет после неё. Подмешать сырой gradientColor — значит вернуть тонировку
// в полную силу мимо этого затухания, и почти прозрачная Multiply-поверхность начнёт
// красить фон. Модулируем цвет градиента тем же способом: lerp(AM(a), AM(g), s) тождественно
// равно AM(lerp(a, g, s)), то есть результат тот же, как если бы градиент шёл до модуляции.
// На непрозрачном пути AlphaModulate — тождество, там ничего не меняется.
half3 ApplyHeightGradient(half3 albedo, float3 positionWS, half alpha)
{
#if defined(_HEIGHT_GRADIENT)
#if defined(_GRADIENTSPACE_WORLD)
    float height = GetAbsolutePositionWS(positionWS).y;
#else
    float height = TransformWorldToObject(positionWS).y;
#endif
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

// Used in Standard (Physically Based) shader
void LitPassFragment(
    Varyings input
    , out half4 outColor : SV_Target0
#ifdef _WRITE_RENDERING_LAYERS
    , out uint outRenderingLayers : SV_Target1
#endif
)
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

#if defined(_PARALLAXMAP)
#if defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR)
    half3 viewDirTS = input.viewDirTS;
#else
    half3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
    half3 viewDirTS = GetViewDirectionTangentSpace(input.tangentWS, input.normalWS, viewDirWS);
#endif
    ApplyPerPixelDisplacement(viewDirTS, input.uv);
#endif

    SurfaceData surfaceData;
    InitializeStandardLitSurfaceData(input.uv, surfaceData);
    surfaceData.albedo = ApplyHeightGradient(surfaceData.albedo, input.positionWS, surfaceData.alpha);

#ifdef LOD_FADE_CROSSFADE
    LODFadeCrossFade(input.positionCS);
#endif

    InputData inputData;
    InitializeInputData(input, surfaceData.normalTS, inputData);
    SETUP_DEBUG_TEXTURE_DATA(inputData, UNDO_TRANSFORM_TEX(input.uv, _BaseMap));

#if defined(_DBUFFER)
    ApplyDecalToSurfaceData(input.positionCS, surfaceData, inputData);
#endif

    InitializeBakedGIData(input, inputData);

    half4 color = UniversalFragmentPBR(inputData, surfaceData);
    color.rgb = MixFog(color.rgb, inputData.fogCoord);

    // ENV_Lit: §6 правило 7 — прозрачность не пишет глубину, фича Fog стоит на
    // BeforeRenderingTransparents (injectionPoint 450 в URP_Main_Renderer) и прозрачные
    // не красит. Применяем туман сами.
    //
    // Берём SR_WorldFogOverlay, а не SR_ApplyWorldFog: первая отдаёт цвет и плотность
    // раздельно, и это ровно тот случай, для которого она заведена (см. WeatherFog.hlsl) —
    // потребитель без доступа к цвету под собой. Смешение делает не она, а аппаратный
    // блендер, и у каждого режима своя нейтраль.
    //
    // Главное: фон под прозрачной поверхностью полноэкранный пасс УЖЕ затуманил на всю
    // глубину. Поэтому наш вклад обязан ограничиться собственной долей покрытия, иначе
    // просвечивающая часть получит туман дважды — от пасса по глубине стены и от нас
    // по своей. Где блендер домножает на альфу сам (SrcBlend = SrcAlpha), это уже сделано;
    // где SrcBlend = One — premultiplied-путь, и долю надо учесть здесь.
#if defined(_SURFACE_TYPE_TRANSPARENT)
    half4 weatherFog = SR_WorldFogOverlay(inputData.positionWS, _WorldSpaceCameraPos);

    #if defined(_ALPHAMODULATE_ON)
    // Multiply: поверхность работает множителем для фона, нейтраль — белый.
    color.rgb = lerp(color.rgb, half3(1.0, 1.0, 1.0), weatherFog.a);
    #else
    // Additive (DstBlend = One): вклад только добавляет свет. Цвет тумана добавлять
    // нельзя — фон уже получил его от пасса; гасим свечение к нулю.
    half3 fogTarget = (_DstBlend == half(1.0)) ? half3(0.0, 0.0, 0.0) : weatherFog.rgb;
    fogTarget = (_SrcBlend == half(1.0)) ? fogTarget * color.a : fogTarget;
    color.rgb = lerp(color.rgb, fogTarget, weatherFog.a);
    #endif
#endif

    color.a = OutputAlpha(color.a, IsSurfaceTypeTransparent(_Surface));

    outColor = color;

#ifdef _WRITE_RENDERING_LAYERS
    outRenderingLayers = EncodeMeshRenderingLayer();
#endif
}

#endif // SPIDERRIG_ENV_LIT_FORWARD_PASS_INCLUDED
