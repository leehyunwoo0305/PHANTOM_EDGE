Shader "Custom/Dissolve"
{
    Properties
    {
        _MainTex ("Base Map", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _DissolveTex ("Dissolve Noise", 2D) = "white" {}
        _DissolveAmount ("Dissolve Amount", Range(0, 1)) = 0
        _EdgeWidth ("Edge Width", Float) = 0.05
        _EdgeColor ("Edge Color", Color) = (1, 0.5, 0.1, 1)
        _EdgeEmission ("Edge Emission", Float) = 5
        _Metallic ("Metallic", Range(0,1)) = 0
        _Smoothness ("Smoothness", Range(0,1)) = 0.5
        _NormalMap ("Normal Map", 2D) = "bump" {}
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert alpha:fade
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _DissolveTex;
        sampler2D _NormalMap;
        float _DissolveAmount;
        float _EdgeWidth;
        float4 _EdgeColor;
        float _EdgeEmission;
        float _Metallic;
        float _Smoothness;
        float4 _Color;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_DissolveTex;
            float3 worldPos;
        };

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float dissolveNoise = tex2D(_DissolveTex, IN.uv_DissolveTex).r;
            float dissolve = step(_DissolveAmount, dissolveNoise);
            
            float edge = smoothstep(_DissolveAmount - _EdgeWidth, _DissolveAmount, dissolveNoise);
            
            clip(dissolve - 0.5);
            
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            o.Alpha = c.a;
            o.Metallic = _Metallic;
            o.Smoothness = _Smoothness;
            o.Normal = UnpackNormal(tex2D(_NormalMap, IN.uv_MainTex));
            
            o.Emission = _EdgeColor.rgb * edge * _EdgeEmission;
        }
        ENDCG
    }
    FallBack "Standard"
}