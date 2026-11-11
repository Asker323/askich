Shader "Zonbee/WindowFabric" {
 Properties { _Color("Faded fabric",Color)=(.58,.54,.43,1) }
 SubShader {
  Tags {"RenderType"="Opaque"}
  Cull Off
  CGPROGRAM
  #pragma surface surf Standard fullforwardshadows
  #pragma target 3.0
  fixed4 _Color;
  struct Input {float3 worldPos;float facing:VFACE;};
  void surf(Input i,inout SurfaceOutputStandard o){float weave=sin(i.worldPos.y*470)*sin(i.worldPos.z*420);float stain=.5+.5*sin(i.worldPos.y*13+sin(i.worldPos.z*25));float hem=1-smoothstep(.65,.78,i.worldPos.y);o.Albedo=_Color.rgb*(.88+.07*weave-.12*stain-hem*.12);o.Normal=float3(0,0,i.facing>=0?1:-1);o.Smoothness=.04;o.Occlusion=.85;o.Alpha=1;}
  ENDCG
 }
 Fallback "Standard"
}
