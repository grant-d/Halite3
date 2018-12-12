using System;

namespace Halite3.Hlt
{
    public readonly struct CostCell : IEquatable<CostCell>
    {
        public static readonly CostCell Zero = new CostCell(0);

        public static readonly CostCell Max = new CostCell(254);

        public static readonly CostCell Wall = new CostCell(byte.MaxValue);

        public byte Cost { get; }

        public CostCell(byte cost)
        {
            Cost = cost;
        }

        public bool Equals(CostCell other)
            => Cost == other.Cost;

        public override bool Equals(object obj)
            => obj is CostCell other
            && Equals(other);

        public override int GetHashCode() => Cost.GetHashCode();

        public static bool operator ==(CostCell left, CostCell right) => left.Equals(right);

        public static bool operator !=(CostCell left, CostCell right) => !(left == right);
    }
}
