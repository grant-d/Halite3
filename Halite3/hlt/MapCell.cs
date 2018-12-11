namespace Halite3.Hlt
{
    /// <summary>
    /// A map cell is an object representation of a cell on the game map.
    /// Map cell has position, halite, ship, and structure as member variables.
    /// </summary>
    /// <see cref="https://halite.io/learn-programming-challenge/api-docs#map-cell"/>
    public sealed class MapCell
    {
        public Position Position { get; }
        public int Halite { get; set; }
        public Ship Ship { get; set; }
        public Entity Structure { get; set; }

        public MapCell(Position position, int halite)
        {
            Position = position;
            Halite = halite;
        }

        /// <summary>
        /// Returns true if there is neither a ship nor a structure on this MapCell.
        /// </summary>
        public bool IsEmpty
            => Ship == null && Structure == null;

        /// <summary>
        /// Returns true if there is a ship on this MapCell.
        /// </summary>
        public bool IsOccupied
            => Ship != null;

        /// <summary>
        /// Returns true if there is a structure on this MapCell.
        /// </summary>
        public bool HasStructure
            => Structure != null;

        /// <summary>
        /// Is used to mark the cell under this ship as unsafe (occupied) for collision avoidance.
        /// <para>
        /// This marking resets every turn and is used by NaiveNavigate to avoid collisions.
        /// </para>
        /// </summary>
        /// <seealso cref="GameMap.NaiveNavigate(Ship, Position)"/>
        public void MarkUnsafe(Ship ship)
        {
            Ship = ship;
        }
    }
}
