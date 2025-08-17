Shader "Quibli/Stylized Lit + P3D Paint"
{
    Properties
    {
        [Gradient] _GradientRamp("Gradient", 2D) = "white" {}

        _SelfShadingSize ("[]Shading Offset", Range(0, 1.0)) = 0.0

        [Space(10)]
        [Toggle(DR_SPECULAR_ON)] _SpecularEnabled("Enable Specular", Int) = 0
        [HDR] _FlatSpecularColor("[DR_SPECULAR_ON]Specular Color", Color) = (0.85023, 0.85034, 0.85045, 0.85056)
        _FlatSpecularSize("[DR_SPECULAR_ON]Specular Size", Range(0.0, 1.0)) = 0.1
        _FlatSpecularEdgeSmoothness("[DR_SPECULAR_ON]Specular Edge Smoothness", Range(0.0, 1.0)) = 0

        [Space(10)]
        [Toggle(DR_RIM_ON)] _RimEnabled("Enable Rim", Int) = 0
        [HDR] _FlatRimColor("[DR_RIM_ON]Rim Color", Color) = (0.85023, 0.85034, 0.85045, 0.85056)
        _FlatRimLightAlign("[DR_RIM_ON]Light Align", Range(0.0, 1.0)) = 0
        _FlatRimSize("[DR_RIM_ON]Rim Size", Range(0, 1)) = 0.5
        _FlatRimEdgeSmoothness("[DR_RIM_ON]Rim Edge Smoothness", Range(0, 1)) = 0.5

        [Space(10)]
        [Toggle(DR_GRADIENT_ON)] _GradientEnabled("Enable Height Gradient", Int) = 0
        [HideInInspector][MainColor] _BaseColor ("Color", Color) = (1,1,1,1)
        _ColorGradient("[DR_GRADIENT_ON]Gradient Color", Color) = (0.85023, 0.85034, 0.85045, 0.85056)
        _GradientCenterX("[DR_GRADIENT_ON]Center X", Float) = 0
        _GradientCenterY("[DR_GRADIENT_ON]Center Y", Float) = 0
        _GradientSize("[DR_GRADIENT_ON]Size", Float) = 10.0
        _GradientAngle("[DR_GRADIENT_ON]Gradient Angle", Range(0, 360)) = 0

        [Space(10)]
        [Toggle(DR_VERTEX_COLORS_ON)] _VertexColorsEnabled("Enable Vertex Colors", Int) = 0

        [Space]
        [Toggle(DR_OUTLINE_ON)] _OutlineEnabled("Enable Outline", Int) = 0
        _OutlineColor("[DR_OUTLINE_ON]Color", Color) = (1, 1, 1, 1)
        _OutlineWidth("[DR_OUTLINE_ON]Width", Float) = 1.0
        _OutlineScale("[DR_OUTLINE_ON]Scale", Float) = 1.0
        _OutlineDepthOffset("[DR_OUTLINE_ON]Depth Offset", Range(0, 1)) = 0.0
        _CameraDistanceImpact("[DR_OUTLINE_ON]Camera Distance Impact", Range(0, 1)) = 0.5

        // This texture will be written by Paint in 3D via P3dPaintableTexture (Group: Albedo/BaseMap)
        [MainTexture] _BaseMap("[FOLDOUT(Texture maps){11}]Albedo (Paint Texture)", 2D) = "black" {}
        [Toggle(_BASEMAP_PREMULTIPLY)]_BaseMapPremultiply("[_]Mix Into Shading", Int) = 0
        [KeywordEnum(Multiply, Add)]_TextureBlendingMode("[]Blending Mode", Float) = 0
        _TextureImpact("[]Texture Impact", Range(0, 1)) = 1.0

        // Detail map support (required by included Quibli lighting files)
        _DetailMap("Detail Map", 2D) = "black" {}
        _DetailMapColor("[]Detail Color", Color) = (1,1,1,1)
        [KeywordEnum(Multiply, Add, Interpolate)]_DetailMapBlendingMode("[]Blending Mode", Float) = 0
        _DetailMapImpact("[]Detail Impact", Range(0, 1)) = 0.0

        _BumpMap ("Normal Map", 2D) = "bump" {}

        [HideInInspector]_Cutoff ("Base Alpha cutoff", Range (0, 1)) = .5
        [HideInInspector] _Surface("__surface", Float) = 0.0
        [HideInInspector] _Blend("__blend", Float) = 0.0
        [HideInInspector] _AlphaClip("__clip", Float) = 0.0
        [HideInInspector] _SrcBlend("__src", Float) = 1.0
        [HideInInspector] _DstBlend("__dst", Float) = 0.0
        [HideInInspector] _ZWrite("__zw", Float) = 1.0
        [HideInInspector] _Cull("__cull", Float) = 2.0

        [HideInInspector] _QueueOffset("Queue offset", Float) = 0.0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForwardOnly" }

            Blend[_SrcBlend][_DstBlend]
            ZWrite[_ZWrite]
            Cull[_Cull]

            HLSLPROGRAM
            #pragma multi_compile_instancing
            #pragma vertex LitPassVertex
            #pragma fragment StylizedPassFragmentPaint

            // Quibli feature toggles
            #pragma shader_feature_local DR_GRADIENT_ON
            #pragma shader_feature_local DR_VERTEX_COLORS_ON
            #pragma shader_feature_local _TEXTUREBLENDINGMODE_MULTIPLY _TEXTUREBLENDINGMODE_ADD
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local _RECEIVE_SHADOWS_OFF

            // URP lighting variants (kept minimal for compatibility)
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #define BUMP_SCALE_NOT_SUPPORTED 1
            #define REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR 1
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_GradientRamp);
            SAMPLER(sampler_GradientRamp);
            // Required by Quibli lighting
            TEXTURE2D(_DetailMap);
            SAMPLER(sampler_DetailMap);
            TEXTURE2D(_BakedGIRamp);
            SAMPLER(sampler_BakedGIRamp);

            #include "../Quibli/Shaders/LibraryUrp/StylizedInput.hlsl"
            #include "../Quibli/Shaders/LibraryUrp/QuibliVertex_Copy.hlsl" // Provides LitPassVertex
            #include "../Quibli/Shaders/LibraryUrp/LitForwardPass_DR_Copy.hlsl" // Provides surface setup helpers

            half3 BlendPaint(half3 baseCol, half3 albedo)
            {
                #if defined(_TEXTUREBLENDINGMODE_ADD)
                    return baseCol + albedo * _TextureImpact;
                #else
                    // Multiply (default)
                    return lerp(baseCol, baseCol * albedo, _TextureImpact);
                #endif
            }

            half4 StylizedPassFragmentPaint(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                SurfaceData surfaceData; // will sample _BaseMap into albedo
                InitializeSimpleLitSurfaceData(input.uv, surfaceData);

                InputData inputData;
                InitializeInputData(input, surfaceData.normalTS, inputData);

                // Gradient driven by vertex color R (0..1)
                half rampT = input.VertexColor.r;
                half3 gradCol = SAMPLE_TEXTURE2D(_GradientRamp, sampler_GradientRamp, half2(rampT, 0.5)).rgb;

                // Combine with paint albedo (_BaseMap filled by Paint in 3D)
                half3 albedo = surfaceData.albedo;
                half3 col = BlendPaint(gradCol, albedo);

                col = MixFog(col, inputData.fogCoord);
                half4 color = half4(col, surfaceData.alpha);
                return color;
            }
            ENDHLSL
        }
    }

    Fallback "Hidden/Universal Render Pipeline/FallbackError"
}


