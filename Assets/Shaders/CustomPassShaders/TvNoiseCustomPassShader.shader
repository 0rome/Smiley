Shader "Renderers/TvNoiseCustomPassShader"
{
    Properties
    {
        _Color("Base Color", Color) = (1,1,1,1)
        _ColorMap("ColorMap", 2D) = "white" {}
        
        [Header(TV Noise Settings)]
        _NoiseIntensity("Noise Intensity", Range(0.0, 1.0)) = 1.0
        _NoiseScale("Noise Scale", Float) = 1.0
        _NoiseSpeed("Noise Speed", Float) = 50.0
        
        [Header(Scanlines)]
        _ScanlineIntensity("Scanline Intensity", Range(0.0, 1.0)) = 0.2
        _ScanlineAmount("Scanline Amount", Float) = 800.0

        [Header(Glow and Emission)]
        [HDR] _EmissiveColor("Emissive Tint", Color) = (1, 1, 1, 1)
        _EmissiveIntensity("Glow Strength (Emission)", Float) = 5.0

        // Transparency
        _AlphaCutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        [HideInInspector]_BlendMode("_BlendMode", Range(0.0, 1.0)) = 0.5
    }

    HLSLINCLUDE
    #pragma target 4.5
    #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch switch2

    // enable GPU instancing support
    #pragma multi_compile_instancing
    #pragma multi_compile _ DOTS_INSTANCING_ON

    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }
        Pass
        {
            Name "FirstPass"
            Tags { "LightMode" = "FirstPass" }

            Blend Off
            ZWrite Off
            ZTest LEqual
            Cull Back

            HLSLPROGRAM

            #define _ALPHATEST_ON

            // Attributes & Varyings
            #define ATTRIBUTES_NEED_TEXCOORD0
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT

            #define VARYINGS_NEED_TEXCOORD0
            #define VARYINGS_NEED_TANGENT_TO_WORLD
            
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            
            TEXTURE2D(_ColorMap);

            CBUFFER_START(UnityPerMaterial)
            float4 _ColorMap_ST;
            float4 _Color;
            
            // TV Noise parameters
            float _NoiseIntensity;
            float _NoiseScale;
            float _NoiseSpeed;
            float _ScanlineIntensity;
            float _ScanlineAmount;

            // Emission & Glow
            float4 _EmissiveColor;
            float _EmissiveIntensity;

            float _AlphaCutoff;
            float _BlendMode;
            CBUFFER_END
            
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassRenderersV2.hlsl"

            // Генератор псевдослучайного шума
            float RandomNoise(float2 uv, float time)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233)) + time) * 43758.5453123);
            }

            void GetSurfaceAndBuiltinData(FragInputs fragInputs, float3 viewDirection, inout PositionInputs posInput, out SurfaceData surfaceData, out BuiltinData builtinData)
            {
                float2 colorMapUv = TRANSFORM_TEX(fragInputs.texCoord0.xy, _ColorMap);
                float4 texColor = SAMPLE_TEXTURE2D(_ColorMap, s_trilinear_clamp_sampler, colorMapUv) * _Color;
                
                // 1. Генерация шума
                float timeVar = _Time.y * _NoiseSpeed;
                float noise = RandomNoise(colorMapUv * _NoiseScale, timeVar);
                
                // 2. Сканлайны (полосы ЭЛТ)
                float scanline = sin(colorMapUv.y * _ScanlineAmount + _Time.y * 10.0) * 0.5 + 0.5;
                float scanlineEffect = lerp(1.0, scanline, _ScanlineIntensity);

                // 3. Смешивание базовой текстуры и шума
                float3 noiseColor = float3(noise, noise, noise);
                float3 baseResult = lerp(texColor.rgb, noiseColor, _NoiseIntensity);
                baseResult *= scanlineEffect;

                // 4. Умножение на мультипликатор свечения (HDR)
                float3 finalEmissive = baseResult * _EmissiveColor.rgb * _EmissiveIntensity;

                float opacity = texColor.a;

                #ifdef _ALPHATEST_ON
                DoAlphaTest(opacity, _AlphaCutoff);
                #endif

                // Вывод данных
                ZERO_BUILTIN_INITIALIZE(builtinData); 
                ZERO_INITIALIZE(SurfaceData, surfaceData);
                
                builtinData.opacity = opacity;
                // Записываем HDR-цвет в свечение
                builtinData.emissiveColor = finalEmissive; 
                surfaceData.color = float3(0, 0, 0);
            }
            
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassForwardUnlit.hlsl"
            
            #pragma vertex Vert
            #pragma fragment Frag

            ENDHLSL
        }
    }
}