Shader "Renderers/RealisticGlassBricks"
{
    Properties
    {
        [Header(Glass Properties)]
        _GlassColor("Glass Tint", Color) = (0.85, 0.95, 1.0, 1.0)
        _Distortion("Refraction Strength", Range(0.0, 0.2)) = 0.05
        _Blur("Blur Amount (Mip Level)", Range(0.0, 8.0)) = 3.5
        _Opacity("Overall Opacity", Range(0.0, 1.0)) = 1.0
        
        [Header(Metal Framework Properties)]
        _Tiling("Grid Tiling (X, Y)", Vector) = (10, 10, 0, 0)
        _MetalColor("Metal Color", Color) = (0.8, 0.85, 0.9, 1.0)
        _MetalThickness("Metal Thickness", Range(0.01, 0.2)) = 0.05
        _MetalSmoothness("Metal Smoothness", Range(0.0, 1.0)) = 0.85
        _MetalBrightness("Metal Brightness Boost", Range(0.5, 2.0)) = 1.2
    }

    HLSLINCLUDE
    #pragma target 4.5
    #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch switch2
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
            #define ATTRIBUTES_NEED_TEXCOORD0
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define VARYINGS_NEED_TEXCOORD0
            #define VARYINGS_NEED_TANGENT_TO_WORLD
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            
            CBUFFER_START(UnityPerMaterial)
                float4 _GlassColor;
                float4 _MetalColor;
                float4 _Tiling;
                float _Distortion;
                float _Blur;
                float _Opacity;
                float _MetalThickness;
                float _MetalSmoothness;
                float _MetalBrightness;
            CBUFFER_END

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassRenderersV2.hlsl"

            void GetSurfaceAndBuiltinData(FragInputs fragInputs, float3 viewDirection, inout PositionInputs posInput, out SurfaceData surfaceData, out BuiltinData builtinData)
            {
                // 1. Сетка
                float2 tileUv = fragInputs.texCoord0.xy * _Tiling.xy;
                float2 gridUv = frac(tileUv); 
                float2 centerOffset = gridUv - 0.5;

                // Размеры металла
                float halfThickness = _MetalThickness * 0.5;
                float brickEdge = 0.5 - halfThickness;

                float absX = abs(centerOffset.x);
                float absY = abs(centerOffset.y);
                float maxDist = max(absX, absY);

                // --- СТЕКЛО (Преломление) ---
                float bulge = length(centerOffset) * 2.0;
                float2 refractionOffset = centerOffset * pow(bulge, 2.0) * _Distortion;

                // --- МЕТАЛЛ (Маска и 3D-Нормали) ---
                // Четкая маска (без размытого сглаживания)
                float metalMask = step(brickEdge, maxDist);

                // Расстояние от края кирпича до центра балки
                float distX = max(0.0, absX - brickEdge);
                float distY = max(0.0, absY - brickEdge);
                
                // Нормализуем координаты внутри металла
                float localX = saturate(distX / halfThickness);
                float localY = saturate(distY / halfThickness);

                // Цилиндрическая 3D нормаль для фаски
                float3 metalNormal = float3(0, 0, 1);
                
                if (distX > distY)
                {
                    float angle = (1.0 - localX) * 1.57079;
                    metalNormal.x = sign(centerOffset.x) * sin(angle);
                    metalNormal.z = cos(angle);
                }
                else
                {
                    float angle = (1.0 - localY) * 1.57079;
                    metalNormal.y = sign(centerOffset.y) * sin(angle);
                    metalNormal.z = cos(angle);
                }

                // --- ШЕЙДИНГ МЕТАЛЛА (Fake PBR) ---
                float3 V = normalize(viewDirection);
                float3 R = reflect(-V, metalNormal);
                
                // Имитация отражения окружения
                float metalMip = (1.0 - _MetalSmoothness) * 6.0;
                float2 metalFetchUV = (posInput.positionNDC.xy + R.xy * 0.15) * _RTHandleScale.xy;
                float3 metalEnv = SAMPLE_TEXTURE2D_X_LOD(_ColorPyramidTexture, s_trilinear_clamp_sampler, metalFetchUV, metalMip).rgb;
                
                // Блик
                float3 lightDir = normalize(float3(0.5, 0.7, 1.0));
                float specular = pow(max(0.0, dot(R, lightDir)), 32.0 * _MetalSmoothness + 1.0);
                
                // Тень внутри швов (AO)
                float ao = saturate((localX + localY) * 0.8 + 0.2);
                ao = lerp(0.6, 1.0, ao);

                float3 metalFinal = (metalEnv * _MetalColor.rgb * _MetalBrightness * 0.9) 
                                  + (specular * _MetalColor.rgb * _MetalSmoothness * 2.5 * _MetalBrightness);
                metalFinal *= ao;


                // --- ШЕЙДИНГ СТЕКЛА ---
                float2 screenUV = posInput.positionNDC.xy + refractionOffset * (1.0 - metalMask);
                float2 glassFetchUV = screenUV * _RTHandleScale.xy;
                float3 glassEnv = SAMPLE_TEXTURE2D_X_LOD(_ColorPyramidTexture, s_trilinear_clamp_sampler, glassFetchUV, _Blur).rgb;
                
                float glassEdgeShadow = smoothstep(0.0, 0.5, bulge) * 0.3;
                float3 glassFinal = (glassEnv * _GlassColor.rgb) - glassEdgeShadow;
                
                // --- СМЕШИВАНИЕ СТЕКЛА И МЕТАЛЛА ---
                float3 fullBrickEffect = lerp(glassFinal, metalFinal, metalMask);

                // --- РЕГУЛИРОВКА ПРОЗРАЧНОСТИ ---
                // Считываем идеальный чистый фон прямо за объектом (без искажений и без Mip-уровней)
                float2 cleanScreenUV = posInput.positionNDC.xy * _RTHandleScale.xy;
                float3 cleanBackground = SAMPLE_TEXTURE2D_X_LOD(_ColorPyramidTexture, s_trilinear_clamp_sampler, cleanScreenUV, 0).rgb;

                // Плавно смешиваем чистый фон с эффектом шейдера по параметру _Opacity
                float3 finalColor = lerp(cleanBackground, fullBrickEffect, _Opacity);

                ZERO_BUILTIN_INITIALIZE(builtinData);
                ZERO_INITIALIZE(SurfaceData, surfaceData);
                
                builtinData.opacity = 1.0;
                builtinData.emissiveColor = float3(0, 0, 0);
                surfaceData.color = finalColor;
            }

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassForwardUnlit.hlsl"
            #pragma vertex Vert
            #pragma fragment Frag

            ENDHLSL
        }
    }
}