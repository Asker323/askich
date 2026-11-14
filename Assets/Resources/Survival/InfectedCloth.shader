Shader "Zonbee/InfectedCloth" {
 Properties {_MainTex("Fabric",2D)="white"{} _Color("Tint",Color)=(.34,.40,.34,1)}
 SubShader {Tags {"RenderType"="Opaque"}
 CGPROGRAM
 #pragma surface surf Standard fullforwardshadows vertex:vert
 #pragma target 3.0
 sampler2D _MainTex;fixed4 _Color;
 struct Input {float2 uv_MainTex;float3 anatomy;};
 void vert(inout appdata_full v,out Input o){UNITY_INITIALIZE_OUTPUT(Input,o);o.anatomy=v.texcoord2.xyz;}
 float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
 float noise(float2 p){float2 c=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(c),hash(c+float2(1,0)),f.x),lerp(hash(c+float2(0,1)),hash(c+1),f.x),f.y);}
 void surf(Input i,inout SurfaceOutputStandard o){
  float3 p=i.anatomy;float2 fabricPoint=float2(p.x+p.z*.7,p.y);float m=.65*noise(fabricPoint*8)+.35*noise(fabricPoint*23);
  float cuff=1-smoothstep(1.02,1.22,p.y);float stain=smoothstep(.49,.76,m);
  fixed3 fabric=tex2D(_MainTex,i.uv_MainTex).rgb*_Color.rgb;
  o.Albedo=lerp(fabric,fabric*float3(.48,.37,.24),stain*.55+cuff*.18);o.Smoothness=.035;o.Metallic=0;o.Alpha=1;
 }
 ENDCG
 }Fallback "Standard"
}
