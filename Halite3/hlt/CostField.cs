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
            double Potential(double halite) => Math.Log(extra / (maxHalite + 1 - halite)) / log75;
            double minPotential = Potential(0);
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
                        double h1 = mapCell.Halite;
                        double h2 = Math.Max(1, h1);

                        //halite = 253 * halite / maxHalite; // Linear

                        // Normalize the amount of halite
                        double potential = Potential(h2);
                        double h3 = (potential - minPotential) / (maxPotential - minPotential); // 0..1
                        double h4 = h3 * 253;
                        double h5 = h4 + 1; // 1..254

                        double halite = h5;
                        if (richIsCheap)
                            halite = byte.MaxValue - halite; // 254..1

                        Debug.Assert(halite > 0 && halite < byte.MaxValue, $"{h1}, {h2}, {h3}, {h4}, {halite}, {minPotential}, {potential}, {maxPotential}"); // 1..254

                        cost = new CostCell((byte)halite);
                    }

                    _cells[y * Width + x] = cost;
                }
            }
        }
    }
}
