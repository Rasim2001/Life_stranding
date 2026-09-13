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
        [NoScaleOffset] _MaskMap("Mask Map (R:Metallic G:AO B:_ A:Smoothness)", 2D) = "white" {}
        [NoScaleOffset] _MetallicMap("Metallic", 2D) = "white" {}
        [NoScaleOffset] _OcclusionMap("Occlusion", 2D) = "white" {}
        [NoScaleOffset] _SmoothnessMap("Smoothness", 2D) = "white" {}
        // Дефолты консервативные, а не «прозрачный множитель»: слоты карт по умолчанию
        // "white", поэтому скаляр 1.0 дал бы материалу без единой текстуры metallic = 1
        // и smoothness = 1, то есть зеркало. Те же числа, что у стокового URP Lit.
        _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5
        [ToggleUI] _SmoothnessIsRoughness("Channel is Roughness", Float) = 0.0
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

        // Градиент по высоте (_HEIGHT_GRADIENT) — lerp к цвету, не умножение; не едет
        // в запечку, см. спек. Local — дефолт (башня из повторяющихся этажей).
        [ToggleUI] _HeightGradient("Height Gradient", Float) = 0.0
        [Enum(Local, 0, World, 1)] _GradientSpace("Space", Float) = 0.0
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
            #pragma shader_feature_local_fragment _MASKMAP_SEPARATE
            #pragma shader_feature_local_fragment _ALBEDO_ADJUST
            #pragma shader_feature_local_fragment _HEIGHT_GRADIENT
            #pragma shader_feature_local_fragment _GRADIENTSPACE_WORLD

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

        // Не участвует в обычном рендере — только запечка лайтмап/APV.
        Pass
        {
            Name "Meta"
            Tags { "LightMode" = "Meta" }

            Cull Off

            HLSLPROGRAM
            #pragma target 2.0

            #pragma vertex UniversalVertexMeta
            #pragma fragment UniversalFragmentMetaLit

            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _MASKMAP_SEPARATE
            #pragma shader_feature_local_fragment _ALBEDO_ADJUST
            #pragma shader_feature EDITOR_VISUALIZATION

            #include "ENV_LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitMetaPass.hlsl"
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
    CustomEditor "SpiderRig.Editor.Shaders.ENV_LitShaderGUI"
}
