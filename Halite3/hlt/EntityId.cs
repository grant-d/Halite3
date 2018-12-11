using System;
using System.Globalization;

namespace Halite3.Hlt
{
    /// <summary>
    /// An EntityId is the identifier that corresponds to a ship, shipyard, or dropoff.
    /// </summary>
    public readonly struct EntityId : IEquatable<EntityId>
    {
        private static readonly EntityId s_none = new EntityId(-1);

        public static ref readonly EntityId None => ref s_none;

        public int Id { get; }

        public EntityId(int id)
        {
            Id = id;
        }

        public override string ToString()
            => Id.ToString(CultureInfo.InvariantCulture);

        public override bool Equals(object obj)
            => obj is EntityId other
            && Equals(other);

        public bool Equals(EntityId other)
            => Id == other.Id;

        public override int GetHashCode()
            => Id.GetHashCode();

        public static bool operator ==(EntityId left, EntityId right) => left.Equals(right);

        public static bool operator !=(EntityId left, EntityId right) => !(left == right);
    }
}
