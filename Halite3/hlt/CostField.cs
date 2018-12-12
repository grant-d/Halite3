using System.Diagnostics;

namespace Halite3.Hlt
{
    public sealed class CostField
    {
        private readonly CostCell[] _cells;

        public int Width { get; }
        public int Height { get; }

#pragma warning disable CA1043 // Use Integral Or String Argument For Indexers
        public CostCell this[Position position]
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

        public CostField(Game game, CostCell myDrop, CostCell theirDrop)
        {
            Debug.Assert(game != null);

            Width = game.Map.Width;
            Height = game.Map.Height;

            (_, int maxHalite) = game.Map.GetMinMaxHalite();

            _cells = new CostCell[Height * Width];

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    CostCell cost = CostCell.Min; // 1

                    MapCell mapCell = game.Map[new Position(x, y)];

                    if (mapCell.HasStructure)
                    {
                        if (mapCell.Structure.Owner == game.MyId)
                        {
                            cost = myDrop;
                        }
                        else
                        {
                            cost = theirDrop;
                        }
                    }
                    else
                    {
                        double norm = 1 + mapCell.Halite * 253.0 / maxHalite; // 1-254
                        cost = new CostCell((byte)norm);
                    }

                    _cells[y * Width + x] = cost;
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
