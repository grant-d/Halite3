using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Halite3.Hlt
{
    /// <summary>
    /// A Direction is one of the 4 cardinal directions or STILL.
    /// </summary>
    /// <see cref="https://halite.io/learn-programming-challenge/api-docs#direction"/>
    public enum Direction
    {
        Still = 'o',

        North = 'n',
        East = 'e',
        South = 's',
        West = 'w',
    }

    public static class DirectionExtensions
    {
        public static IReadOnlyList<Direction> AllCardinals { get; } = new Direction[] { Direction.North, Direction.South, Direction.East, Direction.West };

        /// <summary>
        /// Returns the opposite of this direction. The opposite of STILL is STILL.
        /// </summary>
        public static Direction Invert(this Direction direction)
        {
            Debug.Assert(Enum.IsDefined(typeof(Direction), direction));

            switch (direction)
            {
                case Direction.North: return Direction.South;
                case Direction.East: return Direction.West;
                case Direction.South: return Direction.North;
                case Direction.West: return Direction.East;

                default:
                case Direction.Still: return Direction.Still;
            }
        }
    }
}
