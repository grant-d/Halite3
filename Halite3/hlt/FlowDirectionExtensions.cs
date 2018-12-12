using System;
using System.Diagnostics;

namespace Halite3.Hlt
{
    public static class FlowDirectionExtensions
    {
        public static FlowDirection Invert(this FlowDirection direction)
        {
            Debug.Assert(Enum.IsDefined(typeof(FlowDirection), direction));

            switch (direction)
            {
                default:
                case FlowDirection._: return direction;

                case FlowDirection.N: return FlowDirection.S;
                case FlowDirection.E: return FlowDirection.W;
                case FlowDirection.S: return FlowDirection.N;
                case FlowDirection.W: return FlowDirection.E;
            }
        }

        public static Position FromPosition(this FlowDirection direction, Position start)
        {
            Debug.Assert(Enum.IsDefined(typeof(FlowDirection), direction));

            switch (direction)
            {
                default:
                case FlowDirection._: return start;

                case FlowDirection.N: return new Position(start.X, start.Y - 1);
                case FlowDirection.W: return new Position(start.X - 1, start.Y);
                case FlowDirection.E: return new Position(start.X + 1, start.Y);
                case FlowDirection.S: return new Position(start.X, start.Y + 1);
            }
        }

        public static string ToSymbol(this FlowDirection direction)
        {
            Debug.Assert(Enum.IsDefined(typeof(FlowDirection), direction));

            switch (direction)
            {
                default:
                case FlowDirection._: return "_";

                case FlowDirection.N: return "↑";
                case FlowDirection.W: return "←";
                case FlowDirection.E: return "→";
                case FlowDirection.S: return "↓";
            }
        }
    }
}
