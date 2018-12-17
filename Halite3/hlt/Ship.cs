namespace Halite3.Hlt
{
    /// <summary>
    /// A ship is a type of Entity and is used to collect and transport halite.
    /// <para>
    /// Has a max halite capacity of 1000. Can move once per turn.
    /// </para>
    /// </summary>
    /// <see cref="https://halite.io/learn-programming-challenge/api-docs#ship"></see>
    public sealed class Ship : Entity
    {
        public int Halite { get; }

        public Ship(PlayerId owner, EntityId id, Position position, int halite)
            : base(owner, id, position)
        {
            Halite = halite;
        }

        /// <summary>
        /// Returns true if this ship is carrying the max amount of halite possible.
        /// </summary>
        public bool IsFull
            => Halite >= Constants.MaxHalite;

        /// <summary>
        /// Returns the command to turn this ship into a dropoff.
        /// </summary>
        public Command MakeDropoff()
            => Command.TransformShipIntoDropoffSite(Id);

        /// <summary>
        /// Returns the command to move this ship in a direction.
        /// </summary>
        public Command Move(Direction direction)
            => Command.Move(Id, direction);

        /// <summary>
        /// Returns the command to keep this ship still.
        /// </summary>
        public Command Stay()
            => Command.Move(Id, Direction.X);

        public override string ToString()
            => $"{base.ToString()}({Halite} halite)";

        /// <summary>
        /// Reads in the details of a new ship from the Halite engine.
        /// </summary>
        internal static Ship _generate(PlayerId playerId)
        {
            var input = GameInput.ReadInput();

            var shipId = new EntityId(input.GetInt());
            int x = input.GetInt();
            int y = input.GetInt();
            int halite = input.GetInt();

            return new Ship(playerId, shipId, new Position(x, y), halite);
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
                return true;
            if (obj == null || GetType() != obj.GetType())
                return false;
            if (!base.Equals(obj)) return false;

            var ship = (Ship)obj;

            return Halite == ship.Halite;
        }

        public override int GetHashCode()
        {
            int result = base.GetHashCode();
            result = 31 * result + Halite;
            return result;
        }
    }
}
