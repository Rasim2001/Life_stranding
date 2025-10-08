Shader "SyntyStudios/Prototype_Global_URP"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (0.06228374, 0.8320726, 0.9411765, 1)
        _Grid("Grid", 2D) = "white" {}
        _GridScale("Grid Scale", Float) = 5
        _Falloff("Falloff", Float) = 50
        _OverlayAmount("Overlay Amount", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _GridScale;
                float _Falloff;
                float _OverlayAmount;
            CBUFFER_END

            TEXTURE2D(_Grid);
            SAMPLER(sampler_Grid);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            // Triplanar sampling function
            float4 TriplanarSampling(TEXTURE2D_PARAM(tex, texSampler), float3 worldPos, float3 worldNormal, float tiling, float falloff)
            {
                // Calculate blend weights
                float3 blendWeights = pow(abs(worldNormal), falloff);
                blendWeights /= (blendWeights.x + blendWeights.y + blendWeights.z);

                // Sample texture from three planes
                float4 xProjection = SAMPLE_TEXTURE2D(tex, texSampler, worldPos.zy * tiling);
                float4 yProjection = SAMPLE_TEXTURE2D(tex, texSampler, worldPos.xz * tiling);
                float4 zProjection = SAMPLE_TEXTURE2D(tex, texSampler, worldPos.xy * tiling);

                // Blend the projections
                return xProjection * blendWeights.x + 
                       yProjection * blendWeights.y + 
                       zProjection * blendWeights.z;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalInputs.normalWS;

                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                // Triplanar sampling
                float4 gridPattern = TriplanarSampling(TEXTURE2D_ARGS(_Grid, sampler_Grid), 
                                                       input.positionWS, 
                                                       normalize(input.normalWS), 
                                                       _GridScale, 
                                                       _Falloff);

                // Blend with white based on overlay amount
                float4 overlayColor = lerp(float4(1, 1, 1, 1), gridPattern, _OverlayAmount);

                // Apply base color
                float4 finalColor = _BaseColor * overlayColor;

                // Simple lighting
                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = normalize(input.normalWS);
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = finalColor.rgb;
                surfaceData.alpha = 1.0;
                surfaceData.metallic = 0.0;
                surfaceData.smoothness = 0.5;

                return UniversalFragmentPBR(inputData, surfaceData);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positionInputs.positionCS;
                return output;
            }

            float4 ShadowPassFragment(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
/*ASEBEGIN
Version=15900
2567;29;2510;1385;1033.358;572.502;1;True;False
Node;AmplifyShaderEditor.TexturePropertyNode;4;-367.7379,28.99442;Float;True;Property;_Grid;Grid;1;0;Create;True;0;0;False;0;None;93e718fcc411432439749387d41fa07a;False;white;Auto;Texture2D;0;1;SAMPLER2D;0
Node;AmplifyShaderEditor.RangedFloatNode;2;-300.939,312.7368;Float;False;Property;_Falloff;Falloff;3;0;Create;True;0;0;False;0;50;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;3;-296.328,224.2742;Float;False;Property;_GridScale;GridScale;2;0;Create;True;0;0;False;0;5;1.36;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TriplanarNode;6;-75.25891,158.6845;Float;True;Cylindrical;World;False;Top Texture 0;_TopTexture0;white;2;None;Mid Texture 0;_MidTexture0;white;1;None;Bot Texture 0;_BotTexture0;white;2;None;Triplanar Sampler;False;9;0;SAMPLER2D;;False;5;FLOAT;1;False;1;SAMPLER2D;;False;6;FLOAT;0;False;2;SAMPLER2D;;False;7;FLOAT;0;False;8;FLOAT3;1,1,1;False;3;FLOAT;1;False;4;FLOAT;100;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;16;262,-23.5;Float;False;Constant;_White;White;5;0;Create;True;0;0;False;0;1,1,1,0;0,0,0,0;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;5;17.57001,360.4728;Float;False;Property;_OverlayAmount;OverlayAmount;4;0;Create;True;0;0;False;0;1;3.52;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;9;265,-214.5;Float;False;Property;_BaseColor;BaseColor;0;0;Create;True;0;0;False;0;0.06228374,0.8320726,0.9411765,0;0,0,0,0;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.LerpOp;15;514,126.5;Float;False;3;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0,0,0,0;False;2;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;10;701,-52.5;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT4;0.2627451,0.7960785,0.572549,0;False;1;COLOR;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;1;921,11;Float;False;True;2;Float;ASEMaterialInspector;0;0;Standard;SyntyStudios/Prototype_Global;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Opaque;0.5;True;True;0;False;Opaque;;Geometry;All;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;True;0;0;False;-1;0;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;6;0;4;0
WireConnection;6;1;4;0
WireConnection;6;2;4;0
WireConnection;6;3;3;0
WireConnection;6;4;2;0
WireConnection;15;0;16;0
WireConnection;15;1;6;0
WireConnection;15;2;5;0
WireConnection;10;0;9;0
WireConnection;10;1;15;0
WireConnection;1;0;10;0
ASEEND*/
//CHKSM=7EEB3FD505F6952CCF8CFC59E7E1EF4DB75955EA