Shader "Hidden/MediaPipe/FaceLandmark/Visualizer"
{
    CGINCLUDE

    #include "UnityCG.cginc"

    StructuredBuffer<float4> _Vertices;

    //
    // Wireframe mesh rendering
    //

    float4 VertexWire(uint vid : SV_VertexID) : SV_Position
    {
        float4 p = _Vertices[vid];
        p.xy -= 0.5;
        return UnityObjectToClipPos(p);
    }

    float4 FragmentWire(float4 vertex : SV_Position) : SV_Target
    {
        return float4(1, 1, 1, 0.75);
    }

    //
    // Keypoint marking
    //

    float4 VertexMark(uint vid : SV_VertexID) : SV_Position
    {
        const uint vindices[] = {
            1,           // noteTip
            205,         // rightCheek
            425,         // leftCheek
            33, 133,     // rightEyeLower0
            263, 362,    // leftEyeLower0
            168,         // midwayBetweenEyes
            78, 13, 308, // lipsUpperInner
            14,          // lipsLowerInner
            70, 55,      // rightEyebrowUpper
            300, 285     // leftEyebrowUpper
        };

        float4 p = _Vertices[vindices[vid / 4 % 16]];
        p.xy -= 0.5;

        uint tid = vid & 3;
        p.x += ((tid & 1) - 0.5) * (tid < 2) * 0.05;
        p.y += ((tid & 1) - 0.5) * (tid > 1) * 0.05;

        return UnityObjectToClipPos(float4(p.xy, 0, 1));
    }

    float4 FragmentMark(float4 vertex : SV_Position) : SV_Target
    {
        return float4(1, 0, 0, 0.9);
    }

    ENDCG

    SubShader
    {
        Cull Off ZTest Always ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex VertexWire
            #pragma fragment FragmentWire
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma vertex VertexMark
            #pragma fragment FragmentMark
            ENDCG
        }
    }
}
