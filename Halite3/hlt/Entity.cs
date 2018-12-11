using System;
using System.Globalization;

namespace Halite3.Hlt
{
    /// <summary>
    /// A base class extended by Ship, Dropoff, and Shipyard.
    /// </summary>
    public abstract class Entity
    {
        public PlayerId Owner { get; }
        public EntityId Id { get; }
        public Position Position { get; }

        protected Entity(PlayerId owner, EntityId id, Position position)
        {
            Owner = owner;
            Id = id;
            Position = position;
        }

        public override string ToString()
            => String.Format(CultureInfo.InvariantCulture, "{0}(id={1}, {2}", GetType(), Id, Position);

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj)) return true;

            return obj is Entity other
                && Owner == other.Owner
                && Id == other.Id
                && Position == other.Position;
        }

        public override int GetHashCode()
        {
            int result = Owner.GetHashCode();
            result = 31 * result ^ Id.GetHashCode();
            result = 31 * result ^ Position.GetHashCode();
            return result;
        }
    }
}
