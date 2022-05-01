using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[DefaultExecutionOrder(-1)]
public class Decimate : MonoBehaviour
{
    [SerializeField] private bool _updateEachFrame;
    [SerializeField] [Min(0)] private int _removalIterations = 3;
    [SerializeField] [Min(0f)] private float _minEdgeLength = 0.1f;

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

            if (TryGetMatchingCluster(i0, out var cluster0))
            {
                AddTriangle(cluster0, i0, i1, i2);
                continue;
            }

            if (TryGetMatchingCluster(i1, out var cluster1))
            {
                AddTriangle(cluster1, i0, i1, i2);
                continue;
            }

            if (TryGetMatchingCluster(i2, out var cluster2))
            {
                AddTriangle(cluster2, i0, i1, i2);
                continue;
            }

            var newCluster = new Cluster
            {
                Indices = new List<int>(),
                Index = _clusters.Count,
            };
            AddTriangle(newCluster, i0, i1, i2);
            _clusters.Add(newCluster);
        }

        foreach (var c1 in _clusters)
        {
            foreach (var c2 in _clusters)
            {
                if (c1.Index == c2.Index) continue;

                if (c1.Indices.Intersect(c2.Indices).Any())
                    Debug.Log(c1.Index + " intersects with " + c2.Index);
            }
        }

        var colors = new Color[_vertices.Count];
        var normals = new Vector3[_vertices.Count];

        foreach (var cluster in _clusters)
        {
            var shortestEdgeRemoval = new ShortestEdgeRemoval(cluster.Indices, _vertices);
            shortestEdgeRemoval.Run(_removalIterations, _minEdgeLength);
        }

        mesh.SetTriangles(_clusters.SelectMany(c => c.Indices).ToArray(), 0);
        mesh.SetColors(colors);
        mesh.SetNormals(normals);
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

    private bool TryGetMatchingCluster(int index, out Cluster cluster)
    {
        if (TryGetClusterIndex(index, out var clusterIndex))
        {
            cluster = _clusters[clusterIndex];
            return true;
        }

        cluster = default;
        return false;
    }

    private bool TryGetClusterIndex(int vertexIndex, out int clusterIndex) =>
        _vertexToClusterIndex.TryGetValue(vertexIndex, out clusterIndex);

    private struct Cluster
    {
        public List<int> Indices;
        public int Index;
    }
}