Shader "Unlit/texture_gradient"
{
    Properties
    {
        [Gradient] _GradientRamp("Gradient", 2D) = "white" {}
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };
            sampler2D _GradientRamp; 
            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Sample the texture and return it
                fixed4 col = tex2D(_MainTex, i.uv);
                //return col;
                float gradientT = col.r; // This could be UV.x, vertex color, noise, etc.
    
                // Sample the gradient using the red channel
                float4 gradientColor = tex2D(_GradientRamp, float2(gradientT, 0.5));
                
                // You can use just the red channel if you want
                float redValue = gradientColor.r;
                
                // Or use the full RGB from the gradient
                return float4(gradientColor.rgb, 1.0);

            }
            ENDCG
        }
    }

    
}
