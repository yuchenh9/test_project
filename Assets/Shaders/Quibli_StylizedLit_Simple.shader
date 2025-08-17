// -----------------------------------------------------------------------------
// Quibli/Stylized Lit Simple (URP)
// -----------------------------------------------------------------------------
// Purpose
// - Lightweight stylized surface shader used for quick look-dev and mobile.
// - Color comes from a 1D Gradient Ramp sampled by the mesh vertex color R.
// - Optional Paint-in-3D (or any system) can multiply an albedo BaseMap.
// - Designed to be friendly with Polybrush (always reads vertex COLOR).
//
// How it shades
// - Vertex stage: transforms to clip space, passes UV0 and vertex color, computes fog.
// - Fragment stage:
//     1) Reads vertexColor.r in 0..1 as the ramp coordinate.
//     2) Samples _GradientRamp at (r, 0.5) to get stylized color.
//     3) Samples _BaseMap and blends: final = lerp(grad, grad * baseMap, _TextureImpact).
//     4) Applies material tint and URP fog.
//
// Intended data
// - Vertex colors: R channel drives ramp (GBA ignored). If no vertex colors are painted,
//   author tools typically default to 1 (full ramp end).
// - _GradientRamp: a horizontal ramp (U axis) texture. Any 1D color band works.
// - _BaseMap: bound by P3dPaintableTexture (Group: Albedo/BaseMap) or any albedo texture.
//
// Integration notes
// - Polybrush: Works out of the box because the shader samples vertex COLOR every frame.
// - Paint in 3D: Add P3dPaintable + P3dPaintableTexture(Custom property _BaseMap or Albedo).
// - Performance: Single forward pass, no lighting/normal maps; suitable for mobile.
//
// Property quick ref
// - _GradientRamp   (2D): Gradient lookup texture.
// - _BaseMap        (2D): Optional albedo/paint texture to multiply into gradient.
// - _Color          (RGBA): Material-wide tint.
// - _TextureImpact  (0..1): 0 = gradient only, 1 = gradient * baseMap.
//
// Limitations
// - Unlit look; if you need lighting/specular/rim, extend this or use the full Quibli shader.
// - Alpha is opaque (A=1). Add surface options if you need transparency.
// -----------------------------------------------------------------------------
Shader "Quibli/Stylized Lit Simple"
{
    Properties
    {
        [Gradient]_GradientRamp("Gradient (U:0..1)", 2D) = "white" {}
        [MainTexture]_BaseMap("Paint/Albedo (BaseMap)", 2D) = "white" {}
        [MainColor]_Color("Tint", Color) = (1,1,1,1)
        _TextureImpact("Texture Impact (0..1)", Range(0,1)) = 1
        // Polybrush hint (many tools just need the shader to actually use vertex COLOR semantics)
        [HideInInspector]_SupportsVertexColor("Supports Vertex Color", Float) = 1
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

        Pass
        {
            Name "ForwardSimple"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            // Always uses vertex color; no keyword needed so editor tools detect support

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // Optional paint/albedo texture (e.g., Paint in 3D)
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            // 1D gradient lookup texture (sampled at V=0.5)
            TEXTURE2D(_GradientRamp);
            SAMPLER(sampler_GradientRamp);

            CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            float4 _Color;
            float  _TextureImpact;
            CBUFFER_END

            // Mesh inputs from the vertex buffer
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            // Interpolators to the fragment stage
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
                float  fogCoord   : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);   // safe even if BaseMap isn’t used
                OUT.color = IN.color;
                OUT.fogCoord = ComputeFogFactor(OUT.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // 1) Gradient sample driven by vertex color R (Polybrush paints this)
                half rampT = IN.color.r;
                half3 grad = SAMPLE_TEXTURE2D(_GradientRamp, sampler_GradientRamp, half2(rampT, 0.5)).rgb;

                // 2) Optional paint/albedo from BaseMap
                half3 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv).rgb;
                half3 combined = lerp(grad, grad * albedo, saturate(_TextureImpact));

                // 3) Apply tint & fog
                combined *= _Color.rgb;
                combined = MixFog(combined, IN.fogCoord);

                return half4(combined, 1); // opaque
            }
            ENDHLSL
        }
    }

    Fallback "Hidden/Universal Render Pipeline/FallbackError"
}


