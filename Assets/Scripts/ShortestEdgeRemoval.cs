using System.Collections.Generic;
using UnityEngine;

public class ShortestEdgeRemoval : MonoBehaviour
{
    [SerializeField] [Min(1)] private int _iterations = 1;
    private Dictionary<Edge, int> _edgesPolygonsCount;

    private void OnEnable()
    {
        var meshFilter = GetComponent<MeshFilter>();
        var mesh = meshFilter.mesh;

        var vertices = new List<Vector3>();
        mesh.GetVertices(vertices);

        var triangles = new List<int>();
        mesh.GetTriangles(triangles, 0);

        Debug.Log("Original triangles: " + triangles.Count / 3);

        // http://paulbourke.net/geometry/polygonmesh/
        for (var iter = 0; iter < _iterations; iter++)
        {
            _edgesPolygonsCount = new Dictionary<Edge, int>(new EdgeEqualityComparer());

            for (var i = 0; i < triangles.Count; i += 3)
            {
                var i0 = triangles[i + 0];
                var i1 = triangles[i + 1];
                var i2 = triangles[i + 2];
                AddEdge(i0, i1);
                AddEdge(i1, i2);
                AddEdge(i2, i0);
            }

            Edge? shortestEdge = null;
            var minLength = float.PositiveInfinity;
            var t1Index = -1;
            var t2Index = -1;

            for (var i = 0; i < triangles.Count; i += 3)
            {
                var t1 = new Triangle
                {
                    I0 = triangles[i + 0],
                    I1 = triangles[i + 1],
                    I2 = triangles[i + 2],
                };
                if (HasBorderEdge(t1)) continue;

                for (var j = i + 3; j < triangles.Count; j += 3)
                {
                    var t2 = new Triangle
                    {
                        I0 = triangles[j + 0],
                        I1 = triangles[j + 1],
                        I2 = triangles[j + 2],
                    };
                    if (HasBorderEdge(t2)) continue;
                    if (!TryGetSharedEdge(t1, t2, out var edge)) continue;

                    var length = Vector3.Distance(vertices[edge.I0], vertices[edge.I1]);
                    if (length > minLength) continue;

                    shortestEdge = edge;
                    minLength = length;
                    t1Index = i;
                    t2Index = j;
                }
            }

            if (shortestEdge != null)
            {
                var edge = shortestEdge.Value;
                var vertex0 = vertices[edge.I0];
                var vertex1 = vertices[edge.I1];
                var newVertex = Vector3.Lerp(vertex0, vertex1, 0.5f);
                vertices[edge.I0] = newVertex;

                var tMaxIndex = Mathf.Max(t1Index, t2Index);
                var tMinIndex = Mathf.Min(t1Index, t2Index);
                triangles.RemoveAt(tMaxIndex + 2);
                triangles.RemoveAt(tMaxIndex + 1);
                triangles.RemoveAt(tMaxIndex + 0);
                triangles.RemoveAt(tMinIndex + 2);
                triangles.RemoveAt(tMinIndex + 1);
                triangles.RemoveAt(tMinIndex + 0);

                for (var index = 0; index < triangles.Count; index++)
                {
                    var triangle = triangles[index];
                    if (triangle == edge.I1)
                        triangles[index] = edge.I0;
                }
            }
        }


        mesh.SetVertices(vertices);
        Debug.Log("Resulting triangles: " + triangles.Count / 3);
        mesh.SetTriangles(triangles, 0);
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

    private bool TryGetSharedEdge(in Triangle t1, in Triangle t2, out Edge sharedEdge)
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

    public class EdgeEqualityComparer : IEqualityComparer<Edge>
    {
        public bool Equals(Edge x, Edge y) =>
            x.I0 == y.I0 && x.I1 == y.I1 ||
            x.I0 == y.I1 && x.I1 == y.I0;

        public int GetHashCode(Edge obj) => obj.I0 ^ obj.I1;
    }

    public struct Triangle
    {
        public int I0;
        public int I1;
        public int I2;
    }

    public struct Edge
    {
        public int I0;
        public int I1;
    }
}

public static class TriangleExtensions
{
    public static bool HasEdge(this in ShortestEdgeRemoval.Triangle triangle, int i0, int i1) =>
        HasEdgeLiteral(triangle, i0, i1) ||
        HasEdgeLiteral(triangle, i1, i0);

    private static bool HasEdgeLiteral(ShortestEdgeRemoval.Triangle triangle, int i0, int i1) =>
        triangle.I0 == i0 && triangle.I1 == i1 ||
        triangle.I1 == i0 && triangle.I2 == i1 ||
        triangle.I2 == i0 && triangle.I0 == i1;
}