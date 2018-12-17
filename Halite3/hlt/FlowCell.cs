using System;

namespace Halite3.Hlt
{
    public readonly struct FlowCell : IEquatable<FlowCell>
    {
        public Direction Direction { get; }

        public FlowCell(Direction direction)
        {
            Direction = direction;
        }

        public bool Equals(FlowCell other)
            => Direction == other.Direction;

        public override bool Equals(object obj)
            => obj is FlowCell other
            && Equals(other);

        public override int GetHashCode() => Direction.GetHashCode();

        public static bool operator ==(FlowCell left, FlowCell right) => left.Equals(right);

        public static bool operator !=(FlowCell left, FlowCell right) => !(left == right);
    }
}
