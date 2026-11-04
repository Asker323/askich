Shader "Berezki/AttackCue" {
 Properties { _Color("Warning", Color)=(1,.55,.15,.9) }
 SubShader {
  Tags {"Queue"="Transparent" "RenderType"="Transparent"}
  Pass {
   ZWrite Off Cull Off Blend SrcAlpha OneMinusSrcAlpha
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   struct Input {float4 vertex:POSITION;};
   struct Output {float4 position:SV_POSITION;};
   fixed4 _Color;
   Output vert(Input v){Output o;o.position=UnityObjectToClipPos(v.vertex);return o;}
   fixed4 frag(Output i):SV_Target{return _Color;}
   ENDCG
  }
 }
 Fallback "Unlit/Color"
}
