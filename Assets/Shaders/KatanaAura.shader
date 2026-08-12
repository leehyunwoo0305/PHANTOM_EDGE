Shader "Custom/KatanaAura"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Base Color", Color) = (0.2, 0.6, 1, 1)
        _EmissionColor ("Emission Color", Color) = (0.5, 1, 1, 1)
        _Intensity ("Intensity", Float) = 5.0
        _PulseSpeed ("Pulse Speed", Float) = 3.0
        _NoiseScale ("Noise Scale", Float) = 2.0
        _Distortion ("Distortion", Float) = 0.15
        _FresnelPower ("Fresnel Power", Float) = 3.0
        _FresnelIntensity ("Fresnel Intensity", Float) = 2.0
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
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldNormal : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
                float3 viewDir : TEXCOORD3;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float4 _EmissionColor;
            float _Intensity;
            float _PulseSpeed;
            float _NoiseScale;
            float _Distortion;
            float _FresnelPower;
            float _FresnelIntensity;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldNormal = normalize(mul((float3x3)unity_ObjectToWorld, v.normal));
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.viewDir = normalize(UnityWorldSpaceViewDir(o.worldPos));
                return o;
            }

            float3 hash33(float3 p) {
                p = frac(p * 0.3183099 + 0.1);
                p *= p * p * (3.0 - 2.0 * p);
                return p * 2.0 - 1.0;
            }

            float noise3d(float3 p) {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float n = i.x + i.y * 57.0 + 113.0 * i.z;
                return lerp(lerp(lerp(hash33(n + float3(0,0,0)).x, hash33(n + float3(1,0,0)).x, f.x),
                               lerp(hash33(n + float3(0,1,0)).x, hash33(n + float3(1,1,0)).x, f.x), f.y),
                           lerp(lerp(hash33(n + float3(0,0,1)).x, hash33(n + float3(1,0,1)).x, f.x),
                               lerp(hash33(n + float3(0,1,1)).x, hash33(n + float3(1,1,1)).x, f.x), f.y), f.z);
            }

            float fbm(float3 p, int octaves) {
                float value = 0.0;
                float amplitude = 0.5;
                for (int i = 0; i < octaves; i++) {
                    value += amplitude * noise3d(p);
                    p *= 2.0;
                    amplitude *= 0.5;
                }
                return value;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 viewDir = normalize(i.viewDir);
                float3 normal = normalize(i.worldNormal);
                
                float fresnel = pow(1.0 - abs(dot(viewDir, normal)), _FresnelPower);
                
                float3 noisePos = i.worldPos * _NoiseScale + _Time.y * 0.5;
                float n = fbm(noisePos, 4);
                
                float pulse = sin(_Time.y * _PulseSpeed + i.worldPos.y * 2.0) * 0.5 + 0.5;
                
                float distortion = n * _Distortion;
                float2 distUv = i.uv + distortion * 0.1;
                
                float3 baseColor = _Color.rgb * (0.5 + pulse * 0.5);
                float3 emission = _EmissionColor.rgb * _Intensity * (fresnel * _FresnelIntensity + n * 0.5 + pulse * 0.5);
                
                float alpha = saturate(fresnel * 2.0 + n * 0.3 + pulse * 0.3);
                
                return fixed4(baseColor + emission, alpha);
            }
            ENDCG
        }
    }
    FallBack Off
}