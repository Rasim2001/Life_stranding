// Туман на воде — отдельный проход поверх уже нарисованной воды (RenderObjects, слой Water,
// AfterRenderingTransparents), а не правка вендорского шейдера Stylized Water 3. Экранный
// туман (SHD_Weather_Fog, тикет 01) восстанавливает позицию из буфера глубины и физически
// не видит воду: она рисуется позже (BeforeRenderingTransparents) и не пишет глубину
// (_ZWrite: 0 в MAT_StylizedWater3.mat). См. .scratch/fog-generation-and-layers/spec.md,
// разбор возврата к тикету 03.
//
// Обычный геометрический шейдер по мешу воды: positionWS берётся из вершинного этапа, буфер
// глубины не нужен — тем же способом, каким это делал Cozy (BlendStylizedFog). Вывод —
// цвет тумана и его альфа (SR_WorldFogOverlay), аппаратный Blend SrcAlpha OneMinusSrcAlpha
// даёт тот же lerp, что и SR_ApplyFog — второй копии формулы смешения нет.
//
// Cull Back — вода двусторонняя (_Cull: 0 в материале), а туман должен красить только
// видимую сверху грань, как в вендорской ApplyFog (там это делает множитель vFace).

Shader "SpiderRig/Weather/WaterFog"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent" }

        Pass
        {
            Cull Back
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "WeatherFog.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                return float4(SR_WorldFogOverlay(input.positionWS, _WorldSpaceCameraPos));
            }
            ENDHLSL
        }
    }
}
