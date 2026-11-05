Shader "Berezki/Canopy" {
 SubShader {
  Tags {"RenderType"="Opaque"} Cull Off LOD 150
  CGPROGRAM
  #pragma surface surf Lambert vertex:vert addshadow
  #pragma target 3.0
  struct Input {float4 tint:COLOR;};
  void vert(inout appdata_full v,out Input o){
   UNITY_INITIALIZE_OUTPUT(Input,o);o.tint=v.color;
   float3 world=mul(unity_ObjectToWorld,v.vertex).xyz;
   v.vertex.x+=sin(_Time.y*1.4+world.x*2+world.z)*.045;
   v.vertex.z+=sin(_Time.y*1.1+world.z*2)*.028;
  }
  void surf(Input i,inout SurfaceOutput o){o.Albedo=i.tint.rgb;o.Emission=i.tint.rgb*.10;o.Alpha=1;}
  ENDCG
 }
 Fallback "Diffuse"
}
