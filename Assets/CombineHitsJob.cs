using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public struct CombineHitsJob : IJobParallelFor
{
    public int Samples;
    [ReadOnly] [NativeDisableParallelForRestriction]
    public NativeArray<RaycastHit> Hits;
    [WriteOnly]
    public NativeArray<float4> Colors;

    public void Execute(int index)
    {
        var sum = 0f;
        var samplesSqr = Samples * Samples;

        for (var hitIndex = 0; hitIndex < samplesSqr; hitIndex++)
        {
            var hit = Hits[index * samplesSqr + hitIndex];
            if (hit.normal == Vector3.zero)
                sum += 1f / samplesSqr;
        }

        Colors[index] = math.float4(sum, 1, 1, 1);
    }
}