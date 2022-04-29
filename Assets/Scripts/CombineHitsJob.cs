using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

// 14.42 ms on serapis
public struct CombineHitsJob : IJobParallelFor
{
    public int USamples;
    public int VSamples;
    [ReadOnly] [NativeDisableParallelForRestriction]
    public NativeArray<RaycastHit> Hits;
    [WriteOnly]
    public NativeArray<float4> Colors;

    public void Execute(int index)
    {
        var sum = 0f;
        var totalSamples = USamples * VSamples;

        for (var hitIndex = 0; hitIndex < totalSamples; hitIndex++)
        {
            var hit = Hits[index * totalSamples + hitIndex];
            if (hit.normal == Vector3.zero)
                sum += 1f / totalSamples;
        }

        Colors[index] = math.float4(sum, 1, 1, 1);
    }
}