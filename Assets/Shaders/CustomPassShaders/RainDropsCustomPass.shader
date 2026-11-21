Shader "Renderers/RainDropsCustomPass"
{
    Properties
    {
        _Color("Tint Color", Color) = (1, 1, 1, 1)
        
        [Header(Tiling and Speed)]
        _Tiling("Rain Scale / Tiling", Float) = 2.0
        _RainSpeed("Rain Speed", Float) = 0.25
        _Jerkiness("Jerkiness (Stick-Slip)", Range(1.0, 10.0)) = 3.5
        
        [Header(Refraction and Drops)]
        _DistortionStrength("Refraction / Distortion", Range(0.0, 0.5)) = 0.15
        _StaticDropsDensity("Static Drops Density", Range(0.0, 2.0)) = 1.0
        _TrailIntensity("Trail Drops Intensity", Range(0.0, 2.0)) = 1.2
        _SpecularIntensity("Drop Specular / Highlight", Range(0.0, 5.0)) = 2.0
        
        [Header(More Water and Streams)]
        _StreamsDensity("Water Streams Density", Range(0.0, 2.0)) = 1.0
        _WipeStrength("Wipe Trail Length", Range(0.5, 3.0)) = 1.5
        
        [Header(Glass Fog and Wiping)]
        _FogDensity("Glass Fog / Condensation", Range(0.0, 1.0)) = 0.6
        _GlassFogColor("Glass Fog Color", Color) = (0.8, 0.85, 0.9, 1.0)
        _FrostedGlass("Frosted Glass Blur", Range(0.0, 0.05)) = 0.015

        // Transparency
        _AlphaCutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        [HideInInspector]_BlendMode("_BlendMode", Range(0.0, 1.0)) = 0.5
    }

    HLSLINCLUDE
    #pragma target 4.5
    #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch switch2
    #pragma multi_compile_instancing
    #pragma multi_compile _ DOTS_INSTANCING_ON
    ENDHLSL

    SubShader
    {
        Tags { "RenderPipeline" = "HDRenderPipeline" }
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
            #define ATTRIBUTES_NEED_TEXCOORD0
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define VARYINGS_NEED_TEXCOORD0
            #define VARYINGS_NEED_TANGENT_TO_WORLD
            
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _GlassFogColor;
                float _Tiling;
                float _RainSpeed;
                float _Jerkiness;
                float _DistortionStrength;
                float _StaticDropsDensity;
                float _TrailIntensity;
                float _SpecularIntensity;
                float _StreamsDensity;
                float _WipeStrength;
                float _FogDensity;
                float _FrostedGlass;
                float _AlphaCutoff;
                float _BlendMode;
            CBUFFER_END

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassRenderersV2.hlsl"

            // ==========================================
            // МАТЕМАТИКА И ХЭШИ
            // ==========================================

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 345.45));
                p += dot(p, p + 34.34);
                return frac(p.x * p.y);
            }

            float2 Hash22(float2 p)
            {
                float n = Hash21(p);
                return float2(n, Hash21(p + n * 17.13));
            }

            float GetJerkyTime(float rawTime, float jerkiness)
            {
                float timeStep = floor(rawTime);
                float timeFrac = frac(rawTime);
                return timeStep + pow(timeFrac, jerkiness);
            }

            // ==========================================
            // ГЕНЕРАЦИЯ КАПЕЛЬ И ПОТОКОВ
            // ==========================================

            // Статичные капли на фоне
            float3 StaticDrops(float2 uv, float seed)
            {
                float2 id = floor(uv);
                uv = frac(uv) - 0.5;
                float2 rand = Hash22(id + seed);
                float2 p = (rand - 0.5) * 0.65;
                
                float dropSize = 0.14 * (0.4 + 0.6 * rand.x);
                float d = length(uv - p);
                
                float mask = smoothstep(dropSize, 0.0, d);
                float2 normalXY = (uv - p) / dropSize * mask; 
                
                return float3(normalXY, mask);
            }

            // Динамичные капли со следом (могут быть обычными и крупными "дворниками")
            float4 DynamicDrops(float2 uv, float time, float speedMult, float seed, float2 aspect, float sizeMult)
            {
                float2 grid = uv * aspect;
                float2 id = floor(grid);
                
                float colRand = Hash21(id.x * 13.57 + seed);
                float speed = _RainSpeed * speedMult * (0.6 + colRand * 0.8);
                float jerkyTime = GetJerkyTime(time * speed + colRand * 100.0, _Jerkiness);
                
                grid.y += jerkyTime;
                
                float2 cellId = floor(grid);
                float2 cellUV = frac(grid) - 0.5;
                float cellRand = Hash21(cellId + seed * 3.1415);
                
                // 1. Движущаяся капля
                float xOffset = (cellRand - 0.5) * 0.5;
                float yOffset = (frac(cellRand * 23.4) - 0.5) * 0.3;
                float2 dropCenter = float2(xOffset, yOffset);
                
                float2 dropVec = (cellUV - dropCenter) / aspect;
                float dropDist = length(dropVec);
                
                float mainDropSize = 0.085 * sizeMult * (0.6 + 0.8 * cellRand);
                float mainDropMask = smoothstep(mainDropSize, 0.0, dropDist);
                float2 mainNormal = (dropVec / mainDropSize) * mainDropMask;
                
                // 2. Мокрый след (мелкие капельки позади)
                float trailY = cellUV.y - dropCenter.y; // trailY > 0 означает зону позади капли
                float inTrail = smoothstep(0.0, 0.4, trailY) * smoothstep(0.35, 0.0, abs(cellUV.x - xOffset));
                
                float2 trailCellUV = float2(cellUV.x - xOffset, frac(cellUV.y * 8.0) - 0.5);
                float trailDropDist = length(trailCellUV / aspect);
                float trailDropSize = 0.028 * sizeMult * (1.0 - saturate(trailY / 0.4));
                float trailDropMask = smoothstep(trailDropSize, 0.0, trailDropDist) * inTrail * _TrailIntensity;
                float2 trailNormal = (trailCellUV / aspect / max(0.01, trailDropSize)) * trailDropMask;
                
                // 3. Зона очистки тумана (Wipe Mask)
                // Растягиваем зону стирания в длину (_WipeStrength)
                float wipeMask = smoothstep(0.8 * _WipeStrength, -0.1, trailY) * smoothstep(0.35 * sizeMult, 0.0, abs(cellUV.x - xOffset));
                wipeMask = saturate(wipeMask + mainDropMask);
                
                float totalDropMask = saturate(mainDropMask + trailDropMask);
                float2 totalNormal = mainNormal + trailNormal;
                
                return float4(totalNormal, totalDropMask, wipeMask);
            }

            // Непрерывные ручейки (эффект сильного дождя / потоков воды)
            float3 WaterStreams(float2 uv, float time)
            {
                uv *= float2(1.5, 1.0);
                uv.y -= time * _RainSpeed * 1.5; // Скорость стекания
                
                float id = floor(uv.x);
                float rand = Hash21(id * 15.3);
                
                // Рандомизация смещения и волны по колонкам
                uv.y -= time * rand * 0.5;
                uv.x = frac(uv.x) - 0.5;
                
                // Извилистость потока
                float wave = sin(uv.y * 4.0 + rand * 10.0) * 0.15;
                uv.x += wave;
                
                float dist = abs(uv.x);
                
                // Рваность ручейка по вертикали, чтобы не был сплошной линией
                float breakUp = smoothstep(0.1, 0.8, sin(uv.y * 10.0 + time * 3.0) * 0.5 + 0.5);
                
                // Толщина ручейка зависит от рандома, чтобы некоторые колонки были сухими
                float streamMask = smoothstep(0.04, 0.0, dist) * smoothstep(0.4, 0.9, rand) * breakUp * _StreamsDensity;
                
                float2 normal = float2(uv.x * 25.0, -0.2) * streamMask; // Нормаль смотрит чуть вниз
                return float3(normal, streamMask);
            }

            // ==========================================
            // MAIN LOGIC
            // ==========================================

            void GetSurfaceAndBuiltinData(FragInputs fragInputs, float3 viewDirection, inout PositionInputs posInput, out SurfaceData surfaceData, out BuiltinData builtinData)
{
    float screenAspect = _ScreenSize.x / _ScreenSize.y;
    float2 uv = fragInputs.texCoord0.xy;

    // Коррекция инвертированных UV (отзеркаленных граней)
    float bitangentY = fragInputs.tangentToWorld[1].y;
    if (bitangentY < 0.0)
    {
        uv.y = -uv.y;
    }

    uv.x *= screenAspect;
    uv *= _Tiling;
    float time = _Time.y;

                // --- 1. Слои дождя ---
                // Обычные капли
                float4 rain1 = DynamicDrops(uv, time, 1.0, 0.0, float2(2.5, 1.0), 1.0);
                float4 rain2 = DynamicDrops(uv * 1.5 + float2(3.2, 1.1), time, 1.3, 37.2, float2(2.5, 1.0), 0.7);
                
                // НОВЫЙ СЛОЙ: Крупные редкие капли, оставляющие длинный идеально чистый след (Wipers)
                // Мы используем aspect=(1.5, 0.3) - сетка сильно растянута по вертикали, поэтому след получается очень длинным
                float4 rainWipers = DynamicDrops(uv * 0.8 + float2(1.1, 4.3), time, 1.6, 11.4, float2(1.5, 0.3), 1.4);
                
                // НОВЫЙ СЛОЙ: Текущие ручейки
                float3 streams = WaterStreams(uv * 2.0, time);
                
                // Фон
                float3 staticDrops = StaticDrops(uv * 2.6, 91.5) * _StaticDropsDensity;

                // --- 2. Собираем маски ---
                float2 totalNormalXY = rain1.xy + rain2.xy * 0.6 + rainWipers.xy * 1.2 + streams.xy + staticDrops.xy * 0.5;
                float dropMask = saturate(rain1.z + rain2.z + rainWipers.z + streams.z + staticDrops.z);
                
                // Маска стирания: wipeMask от rainWipers берется с сильным множителем, чтобы гарантированно протереть окно
                float wipeMask = saturate(rain1.w + rain2.w + rainWipers.w * 2.5 + streams.z * 1.5);

                // --- 3. Физически корректная 3D нормаль ---
                // Защита от черных артефактов при наложении многих слоев (длина вектора не должна превышать 1)
                float normalLen = length(totalNormalXY);
                if(normalLen > 0.99) totalNormalXY = (totalNormalXY / normalLen) * 0.99;
                
                float z = sqrt(max(0.0, 1.0 - dot(totalNormalXY, totalNormalXY)));
                float3 dropNormal = normalize(float3(totalNormalXY, z));

                // --- 4. Оптика и преломление ---
                float2 screenUV = posInput.positionSS / _ScreenSize.xy;
                float2 distortedUV = screenUV - (dropNormal.xy * _DistortionStrength * dropMask);

                // --- 5. Конденсат (Матовое стекло) ---
                // В протертых местах (wipeMask -> 1) туман полностью исчезает
                float fogAmount = _FogDensity * saturate(1.0 - wipeMask);
                float2 frostedNoise = Hash22(screenUV * 500.0) - 0.5;
                float2 fogUV = screenUV + frostedNoise * _FrostedGlass * fogAmount;
                
                float2 finalUV = lerp(fogUV, distortedUV, dropMask);
                float3 sceneColor = SampleCameraColor(finalUV);

                float3 finalColor = lerp(sceneColor, sceneColor * _GlassFogColor.rgb, fogAmount * 0.8);

                // --- 6. Блик (Blinn-Phong + Fresnel) ---
                float3 lightDir = normalize(float3(-0.5, 1.0, 0.5));
                float3 halfVector = normalize(lightDir + viewDirection);
                
                float nDotH = max(0.0, dot(dropNormal, halfVector));
                float specular = pow(nDotH, 128.0) * _SpecularIntensity * dropMask; 
                float fresnel = pow(1.0 - max(0.0, dot(dropNormal, viewDirection)), 3.0) * 0.5 * dropMask;

                finalColor = finalColor * _Color.rgb + specular + fresnel;

                float opacity = _Color.a;
                #ifdef _ALPHATEST_ON
                    DoAlphaTest(opacity, _AlphaCutoff);
                #endif

                ZERO_BUILTIN_INITIALIZE(builtinData);
                ZERO_INITIALIZE(SurfaceData, surfaceData);
                
                builtinData.opacity = opacity;
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