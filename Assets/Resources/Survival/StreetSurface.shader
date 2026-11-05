Shader "Berezki/StreetSurface" {
 Properties { _Color("Tint",Color)=(1,1,1,1) }
 SubShader {
  Tags {"RenderType"="Opaque"} LOD 150
  CGPROGRAM
  #pragma surface surf Standard fullforwardshadows vertex:vert
  #pragma target 3.0
  fixed4 _Color;
  struct Input {float3 worldPos;float4 tint:COLOR;};
  float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
  float noise(float2 p){float2 a=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(a),hash(a+float2(1,0)),f.x),lerp(hash(a+float2(0,1)),hash(a+1),f.x),f.y);}
  void vert(inout appdata_full v,out Input o){UNITY_INITIALIZE_OUTPUT(Input,o);o.tint=v.color;}
  void surf(Input i,inout SurfaceOutputStandard o){
   float2 p=i.worldPos.xz;
   float grit=noise(p*48)*.12+noise(p*4)*.12+noise(p*.6)*.13;
   o.Albedo=i.tint.rgb*_Color.rgb*(.73+grit);
   o.Smoothness=.08;o.Metallic=0;o.Alpha=1;
  }
  ENDCG
 }
 Fallback "Standard"
}
