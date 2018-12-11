using System;
using System.Globalization;

namespace Halite3.Hlt
{
    /// <summary>
    /// A PlayerId is the identifier that corresponds to a Player.
    /// </summary>
    public readonly struct PlayerId : IEquatable<PlayerId>
    {
        public int Id { get; }

        public PlayerId(int id)
        {
            Id = id;
        }

        public override string ToString()
            => Id.ToString(CultureInfo.InvariantCulture);

        public override bool Equals(object obj)
            => obj is PlayerId other
            && Equals(other);

        public bool Equals(PlayerId other)
            => Id == other.Id;

        public override int GetHashCode()
            => Id.GetHashCode();

        public static bool operator ==(PlayerId left, PlayerId right) => left.Equals(right);

        public static bool operator !=(PlayerId left, PlayerId right) => !(left == right);
    }
}
