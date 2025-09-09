Shader "Custom/ImageNormalBlit"
{
    Properties
    {   
        _MainTex ("Image Texture", 2D) = "white" {}
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _NormalStrength ("Normal Strength", Range(0, 2)) = 1
        _BlendFactor ("Blend Factor", Range(0, 1)) = 0.5
        _direction ("Direction", Vector) = (1, 1, 1, 1)
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        
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
            
            sampler2D _MainTex;
            sampler2D _NormalMap;
            float _NormalStrength;
            float _BlendFactor;
            float3 _direction;
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // Sample the main texture
                fixed4 col = tex2D(_MainTex, i.uv);
                
                // Sample the normal map
                fixed4 normal = tex2D(_NormalMap, i.uv);
                
                float3 normalVector = normalize(normal.rgb * 2.0 - 1.0);
                if (dot(normalVector,normalize(_direction))>0.1)
                {
                    col.r+=0.1;
                    col.g+=0.1;
                    col.b+=0.1;
                }
                return col;
            }
            ENDCG
        }
    }
}