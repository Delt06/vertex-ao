using System.Collections.Generic;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

public class VertexAttributes
{
    private static readonly Vector3 DefaultNormal = Vector3.zero;
    private static readonly Color DefaultColor = Color.clear;
    private static readonly Vector4 DefaultTangent = Vector4.zero;
    private static readonly Vector4 DefaultUV = Vector4.zero;
    private readonly List<Color> _colors;
    private readonly List<Vector3> _normals;
    private readonly List<Vector4> _tangents;
    private readonly List<Vector4> _uvs;
    private readonly List<Vector3> _vertices;

    public VertexAttributes(List<Vector3> vertices, List<Vector3> normals, List<Color> colors, List<Vector4> tangents,
        List<Vector4> uvs)
    {
        _vertices = vertices;
        _normals = normals;
        _colors = colors;
        _tangents = tangents;
        _uvs = uvs;
    }

    public int Count => _vertices.Count;

    private bool HasNormals => _normals.Count > 0;

    private bool HasColors => _colors.Count > 0;

    private bool HasUVs => _uvs.Count > 0;

    private bool HasTangents => _tangents.Count > 0;

    public ComputeBuffer CreatePositionsBuffer()
    {
        var computeBuffer = new ComputeBuffer(_vertices.Count, UnsafeUtility.SizeOf<float3>());
        computeBuffer.SetData(_vertices);
        return computeBuffer;
    }

    [CanBeNull]
    public ComputeBuffer CreateNormalsBuffer()
    {
        if (!HasNormals) return null;
        var computeBuffer = new ComputeBuffer(_normals.Count, UnsafeUtility.SizeOf<float3>());
        computeBuffer.SetData(_normals);
        return computeBuffer;
    }

    [CanBeNull]
    public ComputeBuffer CreateColorsBuffer()
    {
        if (!HasColors) return null;
        var computeBuffer = new ComputeBuffer(_colors.Count, UnsafeUtility.SizeOf<float4>());
        computeBuffer.SetData(_colors);
        return computeBuffer;
    }

    public void WriteToMesh(Mesh mesh)
    {
        mesh.SetVertices(_vertices);
        mesh.SetNormals(_normals);
        mesh.SetColors(_colors);
        mesh.SetTangents(_tangents);
        mesh.SetUVs(0, _uvs);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [MustUseReturnValue]
    public Vertex GetVertex(int index) =>
        new Vertex(
            _vertices[index],
            HasNormals ? _normals[index] : DefaultNormal,
            HasColors ? _colors[index] : DefaultColor,
            HasTangents ? _tangents[index] : DefaultTangent,
            HasUVs ? _uvs[index] : DefaultUV
        );

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [MustUseReturnValue]
    public Vector3 GetPosition(int index) => _vertices[index];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [MustUseReturnValue]
    public Vector3 GetNormal(int index) => HasNormals ? _normals[index] : Vector3.zero;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [MustUseReturnValue]
    public Color GetColor(int index) => HasColors ? _colors[index] : DefaultColor;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetVertex(int index, Vertex vertex)
    {
        _vertices[index] = vertex.Position;
        if (HasNormals)
            _normals[index] = vertex.Normal;
        if (HasColors)
            _colors[index] = vertex.Color;
        if (HasTangents)
            _tangents[index] = vertex.Tangent;
        if (HasUVs)
            _uvs[index] = vertex.UV;
    }

    public void RemoveAt(int index)
    {
        _vertices.RemoveAt(index);
        if (HasNormals)
            _normals.RemoveAt(index);
        if (HasColors)
            _colors.RemoveAt(index);
        if (HasTangents)
            _tangents.RemoveAt(index);
        if (HasUVs)
            _uvs.RemoveAt(index);
    }

    [CanBeNull]
    public ComputeBuffer CreateUvBuffer()
    {
        if (!HasUVs) return null;
        var computeBuffer = new ComputeBuffer(_uvs.Count, UnsafeUtility.SizeOf<float4>());
        computeBuffer.SetData(_uvs);
        return computeBuffer;
    }

    public Vector4 GetUV(int index) => HasUVs ? _uvs[index] : DefaultUV;

    public struct Vertex
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Color Color;
        public Vector4 Tangent;
        public Vector4 UV;

        public Vertex(Vector3 position, Vector3 normal, Color color, Vector4 tangent, Vector4 uv)
        {
            Position = position;
            Normal = normal;
            Color = color;
            Tangent = tangent;
            UV = uv;
        }

        public static Vertex Interpolate(Vertex va1, Vertex va2, float t) =>
            new Vertex(
                Vector3.Lerp(va1.Position, va2.Position, t),
                Vector3.Slerp(va1.Normal, va2.Normal, t),
                Color.Lerp(va1.Color, va2.Color, t),
                MathExt.LerpTangent(va1.Tangent, va2.Tangent, t),
                Vector4.Lerp(va1.UV, va2.UV, t)
            );
    }
}