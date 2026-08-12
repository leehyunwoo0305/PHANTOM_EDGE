Shader "Custom/KatanaTrail"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1, 0.8, 0.2, 1)
        _EmissionColor ("Emission Color", Color) = (1, 0.5, 0.1, 1)
        _Intensity ("Intensity", Float) = 3.0
        _FadeSpeed ("Fade Speed", Float) = 1.0
        _Distortion ("Distortion", Float) = 0.1
        _PulseSpeed ("Pulse Speed", Float) = 5.0
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        LOD 100
        Blend SrcAlpha One
        ZWrite Off
        Cull Off
        Lighting Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float2 screenPos : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float4 _EmissionColor;
            float _Intensity;
            float _FadeSpeed;
            float _Distortion;
            float _PulseSpeed;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                o.screenPos = ComputeScreenPos(o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float4 col = tex2D(_MainTex, uv) * i.color;
                
                float pulse = sin(_Time.y * _PulseSpeed + uv.x * 10.0) * 0.5 + 0.5;
                float fade = 1.0 - uv.x;
                fade = pow(fade, _FadeSpeed);
                
                float3 emission = _EmissionColor.rgb * _Intensity * pulse * fade;
                float3 finalColor = _Color.rgb * col.rgb * fade + emission;
                
                float alpha = col.a * fade * (0.5 + pulse * 0.5);
                
                return fixed4(finalColor, alpha);
            }
            ENDCG
        }
    }
    FallBack Off
}