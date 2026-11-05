Shader "Berezki/VergeGrass" {
 Properties { _Color("Tint",Color)=(.43,.49,.27,1) }
 SubShader {
  Tags {"RenderType"="Opaque"} Cull Off LOD 100
  CGPROGRAM
  #pragma surface surf Lambert vertex:vert addshadow
  #pragma target 3.0
  fixed4 _Color;
  struct Input {float4 tint:COLOR;};
  void vert(inout appdata_full v,out Input o){
   UNITY_INITIALIZE_OUTPUT(Input,o);o.tint=v.color;
   float3 world=mul(unity_ObjectToWorld,v.vertex).xyz;
   v.vertex.x+=sin(_Time.y*1.7+world.x*1.1+world.z)*.045*v.texcoord.y;
  }
  void surf(Input i,inout SurfaceOutput o){o.Albedo=i.tint.rgb*_Color.rgb;o.Emission=i.tint.rgb*_Color.rgb*.18;o.Alpha=1;}
  ENDCG
 }
 Fallback "Diffuse"
}
