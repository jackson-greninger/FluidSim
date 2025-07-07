Shader "Custom/CircleShader"
{
  Properties
  {
    _Color   ("Tint", Color) = (1,1,1,1)
    _MainTex ("Texture", 2D) = "white" {}
  }
  SubShader
  {
    Tags { "Queue"="Transparent" "RenderType"="Transparent" }
    Blend SrcAlpha OneMinusSrcAlpha
    Pass
    {
      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag
      #include "UnityCG.cginc"

      struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
      struct v2f    { float2 uv : TEXCOORD0; float4 pos : SV_POSITION; };

      fixed4 _Color;
      sampler2D _MainTex;

      v2f vert(appdata v) {
        v2f o;
        o.uv  = v.uv;
        o.pos = UnityObjectToClipPos(v.vertex);
        return o;
      }

      fixed4 frag(v2f i) : SV_Target {
        float2 d = (i.uv - 0.5) * 2;          // remap uv to -1..1
        float  r2 = dot(d,d);
        float  a  = smoothstep(1, 0.9, r2);   // fade edge
        fixed4 col = tex2D(_MainTex, i.uv) * _Color;
        col.a *= a;
        return col;
      }
      ENDCG
    }
  }
}
