Shader "IdleRealms/RadialFillShader"
{
    Properties
    {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _Color ("Main Color", Color) = (.5,.5,.5,1)
        _FillAmount ("Fill Amount", Range(0, 1)) = 0.5
        _InnerRadius ("Inner Radius", Range(0, 1)) = 0.7
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" }
        LOD 100

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha // Enable alpha blending

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR0;
            };

            sampler2D _MainTex;
            float4 _Color;
            float _FillAmount;
            float _InnerRadius;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // Sample the texture
                fixed4 col = tex2D(_MainTex, i.uv) * i.color;

                // Compute the angle and distance from the center of the sprite
                float2 fromCenter = i.uv - float2(0.5, 0.5);
                float angle = atan2(-fromCenter.x, -fromCenter.y) / (2 * 3.14159265359); 
                angle = angle + 0.5;

                float distanceFromCenter = length(fromCenter) * 2;

                // Compare against the fill amount and inner radius
                if (angle > _FillAmount || distanceFromCenter < _InnerRadius || distanceFromCenter > 1)
                    discard; // Discard the pixel

                return col;
            }
            ENDCG
        }
    }
}
