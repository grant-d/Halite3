using System;
using System.Diagnostics;

namespace Halite3.Hlt
{
    // https://leifnode.com/2013/12/flow-field-pathfinding/

    public enum FlowDirection : byte
    {
        _ = 0,

        N,
        //NE,
        E,
        //SE,
        S,
        //SW,
        W,
        //NW
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
        public static Position ToPosition(this FlowDirection direction, Position start)
        {
            Debug.Assert(Enum.IsDefined(typeof(FlowDirection), direction));

            switch (direction)
            {
                default:
                case FlowDirection._: return start;

                //case FlowDirection.NW: return new Position(start.X - 1, start.Y - 1);
                case FlowDirection.N: return new Position(start.X, start.Y - 1);
                //case FlowDirection.NE: return new Position(start.X + 1, start.Y - 1);

                case FlowDirection.W: return new Position(start.X - 1, start.Y);
                case FlowDirection.E: return new Position(start.X + 1, start.Y);

                //case FlowDirection.SW: return new Position(start.X - 1, start.Y + 1);
                case FlowDirection.S: return new Position(start.X, start.Y + 1);
                    //case FlowDirection.SE: return new Position(start.X + 1, start.Y + 1);
            }
        }

        public static string ToSymbol(this FlowDirection direction)
        {
            Debug.Assert(Enum.IsDefined(typeof(FlowDirection), direction));

            switch (direction)
            {
                default:
                case FlowDirection._: return "_";

                //case FlowDirection.NW: return "↖";
                case FlowDirection.N: return "↑";
                //case FlowDirection.NE: return "↗";

                case FlowDirection.W: return "←";
                case FlowDirection.E: return "→";

                //case FlowDirection.SW: return "↙";
                case FlowDirection.S: return "↓";
                //case FlowDirection.SE: return "↘";
            }
        }
    }
}