using System;
using System.Diagnostics;

namespace Halite3.Hlt
{
    // https://leifnode.com/2013/12/flow-field-pathfinding/
    // https://gamedevelopment.tutsplus.com/tutorials/understanding-goal-based-vector-field-pathfinding--gamedev-9007
    // http://www.gameaipro.com/GameAIPro/GameAIPro_Chapter23_Crowd_Pathfinding_and_Steering_Using_Flow_Field_Tiles.pdf
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
                int index = position.ToIndex(Width, Height);
                return _cells[index];
            }

            private set
            {
                int index = position.ToIndex(Width, Height);
                _cells[index] = value;
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

                    ushort best = WaveCell.Max.Cost;
                    FlowDirection direction = FlowDirection._;

                    Check(FlowDirection.N);
                    Check(FlowDirection.E);
                    Check(FlowDirection.S);
                    Check(FlowDirection.W);

                    _cells[y * Width + x] = new FlowCell(direction);

                    void Check(FlowDirection dir)
                    {
                        Position pos = dir.FromPosition(current);

                        ushort cost = waveField[pos].Cost;
                        if (cost < best)
                        {
                            best = cost;
                            direction = dir;
                        }
                    }
                }
            }
        }
    }
}
