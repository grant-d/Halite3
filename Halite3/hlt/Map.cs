using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Halite3.Hlt
{
    /// <summary>
    /// Gameplay takes place on a wrapping rectangular grid 32x32, 40x40, 48x48, 56x56, or 64x64 in dimension.
    /// <para>
    /// This class is responsible for two key functions:
    /// keeping track of what's on the map at any given cell, and helping ships navigate.
    /// </para>
    /// </summary>
    /// <para><see cref="https://halite.io/learn-programming-challenge/api-docs#map"/></para>
    public sealed class Map
    {
        private readonly MapCell[] _cells;

        public int Width { get; }
        public int Height { get; }

        public MapCell this[int x, int y]
        {
            get
            {
                int index = Position.ToIndex(x, y, Width, Height);
                return _cells[index];
            }
        }

        /// <summary>
        /// Normalizes the position of an Entity and returns the corresponding MapCell.
        /// </summary>
#pragma warning disable CA1043 // Use Integral Or String Argument For Indexers
        public MapCell this[Entity entity]
#pragma warning restore CA1043 // Use Integral Or String Argument For Indexers
            => this[entity.Position.X, entity.Position.Y];

        public int MaxHalite { get; private set; }

        /// <summary>
        /// Creates a new instance of a GameMap
        /// </summary>
        /// <para><seealso cref="_generate"/></para>
        /// <param name="width">The width, as an int, of the map</param>
        /// <param name="height">The height, as an int, of the map</param>
        public Map(int width, int height)
            : this(width, height, new MapCell[height * width])
        { }

        private Map(int width, int height, MapCell[] cells)
        {
            Debug.Assert(cells != null);
            Debug.Assert(cells.Length == width * height);

            Width = width;
            Height = height;

            _cells = cells;
        }

        /// <summary>
        /// A method that computes the Manhattan distance between two locations, and accounts for the toroidal wraparound.
        /// </summary>
        public int GetManhattanDistance(Position source, Position target)
        {
            Position normalizedSource = source.Normalize(Width, Height);
            Position normalizedTarget = target.Normalize(Width, Height);

            int dx = Math.Abs(normalizedSource.X - normalizedTarget.X);
            int dy = Math.Abs(normalizedSource.Y - normalizedTarget.Y);

            int toroidal_dx = Math.Min(dx, Width - dx);
            int toroidal_dy = Math.Min(dy, Height - dy);

            return toroidal_dx + toroidal_dy;
        }

        /// <summary>
        /// A method that returns a list of direction(s) to move closer to a target disregarding collision possibilities.
        /// Returns an empty list if the source and destination are the same.
        /// </summary>
        public List<Direction> GetUnsafeMoves(Position source, Position destination)
        {
            var possibleMoves = new List<Direction>();

            Position normalizedSource = source.Normalize(Width, Height);
            Position normalizedDestination = destination.Normalize(Width, Height);

            int dx = Math.Abs(normalizedSource.X - normalizedDestination.X);
            int dy = Math.Abs(normalizedSource.Y - normalizedDestination.Y);
            int wrapped_dx = Width - dx;
            int wrapped_dy = Height - dy;

            if (normalizedSource.X < normalizedDestination.X)
            {
                possibleMoves.Add(dx > wrapped_dx ? Direction.W : Direction.E);
            }
            else if (normalizedSource.X > normalizedDestination.X)
            {
                possibleMoves.Add(dx < wrapped_dx ? Direction.W : Direction.E);
            }

            if (normalizedSource.Y < normalizedDestination.Y)
            {
                possibleMoves.Add(dy > wrapped_dy ? Direction.N : Direction.S);
            }
            else if (normalizedSource.Y > normalizedDestination.Y)
            {
                possibleMoves.Add(dy < wrapped_dy ? Direction.N : Direction.S);
            }

            return possibleMoves;
        }

        /// <summary>
        /// A method that returns a direction to move closer to a target without colliding with other entities.
        /// Returns a direction of “still” if no such move exists.
        /// </summary>
        public Direction NaiveNavigate(Ship ship, Position destination)
        {
            // getUnsafeMoves normalizes for us
            foreach (Direction direction in GetUnsafeMoves(ship.Position, destination))
            {
                Position targetPos = ship.Position.DirectionalOffset(direction);
                if (!this[targetPos.X, targetPos.Y].IsOccupied)
                {
                    this[targetPos.X, targetPos.Y].MarkUnsafe(ship);
                    return direction;
                }
            }

            return Direction.X;
        }

        /// <summary>
        /// Clears all the ships in preparation for player._update() and updates the halite on each cell.
        /// </summary>
        internal void _update()
        {
            int max = 0;
            for (int y = 0; y < Height; ++y)
            {
                for (int x = 0; x < Width; ++x)
                {
                    int index = Position.ToIndex(x, y, Width, Height);
                    MapCell cell = _cells[index];
                    cell.Ship = null;

                    if (cell.Halite > max)
                        max = cell.Halite;
                }
            }

            int updateCount = GameInput.ReadInput().GetInt();

            for (int i = 0; i < updateCount; ++i)
            {
                var input = GameInput.ReadInput();
                int x = input.GetInt();
                int y = input.GetInt();

                int index = Position.ToIndex(x, y, Width, Height);
                MapCell cell = _cells[index];

                cell.Halite = input.GetInt();

                if (cell.Halite > max)
                    max = cell.Halite;
            }

            MaxHalite = max;
        }

        /// <summary>
        /// Reads the starting map for the game from the Halite engine.
        /// </summary>
        /// <returns></returns>
        internal static Map _generate()
        {
            var mapInput = GameInput.ReadInput();
            int width = mapInput.GetInt();
            int height = mapInput.GetInt();

            var map = new Map(width, height);

            for (int y = 0; y < height; ++y)
            {
                var rowInput = GameInput.ReadInput();

                for (int x = 0; x < width; ++x)
                {
                    int index = Position.ToIndex(x, y, width, height);
                    int halite = rowInput.GetInt();
                    map._cells[index] = new MapCell(new Position(x, y), halite);
                }
            }

            return map;
        }
    }
}
