using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

public class VertexWelder
{
    private readonly List<int> _triangles;
    private readonly VertexAttributes _vertexAttributes;
    private readonly ComputeShader _weldPrepareCs;

    public VertexWelder(VertexAttributes vertexAttributes, List<int> triangles, ComputeShader weldPrepareCs)
    {
        _triangles = triangles;
        _vertexAttributes = vertexAttributes;
        _weldPrepareCs = weldPrepareCs;
    }

    public void Run(int iterations)
    {
        var welded = true;

        var countBuffer = new ComputeBuffer(1, sizeof(uint), ComputeBufferType.IndirectArguments);
        var emptyBuffer = new ComputeBuffer(1, sizeof(int));

        for (var i = 0; i < iterations && welded; i++)
        {
            welded = false;

            var positionsBuffer = _vertexAttributes.CreatePositionsBuffer();
            var normalsBuffer = _vertexAttributes.CreateNormalsBuffer();
            var colorsBuffer = _vertexAttributes.CreateColorsBuffer();
            var uvBuffer = _vertexAttributes.CreateUvBuffer();
            var verticesToWeldBuffer = new ComputeBuffer(_vertexAttributes.Count * _vertexAttributes.Count,
                UnsafeUtility.SizeOf<int2>(), ComputeBufferType.Append
            );
            verticesToWeldBuffer.SetCounterValue(0);


            const int kernelIndex = 0;
            _weldPrepareCs.SetBuffer(kernelIndex, "_Positions", positionsBuffer);

            var hasNormals = normalsBuffer != null;
            _weldPrepareCs.SetBuffer(kernelIndex, "_Normals", hasNormals ? normalsBuffer : emptyBuffer);
            _weldPrepareCs.SetBool("_HasNormals", hasNormals);
            var hasColors = colorsBuffer != null;
            _weldPrepareCs.SetBuffer(kernelIndex, "_Colors", hasColors ? colorsBuffer : emptyBuffer);
            _weldPrepareCs.SetBool("_HasColors", hasColors);
            var hasUv = uvBuffer != null;
            _weldPrepareCs.SetBuffer(kernelIndex, "_UV", hasUv ? uvBuffer : emptyBuffer);
            _weldPrepareCs.SetBool("_HasUV", hasUv);

            _weldPrepareCs.SetInt("_VertexBufferSize", _vertexAttributes.Count);
            _weldPrepareCs.SetBuffer(kernelIndex, "_ToWeld", verticesToWeldBuffer);

            _weldPrepareCs.GetKernelThreadGroupSizes(kernelIndex, out var threadGroupSizeX, out _, out _);
            var threadGroupsX = Mathf.CeilToInt((float) _vertexAttributes.Count / (int) threadGroupSizeX);
            _weldPrepareCs.Dispatch(kernelIndex, threadGroupsX, 1, 1);

            positionsBuffer?.Release();
            normalsBuffer?.Release();
            colorsBuffer?.Release();
            uvBuffer?.Release();

            ComputeBuffer.CopyCount(verticesToWeldBuffer, countBuffer, 0);
            var verticesToWeldBufferCounts = new uint[1];
            countBuffer.GetData(verticesToWeldBufferCounts);
            var verticesToWeldBufferCount = verticesToWeldBufferCounts[0];
            var verticesToWeld = new int2[verticesToWeldBufferCount];
            verticesToWeldBuffer.GetData(verticesToWeld);
            verticesToWeldBuffer.Release();

            var verticesToDelete = new List<int>();
            var weldedVertices = new HashSet<int>();
            var indexMapping = new Dictionary<int, int>();

            foreach (var vertices in verticesToWeld)
            {
                var i0 = vertices.x;
                var i1 = vertices.y;

                if (weldedVertices.Contains(i0)) continue;
                if (weldedVertices.Contains(i1)) continue;

                var v0 = _vertexAttributes.GetVertex(i0);
                var v1 = _vertexAttributes.GetVertex(i1);
                var newVertex = VertexAttributes.Vertex.Interpolate(v0, v1, 0.5f);
                _vertexAttributes.SetVertex(i0, newVertex);
                indexMapping[i1] = i0;
                verticesToDelete.Add(i1);

                weldedVertices.Add(i0);
                weldedVertices.Add(i1);
                welded = true;
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

        countBuffer.Release();
        emptyBuffer.Release();
    }
}