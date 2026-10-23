Shader "Sunset/City"
{
    Properties { _Color ("Color", Color) = (1,1,1,1) _Glossiness ("Smoothness", Range(0,1)) = 0.1 }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 150
        CGPROGRAM
        #pragma surface surf Lambert addshadow
        #pragma target 3.0
        fixed4 _Color;
        struct Input { float3 worldPos; };
        void surf(Input input, inout SurfaceOutput output) { output.Albedo = _Color.rgb; output.Alpha = _Color.a; }
        ENDCG
    }
    Fallback "Diffuse"
}
