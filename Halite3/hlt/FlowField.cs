using System;
using System.Diagnostics;

namespace Halite3.Hlt
{
    public sealed class FlowField
    {
        private readonly FlowCell[] _cells;

        public int Width { get; }
        public int Height { get; }

        public FlowCell this[int x, int y]
        {
            get
            {
                int index = Position.ToIndex(x, y, Width, Height);
                return _cells[index];
            }
        }

        public FlowField(WaveField waveField)
        {
            Debug.Assert(waveField != null);

            Width = waveField.Width;
            Height = waveField.Height;

            _cells = new FlowCell[Height * Width];

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    var current = new Position(x, y);

                    ushort best = WaveField.Max;
                    Direction direction = Direction.X;

                    Check(Direction.N);
                    Check(Direction.E);
                    Check(Direction.S);
                    Check(Direction.W);

                    _cells[y * Width + x] = new FlowCell(direction);

                    void Check(Direction dir)
                    {
                        Position pos = dir.FromPosition(current);

                        ushort cost = waveField[pos.X, pos.Y];
                        if (cost < best)
                        {
                            best = cost;
                            direction = dir;
                        }
                    }
                }
            }
        }

        public Position GetTarget(Position origin)
        {
            FlowCell flow = _cells[origin.Y * Width + origin.X];
            Position target = flow.Direction.FromPosition(origin);
            return target;
        }
    }
}
