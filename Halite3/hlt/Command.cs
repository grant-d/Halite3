using System;

namespace Halite3.Hlt
{
    public sealed class Command
    {
        public string Info { get; }

        private Command(string info)
        {
            Info = info;
        }

        /// <summary>
        /// Create a new Spawn Ship command
        /// </summary>
        /// <returns>Command("g")</returns>
        public static Command SpawnShip()
            => new Command("g");

        /// <summary>
        /// Create a new Dropoff command
        /// </summary>
        /// <returns>Command("g")</returns>
        public static Command TransformShipIntoDropoffSite(EntityId id)
            => new Command("c " + id);

        /// <summary>
        /// Create a new command for moving a ship in a given direction
        /// </summary>
        /// <param name="id">EntityId of the ship</param>
        /// <param name="direction">Direction to move in</param>
        /// <returns></returns>
        public static Command Move(EntityId id, Direction direction)
            => new Command("m " + id + ' ' + (char)direction);

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj)) return true;

            return obj is Command other
                && StringComparer.Ordinal.Equals(this, other);
        }

        public override int GetHashCode()
            => Info.GetHashCode(StringComparison.Ordinal);
    }
}
