using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.Vector3;
using static MathExt;

[DefaultExecutionOrder(-3)]
public class Tesselation : MonoBehaviour
{
    [SerializeField] [Min(1)] private int _iterations = 1;
    [SerializeField] [Min(0f)] private float _minTriangleArea;
    [SerializeField] [Min(0)] private int _weldIterations = 100;
    [SerializeField] [HideInInspector] private ComputeShader _weldPrepareCs;

    private List<Vector3> _newNormals;
    private List<Vector4> _newTangents;
    private List<int> _newTriangles;
    private List<Vector4> _newUvs;
    private List<Vector3> _newVertices;
    private int _triangleIndexBase;

    private void Start()
    {
        var meshFilter = GetComponent<MeshFilter>();
        var mesh = meshFilter.mesh;

        var triangles = new List<int>();
        mesh.GetTriangles(triangles, 0);
        var vertices = new List<Vector3>();
        mesh.GetVertices(vertices);

        var normals = new List<Vector3>();
        mesh.GetNormals(normals);
        var recalculateNormals = normals.Count > 0;
        var tangents = new List<Vector4>();
        mesh.GetTangents(tangents);
        var recalculateTangents = tangents.Count > 0;
        var uvs = new List<Vector4>();
        mesh.GetUVs(0, uvs);
        var recalculateUvs = uvs.Count > 0;

        for (var iter = 0; iter < _iterations; iter++)
        {
            if (iter > 0)
            {
                vertices = _newVertices;
                triangles = _newTriangles;
                normals = _newNormals;
                tangents = _newTangents;
                uvs = _newUvs;
            }

            _newVertices = new List<Vector3>();
            _newTriangles = new List<int>();
            _newNormals = new List<Vector3>();
            _newTangents = new List<Vector4>();
            _newUvs = new List<Vector4>();

            var appliedTesselationAtLeastOnce = false;

            for (var i = 0; i < triangles.Count; i += 3)
            {
                // https://lindenreidblog.com/2017/12/03/simple-mesh-tessellation-triangulation-tutorial/
                var i0 = triangles[i];
                var i1 = triangles[i + 1];
                var i2 = triangles[i + 2];

                var v0 = vertices[i0];
                var v1 = vertices[i1];
                var v2 = vertices[i2];
                var triangleArea = GetTriangleArea(v0, v1, v2);
                var applyTesselation = triangleArea > _minTriangleArea;
                if (applyTesselation)
                    appliedTesselationAtLeastOnce = true;

                _triangleIndexBase = _newVertices.Count;
                if (applyTesselation)
                {
                    AddTriangle(3, 1, 4);
                    AddTriangle(4, 2, 5);
                    AddTriangle(5, 0, 3);
                    AddTriangle(3, 4, 5);
                }
                else
                {
                    AddTriangle(0, 1, 2);
                }

                _newVertices.Add(v0);
                _newVertices.Add(v1);
                _newVertices.Add(v2);
                if (applyTesselation)
                {
                    _newVertices.Add(Lerp(v0, v1, 0.5f));
                    _newVertices.Add(Lerp(v1, v2, 0.5f));
                    _newVertices.Add(Lerp(v0, v2, 0.5f));
                }


                if (recalculateNormals)
                {
                    var n0 = normals[i0];
                    var n1 = normals[i1];
                    var n2 = normals[i2];
                    _newNormals.Add(n0);
                    _newNormals.Add(n1);
                    _newNormals.Add(n2);
                    if (applyTesselation)
                    {
                        _newNormals.Add(Normalize(Lerp(n0, n1, 0.5f)));
                        _newNormals.Add(Normalize(Lerp(n1, n2, 0.5f)));
                        _newNormals.Add(Normalize(Lerp(n0, n2, 0.5f)));
                    }
                }

                if (recalculateTangents)
                {
                    var t0 = tangents[i0];
                    var t1 = tangents[i1];
                    var t2 = tangents[i2];
                    _newTangents.Add(t0);
                    _newTangents.Add(t1);
                    _newTangents.Add(t2);
                    if (applyTesselation)
                    {
                        _newTangents.Add(LerpTangent(t0, t1, 0.5f));
                        _newTangents.Add(LerpTangent(t1, t2, 0.5f));
                        _newTangents.Add(LerpTangent(t0, t2, 0.5f));
                    }
                }

                if (recalculateUvs)
                {
                    var uv0 = uvs[i0];
                    var uv1 = uvs[i1];
                    var uv2 = uvs[i2];
                    _newUvs.Add(uv0);
                    _newUvs.Add(uv1);
                    _newUvs.Add(uv2);
                    if (applyTesselation)
                    {
                        _newUvs.Add(Vector4.Lerp(uv0, uv1, 0.5f));
                        _newUvs.Add(Vector4.Lerp(uv1, uv2, 0.5f));
                        _newUvs.Add(Vector4.Lerp(uv0, uv2, 0.5f));
                    }
                }
            }

            if (!appliedTesselationAtLeastOnce)
                break;
        }

        var colors = new List<Color>();
        mesh.GetColors(colors);

        var vertexAttributes = new VertexAttributes(_newVertices, _newNormals, colors, _newTangents, _newUvs);

        if (_weldIterations > 0)
        {
            var vertexWelder = new VertexWelder(vertexAttributes, _newTriangles, _weldPrepareCs);
            vertexWelder.Run(_weldIterations);
        }

        var count = _newVertices.Distinct(new AEC()).Count();

        vertexAttributes.WriteToMesh(mesh);
        mesh.SetTriangles(_newTriangles, 0);
    }

    private float GetTriangleArea(Vector3 v0, Vector3 v1, Vector3 v2) =>
        Cross(v0 - v1, v0 - v2).magnitude * 0.5f;

    private void AddTriangle(int relativeIndex0, int relativeIndex1, int relativeIndex2)
    {
        _newTriangles.Add(_triangleIndexBase + relativeIndex0);
        _newTriangles.Add(_triangleIndexBase + relativeIndex1);
        _newTriangles.Add(_triangleIndexBase + relativeIndex2);
    }

    private class AEC : IEqualityComparer<Vector3>
    {
        public bool Equals(Vector3 x, Vector3 y) => SqrMagnitude(x - y) < 0.0001f;

        public int GetHashCode(Vector3 obj)
        {
            unchecked
            {
                var hashCode = obj.x.GetHashCode();
                hashCode = (hashCode * 397) ^ obj.y.GetHashCode();
                hashCode = (hashCode * 397) ^ obj.z.GetHashCode();
                hashCode = (hashCode * 397) ^ obj.normalized.GetHashCode();
                hashCode = (hashCode * 397) ^ obj.magnitude.GetHashCode();
                hashCode = (hashCode * 397) ^ obj.sqrMagnitude.GetHashCode();
                return hashCode;
            }
        }
    }
}