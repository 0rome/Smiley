Shader "Renderers/OutlineCustomPass"
{
    Properties
    {
        [HDR] _Color("Outline Color (Emission)", Color) = (1,1,1,1)
        _ColorMap("ColorMap", 2D) = "white" {}
        
        _OutlineThickness("Outline Thickness", Range(0.0, 0.1)) = 0.01
        
        // 1. ДОБАВЛЕНО: Свойство для контроля смещения (оффсета) по осям X, Y, Z
        _Offset("Outline Offset", Vector) = (0, 0, 0, 0)

        // Transparency
        _AlphaCutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5        
        [HideInInspector]_BlendMode("_BlendMode", Range(0.0, 1.0)) = 0.5
    }

    HLSLINCLUDE        
    #pragma target 4.5        
    #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch switch2

    //enable GPU instancing support        
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

            Cull Front

            HLSLPROGRAM            
            #define _ALPHATEST_ON            
            #define _ENABLE_FOG_ON_TRANSPARENT
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
            float _OutlineThickness; 
            float4 _Offset; // 2. ДОБАВЛЕНО: Объявляем переменную оффсета
            float _AlphaCutoff;
            float _BlendMode;
            CBUFFER_END            
            
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassRenderersV2.hlsl"

                         #define HAVE_MESH_MODIFICATION
            AttributesMesh ApplyMeshModification(AttributesMesh input, float3 timeParameters)
            {
                // 1. Преобразуем нормаль в мировое пространство и строго нормализуем её (длина = 1)
                // TransformObjectToWorldDir корректно обрабатывает масштаб объекта
                float3 normalWS = normalize(TransformObjectToWorldDir(input.normalOS));
                
                // 2. Преобразуем текущую позицию вершины в мировое пространство
                float3 posWS = TransformObjectToWorld(input.positionOS);
                
                // 3. Смещаем позицию в мировых координатах.
                // ТЕПЕРЬ: _OutlineThickness = 0.2 означает ровно 0.2 метра в мире для ЛЮБОГО объекта!
                posWS += normalWS * _OutlineThickness;
                
                // 4. Возвращаем позицию обратно в локальное пространство объекта 
                // (это требование для корректной работы HDRP Custom Pass)
                input.positionOS = TransformWorldToObject(posWS);
                
                // 5. Применяем дополнительное смещение (оффсет) в локальных координатах, как у вас было
                input.positionOS += _Offset.xyz;
                
                return input;
            }

            void GetSurfaceAndBuiltinData(FragInputs fragInputs, float3 viewDirection, inout PositionInputs posInput, out SurfaceData surfaceData, out BuiltinData builtinData)
            {
                float2 colorMapUv = TRANSFORM_TEX(fragInputs.texCoord0.xy, _ColorMap);
                float4 result = SAMPLE_TEXTURE2D(_ColorMap, s_trilinear_clamp_sampler, colorMapUv) * _Color;
                float opacity = result.a;
                float3 color = result.rgb;                
                
                #ifdef _ALPHATEST_ON
                DoAlphaTest(opacity, _AlphaCutoff);                
                #endif

                ZERO_BUILTIN_INITIALIZE(builtinData); 
                ZERO_INITIALIZE(SurfaceData, surfaceData);
                
                builtinData.opacity = opacity;
                
                builtinData.emissiveColor = color;
                surfaceData.color = float3(0, 0, 0);
            }            
            
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassForwardUnlit.hlsl"            
            
            #pragma vertex Vert                        
            #pragma fragment Frag

            ENDHLSL
        }
    }
}