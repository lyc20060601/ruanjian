Shader "Jelly World/Soft Button"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct appdata { float4 vertex:POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; float4 local:TEXCOORD1; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 vertex:SV_POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0; float4 world:TEXCOORD1; float4 local:TEXCOORD2; UNITY_VERTEX_OUTPUT_STEREO };
            sampler2D _MainTex;
            fixed4 _Color, _TextureSampleAdd;
            float4 _ClipRect;
            v2f vert(appdata v)
            {
                v2f o;UNITY_SETUP_INSTANCE_ID(v);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.world=v.vertex;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color*_Color;o.local=v.local;return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                fixed4 color=(tex2D(_MainTex,i.uv)+_TextureSampleAdd)*i.color;
                // Extra mesh layers have zero local dimensions and retain their shadow/edge color.
                if(i.local.z>1 && i.local.w>1)
                {
                    float2 size=i.local.zw;
                    float2 p=(i.local.xy-.5)*size;
                    float radius=size.y*.5;
                    float2 core=max(float2(0,0),size*.5-radius);
                    float2 delta=p-clamp(p,-core,core);
                    float distance=length(delta);
                    float2 outward=delta/max(distance,.001);
                    float inside=radius-distance;
                    float bevel=saturate(1-inside/clamp(size.y*.20,4,12));
                    float3 normal=normalize(float3(outward*bevel*.82,sqrt(max(.12,1-bevel*bevel*.67))));
                    float light=dot(normal,normalize(float3(-.35,.65,.90)));
                    color.rgb*=.86+.18*light;
                    float shine=pow(saturate(dot(normal,normalize(float3(-.18,.42,.94)))),18)*.19;
                    color.rgb=lerp(color.rgb,float3(1,1,1),shine);
                }
                #ifdef UNITY_UI_CLIP_RECT
                color.a*=UnityGet2DClipping(i.world.xy,_ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a-.001);
                #endif
                return color;
            }
            ENDCG
        }
    }
}
