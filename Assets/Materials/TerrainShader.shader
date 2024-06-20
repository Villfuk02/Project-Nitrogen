Shader"Custom/TerrainShader"
{
    Properties
    {
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _LowColor ("Lowest Base Color", Color) = (1, 1, 1, 1)
        _HighColor ("Highest Base Color", Color) = (1, 1, 1, 1)
        _MaxHeight ("Maximum Height", float) = 2
        _VisLevels ("Visualization Levels", int) = 6
        _Color1 ("Selected Color", Color) = (1, 1, 1, 1)
        _Color2 ("Negative Color", Color) = (1, 1, 1, 1)
        _Color3 ("Affected Color", Color) = (1, 1, 1, 1)
        _Color4 ("Special Color", Color) = (1, 1, 1, 1)
        _Color5 ("Hovered Color", Color) = (1, 1, 1, 1)
        [ShowAsVector2] _Offset ("World Position Offset", Vector) = (0, 0, 0, 0)
        [ShowAsVector2] _WorldSize ("World Size", Vector) = (16, 16, 0, 0)
        _OutlineRadius ("Outline radius", float) = 0.1
        _BaseAlphaMultiplier ("Base alpha multiplier", Range(0,1)) = 0.2
        [NoScaleOffset] _HighlightMap ("Highlight Map", 2D) = "black" {}
    }
    SubShader 
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200

        CGPROGRAM

        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard addshadow fullforwardshadows

        #pragma target 5.0

#include "UnityCG.cginc"

struct Input
{
    float3 worldPos;
};
        
half _Glossiness;
half _Metallic;
fixed4 _LowColor;
fixed4 _HighColor;
fixed4 _Color1;
fixed4 _Color2;
fixed4 _Color3;
fixed4 _Color4;
fixed4 _Color5;
float2 _Offset;
float2 _WorldSize;
float _OutlineRadius;
float _BaseAlphaMultiplier;
sampler2D _HighlightMap;
float _MaxHeight;
int _VisLevels;

uint dataAt(float2 pos)
{
    uint mip = _VisLevels;
    uint r = 255;
    while (r >= 200 && mip >= 0)
    {
        r = tex2Dlod(_HighlightMap, float4(pos / _WorldSize, 0, mip)).r * 255;
        mip--;
    }
    return r;    
}

bool consistent(float2 pos, float size, uint data)
{
    if (dataAt(pos + float2(size, 0)) != data)
    {
        return false;
    }
    if (dataAt(pos + float2(-size, 0)) != data)
    {
        return false;
    }
    if (dataAt(pos + float2(0, size)) != data)
    {
        return false;
    }
    if (dataAt(pos + float2(0, -size)) != data)
    {
        return false;
    }
    return true;
}

void surf(Input IN, inout SurfaceOutputStandard o)
{
    static const fixed4 colors[6] = { fixed4(0, 0, 0, 0), _Color1, _Color2, _Color3, _Color4, _Color5 };
    float2 pos = IN.worldPos.xz + _Offset.xy;
    uint data = dataAt(pos);
    bool notOutline = consistent(pos, _OutlineRadius, data);
    float h = IN.worldPos.y / _MaxHeight;
    fixed3 b = _LowColor.rgb * (1 - h) + _HighColor.rgb * h;
    fixed4 m = colors[data];
    m.a *= notOutline ? _BaseAlphaMultiplier : 1;
    o.Albedo = b * (1 - m.a) + m.rgb * m.a;
            
    o.Metallic = _Metallic;
    o.Smoothness = _Glossiness;
    o.Alpha = 1;
}
        ENDCG
    }    
}
