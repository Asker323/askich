Shader "Zonbee/RoadPaint" {
 Properties {_Color("Paint",Color)=(.5,.48,.32,1)}
 SubShader {Tags {"RenderType"="TransparentCutout" "Queue"="AlphaTest"}
 CGPROGRAM
 #pragma surface surf Standard fullforwardshadows addshadow
 #pragma target 3.0
 fixed4 _Color;struct Input {float3 worldPos;};
 float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
 void surf(Input i,inout SurfaceOutputStandard o){float2 p=i.worldPos.xz;float grain=hash(floor(p*85));float wear=.5+.5*sin(p.x*21+sin(p.y*8))*sin(p.y*37);clip(grain+wear*.4-.25);o.Albedo=_Color.rgb*(.63+.19*grain);o.Smoothness=.04;o.Alpha=1;}
 ENDCG
 }Fallback "Diffuse"
}
