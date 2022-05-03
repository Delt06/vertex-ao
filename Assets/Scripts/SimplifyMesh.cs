using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[DefaultExecutionOrder(-1)]
public class SimplifyMesh : MonoBehaviour
{
    [SerializeField] private bool _updateEachFrame;
    [SerializeField] [Min(0)] private int _removalIterations = 3;
    [SerializeField] [Min(0f)] private float _maxTotalWeight;
    [SerializeField] private EdgeRemovalWeights _edgeRemovalWeights = new EdgeRemovalWeights
    {
        ColorDifference = 10,
        EdgeLength = 0.01f,
        NormalDifference = 1f,
    };

    private readonly List<Cluster> _clusters = new List<Cluster>();
    private readonly Dictionary<int, HashSet<int>> _edges = new Dictionary<int, HashSet<int>>();
    private readonly Dictionary<int, int> _vertexToClusterIndex = new Dictionary<int, int>();
    private List<Color> _colors;
    private List<Vector3> _normals;
    private List<Vector4> _tangents;
    private List<Vector4> _uvs;
    private List<Vector3> _vertices;

    private void Start()
    {
        Run();
    }

    private void Update()
    {
        if (_updateEachFrame)
            Run();
    }

    private HashSet<int> GetOrCreateEdgeSet(int index)
    {
        if (!_edges.TryGetValue(index, out var set))
            _edges[index] = set = new HashSet<int>();
        return set;
    }

    private void Run()
    {
        _clusters.Clear();
        _vertexToClusterIndex.Clear();

        var meshFilter = GetComponent<MeshFilter>();
        var mesh = meshFilter.mesh;

        var triangles = new List<int>();
        mesh.GetTriangles(triangles, 0);

        _vertices = new List<Vector3>();
        mesh.GetVertices(_vertices);

        _normals = new List<Vector3>();
        mesh.GetNormals(_normals);

        _colors = new List<Color>();
        mesh.GetColors(_colors);

        _tangents = new List<Vector4>();
        mesh.GetTangents(_tangents);

        _uvs = new List<Vector4>();
        mesh.GetUVs(0, _uvs);

        var vertexAttributes = new VertexAttributes(_vertices, _normals, _colors, _tangents, _uvs);

        for (var i = 0; i < triangles.Count; i += 3)
        {
            var i0 = triangles[i + 0];
            var i1 = triangles[i + 1];
            var i2 = triangles[i + 2];
            var set0 = GetOrCreateEdgeSet(i0);
            set0.Add(i1);
            set0.Add(i2);

            var set1 = GetOrCreateEdgeSet(i1);
            set1.Add(i0);
            set1.Add(i2);

            var set2 = GetOrCreateEdgeSet(i2);
            set2.Add(i0);
            set2.Add(i1);
        }

        for (var i = 0; i < _vertices.Count; i++)
        {
            FindClusterRecursively(i, null);
        }

        for (var i = 0; i < triangles.Count; i += 3)
        {
            var i0 = triangles[i + 0];
            var i1 = triangles[i + 1];
            var i2 = triangles[i + 2];
            var cluster = _clusters[_vertexToClusterIndex[i0]];
            cluster.Indices.Add(i0);
            cluster.Indices.Add(i1);
            cluster.Indices.Add(i2);
        }

        if (_removalIterations > 0)
            foreach (var cluster in _clusters)
            {
                var shortestEdgeRemoval = new ShortestEdgeRemoval(vertexAttributes, cluster.Indices);
                shortestEdgeRemoval.Run(_removalIterations, _maxTotalWeight, _edgeRemovalWeights);
            }

        var newIndices = GetIndicesFromClusters();
        mesh.SetTriangles(newIndices, 0);
        vertexAttributes.WriteToMesh(mesh);
    }

    private int[] GetIndicesFromClusters()
    {
        return _clusters.SelectMany(c => c.Indices).ToArray();
    }

    private void FindClusterRecursively(int vertexIndex, Cluster? cluster)
    {
        if (_vertexToClusterIndex.ContainsKey(vertexIndex))
            return;

        var set = GetOrCreateEdgeSet(vertexIndex);
        if (cluster == null)
        {
            cluster = new Cluster
            {
                Indices = new List<int>(),
                Index = _clusters.Count,
            };
            _clusters.Add(cluster.Value);
        }

        _vertexToClusterIndex.Add(vertexIndex, cluster.Value.Index);

        foreach (var otherIndex in set)
        {
            FindClusterRecursively(otherIndex, cluster);
        }
    }

    private struct Cluster
    {
        public List<int> Indices;
        public int Index;
    }
}