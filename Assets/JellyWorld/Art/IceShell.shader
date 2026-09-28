Shader "Jelly World/Downloaded Ice Shell"
{
    Properties
    {
        _IceTex ("Downloaded Ice001 color", 2D)="white"{}
        _IceNormal ("Downloaded Ice001 normal", 2D)="bump"{}
        _Roughness ("Downloaded Ice001 roughness", 2D)="gray"{}
        _Tint ("Ice tint", Color)=(0.43,0.81,1,1)
    }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent"}
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _IceTex,_IceNormal,_Roughness;
            fixed4 _Tint;
            struct appdata {float4 vertex:POSITION;float3 normal:NORMAL;};
            struct v2f {float4 vertex:SV_POSITION;float3 local:TEXCOORD0;float3 objectNormal:TEXCOORD1;float3 worldNormal:TEXCOORD2;float3 view:TEXCOORD3;};
            v2f vert(appdata v)
            {
                v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.local=v.vertex.xyz;o.objectNormal=v.normal;
                o.worldNormal=UnityObjectToWorldNormal(v.normal);o.view=_WorldSpaceCameraPos-mul(unity_ObjectToWorld,v.vertex).xyz;return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                float3 weights=pow(abs(normalize(i.objectNormal)),4);weights/=dot(weights,1);
                float3 p=i.local*.65+.5;
                float3 ice=tex2D(_IceTex,p.zy).rgb*weights.x+tex2D(_IceTex,p.xz).rgb*weights.y+tex2D(_IceTex,p.xy).rgb*weights.z;
                float frost=dot(ice,float3(.299,.587,.114));
                float crack=saturate((frost-.40)*2.8);
                float rough=tex2D(_Roughness,p.xy).r;
                float3 detail=tex2D(_IceNormal,p.xy).rgb*2-1;
                float rim=pow(1-saturate(dot(normalize(i.worldNormal),normalize(i.view))),1.4);
                float3 a=abs(i.local);float middle=a.x+a.y+a.z-max(a.x,max(a.y,a.z))-min(a.x,min(a.y,a.z));
                float edge=smoothstep(.35,.49,middle);
                float glint=pow(saturate(dot(normalize(detail+float3(-.3,.5,1.6)),float3(-.2,.4,.894))),24)*(1-rough)*.4;
                float3 color=lerp(_Tint.rgb,float3(.96,1,1),saturate(crack*.8+edge*.48+rim*.4))+glint;
                float alpha=saturate(.26+crack*.32+edge*.25+rim*.16);
                return float4(color,alpha);
            }
            ENDCG
        }
    }
}
