using System;
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

        public CostField(Game game, int maxHalite, CostCell myDrop, CostCell theirDrop, bool richIsCheap)
        {
            Debug.Assert(game != null);

            Width = game.Map.Width;
            Height = game.Map.Height;

            _cells = new CostCell[Height * Width];

            // If mine <= 9, then moveCost = 9/10 == 0. So we must always keep mine >= 10.
            // Then add enough for one more dig: 10 * 4/3 = 13.33. 13.33 * 0.75 == 10.
            double ratio = (Constants.ExtractRatio - 1.0) / Constants.ExtractRatio; // 0.75
            double extra = Constants.MoveCostRatio / ratio; // 13.33
            double log75 = Math.Log(ratio);

            // halite * 0.75^p == 13.33
            double Potential(double halite) => Math.Max(0, Math.Log(extra / halite) / log75);

            double maxPotential = Potential(maxHalite);

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
                        double halite = Math.Max(1, mapCell.Halite);

                        // Normalize the amount of halite
                        double potential = Potential(halite);
                        halite = 253 * potential / maxPotential; // 0..253
                        halite += 1; // 1..254

                        if (richIsCheap)
                            halite = byte.MaxValue - halite; // 254..1

                        Debug.Assert(halite > 0 && halite < byte.MaxValue, $"{halite}"); // 1..254

                        cost = new CostCell((byte)halite);
                    }

                    _cells[y * Width + x] = cost;
                }
            }
        }
    }
}
