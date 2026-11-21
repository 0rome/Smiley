Shader "FullScreen/MatrixWorldSpaceRain"
{
    Properties
    {
        [HDR] _MatrixColor("Matrix Color", Color) = (0.5, 3.0, 1.0, 1.0)
        [HDR] _HeadColor("Head Color", Color) = (2.0, 5.0, 2.0, 1.0)
        _Density("Density (Cells per meter)", Float) = 12.0
        _Speed("Speed", Float) = 4.0
        _Brightness("Overall Brightness", Float) = 1.5
    }

    HLSLINCLUDE
    #pragma vertex Vert
    #pragma target 4.5
    #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch switch2
    
    // Базовые функции Custom Pass
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassCommon.hlsl"
    
    // ДОБАВЛЕНО: Специальный инклуд для работы с буфером нормалей в HDRP
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/NormalBuffer.hlsl"

    CBUFFER_START(UnityPerMaterial)
        float4 _MatrixColor;
        float4 _HeadColor;
        float _Density;
        float _Speed;
        float _Brightness;
    CBUFFER_END

    float random(float2 p)
    {
        return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453123);
    }

    float3 GetMatrixRain(float2 wsUV)
    {
        float2 gridUV = floor(wsUV * _Density);
        float2 cellUV = frac(wsUV * _Density);

        float colId = gridUV.x;
        float colOffset = random(float2(colId, 0.0)) * 100.0;
        float colSpeed = (random(float2(colId, 1.0)) * 0.5 + 0.5) * _Speed;

        float rowId = -gridUV.y; 
        float t = _Time.y * colSpeed + colOffset;
        float headY = floor(t);
        
        float dist = headY - rowId;

        float wrapY = 25.0; 
        dist = fmod(dist, wrapY);
        if (dist < 0.0) dist += wrapY;

        float trailLength = 15.0 + random(float2(colId, 2.0)) * 25.0;
        float fade = pow(1.0 - saturate(dist / trailLength), 1.2);

        float charTime = floor(_Time.y * 15.0);
        float2 innerGrid = floor(cellUV * 3.0);
        float charNoise = random(gridUV * 11.0 + innerGrid + charTime);
        
        float charShape = step(0.35, charNoise);
        float border = step(0.05, cellUV.x) * step(0.05, cellUV.y) * step(cellUV.x, 0.95) * step(cellUV.y, 0.95);

        float3 rainColor = lerp(_MatrixColor.rgb, _HeadColor.rgb, step(dist, 1.0));

        return rainColor * fade * charShape * border * _Brightness;
    }

    float4 FullScreenPass(Varyings varyings) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(varyings);
        
        float depth = LoadCameraDepth(varyings.positionCS.xy);
        
        // Отсекаем небо (Reversed-Z depth)
        if (depth < 0.000001)
            return float4(0.0, 0.0, 0.0, 1.0);

        PositionInputs posInput = GetPositionInput(varyings.positionCS.xy, _ScreenSize.zw, depth, UNITY_MATRIX_I_VP, UNITY_MATRIX_V);
        
        // ИСПРАВЛЕНО: Правильное чтение нормалей для HDRP
        NormalData normalData;
        DecodeFromNormalBuffer(posInput.positionSS, normalData);
        float3 normalWS = normalData.normalWS;
        
        // Считаем веса для трипланарной проекции
        float3 blend = abs(normalWS);
        blend /= (blend.x + blend.y + blend.z); 

        float3 rainX = GetMatrixRain(float2(posInput.positionWS.z, posInput.positionWS.y));
        float3 rainY = GetMatrixRain(float2(posInput.positionWS.x, posInput.positionWS.z));
        float3 rainZ = GetMatrixRain(float2(posInput.positionWS.x, posInput.positionWS.y));

        float3 matrixRain = rainX * blend.x + rainY * blend.y + rainZ * blend.z;

        return float4(matrixRain, 1.0);
    }
    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }
        Pass
        {
            Name "Custom Pass 0"
            ZWrite Off
            ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off

            HLSLPROGRAM
            #pragma fragment FullScreenPass
            ENDHLSL
        }
    }
    Fallback Off
}