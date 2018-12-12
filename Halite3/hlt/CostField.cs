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
                int index = position.ToIndex(Width, Height);
                return _cells[index];
            }

            private set
            {
                int index = position.ToIndex(Width, Height);
                _cells[index] = value;
            }
        }

        public CostField(Game game, CostCell myDrop, CostCell theirDrop, bool richIsCheap)
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
                    MapCell mapCell = game.Map[new Position(x, y)];

                    CostCell cost = CostCell.Min; // 1
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
                        // Normalize the amount of halite
                        int halite = 1 + mapCell.Halite * 253 / maxHalite; // 1-254

                        if (richIsCheap)
                            halite = 255 - halite; // 254-1

                        Debug.Assert(halite >= 1 && halite <= 254);
                        cost = new CostCell((byte)halite);
                    }

                    _cells[y * Width + x] = cost;
                }
            }
        }
    }
}
