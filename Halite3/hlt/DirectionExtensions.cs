using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Halite3.Hlt
{
    public static class DirectionExtensions
    {
        public static IReadOnlyList<Direction> NSEW { get; } = new Direction[] { Direction.N, Direction.S, Direction.E, Direction.W };

        /// <summary>
        /// Returns the opposite of this direction. The opposite of STILL is STILL.
        /// </summary>
        public static Direction Invert(this Direction direction)
        {
            Debug.Assert(Enum.IsDefined(typeof(Direction), direction));

            switch (direction)
            {
                case Direction.N: return Direction.S;
                case Direction.E: return Direction.W;
                case Direction.S: return Direction.N;
                case Direction.W: return Direction.E;

                default:
                case Direction.X: return Direction.X;
            }
        }

        public static Position FromPosition(this Direction direction, Position start)
        {
            Debug.Assert(Enum.IsDefined(typeof(Direction), direction));

            switch (direction)
            {
                default:
                case Direction.X: return start;

                case Direction.N: return new Position(start.X, start.Y - 1);
                case Direction.W: return new Position(start.X - 1, start.Y);
                case Direction.E: return new Position(start.X + 1, start.Y);
                case Direction.S: return new Position(start.X, start.Y + 1);
            }
        }

        public static string ToFlowSymbol(this Direction direction)
        {
            Debug.Assert(Enum.IsDefined(typeof(Direction), direction));

            switch (direction)
            {
                default:
                case Direction.X: return "_";

                case Direction.N: return "↑";
                case Direction.W: return "←";
                case Direction.E: return "→";
                case Direction.S: return "↓";
            }
        }
    }
}
