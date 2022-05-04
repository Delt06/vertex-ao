using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;
using static Unity.Mathematics.math;

[DefaultExecutionOrder(-2)]
public class VertexAo : MonoBehaviour
{
    [SerializeField] [Min(2)] private int _uSamples = 3;
    [SerializeField] [Min(2)] private int _vSamples = 5;
    [SerializeField] private float _initialOffset = 0.0001f;
    [SerializeField] [Min(0f)] private float _sampleRadius = 0.1f;
    [SerializeField] private LayerMask _layerMask = int.MaxValue;
    [SerializeField] [HideInInspector] private ComputeShader _createRaycastCommandsCs;
    [SerializeField] [HideInInspector] private ComputeShader _combineHitsCs;
    [SerializeField] [Range(0f, 180f)] private float _angle = 90f;

    private void Start() => BakeAo();

    private float2 GetURange() => float2(0, _angle / 180f);

    private void BakeAo()
    {
        Profiler.BeginSample("AO");
        var meshFilter = GetComponent<MeshFilter>();
        var mesh = meshFilter.mesh;

        var colors = new Color[mesh.vertexCount];

        using (var dataArray = Mesh.AcquireReadOnlyMeshData(mesh))
        {
            var meshData = dataArray[0];
            var vertices = new NativeArray<Vector3>(meshData.vertexCount, Allocator.TempJob);
            meshData.GetVertices(vertices);
            var normals = new NativeArray<Vector3>(meshData.vertexCount, Allocator.TempJob);
            meshData.GetNormals(normals);
            var totalSamples = _uSamples * _vSamples;

            var commands =
                new NativeArray<RaycastCommand>(meshData.vertexCount * totalSamples, Allocator.TempJob);
            var hits = new NativeArray<RaycastHit>(mesh.vertexCount * totalSamples, Allocator.TempJob);
            var verticesCount = vertices.Length;

            DispatchCreateRaycastCommandsCs(verticesCount, vertices, normals, commands);
            DispatchRaycastCommands(commands, hits);
            DispatchCombineCs(verticesCount, hits, colors);

            hits.Dispose();
        }

        mesh.SetColors(colors);
        Profiler.EndSample();
    }

    private void DispatchCreateRaycastCommandsCs(int verticesCount, NativeArray<Vector3> vertices,
        NativeArray<Vector3> normals,
        NativeArray<RaycastCommand> commands)
    {
        _createRaycastCommandsCs.SetMatrix("_TransformMatrix", transform.localToWorldMatrix);
        _createRaycastCommandsCs.SetInt("_USamples", _uSamples);
        _createRaycastCommandsCs.SetInt("_VSamples", _vSamples);
        _createRaycastCommandsCs.SetVector("_URange", float4(GetURange(), 0));


        _createRaycastCommandsCs.SetInt("_VerticesCount", verticesCount);
        var verticesBuffer = new ComputeBuffer(verticesCount, UnsafeUtility.SizeOf<float3>());
        verticesBuffer.SetData(vertices);
        _createRaycastCommandsCs.SetBuffer(0, "_Vertices", verticesBuffer);

        var normalBuffer = new ComputeBuffer(normals.Length, UnsafeUtility.SizeOf<float3>());
        normalBuffer.SetData(normals);
        _createRaycastCommandsCs.SetBuffer(0, "_Normals", normalBuffer);

        var commandsBuffer = new ComputeBuffer(commands.Length, UnsafeUtility.SizeOf<RaycastCommand>(),
            ComputeBufferType.Structured
        );
        commandsBuffer.SetData(commands);
        _createRaycastCommandsCs.SetBuffer(0, "_Commands", commandsBuffer);

        _createRaycastCommandsCs.SetFloat("_InitialOffset", _initialOffset);
        _createRaycastCommandsCs.SetFloat("_SampleRadius", _sampleRadius);
        _createRaycastCommandsCs.SetInt("_LayerMask", _layerMask);

        _createRaycastCommandsCs.GetKernelThreadGroupSizes(0, out var raycastKernelSizeX, out _, out _);
        _createRaycastCommandsCs.Dispatch(0, Mathf.CeilToInt((float) verticesCount / (int) raycastKernelSizeX), 1,
            1
        );

        var commandsArray = new RaycastCommand[commands.Length];
        commandsBuffer.GetData(commandsArray);
        NativeArray<RaycastCommand>.Copy(commandsArray, commands);

        verticesBuffer.Release();
        normalBuffer.Release();
        commandsBuffer.Release();
        vertices.Dispose();
        normals.Dispose();
    }

    private static void DispatchRaycastCommands(NativeArray<RaycastCommand> commands, NativeArray<RaycastHit> hits)
    {
        RaycastCommand.ScheduleBatch(commands, hits, 1).Complete();
        commands.Dispose();
    }

    private void DispatchCombineCs(int verticesCount, NativeArray<RaycastHit> hits, Color[] colors)
    {
        _combineHitsCs.SetInt("_VertexBufferSize", verticesCount);
        _combineHitsCs.SetInt("_USamples", _uSamples);
        _combineHitsCs.SetInt("_VSamples", _vSamples);

        var hitsBuffer = new ComputeBuffer(hits.Length, UnsafeUtility.SizeOf<RaycastHit>());
        hitsBuffer.SetData(hits);
        _combineHitsCs.SetBuffer(0, "_Hits", hitsBuffer);

        var colorsBuffer = new ComputeBuffer(colors.Length, UnsafeUtility.SizeOf<float4>());
        _combineHitsCs.SetBuffer(0, "_Colors", colorsBuffer);

        _combineHitsCs.GetKernelThreadGroupSizes(0, out var combineKernelSizeX, out _, out _);
        _combineHitsCs.Dispatch(0, Mathf.CeilToInt((float) verticesCount / (int) combineKernelSizeX), 1, 1);
        colorsBuffer.GetData(colors);

        hitsBuffer.Release();
        colorsBuffer.Release();
    }
}