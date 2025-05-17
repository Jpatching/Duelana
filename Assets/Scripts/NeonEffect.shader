Shader "Hidden/NeonEffect"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _NeonColor ("Neon Color", Color) = (0.6, 0.2, 1.0, 1.0)
        _NeonStrength ("Neon Strength", Range(0, 1)) = 0.5
        _BloomIntensity ("Bloom Intensity", Range(0, 5)) = 1.5
        _BloomThreshold ("Bloom Threshold", Range(0, 10)) = 0.85
        _ShadowsTint ("Shadows Tint", Color) = (0.1, 0.0, 0.2, 1.0)
        _HighlightsTint ("Highlights Tint", Color) = (0.8, 0.5, 1.0, 1.0)
        _Contrast ("Contrast", Range(0, 2)) = 1.1
        _Saturation ("Saturation", Range(0, 2)) = 1.2
        _ChromaticStrength ("Chromatic Aberration", Range(0, 5)) = 1.0
        _EnableChromatic ("Enable Chromatic", Int) = 1
        _EnableScanLines ("Enable Scan Lines", Int) = 1
        _ScanLineIntensity ("Scan Line Intensity", Range(0, 1)) = 0.2
        _ScanLineDensity ("Scan Line Density", Range(50, 500)) = 200
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
            #pragma target 3.0
            
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
            float4 _MainTex_TexelSize;
            float4 _NeonColor;
            float _NeonStrength;
            float _BloomIntensity;
            float _BloomThreshold;
            int _BloomIterations;
            float4 _ShadowsTint;
            float4 _HighlightsTint;
            float _Contrast;
            float _Saturation;
            float _ChromaticStrength;
            int _EnableChromatic;
            int _EnableScanLines;
            float _ScanLineIntensity;
            float _ScanLineDensity;
            
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            
            // Helper function for luminance
            float Luminance(float3 color)
            {
                return dot(color, float3(0.299, 0.587, 0.114));
            }
            
            // Bloom effect
            float3 Bloom(sampler2D tex, float2 uv, float threshold)
            {
                float3 bloom = float3(0, 0, 0);
                float2 texelSize = _MainTex_TexelSize.xy;
                
                // Extract bright areas
                float3 color = tex2D(tex, uv).rgb;
                float brightness = Luminance(color);
                float contribution = max(0, brightness - threshold);
                contribution /= (brightness + 0.00001); // Prevent division by zero
                bloom = color * contribution;
                
                // Blur horizontally and vertically
                float totalWeight = 1.0;
                float weight;
                float totalWeightBlur = 0.0;
                float3 blurColor = float3(0, 0, 0);
                
                for (int i = 1; i <= _BloomIterations; i++)
                {
                    weight = 1.0 / (i * 2.0);
                    totalWeight += weight * 4.0;
                    
                    // Sample 4 directions
                    blurColor += tex2D(tex, uv + float2(texelSize.x * i, 0)).rgb * weight;
                    blurColor += tex2D(tex, uv - float2(texelSize.x * i, 0)).rgb * weight;
                    blurColor += tex2D(tex, uv + float2(0, texelSize.y * i)).rgb * weight;
                    blurColor += tex2D(tex, uv - float2(0, texelSize.y * i)).rgb * weight;
                    totalWeightBlur += weight * 4.0;
                }
                
                blurColor /= totalWeightBlur;
                blurColor = max(0, blurColor - threshold) * _BloomIntensity;
                
                return bloom + blurColor;
            }
            
            // Chromatic aberration
            float3 ChromaticAberration(sampler2D tex, float2 uv)
            {
                if (_EnableChromatic == 0)
                    return tex2D(tex, uv).rgb;
                
                float amount = _ChromaticStrength * 0.001;
                float2 direction = normalize(uv - 0.5);
                
                float3 result;
                result.r = tex2D(tex, uv - direction * amount).r;
                result.g = tex2D(tex, uv).g;
                result.b = tex2D(tex, uv + direction * amount).b;
                
                return result;
            }
            
            // Scan lines effect
            float ScanLines(float2 uv)
            {
                if (_EnableScanLines == 0)
                    return 1.0;
                
                float scanLine = sin(uv.y * _ScanLineDensity) * 0.5 + 0.5;
                return lerp(1.0, scanLine, _ScanLineIntensity);
            }
            
            fixed4 frag(v2f i) : SV_Target
            {
                // Apply chromatic aberration
                float3 col = ChromaticAberration(_MainTex, i.uv);
                
                // Extract bloom
                float3 bloomColor = Bloom(_MainTex, i.uv, _BloomThreshold);
                
                // Apply color grading and contrast
                float luma = Luminance(col);
                float3 colorGraded = lerp(_ShadowsTint.rgb, _HighlightsTint.rgb, pow(luma, 1.0 / _Contrast));
                col = lerp(col, colorGraded, 0.5);
                
                // Apply scan lines
                float scanLineEffect = ScanLines(i.uv);
                col *= scanLineEffect;
                
                // Apply bloom and neon glow
                col += bloomColor;
                col += _NeonColor.rgb * _NeonStrength * bloomColor;
                
                // Apply saturation
                float3 grayscale = float3(Luminance(col), Luminance(col), Luminance(col));
                col = lerp(grayscale, col, _Saturation);
                
                return float4(col, 1.0);
            }
            ENDCG
        }
    }
}
