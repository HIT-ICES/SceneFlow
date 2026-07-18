Shader "Hidden/FinalCameraBlend"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "gray" {}
//        _LeftTex ("Texture", 2D) = "red" {}
//        _RightTex ("Texture", 2D) = "white" {}
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
            sampler2D _MainTex;
            sampler2D _LeftTex;
            sampler2D _RightTex;
            fixed4 frag(v2f i) : SV_Target
            {
                float depth = UNITY_SAMPLE_DEPTH(tex2D(_CameraDepthTexture, i.uv));
                depth = Linear01Depth(depth);

                if (depth == 1)
                {
                    if (unity_StereoEyeIndex == 0)
                    {
                        return tex2D(_LeftTex, i.uv);
                    }
                    else
                    {
                        return tex2D(_RightTex, i.uv);
                    }
                }

                return tex2D(_MainTex, i.uv);
            }
            ENDCG
        }
    }
}