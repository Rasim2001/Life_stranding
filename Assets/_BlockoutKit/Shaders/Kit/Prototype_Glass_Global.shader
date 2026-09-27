Shader "URP/Prototype_Glass_Global_URP17"
{
    Properties
    {
        _BaseColor     ("Base Color", Color) = (0.06228374,0.8320726,0.9411765,1)
        _Grid          ("Grid", 2D) = "white" {}
        _GridScale     ("Grid Scale", Float) = 5
        _Falloff       ("Falloff", Float) = 50
        _Opacity       ("Opacity", Color) = (0.5661765,0.5661765,0.5661765,0)
        _OverlayAmount ("Overlay Amount", Range(0,1)) = 1
        _Smoothness    ("Smoothness", Range(0,1)) = 1
        _Specular      ("Specular", Range(0,1)) = 0.1
    }

    SubShader
    {
        Tags 
        { 
            "RenderPipeline"="UniversalPipeline" 
            "RenderType"="Transparent" 
            "Queue"="Transparent" 
            "IgnoreProjector"="True" 
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex   vert
            #pragma fragment frag

            // URP lighting keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float  _GridScale;
                float  _Falloff;
                float4 _Opacity;
                float  _OverlayAmount;
                float  _Smoothness;
                float  _Specular;
            CBUFFER_END

            TEXTURE2D(_Grid);
            SAMPLER(sampler_Grid);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS  : TEXCOORD1;
                float3 normalWS    : TEXCOORD2;
                float  fogFactor   : TEXCOORD3;
                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                float4 shadowCoord : TEXCOORD4;
                #endif
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // Triplanar sampling matching ASE behavior
            float4 TriplanarGrid(float3 worldPos, float3 worldNormal, float falloff, float tiling)
            {
                float3 n = worldNormal;
                float3 an = pow(abs(n), falloff);
                an /= max(1e-5, an.x + an.y + an.z);
                float3 s = sign(n);

                // Sample from three planes with proper orientation
                float2 uvX = worldPos.zy * float2( s.x, 1.0) * tiling;
                float2 uvY = worldPos.xz * float2( s.y, 1.0) * tiling;
                float2 uvZ = worldPos.xy * float2(-s.z, 1.0) * tiling;

                float4 xSample = SAMPLE_TEXTURE2D(_Grid, sampler_Grid, uvX);
                float4 ySample = SAMPLE_TEXTURE2D(_Grid, sampler_Grid, uvY);
                float4 zSample = SAMPLE_TEXTURE2D(_Grid, sampler_Grid, uvZ);

                // Handle negative Y projection (bottom faces)
                float negProjY = max(0, an.y * -s.y);
                float posProjY = max(0, an.y *  s.y);

                // Blend all projections
                return xSample * an.x + ySample * posProjY + ySample * negProjY + zSample * an.z;
            }

            Varyings vert (Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(IN.normalOS);

                OUT.positionWS  = posInputs.positionWS;
                OUT.positionHCS = posInputs.positionCS;
                OUT.normalWS    = normInputs.normalWS;
                OUT.fogFactor   = ComputeFogFactor(posInputs.positionCS.z);

                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                OUT.shadowCoord = GetShadowCoord(posInputs);
                #endif

                return OUT;
            }

            // Convert smoothness to shininess for Blinn-Phong specular
            float SmoothnessToShininess(float smoothness)
            {
                // Map: smoothness 0 → 8, smoothness 1 → ~1024
                return pow(2.0, lerp(3.0, 10.0, saturate(smoothness)));
            }

            half4 frag (Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                float3 N = SafeNormalize(IN.normalWS);
                float3 V = SafeNormalize(GetWorldSpaceViewDir(IN.positionWS));

                // Triplanar grid sampling
                float4 grid = TriplanarGrid(IN.positionWS, N, _Falloff, _GridScale);

                // Blend overlay with grid based on overlay amount
                float4 overlay = lerp(_Opacity, grid, _OverlayAmount);
                
                // Calculate alpha: 1 - overlay gives transparency effect
                float alpha = saturate(1.0 - overlay.r);
                
                // Calculate base color with grid overlay
                float3 baseRGB = (_BaseColor.rgb * lerp(1.0, grid.rgb, _OverlayAmount));

                // === Lighting Calculation ===
                float3 color = 0;

                // Get shadow coordinate
                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                float4 shadowCoord = IN.shadowCoord;
                #elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                #else
                float4 shadowCoord = float4(0, 0, 0, 0);
                #endif

                // Main light (directional)
                Light mainLight = GetMainLight(shadowCoord);
                float3 L = mainLight.direction;
                float NdotL = saturate(dot(N, L));
                
                // Blinn-Phong specular
                float3 H = SafeNormalize(L + V);
                float specPower = SmoothnessToShininess(_Smoothness);
                float spec = pow(saturate(dot(N, H)), specPower) * _Specular;

                // Diffuse + specular from main light
                color += baseRGB * (mainLight.color * NdotL * mainLight.shadowAttenuation);
                color += spec * mainLight.color * mainLight.shadowAttenuation;

                // Ambient lighting
                color += baseRGB * SampleSH(N) * 0.3;

                // Additional lights
                #if defined(_ADDITIONAL_LIGHTS)
                uint pixelLightCount = GetAdditionalLightsCount();
                for (uint i = 0; i < pixelLightCount; i++)
                {
                    Light light = GetAdditionalLight(i, IN.positionWS);
                    float NdotL2 = saturate(dot(N, light.direction));
                    float3 H2 = SafeNormalize(light.direction + V);
                    float spec2 = pow(saturate(dot(N, H2)), specPower) * _Specular;
                    
                    float3 attenuatedLight = light.color * light.distanceAttenuation * light.shadowAttenuation;
                    color += baseRGB * (attenuatedLight * NdotL2);
                    color += spec2 * attenuatedLight;
                }
                #endif

                // Apply fog
                color = MixFog(color, IN.fogFactor);

                return half4(color, alpha);
            }
            ENDHLSL
        }

        // Shadow caster pass (optional for glass, usually transparent objects don't cast shadows)
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float3 _LightDirection;

            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

                // Apply shadow bias
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));

                #if UNITY_REVERSED_Z
                positionCS.z = min(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #else
                positionCS.z = max(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #endif

                output.positionCS = positionCS;
                return output;
            }

            half4 ShadowPassFragment(Varyings input) : SV_TARGET
            {
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack Off
}

/*ASEBEGIN
Version=15900
2567;29;2510;1385;1057.618;574.5319;1.005;True;False
Node;AmplifyShaderEditor.RangedFloatNode;2;-365.939,248.7368;Float;False;Property;_Falloff;Falloff;3;0;Create;True;0;0;False;0;50;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;3;-370.328,165.2742;Float;False;Property;_GridScale;GridScale;2;0;Create;True;0;0;False;0;5;1.36;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TexturePropertyNode;4;-444.7379,-57.00558;Float;True;Property;_Grid;Grid;1;0;Create;True;0;0;False;0;None;93e718fcc411432439749387d41fa07a;False;white;Auto;Texture2D;0;1;SAMPLER2D;0
Node;AmplifyShaderEditor.TriplanarNode;6;-91.2589,129.6845;Float;True;Cylindrical;World;False;Top Texture 0;_TopTexture0;white;2;None;Mid Texture 0;_MidTexture0;white;1;None;Bot Texture 0;_BotTexture0;white;2;None;Triplanar Sampler;False;9;0;SAMPLER2D;;False;5;FLOAT;1;False;1;SAMPLER2D;;False;6;FLOAT;0;False;2;SAMPLER2D;;False;7;FLOAT;0;False;8;FLOAT3;1,1,1;False;3;FLOAT;1;False;4;FLOAT;100;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;5;15.27,378.4728;Float;False;Property;_OverlayAmount;OverlayAmount;5;0;Create;True;0;0;False;0;1;3.52;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;16;267.61,-43.59;Float;False;Property;_Opacity;Opacity;4;0;Create;True;0;0;False;0;0.5661765,0.5661765,0.5661765,0;0.5661765,0.5661765,0.5661765,0;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.OneMinusNode;22;472.9952,426.4481;Float;False;1;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.BlendOpsNode;24;718.2164,420.4175;Float;False;Screen;True;2;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.ColorNode;9;317.2801,621.8003;Float;False;Property;_BaseColor;BaseColor;0;0;Create;True;0;0;False;0;0.06228374,0.8320726,0.9411765,0;0,0,0,0;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.LerpOp;15;557.0811,116.89;Float;False;3;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0,0,0,0;False;2;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.OneMinusNode;19;800.3583,148.8228;Float;False;1;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RangedFloatNode;21;694.642,-161.502;Float;False;Property;_Smoothness;Smoothness;7;0;Create;True;0;0;False;0;1;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;20;690.642,-246.502;Float;False;Property;_Specular;Specular;8;0;Create;True;0;0;False;0;0.1;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;25;935.2972,552.073;Float;False;2;2;0;FLOAT4;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;18;1096,0;Float;False;True;2;Float;ASEMaterialInspector;0;0;StandardSpecular;SyntyStudios/Prototype_Glass_Global;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Transparent;0.5;True;True;0;False;Transparent;;Transparent;All;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;True;2;5;False;-1;10;False;-1;2;5;False;-1;10;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;0;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;6;0;4;0
WireConnection;6;1;4;0
WireConnection;6;2;4;0
WireConnection;6;3;3;0
WireConnection;6;4;2;0
WireConnection;22;0;6;0
WireConnection;24;0;22;0
WireConnection;15;0;16;0
WireConnection;15;1;6;0
WireConnection;15;2;5;0
WireConnection;19;0;15;0
WireConnection;25;0;24;0
WireConnection;25;1;9;0
WireConnection;18;0;25;0
WireConnection;18;3;20;0
WireConnection;18;4;21;0
WireConnection;18;9;19;0
ASEEND*/
//CHKSM=09336F6E99C6C51919FCEB950010D5DA7F958A95