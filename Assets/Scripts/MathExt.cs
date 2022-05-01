using UnityEngine;

public static class MathExt
{
    public static Vector4 LerpTangent(Vector4 t1, Vector4 t2, float t)
    {
        var newTangent = Vector4.Lerp(t1, t2, t).normalized;
        newTangent.w = Mathf.Lerp(t1.w, t2.w, t);
        return newTangent;
    }
}