Shader "Jelly World/Sugar Beam"
{
    Properties { _Color ("Glow", Color) = (1,0.58,0.16,1) _Opacity ("Opacity", Float)=1 }
    SubShader
    {
        Tags {"Queue"="Transparent+20" "RenderType"="Transparent"}
        Blend SrcAlpha One
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; };
            fixed4 _Color;float _Opacity;
            v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;return o;}
            fixed4 frag(v2f i):SV_Target
            {
                float d=abs(i.uv.y-.5)*2;
                float halo=exp(-d*d*4.5);
                float core=exp(-d*d*100);
                float ray=1+.12*sin(i.uv.x*90-_Time.y*55);
                return float4((_Color.rgb*halo*1.7+float3(1,1,1)*core*2.5)*ray,halo*_Opacity);
            }
            ENDCG
        }
    }
}
