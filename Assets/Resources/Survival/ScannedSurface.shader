Shader "Zonbee/ScannedSurface" {
 Properties {_Color("Tint",Color)=(1,1,1,1) _MainTex("Colour",2D)="white"{} [Normal] _BumpMap("Normal",2D)="bump"{} _Roughness("Roughness",2D)="white"{} _Tiling("Metres to UV",Float)=.5 _BumpStrength("Relief",Range(0,1))=.65 _Damp("Ground damp",Range(0,1))=0 _Saturation("Saturation",Range(0,1))=1 _PlasterCover("Weathered plaster",Range(0,1))=0}
 SubShader {Tags {"RenderType"="Opaque"} LOD 250
 CGPROGRAM
 #pragma surface surf Standard fullforwardshadows
 #pragma target 3.0
 sampler2D _MainTex,_BumpMap,_Roughness;fixed4 _Color;float _Tiling,_BumpStrength,_Damp,_Saturation,_PlasterCover;
 struct Input {float3 worldPos;float3 worldNormal; INTERNAL_DATA};
 float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
 float noise(float2 p){float2 c=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(c),hash(c+float2(1,0)),f.x),lerp(hash(c+float2(0,1)),hash(c+1),f.x),f.y);}
 void surf(Input i,inout SurfaceOutputStandard o){
  float3 n=normalize(WorldNormalVector(i,float3(0,0,1))),a=abs(n);float2 uv;float3 u,v;
  if(a.y>=a.x&&a.y>=a.z){uv=i.worldPos.xz;u=float3(1,0,0);v=float3(0,0,1);}
  else if(a.x>a.z){uv=i.worldPos.zy;u=float3(0,0,1);v=float3(0,1,0);}
  else{uv=i.worldPos.xy;u=float3(1,0,0);v=float3(0,1,0);}
  float exposed=smoothstep(.50,.66,noise(uv*1.7)*.58+noise(uv*.31)*.18+(1-smoothstep(.1,1.4,i.worldPos.y))*.22);
  float plaster=_PlasterCover*(1-exposed);
  uv*=_Tiling;float3 detail=UnpackNormal(tex2D(_BumpMap,uv));detail.xy*=_BumpStrength*lerp(1,.15,plaster);
  float3 world=normalize(n*detail.z+u*detail.x+v*detail.y);
  o.Normal=float3(dot(world,WorldNormalVector(i,float3(1,0,0))),dot(world,WorldNormalVector(i,float3(0,1,0))),dot(world,n));
  float damp=_Damp*(1-smoothstep(.1,1.1,i.worldPos.y));
  float3 colour=tex2D(_MainTex,uv).rgb;colour=lerp(dot(colour,float3(.2126,.7152,.0722)).xxx,colour,_Saturation);
  colour=lerp(colour,float3(.66,.65,.60)*(.86+.16*noise(uv*5)),plaster);
  o.Albedo=colour*_Color.rgb*lerp(1,.73,damp);o.Smoothness=clamp(1-tex2D(_Roughness,uv).r,.02,.45);o.Metallic=0;o.Alpha=1;
 }
 ENDCG
 }Fallback "Standard"
}
