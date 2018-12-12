using System.Diagnostics;

namespace Halite3.Hlt
{
    // https://leifnode.com/2013/12/flow-field-pathfinding/

    public sealed class CostField
    {
        private readonly CostCell[][] _cells;

        public int Width { get; }
        public int Height { get; }

#pragma warning disable CA1043 // Use Integral Or String Argument For Indexers
        public CostCell this[Position position]
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

        public CostField(Game game, CostCell myDrop, CostCell theirDrop)
        {
            Debug.Assert(game != null);

            Width = game.Map.Width;
            Height = game.Map.Height;

            (_, int maxHalite) = game.Map.GetMinMaxHalite();

            _cells = new CostCell[Height][];

            for (int y = 0; y < Height; y++)
            {
                _cells[y] = new CostCell[Width];

                for (int x = 0; x < Width; x++)
                {
                    CostCell cost = default;

                    MapCell mapCell = game.Map.At(new Position(x, y));

                    if (mapCell.HasStructure)
                    {
                        if (mapCell.Structure.Owner.Id == game.MyId.Id)
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
                        double norm = mapCell.Halite * 254.0 / maxHalite; // 0-254
                        cost = new CostCell((byte)norm);
                    }

                    _cells[y][x] = cost;
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
