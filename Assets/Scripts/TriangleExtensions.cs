public static class TriangleExtensions
{
    public static bool HasEdge(this in Triangle triangle, int i0, int i1) =>
        HasEdgeLiteral(triangle, i0, i1) ||
        HasEdgeLiteral(triangle, i1, i0);

    private static bool HasEdgeLiteral(Triangle triangle, int i0, int i1) =>
        triangle.I0 == i0 && triangle.I1 == i1 ||
        triangle.I1 == i0 && triangle.I2 == i1 ||
        triangle.I2 == i0 && triangle.I0 == i1;
}