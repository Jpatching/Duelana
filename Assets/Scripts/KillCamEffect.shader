Shader "Hidden/KillCamEffect"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _ColorOverlay ("Color Overlay", Color) = (0.8, 0.2, 0.2, 0.3)
        _Saturation ("Saturation", Range(0, 1)) = 0.5
        _VignetteIntensity ("Vignette Intensity", Range(0, 1)) = 0.5
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            
            sampler2D _MainTex;
            float4 _ColorOverlay;
            float _Saturation;
            float _VignetteIntensity;

            // Helper function to convert RGB to luminance
            float Luminance(float3 color)
            {
                return dot(color, float3(0.2126, 0.7152, 0.0722));
            }
            
            // Vignette effect
            float Vignette(float2 uv)
            {
                // Calculate distance from center (0.5, 0.5)
                float2 dist = (uv - 0.5) * 1.25;
                return saturate(1.0 - dot(dist, dist) * _VignetteIntensity * 4);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Sample original image
                fixed4 col = tex2D(_MainTex, i.uv);
                
                // Calculate luminance (grayscale value)
                float lum = Luminance(col.rgb);
                
                // Mix between original color and grayscale based on saturation
                float3 desaturated = lerp(float3(lum, lum, lum), col.rgb, _Saturation);
                
                // Apply vignette effect
                float vignette = Vignette(i.uv);
                desaturated *= vignette;
                
                // Add color overlay
                float3 result = lerp(desaturated, _ColorOverlay.rgb, _ColorOverlay.a);
                
                return fixed4(result, col.a);
            }
            ENDCG
        }
    }
}
