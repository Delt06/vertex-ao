using Unity.Mathematics;
using static Unity.Mathematics.math;

public static class AoUtils
{
    // https://blog.thomaspoulet.fr/uniform-sampling-on-unit-hemisphere/
    public static float3 SampleHemisphere(float2 uv, float m = 1)
    {
        var theta = acos(pow(1 - uv.x, 1 / (1 + m)));
        var phi = 2 * PI * uv.y;

        var x = sin(theta) * cos(phi);
        var y = sin(theta) * sin(phi);

        return new float3(x, y, cos(theta));
    }
}