using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

[DefaultExecutionOrder(-1)]
public class Decimate : MonoBehaviour
{
    [SerializeField] [Range(-1, 1)] private float _minDot = 0.9f;
    [SerializeField] private bool _updateEachFrame;

    private readonly List<Cluster> _clusters = new List<Cluster>();
    private readonly Dictionary<int, int> _vertexToClusterIndex = new Dictionary<int, int>();
    private List<Vector3> _vertices;

    private void Start()
    {
        CreateClusters();
    }

    private void Update()
    {
        if (_updateEachFrame)
            CreateClusters();
    }

    private void CreateClusters()
    {
        _clusters.Clear();
        _vertexToClusterIndex.Clear();

        var meshFilter = GetComponent<MeshFilter>();
        var mesh = meshFilter.mesh;

        var triangles = new List<int>();
        mesh.GetTriangles(triangles, 0);

        _vertices = new List<Vector3>();
        mesh.GetVertices(_vertices);

        for (var i = 0; i < triangles.Count; i += 3)
        {
            var i0 = triangles[i + 0];
            var i1 = triangles[i + 1];
            var i2 = triangles[i + 2];
            var plane = ConstructPlane(i0, i1, i2);

            if (TryGetMatchingCluster(i0, plane, out var cluster0))
            {
                AddTriangle(cluster0, i0, i1, i2);
                continue;
            }

            if (TryGetMatchingCluster(i1, plane, out var cluster1))
            {
                AddTriangle(cluster1, i0, i1, i2);
                continue;
            }

            if (TryGetMatchingCluster(i2, plane, out var cluster2))
            {
                AddTriangle(cluster2, i0, i1, i2);
                continue;
            }

            var newCluster = new Cluster
            {
                Indices = new HashSet<int>(),
                Plane = plane,
                Index = _clusters.Count,
            };
            AddTriangle(newCluster, i0, i1, i2);
            _clusters.Add(newCluster);
        }

        var colors = new Color[_vertices.Count];
        var oldState = Random.state;
        Random.InitState(0);

        foreach (var cluster in _clusters)
        {
            var color = Random.ColorHSV(0f, 1f, 0.75f, 1f, 0.75f, 1f);
            foreach (var index in cluster.Indices)
            {
                colors[index] = color;
            }
        }

        Random.state = oldState;

        mesh.SetColors(colors);
    }

    private void AddTriangle(in Cluster cluster, int i0, int i1, int i2)
    {
        cluster.Indices.Add(i0);
        cluster.Indices.Add(i1);
        cluster.Indices.Add(i2);
        _vertexToClusterIndex[i0] = cluster.Index;
        _vertexToClusterIndex[i1] = cluster.Index;
        _vertexToClusterIndex[i2] = cluster.Index;
    }

    private bool TryGetMatchingCluster(int index, in Plane plane, out Cluster cluster)
    {
        if (TryGetClusterIndex(index, out var clusterIndex))
        {
            cluster = _clusters[clusterIndex];
            if (IsInSameCluster(plane, cluster)) return true;
        }

        cluster = default;
        return false;
    }

    [MustUseReturnValue]
    private Plane ConstructPlane(int i0, int i1, int i2)
    {
        var v0 = _vertices[i0];
        var v1 = _vertices[i1];
        var v2 = _vertices[i2];

        return new Plane(v0, v1, v2);
    }

    private bool TryGetClusterIndex(int vertexIndex, out int clusterIndex) =>
        _vertexToClusterIndex.TryGetValue(vertexIndex, out clusterIndex);

    private bool IsInSameCluster(in Plane plane, in Cluster cluster) =>
        Vector3.Dot(plane.normal, cluster.Plane.normal) >= _minDot;

    public struct Cluster
    {
        public HashSet<int> Indices;
        public Plane Plane;
        public int Index;
    }
}