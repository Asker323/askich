Shader "Zonbee/HeroCloth" {
 Properties {_Color("Fabric",Color)=(.20,.25,.27,1)}
 SubShader {Tags {"RenderType"="Opaque"}
 CGPROGRAM
 #pragma surface surf Standard fullforwardshadows vertex:vert
 #pragma target 3.0
 fixed4 _Color;
 struct Input {float3 anatomy;};
 void vert(inout appdata_full v,out Input o){UNITY_INITIALIZE_OUTPUT(Input,o);o.anatomy=v.texcoord2.xyz;}
 void surf(Input i,inout SurfaceOutputStandard o){float3 p=i.anatomy;float waist=smoothstep(.986,1.0,p.y);float hem=1-smoothstep(.677,.69,p.y);float seam=exp(-pow((abs(p.x)-.205)/.003,2));float weave=sin(p.y*850)*sin(p.x*900+p.z*500);float crease=sin(p.y*92+abs(p.x)*33)*.035;float stitch=(waist+hem)*pow(.5+.5*sin(p.x*700+p.z*350),5)*.12;o.Albedo=_Color.rgb*(.92+.045*weave+crease-.30*waist-.20*hem-.17*seam+stitch);o.Smoothness=.055;o.Metallic=0;o.Occlusion=.92;o.Alpha=1;}
 ENDCG
 }Fallback "Standard"
}
