Shader "Quibli/Stylized Lit Minimal Copy"
{
    // This is a trimmed version of Quibli/Stylized Lit Copy that keeps only the
    // passes required for identical visual shading in URP (ForwardLit + Outline + ShadowCaster + DepthOnly/Normals).
    // All feature toggles/keywords and properties are preserved for look parity.

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

        [MainTexture] _BaseMap("[FOLDOUT(Texture maps){11}]Albedo", 2D) = "black" {}
        [Toggle(_BASEMAP_PREMULTIPLY)]_BaseMapPremultiply("[_]Mix Into Shading", Int) = 0
        [KeywordEnum(Multiply, Add)]_TextureBlendingMode("[]Blending Mode", Float) = 0
        _TextureImpact("[]Texture Impact", Range(0, 1)) = 1.0

        _DetailMap("Detail Map", 2D) = "black" {}
        _DetailMapColor("[]Detail Color", Color) = (1,1,1,1)
        [KeywordEnum(Multiply, Add, Interpolate)]_DetailMapBlendingMode("[]Blending Mode", Float) = 0
        _DetailMapImpact("[]Detail Impact", Range(0, 1)) = 0.0

        _BumpMap ("Normal Map", 2D) = "bump" {}

        [Space(15)]
        [HideInInspector][HDR]_EmissionColor("Emission Color", Color) = (0,0,0)
        [HideInInspector][NoScaleOffset]_EmissionMap("Emission Map", 2D) = "white" {}

        _LightContribution("[FOLDOUT(Lighting){11}]Light Color Impact", Range(0, 1)) = 1
        [Space][ToggleOff] _ReceiveShadows("Receive Shadows", Float) = 0
        [Toggle(_UNITYSHADOW_OCCLUSION)]_UnityShadowOcclusion("[_RECEIVE_SHADOWS_OFF]Shadow Occlusion", Int) = 0
        [Space][Toggle(DR_LIGHT_ATTENUATION)]_OverrideLightAttenuation("Customize Light", Int) = 0
        [MinMax]_LightAttenuation("[DR_LIGHT_ATTENUATION][]Attenuation Remap", Vector) = (0, 1, 0, 0)
        _ShadowColor("[DR_LIGHT_ATTENUATION][]Shadow Color", Color) = (0, 0, 0, 1)
        [Space][Toggle(DR_BAKED_GI)]_OverrideBakedGi("Override Baked GI", Int) = 0
        [Gradient]_BakedGIRamp("[DR_BAKED_GI]Baked Light Lookup", 2D) = "transparent" {}

        [Space]
        [Toggle(DR_ENABLE_LIGHTMAP_DIR)]_OverrideLightmapDir("Override Light Direction", Int) = 0
        _LightmapDirectionPitch("[DR_ENABLE_LIGHTMAP_DIR][]Pitch", Range(0, 360)) = 0
        _LightmapDirectionYaw("[DR_ENABLE_LIGHTMAP_DIR][]Yaw", Range(0, 360)) = 0
        [HideInInspector] _LightmapDirection("Direction", Vector) = (0, 1, 0, 0)

        [HideInInspector]_Cutoff ("Base Alpha cutoff", Range (0, 1)) = .5

        // Blending state
        [HideInInspector] _Surface("__surface", Float) = 0.0
        [HideInInspector] _Blend("__blend", Float) = 0.0
        [HideInInspector] _AlphaClip("__clip", Float) = 0.0
        [HideInInspector] _SrcBlend("__src", Float) = 1.0
        [HideInInspector] _DstBlend("__dst", Float) = 0.0
        [HideInInspector] _ZWrite("__zw", Float) = 1.0
        [HideInInspector] _Cull("__cull", Float) = 2.0

        // Editmode props
        [HideInInspector] _QueueOffset("Queue offset", Float) = 0.0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        LOD 300

        // ----------------------- Forward Lit -----------------------
        Pass
        {
            Name "ForwardLit"
            Tags{ "LightMode" = "UniversalForwardOnly" }

            Blend[_SrcBlend][_DstBlend]
            ZWrite[_ZWrite]
            Cull[_Cull]

            HLSLPROGRAM
            #pragma shader_feature_local DR_LIGHT_ATTENUATION
            #pragma shader_feature_local DR_BAKED_GI
            #pragma shader_feature_local DR_GRADIENT_ON
            #pragma shader_feature_local DR_SPECULAR_ON
            #pragma shader_feature_local DR_RIM_ON
            #pragma shader_feature_local DR_VERTEX_COLORS_ON
            #pragma shader_feature_local DR_ENABLE_LIGHTMAP_DIR
            #pragma shader_feature_local _TEXTUREBLENDINGMODE_MULTIPLY _TEXTUREBLENDINGMODE_ADD
            #pragma shader_feature_local _UNITYSHADOW_OCCLUSION
            #pragma shader_feature_local _BASEMAP_PREMULTIPLY

            // Material keywords
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ALPHAPREMULTIPLY_ON
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local _RECEIVE_SHADOWS_OFF

            // URP keywords (trimmed set that preserves visuals)
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            // Instancing
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer

            #define BUMP_SCALE_NOT_SUPPORTED 1
            #define REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR 1
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // Declare textures/samplers expected by Quibli lighting includes
            TEXTURE2D(_GradientRamp);           SAMPLER(sampler_GradientRamp);
            TEXTURE2D(_BakedGIRamp);           SAMPLER(sampler_BakedGIRamp);
            TEXTURE2D(_DetailMap);             SAMPLER(sampler_DetailMap);
            // _BaseMap and sampler_BaseMap are declared by URP SurfaceInput.hlsl; do not redeclare here

            // Quibli forward shading
            #include "../Quibli/Shaders/LibraryUrp/StylizedInput.hlsl"
            // Order matters: LitForwardPass defines Attributes/Varyings used by QuibliVertex
            #include "../Quibli/Shaders/LibraryUrp/LitForwardPass_DR_Copy.hlsl" // Defines Attributes/Varyings + StylizedPassFragment
            #include "../Quibli/Shaders/LibraryUrp/QuibliVertex_Copy.hlsl"      // Provides LitPassVertex

            #pragma vertex LitPassVertex
            #pragma fragment StylizedPassFragment
            ENDHLSL
        }

        


        // ----------------------- DepthOnly -----------------------
        Pass
        {
            Name "DepthOnly"
            Tags{ "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask 0
            Cull[_Cull]

            HLSLPROGRAM
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #pragma multi_compile_instancing
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #include "../Quibli/Shaders/LibraryUrp/StylizedInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }

        // ----------------------- DepthNormals (for SSAO) -----------------------
        Pass
        {
            Name "DepthNormals"
            Tags{ "LightMode" = "DepthNormals" }
            ZWrite On
            Cull[_Cull]
            HLSLPROGRAM
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment
            #pragma multi_compile_instancing
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #include "../Quibli/Shaders/LibraryUrp/StylizedInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthNormalsPass.hlsl"
            ENDHLSL
        }
    }

    Fallback "Hidden/Universal Render Pipeline/FallbackError"
    CustomEditor "Quibli.QuibliEditor"
}


