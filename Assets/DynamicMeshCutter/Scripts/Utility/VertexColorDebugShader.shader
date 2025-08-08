Shader "Custom/VertexColorDebug"
{
    Properties
    {
        _ShowAlpha ("Show Alpha", Range(0,1)) = 0
        _Brightness ("Brightness", Range(0,2)) = 1
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
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };

            float _ShowAlpha;
            float _Brightness;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color * _Brightness;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Show RGB by default, or alpha if _ShowAlpha is set
                if (_ShowAlpha > 0.5)
                {
                    // Show alpha channel as grayscale
                    return fixed4(i.color.a, i.color.a, i.color.a, 1);
                }
                else
                {
                    // Show RGB channels
                    return fixed4(i.color.rgb, 1);
                }
            }
            ENDCG
        }
    }
} 