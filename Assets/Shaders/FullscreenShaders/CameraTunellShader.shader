Shader "FullScreen/CameraTunellShader"
{
    Properties
    {
        [Header(Tunnel Settings)]
        _TunnelRadius ("Tunnel Radius", Range(0.0, 1.0)) = 0.5
        _TunnelSoftness ("Tunnel Softness", Range(0.0, 1.0)) = 0.3
        [HDR] _TunnelColor ("Tunnel Color", Color) = (0, 0, 0, 1)

        [Header(Night Vision)]
        [Toggle] _NightVisionEnabled ("Enable Night Vision", Float) = 1.0
        [HDR] _NVColor ("Night Vision Color", Color) = (0.1, 1.0, 0.2, 1.0)
        _NVSensitivity ("Light Amplification", Range(1.0, 10.0)) = 4.0
        _NVVignette ("NV Binocular Mask", Range(0.0, 1.0)) = 0.4

        [Header(Lens Distortion and Aberration)]
        _DistortionIntensity ("Distortion Intensity", Range(0.0, 0.2)) = 0.05
        _AberrationStrength ("Chromatic Aberration", Range(0.0, 0.02)) = 0.005

        [Header(Glitch and Noise)]
        _GlitchIntensity ("Glitch Intensity", Range(0.0, 1.0)) = 0.1
        _NoiseSpeed ("Noise Speed", Range(1.0, 20.0)) = 10.0
    }

    HLSLINCLUDE

    #pragma vertex Vert
    #pragma target 4.5
    #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch switch2
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassCommon.hlsl"

    float _TunnelRadius;
    float _TunnelSoftness;
    float4 _TunnelColor;

    float _NightVisionEnabled;
    float4 _NVColor;
    float _NVSensitivity;
    float _NVVignette;

    float _DistortionIntensity;
    float _AberrationStrength;
    float _GlitchIntensity;
    float _NoiseSpeed;

    // Псевдослучайный генератор
    float Random(float2 st)
    {
        return frac(sin(dot(st.xy, float2(12.9898, 78.233))) * 43758.5453123);
    }

    float4 FullScreenPass(Varyings varyings) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(varyings);

        // Базовые UV координаты экрана
        float2 uv = varyings.positionCS.xy * _ScreenSize.zw;
        float2 centeredUV = uv - 0.5;
        centeredUV.x *= _ScreenSize.x / _ScreenSize.y;

        // --- 1. ЭФФЕКТ ГЛИТЧА И ПОМЕХ ---
        float time = _Time.y * _NoiseSpeed;
        float glitchLine = step(0.95, Random(float2(floor(uv.y * 20.0), floor(time)))) * _GlitchIntensity;
        
        float2 jitteredUV = uv;
        jitteredUV.x += (Random(float2(time, time)) - 0.5) * glitchLine * 0.2;

        // --- 2. ИСКАЖЕНИЕ ЛИНЗЫ И ХРОМАТИЧЕСКАЯ АБЕРРАЦИЯ ---
        float distSq = dot(centeredUV, centeredUV);
        float2 distortedUV = jitteredUV + centeredUV * distSq * _DistortionIntensity;

        float2 uvR = distortedUV + centeredUV * _AberrationStrength;
        float2 uvG = distortedUV;
        float2 uvB = distortedUV - centeredUV * _AberrationStrength;

        float r = CustomPassLoadCameraColor(uvR * _ScreenSize.xy, 0).r;
        float g = CustomPassLoadCameraColor(uvG * _ScreenSize.xy, 0).g;
        float b = CustomPassLoadCameraColor(uvB * _ScreenSize.xy, 0).b;
        float3 sceneColor = float3(r, g, b);

        // --- 3. НОЧНОЕ ВИДЕНИЕ (УСИЛЕНИЕ СВЕТА В ТЕМНОТЕ) ---
        if (_NightVisionEnabled > 0.5)
        {
            float lum = dot(sceneColor, float3(0.299, 0.587, 0.114));
            
            // Экспоненциальное усиление слабых источников света (осветляем темноту)
            float boostedLum = saturate(lum * _NVSensitivity);
            
            // Добавляем специфический шум ПНВ (статичный «зеленый шум»)
            float nvNoise = (Random(uv * time) - 0.5) * 0.25;
            boostedLum = saturate(boostedLum + nvNoise);

            // Окрашиваем в монохромный цвет ночного видения
            sceneColor = boostedLum * _NVColor.rgb;

            // Дополнительная маска бинокля ПНВ по краям
            float nvVignetteMask = smoothstep(_NVVignette, _NVVignette + 0.4, length(centeredUV));
            sceneColor *= (1.0 - nvVignetteMask * 0.7);
        }

        // Общий цифровой шум
        float noise = (Random(uv * time) - 0.5) * _GlitchIntensity * 0.2;
        sceneColor += noise;

        // --- 4. ТУННЕЛЬНЫЙ ЭФФЕКТ (ВИНИЕТКА) ---
        float dist = length(centeredUV);
        float vignette = smoothstep(_TunnelRadius, _TunnelRadius + _TunnelSoftness, dist);

        float3 finalColor = lerp(sceneColor, _TunnelColor.rgb, vignette * _TunnelColor.a);

        return float4(finalColor, 1.0);
    }

    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }
        Pass
        {
            Name "CameraTunnelPass" // <-- ИЗМЕНИТЕ ЭТО ИМЯ

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