Shader "UpIzUpMini/Lalay Tool Colorway"
{
    Properties
    {
        _MainTex ("Source Texture", 2D) = "white" {}
        _SlideColor ("Slide Color", Color) = (0.35, 0.35, 0.35, 1)
        _FrameColor ("Frame Color", Color) = (0.35, 0.35, 0.35, 1)
        _AccentColor ("Accent Color", Color) = (0.2, 0.55, 0.55, 1)
        _CutY ("Slide Split", Range(-0.03, 0.04)) = 0.015
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert
        #pragma target 3.0

        sampler2D _MainTex;
        fixed4 _SlideColor;
        fixed4 _FrameColor;
        fixed4 _AccentColor;
        float _CutY;

        struct Input
        {
            float2 uv_MainTex;
            float3 localPosition;
        };

        void vert(inout appdata_full vertex, out Input output)
        {
            UNITY_INITIALIZE_OUTPUT(Input, output);
            output.localPosition = vertex.vertex.xyz;
        }

        void surf(Input input, inout SurfaceOutputStandard output)
        {
            fixed3 source = tex2D(_MainTex, input.uv_MainTex).rgb;
            float slide = smoothstep(_CutY - 0.003, _CutY + 0.003, input.localPosition.y);
            fixed3 tint = lerp(_FrameColor.rgb, _SlideColor.rgb, slide);
            fixed3 color = saturate(source * tint * lerp(2.0, 1.15, slide));
            float accent = step(source.r * 1.35 + 0.02, source.g)
                * step(source.r * 1.2 + 0.02, source.b)
                * step(0.18, source.g);
            output.Albedo = lerp(color, _AccentColor.rgb * source.g, accent);
            output.Metallic = 0.06;
            output.Smoothness = 0.32;
            output.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
