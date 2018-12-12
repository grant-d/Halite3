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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int ToIndex(int width, int height)
        {
            Debug.Assert(width > 0);
            Debug.Assert(height > 0);

            int x = ((X % width) + width) % width;
            int y = ((Y % height) + height) % height;

            return y * width + x;
        }

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
        public override string ToString() => $"{X}, {Y}";

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
