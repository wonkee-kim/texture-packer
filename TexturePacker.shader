Shader "Hidden/TexturePacker"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _MetallicTex ("Metallic Texture", 2D) = "white" {}
        _SmoothnessTex ("Smoothness Texture", 2D) = "white" {}
        _AmbientOcclusionTex ("Ambient Occlusion Texture", 2D) = "white" {}

        _HasMetallic ("Has Metallic", Int) = 0
        _HasSmoothness ("Has Smoothness", Int) = 0
        _HasAO ("Has AmbientOcclusion", Int) = 0

        _MetallicDefault ("Metallic Default", Range(0,1)) = 0
        _SmoothnessDefault ("Smoothness Default", Range(0,1)) = 0.5

        _InvertSmoothness ("Invert Smoothness", Int) = 0

        // Below values should be matched with SurfaceData(enum) (0:metallic, 1:smoothness, 2:ao)
        _RedData ("Red Data", Int) = 0
        _GreenData ("Red Data", Int) = 1
        _BlueData ("Red Data", Int) = 2
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

            float4 _MainTex_ST;
            sampler2D _MainTex;
            sampler2D _MetallicTex;
            sampler2D _SmoothnessTex;
            sampler2D _AmbientOcclusionTex;

            float _MetallicDefault;
            float _SmoothnessDefault;

            int _HasMetallic;
            int _HasSmoothness;
            int _HasAO;

            int _InvertSmoothness;

            int _RedData;
            int _GreenData;
            int _BlueData;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float metallic = _MetallicDefault;
                if(_HasMetallic == 1)
                {
                    metallic = tex2D(_MetallicTex, i.uv).g;
                }
                
                float smoothness = _SmoothnessDefault;
                if(_HasSmoothness == 1)
                {
                    smoothness = tex2D(_SmoothnessTex, i.uv).g;
                    if(_InvertSmoothness == 1)
                    {
                        smoothness = 1.0 - smoothness;
                    }
                }

                float occlusion = 1.0;
                if(_HasAO == 1)
                {
                    occlusion = tex2D(_AmbientOcclusionTex, i.uv).g;
                }

                float4 result = (float4)1;

                // RED
                if(_RedData == 0)
                {
                    result.r = metallic;
                }
                else if(_RedData == 1)
                {
                    result.r = smoothness;
                }
                else if(_RedData == 2)
                {
                    result.r = occlusion;
                }

                // GREEN
                if(_GreenData == 0)
                {
                    result.g = metallic;
                }
                else if(_GreenData == 1)
                {
                    result.g = smoothness;
                }
                else if(_GreenData == 2)
                {
                    result.g = occlusion;
                }

                // BLUE
                if(_BlueData == 0)
                {
                    result.b = metallic;
                }
                else if(_BlueData == 1)
                {
                    result.b = smoothness;
                }
                else if(_BlueData == 2)
                {
                    result.b = occlusion;
                }

                return result;
            }
            ENDCG
        }
    }
}
