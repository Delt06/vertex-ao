using System.Collections.Generic;

public class VertexWelder
{
    private readonly List<int> _triangles;
    private readonly VertexAttributes _vertexAttributes;

    public VertexWelder(VertexAttributes vertexAttributes, List<int> triangles)
    {
        _triangles = triangles;
        _vertexAttributes = vertexAttributes;
    }

    public void Run(in EdgeRemovalWeights weights, float maxWeight, int iterations)
    {
        var welded = true;

        for (var i = 0; i < iterations && welded; i++)
        {
            welded = false;

            var weldedVertices = new HashSet<int>();

            var weldedVertexPairs = new List<(int i0, int i1)>();

            for (var iv0 = 0; iv0 < _vertexAttributes.Count; iv0++)
            {
                if (weldedVertices.Contains(iv0)) continue;

                for (var iv1 = iv0 + 1; iv1 < _vertexAttributes.Count; iv1++)
                {
                    if (weldedVertices.Contains(iv1)) continue;

                    var v0 = _vertexAttributes.GetVertex(iv0);
                    var v1 = _vertexAttributes.GetVertex(iv1);
                    var weightedSum = EdgeRemovalWeights.ComputeWeightedSum(v0, v1, weights);

                    if (weightedSum > maxWeight) continue;

                    weldedVertices.Add(iv0);
                    weldedVertices.Add(iv1);
                    weldedVertexPairs.Add((iv0, iv1));


                    welded = true;
                }
            }

            var verticesToDelete = new List<int>();
            var indexMapping = new Dictionary<int, int>();

            foreach (var (i0, i1) in weldedVertexPairs)
            {
                var v0 = _vertexAttributes.GetVertex(i0);
                var v1 = _vertexAttributes.GetVertex(i1);
                var newVertex = VertexAttributes.Vertex.Interpolate(v0, v1, 0.5f);
                _vertexAttributes.SetVertex(i0, newVertex);
                indexMapping[i1] = i0;
                verticesToDelete.Add(i1);
            }

            for (var it = 0; it < _triangles.Count; it++)
            {
                if (indexMapping.TryGetValue(_triangles[it], out var newTriangle))
                    _triangles[it] = newTriangle;
            }

            verticesToDelete.Sort((i1, i2) => i2.CompareTo(i1));

            foreach (var vertexIndex in verticesToDelete)
            {
                _vertexAttributes.RemoveAt(vertexIndex);

                for (var it = 0; it < _triangles.Count; it++)
                {
                    if (_triangles[it] > vertexIndex)
                        _triangles[it]--;
                }
            }
        }
    }
}