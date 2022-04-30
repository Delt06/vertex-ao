using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;
using static AoUtils;
using static Unity.Mathematics.math;

[DefaultExecutionOrder(-2)]
public class VertexAo : MonoBehaviour
{
    [SerializeField] [Min(2)] private int _uSamples = 3;
    [SerializeField] [Min(2)] private int _vSamples = 5;
    [SerializeField] private float _initialOffset = 0.0001f;
    [SerializeField] [Min(0f)] private float _sampleRadius = 0.1f;
    [SerializeField] private LayerMask _layerMask = int.MaxValue;
    [SerializeField] private ComputeShader _createRaycastCommandsCs;
    [SerializeField] private bool _updateEachFrame;
    [SerializeField] [Range(0f, 180f)] private float _angle = 90f;

    private void Start() => BakeAo();

    private void Update()
    {
        if (_updateEachFrame)
            BakeAo();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        var uRange = GetURange();

        for (var iu = 0; iu < _uSamples; iu++)
        {
            var u = lerp(uRange.x, uRange.y, (float) iu / (_uSamples - 1));

            for (var iv = 0; iv < _vSamples; iv++)
            {
                var v = (float) iv / (_vSamples - 1);
                var sample = SampleHemisphere(float2(u, v));
                Gizmos.DrawRay(transform.position + Vector3.up * 10f, sample * _sampleRadius);
            }
        }
    }

    private float2 GetURange() => float2(0, _angle / 180f);

    private void BakeAo()
    {
        Profiler.BeginSample("AO");
        var meshFilter = GetComponent<MeshFilter>();
        var mesh = meshFilter.mesh;

        var colors = new NativeArray<float4>(mesh.vertexCount, Allocator.TempJob);

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

            _createRaycastCommandsCs.SetMatrix("_TransformMatrix", transform.localToWorldMatrix);
            _createRaycastCommandsCs.SetInt("_USamples", _uSamples);
            _createRaycastCommandsCs.SetInt("_VSamples", _vSamples);
            _createRaycastCommandsCs.SetVector("_URange", float4(GetURange(), 0));

            _createRaycastCommandsCs.SetInt("_VerticesCount", vertices.Length);
            var verticesBuffer = new ComputeBuffer(vertices.Length, UnsafeUtility.SizeOf<float3>());
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

            _createRaycastCommandsCs.GetKernelThreadGroupSizes(0, out var kernelSizeX, out _, out _);
            _createRaycastCommandsCs.Dispatch(0, Mathf.CeilToInt((float) vertices.Length / (int) kernelSizeX), 1, 1);

            var commandsArray = new RaycastCommand[commands.Length];
            commandsBuffer.GetData(commandsArray);
            NativeArray<RaycastCommand>.Copy(commandsArray, commands);

            verticesBuffer.Release();
            normalBuffer.Release();
            commandsBuffer.Release();

            JobHandle jobHandle = default;
            jobHandle = RaycastCommand.ScheduleBatch(commands, hits, 1, jobHandle);

            var combineHitsJob = new CombineHitsJob
            {
                Colors = colors,
                Hits = hits,
                USamples = _uSamples,
                VSamples = _vSamples,
            };
            jobHandle = combineHitsJob.Schedule(meshData.vertexCount, 64, jobHandle);

            jobHandle.Complete();

            vertices.Dispose();
            normals.Dispose();
            commands.Dispose();
            hits.Dispose();
        }

        mesh.SetColors(colors);
        colors.Dispose();
        Profiler.EndSample();
    }
}