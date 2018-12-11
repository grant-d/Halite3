using System;
using System.Collections.Generic;

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
        private readonly MapCell[][] _cells;

        public int Width { get; }
        public int Height { get; }

        /// <summary>
        /// Creates a new instance of a GameMap
        /// </summary>
        /// <para><seealso cref="_generate"/></para>
        /// <param name="width">The width, as an int, of the map</param>
        /// <param name="height">The height, as an int, of the map</param>
        public Map(int width, int height)
        {
            Width = width;
            Height = height;

            _cells = new MapCell[height][];
            for (int y = 0; y < height; ++y)
            {
                _cells[y] = new MapCell[width];
            }
        }

        /// <summary>
        /// Normalizes the given Position and then returns the corresponding MapCell.
        /// </summary>
        public MapCell At(Position position)
        {
            Position normalized = Normalize(position);
            return _cells[normalized.Y][normalized.X];
        }

        /// <summary>
        /// Normalizes the position of an Entity and returns the corresponding MapCell.
        /// </summary>
        public MapCell At(Entity entity)
            => At(entity.Position);

        /// <summary>
        /// A method that computes the Manhattan distance between two locations, and accounts for the toroidal wraparound.
        /// </summary>
        public int GetManhattanDistance(Position source, Position target)
        {
            Position normalizedSource = Normalize(source);
            Position normalizedTarget = Normalize(target);

            int dx = Math.Abs(normalizedSource.X - normalizedTarget.X);
            int dy = Math.Abs(normalizedSource.Y - normalizedTarget.Y);

            int toroidal_dx = Math.Min(dx, Width - dx);
            int toroidal_dy = Math.Min(dy, Height - dy);

            return toroidal_dx + toroidal_dy;
        }

        /// <summary>
        /// A method that normalizes a position within the bounds of the toroidal map.
        /// </summary>
        /// <remarks>
        /// Useful for handling the wraparound modulus arithmetic on x and y.
        /// For example, if a ship at (x = 31, y = 4) moves to the east on a 32x32 map,
        /// the normalized position would be (x = 0, y = 4), rather than the off-the-map position of (x = 32, y = 4).
        /// </remarks>
        public Position Normalize(Position position)
        {
            int x = ((position.X % Width) + Width) % Width;
            int y = ((position.Y % Height) + Height) % Height;
            return new Position(x, y);
        }

        /// <summary>
        /// A method that returns a list of direction(s) to move closer to a target disregarding collision possibilities.
        /// Returns an empty list if the source and destination are the same.
        /// </summary>
        public List<Direction> GetUnsafeMoves(Position source, Position destination)
        {
            var possibleMoves = new List<Direction>();

            Position normalizedSource = Normalize(source);
            Position normalizedDestination = Normalize(destination);

            int dx = Math.Abs(normalizedSource.X - normalizedDestination.X);
            int dy = Math.Abs(normalizedSource.Y - normalizedDestination.Y);
            int wrapped_dx = Width - dx;
            int wrapped_dy = Height - dy;

            if (normalizedSource.X < normalizedDestination.X)
            {
                possibleMoves.Add(dx > wrapped_dx ? Direction.West : Direction.East);
            }
            else if (normalizedSource.X > normalizedDestination.X)
            {
                possibleMoves.Add(dx < wrapped_dx ? Direction.West : Direction.East);
            }

            if (normalizedSource.Y < normalizedDestination.Y)
            {
                possibleMoves.Add(dy > wrapped_dy ? Direction.North : Direction.South);
            }
            else if (normalizedSource.Y > normalizedDestination.Y)
            {
                possibleMoves.Add(dy < wrapped_dy ? Direction.North : Direction.South);
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
                if (!At(targetPos).IsOccupied)
                {
                    At(targetPos).MarkUnsafe(ship);
                    return direction;
                }
            }

            return Direction.Still;
        }

        /// <summary>
        /// Clears all the ships in preparation for player._update() and updates the halite on each cell.
        /// </summary>
        internal void _update()
        {
            for (int y = 0; y < Height; ++y)
            {
                for (int x = 0; x < Width; ++x)
                {
                    _cells[y][x].Ship = null;
                }
            }

            int updateCount = GameInput.ReadInput().GetInt();

            for (int i = 0; i < updateCount; ++i)
            {
                var input = GameInput.ReadInput();
                int x = input.GetInt();
                int y = input.GetInt();

                _cells[y][x].Halite = input.GetInt();
            }
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
                    int halite = rowInput.GetInt();
                    map._cells[y][x] = new MapCell(new Position(x, y), halite);
                }
            }

            return map;
        }
    }
}
