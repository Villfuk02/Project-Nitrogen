Shader "Custom/TerrainShader"
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
        _Offset ("World Position Offset (z and w are ignored)", Vector) = (0, 0, 0, 0)
        _Scale ("Scale", float) = 4
        _OutlineRadius ("Outline radius", float) = 0.1
        _BaseAlphaMultiplier ("Base alpha multiplier", Range(0,1)) = 0.2
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

        struct Node
        {
            uint children;
            uint data;
        };

        struct Input
        {
            float3 worldPos;
        };
        
        #ifdef SHADER_API_D3D11
            StructuredBuffer<uint> _Data;
        #endif
        
        half _Glossiness;
        half _Metallic;
        fixed4 _LowColor;
        fixed4 _HighColor;
        fixed4 _NegativeColor;
        fixed4 _LargeColor;
        fixed4 _SmallColor;
        float2 _Offset;
        float _Scale;
        float _OutlineRadius;
        float _BaseAlphaMultiplier;
        float _MaxHeight;

        #ifdef SHADER_API_D3D11
            Node parseNode (uint index)
            {
                uint data = _Data[index / 2] >> ((index % 2) * 16);
                data = data & 0xffff;
                Node n;
                n.children = data * (1 - (data >> 15));
                n.data = (data & 0x7fff) * (data >> 15);
                return n;
            }
        #endif

        uint dataAt(float2 pos)
        {
            Node n;
            n.children = 0;
            // make it known we're using pos, so it's not optimized away
            n.data = pos.x >= 0;
            float scale = _Scale;            

            #ifdef SHADER_API_D3D11            
                n = parseNode(0);
                          
                while (n.children > 0) 
                {
                    uint positiveX = pos.x >= 0;
                    uint positiveY = pos.y >= 0;
                    uint indexOffset = positiveX | (positiveY << 1);
                   
                    float2 posOffset = scale * float2(2 * (float)positiveX - 1, 2 * (float)positiveY - 1);
                    pos -= posOffset;
                    scale *= 0.5;
                    n = parseNode(n.children + indexOffset);
                 }    
            #endif

            return n.data;
        }

        bool consistent(float2 pos, float size, uint data)
        {
            [branch] if (dataAt(pos + float2(size, 0)) != data) {
                return false;
            }
            [branch] if (dataAt(pos + float2(-size, 0)) != data) {
                return false;
            }
            [branch] if (dataAt(pos + float2(0, size)) != data) {
                return false;
            }
            [branch] if (dataAt(pos + float2(0, -size)) != data) {
                return false;
            }
            return true;
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float2 pos = IN.worldPos.xz + _Offset.xy;
            uint data = dataAt(pos);
            bool notOutline = consistent(pos, _OutlineRadius, data);  
            float h = IN.worldPos.y / _MaxHeight;
            fixed3 b = _LowColor.rgb * (1 - h) + _HighColor.rgb * h;
            fixed4 m = (data >> 1) ? ((data & 1) ? _SmallColor : _LargeColor) : ((data & 1) ? _NegativeColor : fixed4(0, 0, 0, 0));
            m.a *= notOutline ? _BaseAlphaMultiplier : 1;
            o.Albedo = b * (1 - m.a) + m.rgb * m.a;  
            
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = 1;
        }
        ENDCG
    }    
}
