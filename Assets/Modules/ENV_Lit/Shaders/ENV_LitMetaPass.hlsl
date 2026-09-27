#ifndef SPIDERRIG_ENV_LIT_META_PASS_INCLUDED
#define SPIDERRIG_ENV_LIT_META_PASS_INCLUDED

// Форк Packages/com.unity.render-pipelines.universal/ShaderLibrary/UniversalMetaPass.hlsl
// + Packages/com.unity.render-pipelines.universal/Shaders/LitMetaPass.hlsl (URP 17.3.0,
// com.unity.render-pipelines.universal@37e06a5b08b3), слитые в один файл с правками,
// помеченными "ENV_Lit:" ниже. На мажорном апгрейде URP — сверить с новыми версиями обоих
// файлов. Спек: .scratch/env-lit-layers/issues/01-projection-space-and-meta-pass.md.
//
// Причина форка: стоковый мета-пасс несёт во фрагмент только positionCS и uv. Градиент
// по высоте (и следующие за ним нанос и узор) зависят от позиции, а она есть на входе
// вершинника — positionOS и normalOS объявлены в Attributes стокового UniversalMetaPass.hlsl,
// просто не пробрасываются дальше. Матрица объекта в мета-пассе валидна — проверено
// зондом 13.09.2026, объект на высоте 10.5 дал мировую координату 10.04...10.98.
//
// MetaInput.hlsl включаем напрямую, а не UniversalMetaPass.hlsl — тот объявляет свои
// Attributes/Varyings, и это ровно то, что форкается.
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"

