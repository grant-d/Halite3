using System;
using System.Diagnostics;

namespace Halite3.Hlt
{
    // https://leifnode.com/2013/12/flow-field-pathfinding/

    public sealed class FlowField
    {
        private readonly FlowCell[] _cells;

        public int Width { get; }
        public int Height { get; }

#pragma warning disable CA1043 // Use Integral Or String Argument For Indexers
        public FlowCell this[Position position]
#pragma warning restore CA1043 // Use Integral Or String Argument For Indexers
        {
            get
            {
                int index = Normalize(position);
                return _cells[index];
            }

            private set
            {
                int index = Normalize(position);
                _cells[index] = value;
            }
        }

        public FlowField(IntegrationField integrationField)
        {
            Debug.Assert(integrationField != null);

            Width = integrationField.Width;
            Height = integrationField.Height;

            _cells = new FlowCell[Height * Width];

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    var current = new Position(x, y);

                    ushort best = IntegrationCell.Max.Cost;
                    FlowDirection direction = FlowDirection._;

                    foreach (FlowDirection dir in Enum.GetValues(typeof(FlowDirection)))
                    {
                        if (dir == FlowDirection._)
                            continue;

                        var pos = dir.FromPosition(current);

                        ushort cost = integrationField[pos].Cost;
                        if (cost < best)
                        {
                            best = cost;
                            direction = dir;
                        }
                    }

                    _cells[y * Width + x] = new FlowCell(direction);
                }
            }
        }

        private int Normalize(Position position)
        {
            int x = ((position.X % Width) + Width) % Width;
            int y = ((position.Y % Height) + Height) % Height;

            return y * Width + x;
        }
    }
}
