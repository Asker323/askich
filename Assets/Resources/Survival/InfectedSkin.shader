Shader "Zonbee/InfectedSkin" {
 Properties {_MainTex("Skin",2D)="white"{} _Color("Tint",Color)=(.8,.84,.78,1)}
 SubShader {Tags {"RenderType"="Opaque"}
 CGPROGRAM
 #pragma surface surf Standard fullforwardshadows vertex:vert
 #pragma target 3.0
 sampler2D _MainTex;fixed4 _Color;
 struct Input {float2 uv_MainTex;float3 anatomy;};
 void vert(inout appdata_full v,out Input o){UNITY_INITIALIZE_OUTPUT(Input,o);o.anatomy=v.texcoord2.xyz;}
 float g(float p,float c,float w){return exp(-pow((p-c)/w,2));}
 void surf(Input i,inout SurfaceOutputStandard o){
  float3 p=i.anatomy;fixed3 skin=tex2D(_MainTex,i.uv_MainTex).rgb;float l=dot(skin,float3(.2126,.7152,.0722));skin=lerp(l.xxx,skin,.38)*_Color.rgb;
  float front=smoothstep(.025,.085,p.z);
  float sockets=g(abs(p.x),.030,.020)*g(p.y,1.695,.017)*front;
  float cheeks=g(abs(p.x),.052,.024)*g(p.y,1.652,.028)*front;
  float mouth=g(p.x,0,.033)*g(p.y,1.606,.010)*front;
  float bruise=g(p.x,-.052,.027)*g(p.y,1.67,.042)*front;
  skin=lerp(skin,skin*float3(.47,.44,.53),sockets*.65+cheeks*.18);
  skin=lerp(skin,float3(.18,.12,.13),mouth*.65+bruise*.18);
  float rough=.5+.5*sin(p.x*153+p.z*117)*sin(p.y*189);o.Albedo=skin*(.97+.05*rough);o.Smoothness=.09;o.Metallic=0;o.Alpha=1;
 }
 ENDCG
 }Fallback "Standard"
}
