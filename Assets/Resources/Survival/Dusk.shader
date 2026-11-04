Shader "Berezki/Dusk" {
 Properties { _MainTex("Scene",2D)="white"{} _Strength("Strength",Range(0,1))=.55 }
 SubShader { Cull Off ZWrite Off ZTest Always
  Pass { CGPROGRAM
   #pragma vertex vert_img
   #pragma fragment frag
   #include "UnityCG.cginc"
   sampler2D _MainTex; float _Strength;
   fixed4 frag(v2f_img i):SV_Target {
    fixed4 c=tex2D(_MainTex,i.uv);
    float l=dot(c.rgb,float3(.2126,.7152,.0722));
    c.rgb=lerp(c.rgb,l.xxx,.13*_Strength);
    float2 p=(i.uv-.5)*float2(1,.8);
    float vignette=smoothstep(.12,.6,dot(p,p));
    c.rgb*=1-vignette*.42*_Strength;
    return c;
   }
   ENDCG
  }
 }
 Fallback Off
}
