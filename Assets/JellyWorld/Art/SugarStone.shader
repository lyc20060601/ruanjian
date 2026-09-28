Shader "Jelly World/Sugar Stone"
{
    Properties
    {
        _Color ("Stone color", Color) = (.48,.57,.63,1)
        _Glossiness ("Smoothness", Range(0,1)) = .25
        _Metallic ("Metallic", Range(0,1)) = .05
        _EmissionColor ("Emission", Color) = (0,0,0,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        struct Input { float3 worldPos; };
        fixed4 _Color, _EmissionColor;
        half _Glossiness, _Metallic;
        float hash(float3 p) {return frac(sin(dot(p,float3(127.1,311.7,74.7)))*43758.5453);}
        void surf(Input IN,inout SurfaceOutputStandard o)
        {
            float grain=hash(floor(IN.worldPos*65));
            float fleck=step(.986,grain);
            o.Albedo=_Color.rgb*(.93+grain*.10)+fleck*.045;
            o.Metallic=_Metallic; o.Smoothness=_Glossiness;
            o.Emission=_EmissionColor.rgb;o.Alpha=1;
        }
        ENDCG
    }
    FallBack "Standard"
}
