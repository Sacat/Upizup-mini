Shader "UpIzUpMini/ApprovedArtDiffuse"
{
 Properties { _MainTex ("Shared colour atlas", 2D)="white" {} _Color ("Tint",Color)=(1,1,1,1) }
 SubShader {
 Tags { "RenderType"="Opaque" } LOD 150 Cull Off
 CGPROGRAM
 #pragma surface surf Lambert fullforwardshadows
 #pragma target 3.0
 sampler2D _MainTex; fixed4 _Color;
 struct Input { float2 uv_MainTex; };
 void surf(Input IN,inout SurfaceOutput o) { fixed4 c=tex2D(_MainTex,IN.uv_MainTex)*_Color; o.Albedo=c.rgb;o.Alpha=1; }
 ENDCG
 }
 Fallback "Diffuse"
}

