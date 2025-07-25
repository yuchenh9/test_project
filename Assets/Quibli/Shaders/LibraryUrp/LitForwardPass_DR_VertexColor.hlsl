#ifndef LIT_FORWARD_PASS_DR_VERTEXCOLOR
#define LIT_FORWARD_PASS_DR_VERTEXCOLOR

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Version.hlsl"
#include "Lighting_DR_Copy.hlsl"

// This file duplicates LitForwardPass_DR_Copy but replaces the fragment function so that
// colour comes from vertex colour (luminance) mapped through _GradientRamp, independent of lights.
// Varyings and other helper functions are left unchanged by including the original copy.

#include "LitForwardPass_DR_Copy.hlsl"
 
// Declare gradient ramp texture/sampler
TEXTURE2D(_GradientRamp);
SAMPLER(sampler_GradientRamp);
// Declare detail map to satisfy unused references in included code
TEXTURE2D(_DetailMap);
SAMPLER(sampler_DetailMap);

// Undefine the guard so we can override the fragment
#undef StylizedPassFragment

half4 StylizedPassFragment(Varyings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    SurfaceData surfaceData;
    InitializeSimpleLitSurfaceData(input.uv, surfaceData);

    InputData inputData;
    InitializeInputData(input, surfaceData.normalTS, inputData);

    // Obtain vertex colour (stored in vertexLighting when DR_VERTEX_COLORS_ON)
    half3 vcol = inputData.vertexLighting;

    // Use luminance or just red channel to sample ramp
    half colT = saturate(dot(vcol, half3(0.299, 0.587, 0.114)));
    half3 col = SAMPLE_TEXTURE2D(_GradientRamp, sampler_GradientRamp, half2(colT, 0.5)).rgb;

    // Apply fog
    col = MixFog(col, inputData.fogCoord);

    half4 outCol = half4(col, surfaceData.alpha);

    #if UNITY_VERSION >= 202220
    outCol.a = OutputAlpha(outCol.a, IsSurfaceTypeTransparent(_Surface));
    #else
    outCol.a = OutputAlpha(outCol.a, _Surface);
    #endif

    return outCol;
}

#endif 