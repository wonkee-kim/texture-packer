Shader "Hidden/TexturePacker(Terrain)"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BaseMap ("Albedo Texture", 2D) = "white" {}
        _SmoothnessTex ("Smoothness Texture", 2D) = "white" {}

        _HasSmoothness ("Has Smoothness", Int) = 0
        _SmoothnessDefault ("Smoothness Default", Range(0,1)) = 0.5
        _InvertSmoothness ("Invert Smoothness", Int) = 0
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
            sampler2D _BaseMap;
            sampler2D _SmoothnessTex;

            int _HasSmoothness;
            float _SmoothnessDefault;
            int _InvertSmoothness;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {    
                float smoothness = _SmoothnessDefault;
                if(_HasSmoothness == 1)
                {
                    smoothness = tex2D(_SmoothnessTex, i.uv).a; // TODO: channel selector
                    if(_InvertSmoothness == 1)
                    {
                        smoothness = 1.0 - smoothness;
                    }
                }

                float4 result = tex2D(_BaseMap, i.uv);
                result.a = smoothness;

                return result;
            }
            ENDCG
        }
    }
}
