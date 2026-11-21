Shader "FullScreen/MentalDisorderShader"
{
    Properties
    {
        [Header(Tunnel Settings)]
        _TunnelRadius ("Tunnel Radius", Range(0.0, 1.0)) = 0.4
        _TunnelSoftness ("Tunnel Softness", Range(0.0, 1.0)) = 0.3
        [HDR] _TunnelColor ("Tunnel Color", Color) = (0.05, 0, 0.1, 1)

        [Header(Disorder Wave Distortion)]
        _WobbleIntensity ("Wobble Intensity", Range(0.0, 0.05)) = 0.015
        _WobbleSpeed ("Wobble Speed", Range(0.0, 10.0)) = 3.0
        _WobbleFrequency ("Wobble Frequency", Range(1.0, 50.0)) = 20.0

        [Header(Panic Aberration)]
        _AberrationStrength ("Panic Aberration", Range(0.0, 0.05)) = 0.015

        [Header(Rainbow Madness Tint)]
        _RainbowSpeed ("Rainbow Shift Speed", Range(0.0, 5.0)) = 1.0
        _MadnessIntensity ("Madness Pulse Intensity", Range(0.0, 1.0)) = 0.5
    }

    HLSLINCLUDE

    #pragma vertex Vert
    #pragma target 4.5
    #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch switch2
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassCommon.hlsl"

    float _TunnelRadius;
    float _TunnelSoftness;
    float4 _TunnelColor;

    float _WobbleIntensity;
    float _WobbleSpeed;
    float _WobbleFrequency;

    float _AberrationStrength;

    float _RainbowSpeed;
    float _MadnessIntensity;

    // Функция перевода из HSL в RGB для создания радужного эффекта
    float3 HslToRgb(float3 hsl)
    {
        float3 rgb = saturate(abs(frac(hsl.x + float3(0.0, 0.6666, 0.3333)) * 6.0 - 3.0) - 1.0);
        return hsl.z + hsl.y * (rgb - 0.5) * (1.0 - abs(2.0 * hsl.z - 1.0));
    }

    float4 FullScreenPass(Varyings varyings) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(varyings);

        float2 uv = varyings.positionCS.xy * _ScreenSize.zw;
        float2 centeredUV = uv - 0.5;
        centeredUV.x *= _ScreenSize.x / _ScreenSize.y;

        float time = _Time.y * _WobbleSpeed;

        // --- 1. ВОЛНОВОЕ ИСКАЖЕНИЕ ---
        float wave = sin(uv.y * _WobbleFrequency + time) * cos(uv.x * _WobbleFrequency + time);
        float2 wobbledUV = uv + wave * _WobbleIntensity;

        float distFromCenter = length(centeredUV);
        wobbledUV += centeredUV * sin(distFromCenter * 10.0 - time * 2.0) * (_WobbleIntensity * 0.5);

        // --- 2. РАССИНХРОНИЗАЦИЯ ЦВЕТОВ (АБЕРРАЦИЯ) ---
        float2 offsetUV = wobbledUV - 0.5;
        float2 uvR = wobbledUV + offsetUV * _AberrationStrength;
        float2 uvG = wobbledUV;
        float2 uvB = wobbledUV - offsetUV * _AberrationStrength;

        float r = CustomPassLoadCameraColor(uvR * _ScreenSize.xy, 0).r;
        float g = CustomPassLoadCameraColor(uvG * _ScreenSize.xy, 0).g;
        float b = CustomPassLoadCameraColor(uvB * _ScreenSize.xy, 0).b;
        float3 sceneColor = float3(r, g, b);

        // --- 3. РАДУЖНОЕ ПЕРЕЛИВАНИЕ ЦВЕТОВ ---
        // Считаем среднюю яркость пикселя
        float lum = dot(sceneColor, float3(0.299, 0.587, 0.114));
        
        // Динамический сдвиг оттенка по кругу радуги на основе времени
        float hueShift = frac(_Time.y * _RainbowSpeed);
        
        // Превращаем яркость в радужный цвет (насыщенность 0.8, яркость равна luminance)
        float3 rainbowColor = HslToRgb(float3(hueShift, 0.8, lum));
        
        // Плавная пульсация смешивания с радугой
        float pulse = (sin(_Time.y * 3.0) * 0.5 + 0.5) * _MadnessIntensity;
        sceneColor = lerp(sceneColor, rainbowColor, pulse);

        // --- 4. ТУННЕЛЬНЫЙ ЭФФЕКТ ---
        float vignette = smoothstep(_TunnelRadius, _TunnelRadius + _TunnelSoftness, distFromCenter);
        float3 finalColor = lerp(sceneColor, _TunnelColor.rgb, vignette * _TunnelColor.a);

        return float4(finalColor, 1.0);
    }

    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }
        Pass
        {
            Name "MentalDisorderPass" // <-- ИЗМЕНИТЕ ЭТО ИМЯ

            ZWrite Off
            ZTest Always
            Blend Off
            Cull Off

            HLSLPROGRAM
                #pragma fragment FullScreenPass
            ENDHLSL
        }
    }
    Fallback Off
}