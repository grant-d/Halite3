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

                    ushort mineBest = IntegrationCell.Wall;
                    FlowDirection mineDir = FlowDirection.None;

                    ushort homeBest = IntegrationCell.Wall;
                    FlowDirection homeDir = FlowDirection.None;

                    foreach (FlowDirection dir in Enum.GetValues(typeof(FlowDirection)))
                    {
                        var pos = dir.ToPosition(current);

                        ushort mine = integrationField.At(pos).Mine;
                        if (mine < mineBest)
                        {
                            mineBest = mine;
                            mineDir = dir;
                        }

                        ushort home = integrationField.At(pos).Home;
                        if (home < homeBest)
                        {
                            homeBest = home;
                            homeDir = dir;
                        }
                    }

                    _cells[y][x] = new FlowCell(mineDir, homeDir);
                }
            }
        }

        public FlowCell At(Position position)
        {
            Position normalized = Normalize(position);
            return _cells[normalized.Y][normalized.X];
        }

        private Position Normalize(Position position)
        {
            int x = ((position.X % Width) + Width) % Width;
            int y = ((position.Y % Height) + Height) % Height;
            return new Position(x, y);
        }
    }
}
