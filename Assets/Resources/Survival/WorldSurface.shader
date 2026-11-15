Shader "Berezki/WorldSurface" {
 Properties {_Color("Tint",Color)=(.5,.5,.4,1) _Surface("Surface",Float)=0}
 SubShader {
  Tags {"RenderType"="Opaque"} LOD 200
  CGPROGRAM
  #pragma surface surf Standard fullforwardshadows
  #pragma target 3.0
  fixed4 _Color;float _Surface;
  struct Input {float3 worldPos;float3 worldNormal; INTERNAL_DATA};
  float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
  float noise(float2 p){float2 c=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(c),hash(c+float2(1,0)),f.x),lerp(hash(c+float2(0,1)),hash(c+1),f.x),f.y);}
  void surf(Input i,inout SurfaceOutputStandard o){
   float3 p=i.worldPos;float3 n=abs(WorldNormalVector(i,float3(0,0,1)));
   float2 uv=n.y>.65?p.xz:n.x>n.z?p.zy:p.xy;
   float grain=noise(uv*35),patch=noise(uv*2.4),broad=noise(uv*.45);
   float3 c=_Color.rgb*(.85+.18*patch+.07*grain);
   float relief=0;
   if(_Surface<.5){
    float row=floor(uv.y/.23);float2 brick=frac(float2(uv.x/.52+fmod(row,2)*.5,uv.y/.23));
    float mortar=(1-smoothstep(.025,.07,min(brick.x,brick.y)));
    float chips=smoothstep(.62,.73,patch*.65+noise(uv*7)*.35)*(1-smoothstep(.4,2.7,p.y));
    c=lerp(c,float3(.28,.22,.17)*(1-mortar*.38),chips*.65);
    float damp=(1-smoothstep(.1,1.05,p.y))*(.35+broad*.35);
    c=lerp(c,c*float3(.57,.66,.55),damp);relief=grain*.12;
   }else if(_Surface<1.5){
    float seam=1-smoothstep(.018,.045,min(frac(uv.x/.23),1-frac(uv.x/.23)));
    float fibres=noise(float2(uv.x*115,uv.y*3));c*=.86+.2*fibres-seam*.3;relief=fibres*.15;
   }else if(_Surface<2.5){
    float seam=pow(abs(sin(p.z*8.5)),14);float rust=smoothstep(.56,.75,patch*.45+broad*.55);
    c=lerp(c,float3(.28,.19,.13),rust*.35);c*=1-seam*.22;relief=sin(p.z*8.5)*.06;
   }else{
    c*=.88+.16*broad;relief=grain*.12;
   }
   // Derivative bump adds shallow relief without increasing geometry or texture memory.
   float2 gradient=float2(ddx(relief),ddy(relief));o.Normal=normalize(float3(-gradient*.6,1));
   o.Albedo=max(c,.025);o.Metallic=_Surface>1.5&&_Surface<2.5?.1:0;o.Smoothness=.06;o.Occlusion=.87+.13*patch;o.Alpha=1;
  }
  ENDCG
 }
 Fallback "Standard"
}
