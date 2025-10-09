Shader "SyntyStudios/Prototype_Glass_Object_URP"
{
    Properties
    {
        _BaseColor     ("Base Color", Color) = (0.06228374, 0.8320726, 0.9411765, 1)
        _Grid          ("Grid", 2D) = "white" {}
        _GridScale     ("Grid Scale", Float) = 5
        _Falloff       ("Falloff", Float) = 50
        _Opacity       ("Opacity", Color) = (0.5661765, 0.5661765, 0.5661765, 0)
        _OverlayAmount ("Overlay Amount", Range(0, 1)) = 1
        _Smoothness    ("Smoothness", Range(0, 1)) = 1
        _Specular      ("Specular", Range(0, 1)) = 0.1
    }

    SubShader
    {
        Tags
        {
            "RenderType"   = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue"        = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            // стекло = альфа-бленд
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex   vert
            #pragma fragment frag

            // как в твоЄм рабочем шейдере
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _SHADOWS_SOFT
            // (по желанию) #pragma multi_compile_fog
            // (по желанию) #pragma multi_compile_instancing

            // —пекул€рный workflow дл€ PBR:
            #define _SPECULAR_SETUP 1

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
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;

                // чтобы сделать трипланар в Object Space Ч пробросим и OS данные
                float3 positionOS : TEXCOORD2;
                float3 normalOS   : TEXCOORD3;
            };

            // Triplanar (как в ASE): учЄт знака нормали + Ђneg Yї ветка; здесь Ч в объектных координатах
            float4 TriplanarASE_Object(TEXTURE2D_PARAM(tex, texSampler),
                                       float3 objPos, float3 objNormal,
                                       float tiling, float falloff)
            {
                float3 n  = objNormal;
                float3 an = pow(abs(n), falloff);
                an /= max(1e-5, an.x + an.y + an.z);
                float3 s = sign(n);

                float2 uvX = objPos.zy * float2( s.x, 1.0) * tiling;
                float2 uvY = objPos.xz * float2( s.y, 1.0) * tiling;
                float2 uvYN= objPos.xz * float2( s.y, 1.0) * tiling; // отрицательна€ Y-ветка (как в исходнике)
                float2 uvZ = objPos.xy * float2(-s.z, 1.0) * tiling;

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

            float4 frag(Varyings IN) : SV_Target
            {
                // нормаль в мире дл€ освещени€
                float3 N_world = normalize(IN.normalWS);

                // трипланар ¬ ќЅЏ≈ “Ќџ’ координатах (как в исходном ASE графе)
                float4 grid = TriplanarASE_Object(TEXTURE2D_ARGS(_Grid, sampler_Grid),
                                                  IN.positionOS, normalize(IN.normalOS),
                                                  _GridScale, _Falloff);

                // ÷вет: screen(1 - grid, 0) => (1 - grid); затем + _BaseColor  (как в ASE узлах)
                float3 colorRGB = saturate(1.0 - grid.rgb) + _BaseColor.rgb;

                // јльфа: 1 - lerp(_Opacity, grid, _OverlayAmount).r
                float alpha = 1.0 - lerp(_Opacity.r, grid.r, _OverlayAmount);

                // PBR через UniversalFragmentPBR (specular workflow)
                InputData inputData = (InputData)0;
                inputData.positionWS      = IN.positionWS;
                inputData.normalWS        = N_world;
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(IN.positionWS);
                inputData.shadowCoord     = TransformWorldToShadowCoord(IN.positionWS);
                // (если включишь туман:) inputData.fogCoord = ComputeFogFactor(IN.positionCS.z);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo     = colorRGB;
                surfaceData.alpha      = alpha;
                surfaceData.specular   = _Specular.xxx; // т.к. _SPECULAR_SETUP
                surfaceData.metallic   = 0.0;           // игнорируетс€ при _SPECULAR_SETUP, но оставим
                surfaceData.smoothness = _Smoothness;
                surfaceData.occlusion  = 1.0;
                surfaceData.emission   = 0;

                return UniversalFragmentPBR(inputData, surfaceData);
            }
            ENDHLSL
        }

        // ѕрозрачные материалы обычно тени не отбрасывают; если нужно Ч можно добавить dither ShadowCaster отдельно.
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}

/*ASEBEGIN
Version=15900
2567;29;2510;1385;1057.618;574.5319;1.005;True;False
Node;AmplifyShaderEditor.TexturePropertyNode;4;-444.7379,-57.00558;Float;True;Property;_Grid;Grid;1;0;Create;True;0;0;False;0;None;93e718fcc411432439749387d41fa07a;False;white;Auto;Texture2D;0;1;SAMPLER2D;0
Node;AmplifyShaderEditor.RangedFloatNode;3;-370.328,165.2742;Float;False;Property;_GridScale;GridScale;2;0;Create;True;0;0;False;0;5;1.36;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;2;-365.939,248.7368;Float;False;Property;_Falloff;Falloff;3;0;Create;True;0;0;False;0;50;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TriplanarNode;6;-91.2589,129.6845;Float;True;Cylindrical;Object;False;Top Texture 0;_TopTexture0;white;2;None;Mid Texture 0;_MidTexture0;white;1;None;Bot Texture 0;_BotTexture0;white;2;None;Triplanar Sampler;False;9;0;SAMPLER2D;;False;5;FLOAT;1;False;1;SAMPLER2D;;False;6;FLOAT;0;False;2;SAMPLER2D;;False;7;FLOAT;0;False;8;FLOAT3;1,1,1;False;3;FLOAT;1;False;4;FLOAT;100;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.OneMinusNode;22;472.9952,426.4481;Float;False;1;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RangedFloatNode;5;15.27,378.4728;Float;False;Property;_OverlayAmount;OverlayAmount;5;0;Create;True;0;0;False;0;1;3.52;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;16;267.61,-43.59;Float;False;Property;_Opacity;Opacity;4;0;Create;True;0;0;False;0;0.5661765,0.5661765,0.5661765,0;0.5661765,0.5661765,0.5661765,0;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.LerpOp;15;557.0811,116.89;Float;False;3;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0,0,0,0;False;2;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.BlendOpsNode;24;718.2164,420.4175;Float;False;Screen;True;2;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.ColorNode;9;317.2801,621.8003;Float;False;Property;_BaseColor;BaseColor;0;0;Create;True;0;0;False;0;0.06228374,0.8320726,0.9411765,0;0,0,0,0;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;25;935.2972,552.073;Float;False;2;2;0;FLOAT4;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RangedFloatNode;20;690.642,-246.502;Float;False;Property;_Specular;Specular;7;0;Create;True;0;0;False;0;0.1;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;19;800.3583,148.8228;Float;False;1;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RangedFloatNode;21;694.642,-161.502;Float;False;Property;_Smoothness;Smoothness;6;0;Create;True;0;0;False;0;1;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;18;1096,0;Float;False;True;2;Float;ASEMaterialInspector;0;0;StandardSpecular;SyntyStudios/Prototype_Glass_Object;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Transparent;0.5;True;True;0;False;Transparent;;Transparent;All;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;True;2;5;False;-1;10;False;-1;2;5;False;-1;10;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;6;0;4;0
WireConnection;6;1;4;0
WireConnection;6;2;4;0
WireConnection;6;3;3;0
WireConnection;6;4;2;0
WireConnection;22;0;6;0
WireConnection;15;0;16;0
WireConnection;15;1;6;0
WireConnection;15;2;5;0
WireConnection;24;0;22;0
WireConnection;25;0;24;0
WireConnection;25;1;9;0
WireConnection;19;0;15;0
WireConnection;18;0;25;0
WireConnection;18;3;20;0
WireConnection;18;4;21;0
WireConnection;18;9;19;0
ASEEND*/
//CHKSM=3949BCB5B391F1A1441DCFF0FCA10CD9A960D1CC