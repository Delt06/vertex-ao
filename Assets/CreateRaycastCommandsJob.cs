using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public struct CreateRaycastCommandsJob : IJobParallelFor
{
    public float4x4 TransformMatrix;
    public int Samples;

    [ReadOnly]
    public NativeArray<float3> Vertices;
    [ReadOnly]
    public NativeArray<float3> Normals;

    [WriteOnly] [NativeDisableParallelForRestriction]
    public NativeArray<RaycastCommand> Commands;

    public float InitialOffset;
    public float SampleRadius;
    public int LayerMask;

    public void Execute(int index)
    {
        var samplesSqr = Samples * Samples;
        for (var i = 0; i < samplesSqr; i++)
        {
            var iu = i % Samples;
            var iv = i / Samples;
            var step = 1f / Samples;
            var u = step * iu;
            var v = step * iv;

            var normal = Normals[index];
            var direction = SampleHemisphere(math.float2(u, v));
            var worldNormal = math.mul(TransformMatrix, math.float4(normal, 0)).xyz;
            var rotation = quaternion.LookRotation(worldNormal, WorldUp);
            direction = math.rotate(rotation, direction);

            var commandIndex = index * samplesSqr + i;
            var from = math.mul(TransformMatrix, math.float4(Vertices[index], 1)).xyz +
                       worldNormal * InitialOffset;
            Commands[commandIndex] = new RaycastCommand
            {
                direction = direction,
                distance = SampleRadius,
                @from = from,
                layerMask = LayerMask,
                maxHits = 1,
            };
        }
    }

    private static readonly float3 WorldUp = math.float3(0, 1, 0);

    // https://blog.thomaspoulet.fr/uniform-sampling-on-unit-hemisphere/
    private float3 SampleHemisphere(float2 uv, float m = 1)
    {
        var theta = math.acos(math.pow(1 - uv.x, 1 / (1 + m)));
        var phi = 2 * math.PI * uv.y;

        var x = math.sin(theta) * math.cos(phi);
        var y = math.sin(theta) * math.sin(phi);

        return new float3(x, y, math.cos(theta));
    }
}