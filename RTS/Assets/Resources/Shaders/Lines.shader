Shader "Custom/Lines"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Intensity("Intensity", float) = 1.25
        _NumLines ("Width", Range(2,50)) = 30
    }
    SubShader
    {
        // No culling or depth
        Cull Back ZWrite On ZTest Always

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

            float _NumLines;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv*_NumLines;
                return o;
            }

            sampler2D _MainTex;
            fixed4 _Color;
            float _Intensity;

            fixed4 toGrayscale(in fixed4 color)
            {
                float average = (color.r + color.g + color.b) / 3.0;
                return fixed4(average, average, average, color.a);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = toGrayscale(tex2D(_MainTex, i.uv));
                float2 c = i.uv;
                c = floor(c) / 2;

                float yStep = frac(c.y)*2;
                if(yStep > 0) {
                    col.rgb = col.rgb*_Color*_Intensity;
                } else {
                    col.rgb = col.rgb*_Color;
                }
                /*
                col.r = _Color.r;
                col.g = _Color.g;
                col.b = col.b + frac(c.y) * 2;
                */
                return col;
            }

            ENDCG
        }
    }
}
