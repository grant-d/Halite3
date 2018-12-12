using System;
using System.Diagnostics;

namespace Halite3.Hlt
{
    // https://leifnode.com/2013/12/flow-field-pathfinding/

    public sealed class FlowField
    {
        private readonly FlowCell[][] _cells;

        public int Width { get; }
        public int Height { get; }

#pragma warning disable CA1043 // Use Integral Or String Argument For Indexers
        public FlowCell this[Position position]
#pragma warning restore CA1043 // Use Integral Or String Argument For Indexers
        {
            get
            {
                Position normalized = Normalize(position);
                return _cells[normalized.Y][normalized.X];
            }

            private set
            {
                Position normalized = Normalize(position);
                _cells[normalized.Y][normalized.X] = value;
            }
        }

        public FlowField(IntegrationField integrationField)
        {
            Debug.Assert(integrationField != null);

            Width = integrationField.Width;
            Height = integrationField.Height;

            _cells = new FlowCell[Height][];

            for (int y = 0; y < Height; y++)
            {
                _cells[y] = new FlowCell[Width];

                for (int x = 0; x < Width; x++)
                {
                    var current = new Position(x, y);

                    ushort best = IntegrationCell.Max.Cost;
                    FlowDirection direction = FlowDirection._;

                    foreach (FlowDirection dir in Enum.GetValues(typeof(FlowDirection)))
                    {
                        if (dir == FlowDirection._)
                            continue;

                        var pos = dir.ToPosition(current);

                        ushort cost = integrationField[pos].Cost;
                        if (cost < best)
                        {
                            best = cost;
                            direction = dir;
                        }
                    }

                    _cells[y][x] = new FlowCell(direction);
                }
            }
        }

        private Position Normalize(Position position)
        {
            int x = ((position.X % Width) + Width) % Width;
            int y = ((position.Y % Height) + Height) % Height;
            return new Position(x, y);
        }
    }
}
