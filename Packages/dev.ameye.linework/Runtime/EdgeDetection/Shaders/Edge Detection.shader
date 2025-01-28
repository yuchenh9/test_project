﻿Shader "Hidden/Outlines/Edge Detection/Outline"
{
    Properties
    {
        // Discontinuity sections.
        [Toggle(SECTIONS_MASK)] _SectionsMask ("Sections Mask", Float) = 0

        // Discontinuity depth.
        _DepthSensitivity ("Depth Sensitivity", Range(0, 50)) = 0
        _DepthDistanceModulation ("Depth Non-Linearity Factor", Range(0, 3)) = 1
        _GrazingAngleMaskPower ("Grazing Angle Mask Power", Range(0, 1)) = 1
        _GrazingAngleMaskHardness("Grazing Angle Mask Hardness", Range(0,1)) = 1
        [Toggle(DEPTH_MASK)] _DepthMask ("Depth Mask", Float) = 0

        // Discontinuity normals.
        _NormalSensitivity ("Normals Sensitivity", Range(0, 50)) = 0
        [Toggle(NORMALS_MASK)] _NormalsMask ("Normals Mask", Float) = 0

        // Discontinuity luminance.
        _LuminanceSensitivity ("Luminance Sensitivity", Range(0, 50)) = 0
        [Toggle(LUMINANCE_MASK)] _LuminanceMask ("Luminance Mask", Float) = 0

        // Outline sampling.
        [KeywordEnum(Cross, Sobel)] _Operator("Edge Detection Operator", Float) = 0
        _OutlineThickness ("Outline Thickness", Float) = 1
        [Toggle(SCALE_WITH_RESOLUTION)] _ResolutionDependent ("Resolution Dependent", Float) = 0
        _ReferenceResolution ("Reference Resolution", Float) = 1080

        // Outline colors.
        _OutlineColor ("Outline Color", Color) = (0, 0, 0, 1)
        [Toggle(OVERRIDE_SHADOW)] _OverrideShadow ("Override Outline Color In Shadow", Float) = 0
        _OutlineColorShadow ("Outline Color Shadow", Color) = (1, 1, 1, 1)
        _BackgroundColor ("Background Color", Color) = (0, 0, 0, 0)
        _FillColor ("Fill Color", Color) = (0, 0, 0, 1)
        [Toggle(FADE_IN_DISTANCE)] _FadeInDistance ("Fade Outline In Distance", Float) = 0
        _FadeStart ("Fade Start", Float) = 100
        _FadeDistance ("Fade Distance", Float) = 10
        _FadeColor ("Fade Color", Color) = (0, 0, 0, 0)

        _SrcBlend ("_SrcBlend", Int) = 0
        _DstBlend ("_DstBlend", Int) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType"="Opaque"
        }

        ZWrite Off
        Cull Off

        HLSLINCLUDE
        #pragma multi_compile_local _ DEPTH
        #pragma multi_compile_local _ NORMALS
        #pragma multi_compile_local _ LUMINANCE
        #pragma multi_compile_local _ SECTIONS

        #pragma multi_compile_local _ OVERRIDE_SHADOW
        #pragma multi_compile_local _ SCALE_WITH_RESOLUTION
        #pragma multi_compile_local _ FADE_IN_DISTANCE
        #pragma multi_compile_local _ SECTIONS_MASK
        #pragma multi_compile_local _ DEPTH_MASK
        #pragma multi_compile_local _ NORMALS_MASK
        #pragma multi_compile_local _ LUMINANCE_MASK
        #pragma multi_compile_local OPERATOR_CROSS OPERATOR_SOBEL

        #pragma shader_feature_local _ DEBUG_DEPTH DEBUG_NORMALS DEBUG_LUMINANCE DEBUG_SECTIONS
        #pragma shader_feature_local _ DEBUG_SECTIONS_RAW_VALUES
        ENDHLSL

        Pass // 0: EDGE DETECTION OUTLINE
        {
            Name "EDGE DETECTION OUTLINE"

            Blend [_SrcBlend] [_DstBlend]

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            #if defined(DEPTH) || defined(OVERRIDE_SHADOW) || defined(FADE_IN_DISTANCE) || defined(DEBUG_DEPTH)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #endif

            #if defined(NORMALS) || defined(DEPTH) || defined(DEBUG_NORMALS)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
            #endif

            #if defined(LUMINANCE) || defined(DEBUG_LUMINANCE)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            #endif

            #include "Packages/dev.ameye.linework/Runtime/EdgeDetection/Shaders/DeclareSectioningTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            float4 _BackgroundColor, _OutlineColor, _FillColor, _OutlineColorShadow, _FadeColor;
            float _OverrideOutlineColorShadow;
            float _OutlineThickness;
            float _ReferenceResolution;
            float _FadeStart, _FadeDistance;
            float _DepthSensitivity, _DepthDistanceModulation, _GrazingAngleMaskPower, _GrazingAngleMaskHardness;
            float _NormalSensitivity;
            float _LuminanceSensitivity;

            #pragma vertex Vert
            #pragma fragment frag

            float RobertsCross(float3 samples[4])
            {
                const float3 difference_1 = samples[1] - samples[2];
                const float3 difference_2 = samples[0] - samples[3];
                return sqrt(dot(difference_1, difference_1) + dot(difference_2, difference_2));
            }

            float RobertsCross(float samples[4])
            {
                const float difference_1 = samples[1] - samples[2];
                const float difference_2 = samples[0] - samples[3];
                return sqrt(difference_1 * difference_1 + difference_2 * difference_2);
            }

            float Sobel(float3 samples[9])
            {
                const float3 difference_1 = samples[0] - samples[2] + 2 * samples[3] - 2 * samples[5] + samples[6] - samples[8];
                const float3 difference_2 = samples[0] - samples[6] + 2 * samples[1] - 2 * samples[7] + samples[2] - samples[8];
                return sqrt(dot(difference_1, difference_1) + dot(difference_2, difference_2));
            }

            float Sobel(float samples[9])
            {
                const float difference_1 = samples[0] - samples[2] + 2 * samples[3] - 2 * samples[5] + samples[6] - samples[8];
                const float difference_2 = samples[0] - samples[6] + 2 * samples[1] - 2 * samples[7] + samples[2] - samples[8];
                return sqrt(difference_1 * difference_1 + difference_2 * difference_2);
            }

            #if defined(NORMALS)
            float3 SampleSceneNormalsRemapped(float2 uv)
            {
                return SampleSceneNormals(uv) * 0.5 + 0.5;
            }
            #endif

            #if defined(LUMINANCE) || defined(DEBUG_LUMINANCE)
            float SampleSceneLuminance(float2 uv)
            {
                float3 color = SampleSceneColor(uv);
                return color.r * 0.3 + color.g * 0.59 + color.b * 0.11;
            }
            #endif

            half3 HSVToRGB(half3 In)
            {
                half4 K = half4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
                half3 P = abs(frac(In.xxx + K.xyz) * 6.0 - K.www);
                return In.z * lerp(K.xxx, saturate(P - K.xxx), In.y);
            }

            half4 frag(Varyings IN) : SV_TARGET
            {
                float2 uv = IN.texcoord;

                ///
                /// DISCONTINUITY SOURCES
                ///

                #if defined(DEPTH) || defined(OVERRIDE_SHADOW) || defined(FADE_IN_DISTANCE) || defined(DEBUG_DEPTH)
                float center_depth = SampleSceneDepth(uv);
                #if !UNITY_REVERSED_Z // Transform depth from [0, 1] to [-1, 1] on OpenGL.
                center_depth = lerp(UNITY_NEAR_CLIP_VALUE, 1.0, center_depth); // Alternatively: depth = 1.0 - depth
                #endif
                float3 positionWS = ComputeWorldSpacePosition(uv, center_depth, UNITY_MATRIX_I_VP); // Calculate world position from depth.
                #endif

                #if defined(DEPTH) || defined(DEBUG_NORMALS)
                float3 center_normal = SampleSceneNormals(uv);
                #endif

                bool mask = false;
                bool fill = false;
                float section = SampleSceneSection(uv).r;
                if (section == 1.0) fill = true;
                if (section == 0.0) mask = true;

                ///
                /// EDGE DETECTION
                ///

                float edge_depth = 0;
                float edge_normal = 0;
                float edge_luminance = 0;
                float edge_section = 0;

                float2 texel_size = float2(1.0 / _ScreenParams.x, 1.0 / _ScreenParams.y); // Same as _BlitTexture_TexelSize.xy but this is broken atm

                #if defined(SCALE_WITH_RESOLUTION)
                float scaled_outline_thickness = _OutlineThickness * _ScreenParams.y / _ReferenceResolution;
                #else
                float scaled_outline_thickness = _OutlineThickness;
                #endif

                #if defined(OPERATOR_CROSS)
                const float half_width_f = floor(scaled_outline_thickness * 0.5);
                const float half_width_c = ceil(scaled_outline_thickness * 0.5);

                // Generate samples.
                float2 uvs[4];
                uvs[0] = uv + texel_size * float2(half_width_f, half_width_c) * float2(-1, 1);  // top left
                uvs[1] = uv + texel_size * float2(half_width_c, half_width_c) * float2(1, 1);   // top right
                uvs[2] = uv + texel_size * float2(half_width_f, half_width_f) * float2(-1, -1); // bottom left
                uvs[3] = uv + texel_size * float2(half_width_c, half_width_f) * float2(1, -1);  // bottom right
                
                float3 normal_samples[4];
                float depth_samples[4], section_samples[4], luminance_samples[4];
                
                for (int i = 0; i < 4; i++) {
                #if defined(DEPTH)
                    depth_samples[i] = SampleSceneDepth(uvs[i]);
                #endif

                #if defined(NORMALS)
                    normal_samples[i] = SampleSceneNormalsRemapped(uvs[i]);
                #endif

                #if defined(LUMINANCE)
                    luminance_samples[i] = SampleSceneLuminance(uvs[i]);
                #endif
                    
                    section_samples[i] = SampleSceneSection(uvs[i]).r;
                    if(section_samples[i] == 1) fill = true;
                    if(section_samples[i] == 0) mask = true;
                }

                #if defined(DEPTH)
                #if defined(DEPTH_MASK)
                edge_depth = mask ? 0 : RobertsCross(depth_samples);
                #else
                edge_depth = RobertsCross(depth_samples);
                #endif
                #endif

                #if defined(NORMALS)
                #if defined(NORMALS_MASK)
                edge_normal = mask ? 0 : RobertsCross(normal_samples);
                #else
                edge_normal = RobertsCross(normal_samples);
                #endif
                #endif

                #if defined(LUMINANCE)
                #if defined(LUMINANCE_MASK)
                edge_luminance = mask ? 0 : RobertsCross(luminance_samples);
                #else
                edge_luminance = RobertsCross(luminance_samples);
                #endif
                #endif

                #if defined(SECTIONS)
                #if defined(SECTIONS_MASK)
                edge_section = mask ? 0 : RobertsCross(section_samples);
                #else
                edge_section = RobertsCross(section_samples);
                #endif
                #endif

                #elif defined(OPERATOR_SOBEL)
                float scale = floor(scaled_outline_thickness);

                float2 uvs[9];
                uvs[0] = uv + texel_size * scale * float2(-1, 1); // top left
                uvs[1] = uv + texel_size * scale * float2(0, 1);  // top center
                uvs[2] = uv + texel_size * scale * float2(1, 1);  // top right
                uvs[3] = uv + texel_size * scale * float2(-1, 0); // middle left
                uvs[4] = uv + texel_size * scale * float2(0, 0);  // middle center
                uvs[5] = uv + texel_size * scale * float2(1, 0);  // middle right
                uvs[6] = uv + texel_size * scale * float2(-1, -1); // bottom left
                uvs[7] = uv + texel_size * scale * float2(0, -1);  // bottom center
                uvs[8] = uv + texel_size * scale * float2(1, -1);  // bottom right

                float3 normal_samples[9];
                float depth_samples[9], section_samples[9], luminance_samples[9];

                for (int i = 0; i < 9; i++) {
                #if defined(DEPTH)
                    depth_samples[i] = SampleSceneDepth(uvs[i]);
                #endif

                #if defined(NORMALS)
                    normal_samples[i] = SampleSceneNormalsRemapped(uvs[i]);
                #endif

                #if defined(LUMINANCE)
                    luminance_samples[i] = SampleSceneLuminance(uvs[i]);
                #endif
                    
                    section_samples[i] = SampleSceneSection(uvs[i]).r;
                    if(section_samples[i] == 1) fill = true;
                    if(section_samples[i] == 0) mask = true;
                }
                
                #if defined(DEPTH)
                #if defined(DEPTH_MASK)
                edge_depth = mask ? 0 : Sobel(depth_samples);
                #else
                edge_depth = Sobel(depth_samples);
                #endif
                #endif

                #if defined(NORMALS)
                #if defined(NORMALS_MASK)
                edge_normal = mask ? 0 : Sobel(normal_samples);
                #else
                edge_normal = Sobel(normal_samples);
                #endif
                #endif

                #if defined(LUMINANCE)
                #if defined(LUMINANCE_MASK)
                edge_luminance = mask ? 0 : Sobel(luminance_samples);
                #else
                edge_luminance = Sobel(luminance_samples);
                #endif
                #endif

                #if defined(SECTIONS)
                #if defined(SECTIONS_MASK)
                edge_section = mask ? 0 : Sobel(section_samples);
                #else
                edge_section = Sobel(section_samples);
                #endif
                #endif

                #endif

                ///
                /// DISCONTINUITIY THRESHOLDING
                ///

                #if defined(DEPTH)
                float depth_threshold = 1 / _DepthSensitivity;

                // 1. The depth buffer is non-linear so two objects 1m apart close to camera will have much larger depth difference than two
                //    objects 1m apart far away from the camera. For this, we multiply the threshold by the depth buffer so that nearby objects
                //    will have to have a larger discontinuity in order to be detected as an 'edge'.
                depth_threshold = max(depth_threshold * 0.01, depth_threshold * _DepthDistanceModulation * SampleSceneDepth(uv));

                // 2. At small grazing angles, the depth difference will grow larger and so faces can be wrongly detected. For this, the depth threshold
                //    can be modulated by the grazing angle, given by the dot product between the normal vector and the view direction. If the normal vector
                //    and the view direction are almost perpendicular, the depth threshold should be increased.
                float3 viewWS = normalize(_WorldSpaceCameraPos.xyz - positionWS);
                float fresnel = pow(1.0 - dot(normalize(center_normal), normalize(viewWS)), 1.0);
                float grazingAngleMask = saturate((fresnel + _GrazingAngleMaskPower - 1) / _GrazingAngleMaskPower); // a mask between 0 and 1
                depth_threshold = depth_threshold * (1 + _GrazingAngleMaskHardness * grazingAngleMask);
                
                edge_depth = edge_depth > depth_threshold ? 1 : 0;
                #endif

                #if defined(NORMALS)
                float normalThreshold = 1 / _NormalSensitivity;
                edge_normal = edge_normal > normalThreshold ? 1 : 0;
                #endif

                #if defined(LUMINANCE)
                float luminanceThreshold = 1 / _LuminanceSensitivity;
                edge_luminance = edge_luminance > luminanceThreshold ? 1 : 0;
                #endif

                #if defined(SECTIONS)
                edge_section = edge_section > 0 ? 1 : 0;
                #endif

                float edge = max(edge_depth, max(edge_normal, max(edge_luminance, edge_section)));

                ///
                /// DEBUG VIEWS
                ///

                #if defined(DEBUG_DEPTH)
                return lerp(half4(center_depth, center_depth, center_depth, 1), half4(1,1,1,1), edge_depth);
                #endif

                #if defined(DEBUG_NORMALS)
                return lerp(half4(center_normal * 0.5 + 0.5, 1), half4(0,0,0,1), edge_normal);
                #endif

                #if defined(DEBUG_LUMINANCE)
                half3 luminance = SampleSceneLuminance(uv);
                return lerp(half4(luminance, luminance, luminance, 1), half4(1,0,0,1), edge_luminance);
                #endif

                #if defined(DEBUG_SECTIONS)
                if(fill) return half4(0,1,0,1);
                if(mask) return half4(0,0,1,1);

                #if defined(DEBUG_SECTIONS_RAW_VALUES)
                half4 section_raw = half4(section,0,0,1);
                return lerp(section_raw, half4(1,1,1,1), edge_section);
                #else
                half4 section_perceptual = half4(HSVToRGB(half3(section * 360.0, 0.5, 1.0)), 1.0);
                if(mask) section_perceptual = half4(1.0, 1.0, 1.0, 1.0);
                return lerp(section_perceptual, half4(0,0,0,1), edge_section);
                #endif
                #endif

                ///
                /// COMPOSITE EDGES
                ///

                float4 line_color = _OutlineColor;

                // Shadows.
                #if defined(OVERRIDE_SHADOW)
                float shadow = 1 - SampleShadowmap(
                    TransformWorldToShadowCoord(positionWS),
                    TEXTURE2D_ARGS(_MainLightShadowmapTexture, sampler_MainLightShadowmapTexture),
                    GetMainLightShadowSamplingData(),
                    GetMainLightShadowStrength(),
                    false);
                line_color = lerp(line_color, _OutlineColorShadow, shadow);
                #endif

                #if defined(FADE_IN_DISTANCE)
                float distance = length(positionWS - _WorldSpaceCameraPos);
                float fade = 1.0 - saturate(1.0 - (distance - _FadeStart) / _FadeDistance);
                float4 fade_color = lerp(line_color, _FadeColor * _FadeColor.a, fade);
                return lerp(_BackgroundColor, fade_color, edge);
                #endif

                return lerp(_BackgroundColor, line_color, edge);
            }
            ENDHLSL
        }
    }
}