struct Attributes
{
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    // ENV_Lit: сток мета-пасса тангент не читает вовсе. Нужен для мировой нормали под
    // _NORMALMAP — см. InitializeInputData в ENV_LitForwardPass.hlsl, тот же приём.
    float4 tangentOS    : TANGENT;
    float2 uv0          : TEXCOORD0;
    float2 uv1          : TEXCOORD1;
    float2 uv2          : TEXCOORD2;
    // ENV_Lit: вертекс-цвет — вес мазка смешивания (04). G — первый подмешиваемый слой,
    // B — второй, R и A зарезервированы. См. SampleMixCoverage в ENV_LitInput.hlsl.
    float4 color        : COLOR;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

// ENV_Lit: бюджет интерполяторов при target 2.0 — восемь. Заняты: uv(0), VizUV(1),
// LightCoord(2) — оба только под EDITOR_VISUALIZATION, positionWS(3), normalWS(4),
// tangentWS(5) — только под _NORMALMAP, vertexColor(6). Худший случай — 7 из 8, один
// слот свободен под будущие тикеты 02-04.
struct Varyings
{
    float4 positionCS   : SV_POSITION;
    // ENV_Lit: xy — TRANSFORM_TEX(uv0, _BaseMap), zw — сырой uv0 (см. ENV_LitForwardPass.hlsl).
    float4 uv           : TEXCOORD0;
#ifdef EDITOR_VISUALIZATION
    float2 VizUV        : TEXCOORD1;
    float4 LightCoord   : TEXCOORD2;
#endif
    // ENV_Lit: мировая позиция. Пространство проекции резолвится во фрагменте так же, как
    // в ForwardLit (positionOS = TransformWorldToObject, дальше ENV_OverlayPosition):
    // трипланар карт Base нужны и мировая, и объектная позиция, а преобразование аффинное,
    // поэтому интерполяция даёт то же число, что считалось бы в вершине.
    float3 positionWS   : TEXCOORD3;
    // ENV_Lit: мировая нормаль меша — вход для маски слоя наноса (02), она считается
    // по нормали ПОСЛЕ карты нормалей (см. ENV_ResolveNormalWS в ENV_LitInput.hlsl).
    float3 normalWS     : TEXCOORD4;
#if defined(_NORMALMAP)
    half4 tangentWS     : TEXCOORD5;    // xyz: tangent, w: sign
#endif
    // ENV_Lit: вес мазка смешивания, см. правку Attributes выше.
    half4 vertexColor   : TEXCOORD6;
};

Varyings ENV_LitMetaVertex(Attributes input)
{
    Varyings output = (Varyings)0;

    // ENV_Lit: вызывать с исходным input.positionOS.xyz — функция берёт позицию по значению
    // и для UV-развёртки лайтмапа затирает только локальную копию x/y внутри себя. Читать
    // input.positionOS дальше для мировой/объектной позиции безопасно.
    output.positionCS = UnityMetaVertexPosition(input.positionOS.xyz, input.uv1, input.uv2);
    output.uv = float4(TRANSFORM_TEX(input.uv0, _BaseMap), input.uv0);

#ifdef EDITOR_VISUALIZATION
    UnityEditorVizData(input.positionOS.xyz, input.uv0, input.uv1, input.uv2, output.VizUV, output.LightCoord);
#endif

    output.positionWS = TransformObjectToWorld(input.positionOS.xyz);

    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
    output.normalWS = normalInput.normalWS;
#if defined(_NORMALMAP)
    real sign = input.tangentOS.w * GetOddNegativeScale();
    output.tangentWS = half4(normalInput.tangentWS.xyz, sign);
#endif

    output.vertexColor = half4(input.color);

    return output;
}

half4 ENV_LitMetaFragment(Varyings input) : SV_Target
{
#ifdef EDITOR_VISUALIZATION
    UnityMetaInput metaInputViz;
    metaInputViz.VizUV = input.VizUV;
    metaInputViz.LightCoord = input.LightCoord;
#endif

    // ENV_Lit: та же геометрия карт Base, что в ForwardLit — покрытие и цвет в запечке
    // обязаны совпадать с видимыми при любой проекции.
#if defined(_NORMALMAP)
    half4 baseTangentWS = input.tangentWS;
#else
    half4 baseTangentWS = half4(0.0, 0.0, 0.0, 1.0);
#endif
    ENV_BaseGeometry baseGeo = ENV_MakeBaseGeometry(input.uv, input.positionWS, input.normalWS, baseTangentWS);

    SurfaceData surfaceData;
    InitializeStandardLitSurfaceData(baseGeo, surfaceData);

    // ENV_Lit: та же точка входа, что и в ForwardLit — оба пасса считают эффект одинаково.
    surfaceData.albedo = ApplyHeightGradient(surfaceData.albedo,
        ENV_GradientHeight(input.positionWS, baseGeo.positionOS), surfaceData.alpha);

    // ENV_Lit: те же ApplyMaterialMix/ComputeOverlayMask0, что и в ForwardLit — оба пасса
    // считают поверхность одной парой функций (каждая сама сэмплирует свой блок узора,
    // тикет 06), иначе площадь эффекта в кадре и в запечке разойдётся. Нормаль здесь
    // не собирается: UnityMetaFragment её не читает, но normalTS мазок всё равно правит —
    // её читает маска наноса ниже.
    ApplyMaterialMix(baseGeo, input.vertexColor, surfaceData.alpha, surfaceData);

#if defined(_OVERLAY_LAYER_0)
    // ENV_Lit: та же ComputeOverlayMask0/ApplyOverlayLayer0, что и в ForwardLit — то самое
    // место, где «площадь наноса в запечке совпадает с видимой» либо выполняется, либо нет.
    // Мировая нормаль слоя здесь не считается: UnityMetaFragment её не читает вовсе.
    // ENV_Lit Top Relief: Meta uses the same mesh-normal mask contract as ForwardLit.
    float3 positionPS = ENV_OverlayPosition(input.positionWS, baseGeo.positionOS);
    half3 overlayBaseNormalWS = NormalizeNormalPerPixel(input.normalWS);
    half overlayMask = ComputeOverlayMask0(baseGeo.uv0, positionPS, overlayBaseNormalWS);
    ApplyOverlayLayer0(positionPS, overlayMask, surfaceData.alpha, surfaceData);
#endif

    BRDFData brdfData;
    InitializeBRDFData(surfaceData.albedo, surfaceData.metallic, surfaceData.specular, surfaceData.smoothness, surfaceData.alpha, brdfData);

    UnityMetaInput metaInput;
    metaInput.Albedo = brdfData.diffuse + brdfData.specular * brdfData.roughness * 0.5;
    metaInput.Emission = surfaceData.emission;
#ifdef EDITOR_VISUALIZATION
    metaInput.VizUV = metaInputViz.VizUV;
    metaInput.LightCoord = metaInputViz.LightCoord;
#endif

    return UnityMetaFragment(metaInput);
}

#endif // SPIDERRIG_ENV_LIT_META_PASS_INCLUDED
