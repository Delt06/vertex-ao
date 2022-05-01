using System;
using UnityEngine;
using static UnityEngine.Vector3;

[Serializable]
public struct EdgeRemovalWeights
{
    public float EdgeLength;
    public float NormalDifference;
    public float ColorDifference;

    public static EdgeRemovalWeights Uniform => new EdgeRemovalWeights
    {
        ColorDifference = 1f,
        EdgeLength = 1f,
        NormalDifference = 1f,
    };

    public static float ComputeWeightedSum(in VertexAttributes.Vertex v0, in VertexAttributes.Vertex v1,
        in EdgeRemovalWeights weights)
    {
        var lengthSqr = SqrMagnitude(v0.Position - v1.Position);
        var normalsDiffSqr = SqrMagnitude(Normalize(v0.Normal) - Normalize(v1.Normal));
        var colorSqrDifference = ColorSqrDifference(v0.Color, v1.Color);
        var value = lengthSqr * weights.EdgeLength +
                    normalsDiffSqr * weights.NormalDifference +
                    colorSqrDifference * weights.ColorDifference
            ;
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