Shader "Custom/GreenReplaceLit"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        [PerRendererData] _NetworkID ("Network ID", int) = 0
        _GreenThresh("Green Threshold", Range(0,1)) = 0.7
        _RbThresh("Red/Blue Threshold", Range(0,1)) = 0.8
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        int _NetworkID;
        fixed _GreenThresh;
        fixed _RbThresh;
        

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            fixed3 col = o.Albedo;
            //red player
            if(_NetworkID == 0) {
                if(col.r < .02 && col.b < .02) {
                    col.r = col.g;
                    col.g = 0;
                    col.b = 0;
                }
                else if(col.g > _GreenThresh && col.r < _RbThresh && col.b < _RbThresh)
                {
                    fixed gColor = col.g;
                    fixed rbColor = col.r;
                    col.r = gColor;
                    col.g = rbColor;
                    col.b = rbColor;
                }
            }
            //blue player
            else if(_NetworkID == 1) {
                if(col.r < .02 && col.b < .02) {
                    col.b = col.g;
                    col.r = 0;
                    col.g = 0;
                }
                else if(col.g > _GreenThresh && col.r < _RbThresh && col.b < _RbThresh)
                {
                    fixed gColor = col.g;
                    fixed rbColor = col.r;
                    col.b = gColor;
                    col.g = rbColor;
                    col.r = rbColor;
                }
            }
            //green player (just do nothing lol)
            //yellow player
            else if(_NetworkID == 3) {
                if(col.r < .02 && col.b < .02) {
                    col.r = col.g;
                    col.b = 0;
                }
                else if(col.g > _GreenThresh && col.r < _RbThresh && col.b < _RbThresh)
                {
                    fixed gColor = col.g;
                    fixed rbColor = col.r;
                    col.r = gColor;
                    col.b = rbColor;
                }
            }
            o.Albedo = col;
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
