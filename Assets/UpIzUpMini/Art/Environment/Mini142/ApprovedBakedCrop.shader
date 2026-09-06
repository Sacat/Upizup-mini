Shader "UpIzUpMini/ApprovedBakedCrop"
{
    Properties
    {
        _Color("Stage tint",Color)=(1,1,1,1)
        _MainTex("Baked albedo",2D)="white"{}
        _BumpMap("Baked normal",2D)="bump"{}
        _TintStrength("Stage tint strength",Range(0,1))=.55
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Off
        CGPROGRAM
        #pragma surface surf Lambert fullforwardshadows
        #pragma target 3.0
        sampler2D _MainTex,_BumpMap;fixed4 _Color;half _TintStrength;
        struct Input { float2 uv_MainTex; float2 uv_BumpMap; };
        void surf(Input IN,inout SurfaceOutput o)
        {
            o.Albedo=tex2D(_MainTex,IN.uv_MainTex).rgb*lerp(fixed3(1,1,1),_Color.rgb,_TintStrength);
            o.Normal=UnpackNormal(tex2D(_BumpMap,IN.uv_BumpMap));o.Alpha=1;
        }
        ENDCG
    }
    Fallback "Diffuse"
}
