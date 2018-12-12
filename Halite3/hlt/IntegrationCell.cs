using System;

namespace Halite3.Hlt
{
    // https://leifnode.com/2013/12/flow-field-pathfinding/

    public readonly struct IntegrationCell : IEquatable<IntegrationCell>
    {
        public static readonly IntegrationCell Zero = new IntegrationCell(0);

        public static readonly IntegrationCell Max = new IntegrationCell(ushort.MaxValue);

        public ushort Cost { get; }

        public IntegrationCell(ushort cost)
        {
            Cost = cost;
        }

        public bool Equals(IntegrationCell other)
            => Cost == other.Cost;

        public override bool Equals(object obj)
            => obj is IntegrationCell other
            && Equals(other);

        public override int GetHashCode() => Cost.GetHashCode();

        public static bool operator ==(IntegrationCell left, IntegrationCell right) => left.Equals(right);

        public static bool operator !=(IntegrationCell left, IntegrationCell right) => !(left == right);
    }
}
