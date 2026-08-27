// Туман на мировой геометрии — fullscreen-пасс поверх непрозрачного кадра. Материал под
// URP.FullScreenPassRendererFeature (тот же встроенный механизм, что уже держит
// TerrainScan — фича "TerrainScan" в URP-Renderer.asset это как раз он, injectionPoint
// BeforeRenderingTransparents), не собственный ScriptableRendererFeature: своя
// Render Graph-обвязка была бы переизобретением уже готового, протестированного пасса.
//
// Вершинный этап — Vert() из Core Blit.hlsl (fullscreen-triangle по SV_VertexID,
// без вершинного буфера), как того требует контракт feature: она вызывает
// DrawProcedural(..., MeshTopology.Triangles, 3, 1, ...) и передаёт исходный цвет через
// _BlitTexture/_BlitScaleBias в MaterialPropertyBlock, а не через свойства материала.
//
// Математика тумана — Assets/Art/Shaders/WeatherFog.hlsl, общий include с будущими
// потребителями (вода, стекло, VFX). Порядок фильтр→туман — как в SHD_Weather_DomeSky.

Shader "SpiderRig/Weather/Fog"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Cull Off
            ZWrite Off
            ZTest Always

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "WeatherFog.hlsl"

            float4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                float3 sceneColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).rgb;

                float deviceDepth = SampleSceneDepth(uv);

                // Дальняя плоскость — фон без непрозрачной геометрии (купол/скайбокс
                // рисуются отдельно и цвет тумана получают через свою фичу, тикет 02).
                // Раздельные ветки reversed/non-reversed Z — сентинел дальней плоскости
                // лежит на разных концах диапазона глубины в зависимости от режима.
                #if UNITY_REVERSED_Z
                if (deviceDepth <= 0.00001h)
                    return float4(sceneColor, 1.0h);
                #else
                if (deviceDepth >= 0.99999h)
                    return float4(sceneColor, 1.0h);
                #endif

                float2 positionNDC = uv;
                float3 positionWS = ComputeWorldSpacePosition(positionNDC, deviceDepth, UNITY_MATRIX_I_VP);

                half3 result = SR_ApplyWorldFog(positionWS, _WorldSpaceCameraPos, half3(sceneColor));
                return float4(float3(result), 1.0h);
            }
            ENDHLSL
        }
    }
}
