using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;

public class AO : MonoBehaviour
{
    [SerializeField] [Min(0)] private int _samples = 10;
    [SerializeField] [Min(0f)] private float _initialOffset = 0.1f;
    [SerializeField] [Min(0f)] private float _sampleRadius = 0.1f;
    [SerializeField] private LayerMask _layerMask;

    private void Start()
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
            var samplesSqr = _samples * _samples;
            var commands =
                new NativeArray<RaycastCommand>(meshData.vertexCount * samplesSqr, Allocator.TempJob);
            var hits = new NativeArray<RaycastHit>(mesh.vertexCount * samplesSqr, Allocator.TempJob);

            var createRaycastCommandsJob = new CreateRaycastCommandsJob
            {
                Commands = commands,
                Normals = normals.Reinterpret<float3>(),
                Vertices = vertices.Reinterpret<float3>(),
                Samples = _samples,
                TransformMatrix = transform.localToWorldMatrix,
                LayerMask = _layerMask,
                InitialOffset = _initialOffset,
                SampleRadius = _sampleRadius,
            };
            var jobHandle = createRaycastCommandsJob.Schedule(meshData.vertexCount, 64);
            jobHandle = RaycastCommand.ScheduleBatch(commands, hits, 1, jobHandle);

            var combineHitsJob = new CombineHitsJob
            {
                Colors = colors,
                Hits = hits,
                Samples = _samples,
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
        meshFilter.mesh = mesh;
        Profiler.EndSample();
    }

    private void OnDrawGizmos()
    {
        var meshFilter = GetComponent<MeshFilter>();
        var sharedMesh = meshFilter.sharedMesh;
        var vertices = sharedMesh.vertices;
        var normals = sharedMesh.normals;

        Gizmos.color = Color.red;

        for (var i = 0; i < sharedMesh.vertexCount; i++)
        {
            var vertex = transform.TransformPoint(vertices[i]);
            var normal = transform.TransformDirection(normals[i]);

            Gizmos.DrawRay(vertex + normal * _initialOffset, normal * _sampleRadius);
        }
    }
}