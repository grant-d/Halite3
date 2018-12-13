using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Halite3.Hlt
{
    /// <summary>
    /// A position is an object with x and y values indicating the absolute position on the game map.
    /// </summary>
    /// <see cref="https://halite.io/learn-programming-challenge/api-docs#position"/>
    public readonly struct Position : IEquatable<Position>
    {
        public int X { get; }
        public int Y { get; }

        public Position(int x, int y)
        {
            X = x;
            Y = y;
        }

        /// <summary>
        /// A method that normalizes a position within the bounds of the toroidal map.
        /// </summary>
        /// <remarks>
        /// Useful for handling the wraparound modulus arithmetic on x and y.
        /// For example, if a ship at (x = 31, y = 4) moves to the east on a 32x32 map,
        /// the normalized position would be (x = 0, y = 4), rather than the off-the-map position of (x = 32, y = 4).
        /// </remarks>
        public Position Normalize(int width, int height)
        {
            (int x, int y) = Normalize(X, Y, width, height);
            return new Position(x, y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (int x, int y) Normalize(int x, int y, int width, int height)
        {
            Debug.Assert(width > 0);
            Debug.Assert(height > 0);

            x = ((x % width) + width) % width;
            y = ((y % height) + height) % height;

            return (x, y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int ToIndex(int x, int y, int width, int height)
        {
            (x, y) = Normalize(x, y, width, height);
            return y * width + x;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int ToIndex(int width, int height)
            => ToIndex(X, Y, width, height);

        /// <summary>
        /// Returns a new position based on moving one unit in the given direction from the given position.
        /// Does not account for toroidal wraparound, that's done in GameMap.
        /// </summary>
        /// <seealso cref="Map.Normalize(Position)"/>
        public Position DirectionalOffset(Direction dir)
        {
            Debug.Assert(Enum.IsDefined(typeof(Direction), dir));

            int dx;
            int dy;

            switch (dir)
            {
                case Direction.North:
                    dx = 0;
                    dy = -1;
                    break;

                case Direction.South:
                    dx = 0;
                    dy = 1;
                    break;

                case Direction.East:
                    dx = 1;
                    dy = 0;
                    break;

                case Direction.West:
                    dx = -1;
                    dy = 0;
                    break;

                default:
                case Direction.Still:
                    dx = 0;
                    dy = 0;
                    break;
            }

            return new Position(X + dx, Y + dy);
        }
        public override string ToString() => $"({X}, {Y})";

        public override bool Equals(object obj)
            => obj is Position other
            && Equals(other);

        public bool Equals(Position other)
            => X == other.X
            && Y == other.Y;

        public override int GetHashCode()
            => X.GetHashCode() ^ Y.GetHashCode();

        public static bool operator ==(Position left, Position right) => left.Equals(right);

        public static bool operator !=(Position left, Position right) => !(left == right);
    }
}
