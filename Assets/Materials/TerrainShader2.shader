Shader"Custom/TerrainShader"
{
    Properties
    {
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _LowColor ("Lowest Base Color", Color) = (1, 1, 1, 1)
        _HighColor ("Highest Base Color", Color) = (1, 1, 1, 1)
        _MaxHeight ("Maximum Height", float) = 2
        _NegativeColor ("Negative Color", Color) = (1, 1, 1, 1)
        _LargeColor ("Large Color", Color) = (1, 1, 1, 1)
        _SmallColor ("Small Color", Color) = (1, 1, 1, 1)
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
fixed4 _NegativeColor;
fixed4 _LargeColor;
fixed4 _SmallColor;
float2 _Offset;
float2 _WorldSize;
float _OutlineRadius;
float _BaseAlphaMultiplier;
sampler2D _HighlightMap;
float _MaxHeight;

bool2 dataAt(float2 pos)
{
    uint mip = 0;
    uint r = 4;
    while (r >= 4)
    {
        r = tex2Dlod(_HighlightMap, float4(pos / _WorldSize, 0, mip)).r * 255;
        mip++;
        if (mip > 10)
        {
            return bool2(false, false);
        }
    }
    return bool2(r & 1, r >> 1);    
}

bool consistent(float2 pos, float size, bool2 data)
{
    if (any(dataAt(pos + float2(size, 0)) != data))
    {
        return false;
    }
    if (any(dataAt(pos + float2(-size, 0)) != data))
    {
        return false;
    }
    if (any(dataAt(pos + float2(0, size)) != data))
    {
        return false;
    }
    if (any(dataAt(pos + float2(0, -size)) != data))
    {
        return false;
    }
    return true;
}

void surf(Input IN, inout SurfaceOutputStandard o)
{
    float2 pos = IN.worldPos.xz + _Offset.xy;
    bool2 data = dataAt(pos);
    bool notOutline = consistent(pos, _OutlineRadius, data);
    float h = IN.worldPos.y / _MaxHeight;
    fixed3 b = _LowColor.rgb * (1 - h) + _HighColor.rgb * h;
    fixed4 m = data.y ? (data.x ? _SmallColor : _LargeColor) : (data.x ? _NegativeColor : fixed4(0, 0, 0, 0));
    m.a *= notOutline ? _BaseAlphaMultiplier : 1;
    o.Albedo = b * (1 - m.a) + m.rgb * m.a;
            
    o.Metallic = _Metallic;
    o.Smoothness = _Glossiness;
    o.Alpha = 1;
}
        ENDCG
    }    
}
