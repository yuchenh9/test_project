
#ifndef LIGHTING_DR_INCLUDED
#define LIGHTING_DR_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#if LIGHTMAP_ON && defined(DR_BAKED_GI)
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#endif

inline half NdotLTransition(half3 normal, half3 lightDir, half selfShadingSize) {
    const half NdotL = dot(normal, lightDir);
    const half angleDiff = saturate((NdotL * 0.5 + 0.5) - selfShadingSize);
    return angleDiff;
}

inline half NdotLTransitionPrimary(half3 normal, half3 lightDir) {
    return NdotLTransition(normal, lightDir, _SelfShadingSize);
}

half3 LightingPhysicallyBased_DSTRM(Light light, InputData inputData)
{
    // Sample gradient ramp purely from world normal Y (up/down) independent of lights
    half rampT = saturate(inputData.normalWS.y * 0.5 + 0.5); // Map -1..1 -> 0..1
    half3 c = SAMPLE_TEXTURE2D(_GradientRamp, sampler_GradientRamp, half2(rampT, 0.5)).rgb;
    return c;
}

void StylizeLight(inout Light light) {
#if defined(DR_LIGHT_ATTENUATION)
    // Handled above.
#else
    const half shadowAttenuation = saturate(light.shadowAttenuation * 10.0);
    light.shadowAttenuation = shadowAttenuation;

    const float distanceAttenuation = smoothstep(0, 0.001, light.distanceAttenuation);
    light.distanceAttenuation = distanceAttenuation;
#endif

    const half3 lightColor = lerp(half3(1, 1, 1), light.color, _LightContribution);
    light.color = lightColor;
}

half4 UniversalFragment_DSTRM(InputData inputData, SurfaceData surfaceData, float2 uv) {
    // To ensure backward compatibility we have to avoid using shadowMask input, as it is not present in older shaders
#if defined(SHADOWS_SHADOWMASK) && defined(LIGHTMAP_ON)
    const half4 shadowMask = inputData.shadowMask;
#elif !defined(LIGHTMAP_ON)
    const half4 shadowMask = unity_ProbesOcclusion;
#else
    const half4 shadowMask = half4(1, 1, 1, 1);
#endif

    Light mainLight = GetMainLight(inputData.shadowCoord, inputData.positionWS, shadowMask);

#if LIGHTMAP_ON
    mainLight.distanceAttenuation = 1.0;
    mainLight.color = half3(1, 1, 1);
#endif

    StylizeLight(mainLight);

#if defined(_SCREEN_SPACE_OCCLUSION)
    AmbientOcclusionFactor aoFactor = GetScreenSpaceAmbientOcclusion(inputData.normalizedScreenSpaceUV);
    mainLight.color *= aoFactor.directAmbientOcclusion;
    inputData.bakedGI *= aoFactor.indirectAmbientOcclusion;
#endif

    MixRealtimeAndBakedGI(mainLight, inputData.normalWS, inputData.bakedGI);

    // Apply stylizing to `inputData.bakedGI` (which is half3).
#if LIGHTMAP_ON && defined(DR_BAKED_GI)
    half2 rampUV = half2(Luminance(inputData.bakedGI), 0.5);
    inputData.bakedGI = SAMPLE_TEXTURE2D(_BakedGIRamp, sampler_BakedGIRamp, rampUV);
#endif

    const half4 albedo = half4(surfaceData.albedo, surfaceData.alpha);

    half3 brdf = _LightContribution;
#if defined(_BASEMAP_PREMULTIPLY)
    brdf *= albedo.rgb;
#endif

    BRDFData brdfData;
    InitializeBRDFData(brdf, 1.0 - 1.0 / kDielectricSpec.a, 0, 0, surfaceData.alpha, brdfData);
    half3 color = GlobalIllumination(brdfData, inputData.bakedGI, 1.0, inputData.normalWS, inputData.viewDirectionWS);
    color += LightingPhysicallyBased_DSTRM(mainLight, inputData);

#ifdef _ADDITIONAL_LIGHTS
    const uint pixelLightCount = GetAdditionalLightsCount();
    uint meshRenderingLayers = GetMeshRenderingLayer();

    #if USE_FORWARD_PLUS
    for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
    {
        FORWARD_PLUS_SUBTRACTIVE_LIGHT_CHECK

        Light light = GetAdditionalLight(lightIndex, inputData.positionWS, shadowMask);//, aoFactor);
        StylizeLight(light);

        #ifdef _LIGHT_LAYERS
        if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
            #endif
        {
            color += LightingPhysicallyBased_DSTRM(light, inputData);
        }
    }
    #endif
    
    LIGHT_LOOP_BEGIN(pixelLightCount)
        Light light = GetAdditionalLight(lightIndex, inputData.positionWS, shadowMask);

#if defined(_SCREEN_SPACE_OCCLUSION)
        light.color *= aoFactor.directAmbientOcclusion;
#endif

        StylizeLight(light);
    
        #ifdef _LIGHT_LAYERS
        if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
        #endif
        {
            color += LightingPhysicallyBased_DSTRM(light, inputData);
        }
    LIGHT_LOOP_END
#endif

#ifdef _ADDITIONAL_LIGHTS_VERTEX
    color += inputData.vertexLighting * brdfData.diffuse;
#endif

    // Base map.
    {
        // Workaround to render decals without requiring the _BaseMap texture.
#ifdef _DBUFFER
        _TextureImpact = 1.0;
#endif

#if defined(_TEXTUREBLENDINGMODE_ADD)
        color += lerp(half3(0.0f, 0.0f, 0.0f), albedo.rgb, _TextureImpact);
#else  // _TEXTUREBLENDINGMODE_MULTIPLY
        color *= lerp(half3(1.0f, 1.0f, 1.0f), albedo.rgb, _TextureImpact);
#endif
    }

    // Detail map.
    {
        const half4 detail = SAMPLE_TEXTURE2D(_DetailMap, sampler_DetailMap, uv);
        #if defined(_DETAILMAPBLENDINGMODE_ADD)
        color += lerp(0, _DetailMapColor.rgb, detail.rgb * _DetailMapImpact).rgb;
        #endif
        #if defined(_DETAILMAPBLENDINGMODE_MULTIPLY)
        color *= lerp(1, _DetailMapColor.rgb, detail.rgb * _DetailMapImpact).rgb;
        #endif
        #if defined(_DETAILMAPBLENDINGMODE_INTERPOLATE)
        color = lerp(color, detail.rgb, _DetailMapImpact * _DetailMapColor.rgb * detail.a).rgb;
        #endif
    }

    // Vertex color.
    {
        #if defined(DR_VERTEX_COLORS_ON)
        color *= inputData.vertexLighting;
        #endif
    }

    color += surfaceData.emission;
    return half4(color, surfaceData.alpha);
}

#endif // LIGHTING_DR_INCLUDED
