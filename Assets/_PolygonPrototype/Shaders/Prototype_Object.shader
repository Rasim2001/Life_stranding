Shader "SyntyStudios/Prototype_Object_URP"
{
    Properties
    {
        _BaseColor     ("Base Color", Color) = (0.06228374, 0.8320726, 0.9411765, 1)
        _Grid          ("Grid", 2D) = "white" {}
        _GridScale     ("Grid Scale", Float) = 5
        _Falloff       ("Falloff", Float) = 50
        _OverlayAmount ("Overlay Amount", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags{ "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        Cull Back

        // ---------- Forward (как у теб€) ----------
        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex   vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _SHADOWS_SOFT
            // #pragma multi_compile_fog
            // #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float  _GridScale;
                float  _Falloff;
                float  _OverlayAmount;
            CBUFFER_END

            TEXTURE2D(_Grid);
            SAMPLER(sampler_Grid);

            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; };
            struct Varyings
            {
                float4 positionCS:SV_POSITION;
                float3 positionWS:TEXCOORD0;
                float3 normalWS  :TEXCOORD1;
                float3 positionOS:TEXCOORD2; // дл€ трипланара в Object
                float3 normalOS  :TEXCOORD3;
            };

            float4 TriplanarASE_Object(TEXTURE2D_PARAM(tex, texSampler),
                                       float3 objPos, float3 objNormal,
                                       float tiling, float falloff)
            {
                float3 n  = objNormal;
                float3 an = pow(abs(n), falloff);
                an /= max(1e-5, an.x + an.y + an.z);
                float3 s = sign(n);

                float2 uvX  = objPos.zy * float2( s.x, 1.0) * tiling;
                float2 uvY  = objPos.xz * float2( s.y, 1.0) * tiling;
                float2 uvYN = objPos.xz * float2( s.y, 1.0) * tiling; // отрицательна€ Y-ветка
                float2 uvZ  = objPos.xy * float2(-s.z, 1.0) * tiling;

                float4 xS = SAMPLE_TEXTURE2D(tex, texSampler, uvX);
                float4 yS = SAMPLE_TEXTURE2D(tex, texSampler, uvY);
                float4 yN = SAMPLE_TEXTURE2D(tex, texSampler, uvYN);
                float4 zS = SAMPLE_TEXTURE2D(tex, texSampler, uvZ);

                float negProjY = max(0, an.y * -s.y);
                float posProjY = max(0, an.y *  s.y);

                return xS * an.x + yS * posProjY + yN * negProjY + zS * an.z;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs vp = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   vn = GetVertexNormalInputs(IN.normalOS);
                OUT.positionCS = vp.positionCS;
                OUT.positionWS = vp.positionWS;
                OUT.normalWS   = vn.normalWS;
                OUT.positionOS = IN.positionOS.xyz;
                OUT.normalOS   = IN.normalOS;
                return OUT;
            }

            float4 frag(Varyings IN):SV_Target
            {
                float3 Nw = normalize(IN.normalWS);

                float4 grid = TriplanarASE_Object(TEXTURE2D_ARGS(_Grid, sampler_Grid),
                                                  IN.positionOS, normalize(IN.normalOS),
                                                  _GridScale, _Falloff);

                float4 overlay = lerp(float4(1,1,1,1), grid, _OverlayAmount);
                float3 albedo  = (_BaseColor * overlay).rgb;

                InputData inputData = (InputData)0;
                inputData.positionWS      = IN.positionWS;
                inputData.normalWS        = Nw;
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(IN.positionWS);
                inputData.shadowCoord     = TransformWorldToShadowCoord(IN.positionWS);
                // inputData.fogCoord = ComputeFogFactor(IN.positionCS.z);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo     = albedo;
                surfaceData.alpha      = 1.0;
                surfaceData.metallic   = 0.0;
                surfaceData.smoothness = 0.5;
                surfaceData.occlusion  = 1.0;
                surfaceData.emission   = 0;

                return UniversalFragmentPBR(inputData, surfaceData);
            }
            ENDHLSL
        }

        // ---------- ShadowCaster (самописный, без инклюда) ----------
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ZTest  LEqual
            Cull   Back
            ColorMask 0

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex   vertSC
            #pragma fragment fragSC
            // #pragma multi_compile_instancing   // при необходимости инстансинга

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS:POSITION; };
            struct Varyings   { float4 positionCS:SV_POSITION; };

            Varyings vertSC(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs vp = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = vp.positionCS; // базовый кастинг (без biasТа)
                return OUT;
            }

            float4 fragSC(Varyings IN):SV_Target { return 0; }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}


/*ASEBEGIN
Version=15900
0;92;2040;787;798.358;273.502;1;True;False
Node;AmplifyShaderEditor.RangedFloatNode;2;-365.939,248.7368;Float;False;Property;_Falloff;Falloff;3;0;Create;True;0;0;False;0;50;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TexturePropertyNode;4;-430.7379,-43.00558;Float;True;Property;_Grid;Grid;1;0;Create;True;0;0;False;0;None;93e718fcc411432439749387d41fa07a;False;white;Auto;Texture2D;0;1;SAMPLER2D;0
Node;AmplifyShaderEditor.RangedFloatNode;3;-370.328,165.2742;Float;False;Property;_GridScale;GridScale;2;0;Create;True;0;0;False;0;5;1.36;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TriplanarNode;6;-128.2589,26.68451;Float;True;Cylindrical;Object;False;Top Texture 0;_TopTexture0;white;2;None;Mid Texture 0;_MidTexture0;white;1;None;Bot Texture 0;_BotTexture0;white;2;None;Triplanar Sampler;False;9;0;SAMPLER2D;;False;5;FLOAT;1;False;1;SAMPLER2D;;False;6;FLOAT;0;False;2;SAMPLER2D;;False;7;FLOAT;0;False;8;FLOAT3;1,1,1;False;3;FLOAT;1;False;4;FLOAT;100;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;5;24.57001,259.4728;Float;False;Property;_OverlayAmount;OverlayAmount;4;0;Create;True;0;0;False;0;1;3.52;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;16;262,-23.5;Float;False;Constant;_White;White;5;0;Create;True;0;0;False;0;1,1,1,0;0,0,0,0;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;9;265,-214.5;Float;False;Property;_BaseColor;BaseColor;0;0;Create;True;0;0;False;0;0.06228374,0.8320726,0.9411765,0;0,0,0,0;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.LerpOp;15;514,126.5;Float;False;3;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0,0,0,0;False;2;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;10;701,-52.5;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT4;0.2627451,0.7960785,0.572549,0;False;1;COLOR;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;1;921,11;Float;False;True;2;Float;ASEMaterialInspector;0;0;Standard;SyntyStudios/Prototype_Object;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Opaque;0.5;True;True;0;False;Opaque;;Geometry;All;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;True;0;0;False;-1;0;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
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
//CHKSM=9DFBB57FBFD631EA24E55184E1F6D105DDEB889F