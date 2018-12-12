using System;
using System.Diagnostics;

namespace Halite3.Hlt
{
    // https://leifnode.com/2013/12/flow-field-pathfinding/

    public enum FlowDirection : byte
    {
        None = 0,

        N,
        NE,
        E,
        SE,
        S,
        SW,
        W,
        NW
    }

    public sealed class FlowCell
    {
        public FlowDirection Mine { get; set; }

        public FlowDirection Home { get; set; }

        public FlowCell(FlowDirection mine, FlowDirection home)
        {
            Mine = mine;
            Home = home;
        }
    }

    public static class FlowDirectionExtensions
    {
        public static Position ToPosition(this FlowDirection direction, Position position)
        {
            Debug.Assert(Enum.IsDefined(typeof(FlowDirection), direction));

            switch (direction)
            {
                default:
                case FlowDirection.None: return position;

                case FlowDirection.NW: return new Position(position.X - 1, position.Y - 1);
                case FlowDirection.N: return new Position(position.X, position.Y - 1);
                case FlowDirection.NE: return new Position(position.X + 1, position.Y - 1);

                case FlowDirection.W: return new Position(position.X - 1, position.Y);
                case FlowDirection.E: return new Position(position.X + 1, position.Y);

                case FlowDirection.SW: return new Position(position.X - 1, position.Y + 1);
                case FlowDirection.S: return new Position(position.X, position.Y + 1);
                case FlowDirection.SE: return new Position(position.X + 1, position.Y + 1);
            }
        }
    }
}
