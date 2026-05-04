Shader "Custom/EyepieceMaskUnlit"
{
    Properties
    {
        _Radius ("Radius", Range(0,1)) = 0.5
        _Feather ("Feather", Range(0,0.5)) = 0.05
        _Color ("Color", Color) = (0,0,0,1)
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float _Radius;
            float _Feather;
            float4 _Color;

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                float2 center = float2(0.5, 0.5);
                float2 uv = i.uv - center;

                // correct for aspect ratio
                float aspect = _ScreenParams.x / _ScreenParams.y;
                uv.x *= aspect;

                float dist = length(uv);

                float circle = smoothstep(_Radius, _Radius - _Feather, dist);

                // OUTSIDE = visible black
                float3 color = _Color.rgb;

                // alpha: outside opaque, inside transparent
                float alpha = 1.0 - circle;

                return float4(color, alpha);
            }

            ENDHLSL
        }
    }
}