using System;

namespace Halite3.Hlt
{
    public readonly struct WaveCell : IEquatable<WaveCell>
    {
        public static readonly WaveCell Zero = new WaveCell(0);

        public static readonly WaveCell Max = new WaveCell(ushort.MaxValue);

        public ushort Cost { get; }

        public WaveCell(ushort cost)
        {
            Cost = cost;
        }

        public bool Equals(WaveCell other)
            => Cost == other.Cost;

        public override bool Equals(object obj)
            => obj is WaveCell other
            && Equals(other);

        public override int GetHashCode() => Cost.GetHashCode();

        public static bool operator ==(WaveCell left, WaveCell right) => left.Equals(right);

        public static bool operator !=(WaveCell left, WaveCell right) => !(left == right);
    }
}
