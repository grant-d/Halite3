namespace Halite3.Hlt
{
    /// <summary>
    /// A type of Entity that can be used to spawn Ships.
    /// </summary>
    /// <see cref="https://halite.io/learn-programming-challenge/api-docs#shipyard"/>
    public sealed class Shipyard : Entity
    {
        public Shipyard(PlayerId owner, Position position)
            : base(owner, EntityId.None, position)
        { }

        /// <summary>
        /// Returns the command to spawn a new ship
        /// </summary>
        public static Command SpawnShip()
            => Command.SpawnShip();
    }
}
