Shader "Berezki/Weathered" {
 Properties {
  _MainTex("Atlas",2D)="white"{}
  _Color("Tint",Color)=(.9,.9,.85,1)
  _Saturation("Saturation",Range(0,1))=.42
  _Glossiness("Smoothness",Range(0,1))=.16
  _Decay("Decay",Range(0,1))=0
  _Variant("Variation",Float)=0
  _PatternHeight("Mesh height",Float)=1
  _PatternBase("Mesh base",Float)=0
 }
 SubShader {
  Tags {"RenderType"="Opaque"}
  LOD 200
  CGPROGRAM
  #pragma surface surf Standard fullforwardshadows vertex:vert
  #pragma target 3.0
  sampler2D _MainTex; fixed4 _Color; half _Saturation,_Glossiness,_Decay;float _Variant,_PatternHeight,_PatternBase;
  struct Input {float2 uv_MainTex;float3 objectPosition;};
  void vert(inout appdata_full v,out Input o){UNITY_INITIALIZE_OUTPUT(Input,o);o.objectPosition=v.vertex.xyz;}
  float hash(float3 p){return frac(sin(dot(p,float3(127.1,311.7,74.7)))*43758.5453);}
  float noise(float3 p){float3 c=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(lerp(hash(c),hash(c+float3(1,0,0)),f.x),lerp(hash(c+float3(0,1,0)),hash(c+float3(1,1,0)),f.x),f.y),lerp(lerp(hash(c+float3(0,0,1)),hash(c+float3(1,0,1)),f.x),lerp(hash(c+float3(0,1,1)),hash(c+1),f.x),f.y),f.z);}

  void surf(Input i,inout SurfaceOutputStandard o){
   fixed3 c=tex2D(_MainTex,i.uv_MainTex).rgb;
   half l=dot(c,half3(.2126,.7152,.0722));
   half wear=.94+.06*sin(i.uv_MainTex.x*113)*sin(i.uv_MainTex.y*97);
   float3 p=i.objectPosition/max(.01,_PatternHeight);p.y-=_PatternBase/max(.01,_PatternHeight);
   float dirt=noise(p*28+_Variant*3),mottle=noise(p*8+_Variant);
   float3 decayed=lerp(l.xxx,c,.22)*lerp(float3(.65,.72,.59),float3(.69,.63,.56),frac(_Variant*.37));
   float stain=smoothstep(.57,.76,mottle)*smoothstep(.25,.45,p.y)*(1-smoothstep(.82,.96,p.y));
   decayed=lerp(decayed,float3(.20,.075,.06),stain*.68);
   o.Albedo=lerp(lerp(l.xxx,c,_Saturation),decayed*(.75+.35*dirt),_Decay)*_Color.rgb*wear;
   o.Metallic=0;o.Smoothness=_Glossiness;o.Alpha=1;
  }
  ENDCG
 }
 Fallback "Standard"
}
