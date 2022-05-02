using System.Collections.Generic;
using UnityEngine;

public class ShortestEdgeRemoval
{
    private readonly List<int> _triangles;
    private readonly VertexAttributes _vertexAttributes;
    private Dictionary<Edge, int> _edgesPolygonsCount;

    public ShortestEdgeRemoval(VertexAttributes vertexAttributes, List<int> triangles)
    {
        _vertexAttributes = vertexAttributes;
        _triangles = triangles;
    }

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

    public void Run(int iterations, float minTotalWeight, in EdgeRemovalWeights weights)
    {
        // http://paulbourke.net/geometry/polygonmesh/
        for (var iter = 0; iter < iterations; iter++)
        {
            ComputeBorderEdges();

            Edge? shortestEdge = null;
            var minValue = float.PositiveInfinity;
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

                    var value = EdgeRemovalWeights.ComputeWeightedSum(_vertexAttributes, edge.I0, edge.I1, weights);
                    if (value > minValue) continue;
                    if (value < minTotalWeight) continue;

                    shortestEdge = edge;
                    minValue = value;
                    t1Index = i;
                    t2Index = j;
                }
            }

            if (shortestEdge != null)
            {
                var edge = shortestEdge.Value;
                var vertex0 = _vertexAttributes.GetVertex(edge.I0);
                var vertex1 = _vertexAttributes.GetVertex(edge.I1);
                var newVertex = VertexAttributes.Vertex.Interpolate(vertex0, vertex1, 0.5f);
                _vertexAttributes.SetVertex(edge.I0, newVertex);

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
            else
            {
                break;
            }
        }
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