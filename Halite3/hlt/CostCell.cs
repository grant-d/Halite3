using System;

namespace Halite3.Hlt
{
    // https://leifnode.com/2013/12/flow-field-pathfinding/

    public readonly struct CostCell : IEquatable<CostCell>
    {
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
