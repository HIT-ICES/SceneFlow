Shader "Hidden/ExtractDepth"
{
    Properties {}
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local __ DISABLE_LINEARIZE_DEPTH

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

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D_float _CameraDepthTexture;
            fixed4 frag(v2f i) : SV_Target
            {
                float depth = UNITY_SAMPLE_DEPTH(tex2D(_CameraDepthTexture, i.uv));

                #if !DISABLE_LINEARIZE_DEPTH
                depth = Linear01Depth(depth);
                #else
                #if defined(UNITY_REVERSED_Z)
                depth = 1.0f - depth; //d3d, metal to do it
                #endif
                #endif

                fixed4 res;
                res.r = res.g = res.b = depth;
                res.a = 1;
                return res;
                // fixed4 col = tex2D(_MainTex, i.uv);
                // // just invert the colors
                // col = 1 - col;
                // return col;
            }
            ENDCG
        }
    }
}