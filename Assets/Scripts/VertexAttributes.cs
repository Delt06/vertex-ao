using System.Collections.Generic;
using UnityEngine;

public class VertexAttributes
{
    private readonly List<Color> _colors;
    private readonly List<Vector3> _normals;
    private readonly List<Vector3> _vertices;

    public VertexAttributes(List<Vector3> vertices, List<Vector3> normals, List<Color> colors)
    {
        _vertices = vertices;
        _normals = normals;
        _colors = colors;
    }

    private bool HasNormals => _normals.Count > 0;

    private bool HasColors => _colors.Count > 0;

    public Vertex GetVertex(int index) =>
        new Vertex(
            _vertices[index],
            HasNormals ? _normals[index] : Vector3.zero,
            HasColors ? _colors[index] : Color.clear
        );

    public void SetVertex(int index, Vertex vertex)
    {
        _vertices[index] = vertex.Position;
        if (HasNormals)
            _normals[index] = vertex.Normal;
        if (HasColors)
            _colors[index] = vertex.Color;
    }

    public struct Vertex
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Color Color;

        public Vertex(Vector3 position, Vector3 normal, Color color)
        {
            Position = position;
            Normal = normal;
            Color = color;
        }

        public static Vertex Interpolate(Vertex va1, Vertex va2, float t) =>
            new Vertex(
                Vector3.Lerp(va1.Position, va2.Position, t),
                Vector3.Slerp(va1.Normal, va2.Normal, t),
                Color.Lerp(va1.Color, va2.Color, t)
            );
    }
}