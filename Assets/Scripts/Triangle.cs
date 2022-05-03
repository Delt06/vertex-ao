using System;

public struct Triangle : IEquatable<Triangle>
{
    public int I0;
    public int I1;
    public int I2;

    public bool Equals(Triangle other) => I0 == other.I0 && I1 == other.I1 && I2 == other.I2;

    public override bool Equals(object obj) => obj is Triangle other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = I0;
            hashCode = (hashCode * 397) ^ I1;
            hashCode = (hashCode * 397) ^ I2;
            return hashCode;
        }
    }

    public override string ToString() => $"({I0}, {I1}, {I2})";
}