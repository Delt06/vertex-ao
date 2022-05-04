using System;
using UnityEngine;
using static UnityEngine.Vector3;

[Serializable]
public struct EdgeRemovalWeights
{
    public float EdgeLength;
    public float NormalDifference;
    public float ColorDifference;
    public float UvDifference;

    public static float ComputeWeightedSum(VertexAttributes vertexAttributes, int i0, int i1,
        in EdgeRemovalWeights weights)
    {
        var lengthSqr = SqrMagnitude(vertexAttributes.GetPosition(i0) - vertexAttributes.GetPosition(i1));
        var normalsDiffSqr =
            SqrMagnitude(Normalize(vertexAttributes.GetNormal(i0)) - Normalize(vertexAttributes.GetNormal(i1)));
        var colorSqrDifference = ColorSqrDifference(vertexAttributes.GetColor(i0), vertexAttributes.GetColor(i1));
        var uvSqrDifference = SqrMagnitude(vertexAttributes.GetUV(i0) - vertexAttributes.GetUV(i1));
        var value = lengthSqr * weights.EdgeLength +
                    normalsDiffSqr * weights.NormalDifference +
                    colorSqrDifference * weights.ColorDifference +
                    uvSqrDifference * weights.UvDifference;
        return value;
    }

    private static float ColorSqrDifference(Color c1, Color c2)
    {
        var dr = c1.r - c2.r;
        var dg = c1.g - c2.g;
        var db = c1.b - c2.b;
        var da = c1.a - c2.a;
        return dr * dr +
               dg * dg +
               db * db +
               da * da;
    }
}