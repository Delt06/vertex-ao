using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Color;
using static UnityEngine.Vector3;

public class ShortestEdgeRemoval
{
    private readonly List<Color> _colors;
    private readonly List<Vector3> _normals;
    private readonly List<int> _triangles;
    private readonly List<Vector3> _vertices;
    private Dictionary<Edge, int> _edgesPolygonsCount;

    public ShortestEdgeRemoval(List<int> triangles, List<Vector3> vertices, List<Vector3> normals, List<Color> colors)
    {
        _triangles = triangles;
        _vertices = vertices;
        _normals = normals;
        _colors = colors;
    }

    private bool HasNormals => _normals.Count > 0;

    public bool HasColors => _colors.Count > 0;

    private void AddEdge(int i0, int i1)
    {
        var edge = new Edge
        {
            I0 = i0,
            I1 = i1,
        };
        if (!_edgesPolygonsCount.ContainsKey(edge))
            _edgesPolygonsCount.Add(edge, 0);
        _edgesPolygonsCount[edge]++;
    }

    private bool IsBorderEdge(int i0, int i1)
    {
        var edge = new Edge
        {
            I0 = i0,
            I1 = i1,
        };
        return !_edgesPolygonsCount.TryGetValue(edge, out var polyCount) ||
               polyCount <= 1;
    }

    private bool HasBorderEdge(in Triangle triangle) =>
        IsBorderEdge(triangle.I0, triangle.I1) ||
        IsBorderEdge(triangle.I1, triangle.I2) ||
        IsBorderEdge(triangle.I2, triangle.I0);

    private static bool TryGetSharedEdge(in Triangle t1, in Triangle t2, out Edge sharedEdge)
    {
        if (t2.HasEdge(t1.I0, t1.I1))
        {
            sharedEdge = new Edge { I0 = t1.I0, I1 = t1.I1 };
            return true;
        }

        if (t2.HasEdge(t1.I1, t1.I2))
        {
            sharedEdge = new Edge { I0 = t1.I1, I1 = t1.I2 };
            return true;
        }

        if (t2.HasEdge(t1.I2, t1.I0))
        {
            sharedEdge = new Edge { I0 = t1.I2, I1 = t1.I0 };
            return true;
        }

        sharedEdge = default;
        return false;
    }

    public void Run(int iterations, float minEdgeLength)
    {
        // http://paulbourke.net/geometry/polygonmesh/
        for (var iter = 0; iter < iterations; iter++)
        {
            ComputeBorderEdges();

            Edge? shortestEdge = null;
            var minLengthSqr = float.PositiveInfinity;
            var t1Index = -1;
            var t2Index = -1;

            for (var i = 0; i < _triangles.Count; i += 3)
            {
                var t1 = new Triangle
                {
                    I0 = _triangles[i + 0],
                    I1 = _triangles[i + 1],
                    I2 = _triangles[i + 2],
                };
                if (HasBorderEdge(t1)) continue;

                for (var j = i + 3; j < _triangles.Count; j += 3)
                {
                    var t2 = new Triangle
                    {
                        I0 = _triangles[j + 0],
                        I1 = _triangles[j + 1],
                        I2 = _triangles[j + 2],
                    };
                    if (HasBorderEdge(t2)) continue;
                    if (!TryGetSharedEdge(t1, t2, out var edge)) continue;

                    var lengthSqr = SqrMagnitude(_vertices[edge.I0] - _vertices[edge.I1]);
                    if (lengthSqr > minLengthSqr) continue;

                    shortestEdge = edge;
                    minLengthSqr = lengthSqr;
                    t1Index = i;
                    t2Index = j;
                }
            }

            if (shortestEdge != null && Mathf.Sqrt(minLengthSqr) > minEdgeLength)
            {
                var edge = shortestEdge.Value;
                var vertex0 = GetVertex(edge.I0);
                var vertex1 = GetVertex(edge.I1);
                var newVertex = VertexAttributes.Interpolate(vertex0, vertex1, 0.5f);
                SetVertex(edge.I0, newVertex);

                var tMaxIndex = Mathf.Max(t1Index, t2Index);
                var tMinIndex = Mathf.Min(t1Index, t2Index);
                _triangles.RemoveAt(tMaxIndex + 2);
                _triangles.RemoveAt(tMaxIndex + 1);
                _triangles.RemoveAt(tMaxIndex + 0);
                _triangles.RemoveAt(tMinIndex + 2);
                _triangles.RemoveAt(tMinIndex + 1);
                _triangles.RemoveAt(tMinIndex + 0);

                for (var index = 0; index < _triangles.Count; index++)
                {
                    var triangle = _triangles[index];
                    if (triangle == edge.I1)
                        _triangles[index] = edge.I0;
                }
            }
        }

        ComputeBorderEdges();
    }

    private VertexAttributes GetVertex(int index) =>
        new VertexAttributes(
            _vertices[index],
            HasNormals ? _normals[index] : zero,
            HasColors ? _colors[index] : clear
        );

    private void SetVertex(int index, VertexAttributes vertex)
    {
        _vertices[index] = vertex.Position;
        if (HasNormals)
            _normals[index] = vertex.Normal;
        if (HasColors)
            _colors[index] = vertex.Color;
    }

    private void ComputeBorderEdges()
    {
        _edgesPolygonsCount = new Dictionary<Edge, int>(new EdgeEqualityComparer());

        for (var i = 0; i < _triangles.Count; i += 3)
        {
            var i0 = _triangles[i + 0];
            var i1 = _triangles[i + 1];
            var i2 = _triangles[i + 2];
            AddEdge(i0, i1);
            AddEdge(i1, i2);
            AddEdge(i2, i0);
        }
    }

    public struct VertexAttributes
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Color Color;

        public VertexAttributes(Vector3 position, Vector3 normal, Color color)
        {
            Position = position;
            Normal = normal;
            Color = color;
        }

        public static VertexAttributes Interpolate(VertexAttributes va1, VertexAttributes va2, float t) =>
            new VertexAttributes(
                Lerp(va1.Position, va2.Position, t),
                Slerp(va1.Normal, va2.Normal, t),
                Lerp(va1.Color, va2.Color, t)
            );
    }

    private class EdgeEqualityComparer : IEqualityComparer<Edge>
    {
        public bool Equals(Edge x, Edge y) =>
            x.I0 == y.I0 && x.I1 == y.I1 ||
            x.I0 == y.I1 && x.I1 == y.I0;

        public int GetHashCode(Edge obj) => obj.I0 ^ obj.I1;
    }

    private struct Edge
    {
        public int I0;
        public int I1;
    }
}