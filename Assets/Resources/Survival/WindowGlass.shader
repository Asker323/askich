Shader "Zonbee/WindowGlass" {
 Properties { _Color("Glass tint",Color)=(.46,.62,.65,.24) }
 SubShader {
  Tags {"Queue"="Transparent" "RenderType"="Transparent"}
  Blend SrcAlpha OneMinusSrcAlpha
  ZWrite Off
  Cull Off
  Pass {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   fixed4 _Color;
   struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float3 world:TEXCOORD1;float3 normal:TEXCOORD2;};
   v2f vert(appdata_base v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.texcoord;o.world=mul(unity_ObjectToWorld,v.vertex).xyz;o.normal=UnityObjectToWorldNormal(v.normal);return o;}
   fixed4 frag(v2f i):SV_Target {
    float facing=abs(dot(normalize(_WorldSpaceCameraPos-i.world),normalize(i.normal)));
    float edge=1-smoothstep(0,.13,min(min(i.uv.x,1-i.uv.x),min(i.uv.y,1-i.uv.y)));
    float streak=pow(.5+.5*sin(i.uv.x*139+sin(i.uv.y*8)*.5),18)*.10;
    float sheen=pow(1-facing,3)*.35;
    float reflection=smoothstep(.015,0,abs(i.uv.x*.55+i.uv.y-.77))*.15;
    return fixed4(lerp(_Color.rgb,float3(.22,.24,.20),edge*.65)+sheen+reflection,_Color.a+edge*.24+sheen+streak);
   }
   ENDCG
  }
 }
}
