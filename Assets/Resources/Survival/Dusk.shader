Shader "Berezki/Dusk" {
 Properties { _MainTex("Scene",2D)="white"{} _Strength("Strength",Range(0,1))=.55 }
 SubShader { Cull Off ZWrite Off ZTest Always
  Pass { CGPROGRAM
   #pragma vertex vert_img
   #pragma fragment frag
   #pragma target 3.0
   #include "UnityCG.cginc"
   sampler2D _MainTex,_CameraDepthNormalsTexture; float _Strength;
   fixed4 frag(v2f_img i):SV_Target {
    fixed4 c=tex2D(_MainTex,i.uv);
    float depth;float3 normal;DecodeDepthNormal(tex2D(_CameraDepthNormalsTexture,i.uv),depth,normal);
    if(depth<.999){float ao=0;float radius=.40/max(1,unity_OrthoParams.y*2);float aspect=unity_OrthoParams.y/max(1,unity_OrthoParams.x);
     [unroll] for(int tap=0;tap<8;tap++){float angle=tap*2.399963;float2 offset=float2(cos(angle)*aspect,sin(angle))*radius*(.45+.55*(tap+1)/8.0);float other;float3 unused;DecodeDepthNormal(tex2Dlod(_CameraDepthNormalsTexture,float4(i.uv+offset,0,0)),other,unused);float delta=(depth-other)*_ProjectionParams.z;ao+=smoothstep(.025,.13,delta)*(1-smoothstep(.45,1.2,delta));}
     c.rgb*=1-ao*.026;
    }
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
