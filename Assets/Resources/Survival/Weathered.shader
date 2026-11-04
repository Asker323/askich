Shader "Berezki/Weathered" {
 Properties {
  _MainTex("Atlas",2D)="white"{}
  _Color("Tint",Color)=(.9,.9,.85,1)
  _Saturation("Saturation",Range(0,1))=.42
  _Glossiness("Smoothness",Range(0,1))=.16
 }
 SubShader {
  Tags {"RenderType"="Opaque"}
  LOD 200
  CGPROGRAM
  #pragma surface surf Standard fullforwardshadows
  #pragma target 3.0
  sampler2D _MainTex; fixed4 _Color; half _Saturation,_Glossiness;
  struct Input {float2 uv_MainTex;};
  void surf(Input i,inout SurfaceOutputStandard o){
   fixed3 c=tex2D(_MainTex,i.uv_MainTex).rgb;
   half l=dot(c,half3(.2126,.7152,.0722));
   half wear=.94+.06*sin(i.uv_MainTex.x*113)*sin(i.uv_MainTex.y*97);
   o.Albedo=lerp(l.xxx,c,_Saturation)*_Color.rgb*wear;
   o.Metallic=0;o.Smoothness=_Glossiness;o.Alpha=1;
  }
  ENDCG
 }
 Fallback "Standard"
}
