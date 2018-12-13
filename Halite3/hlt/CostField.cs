using System;
using System.Diagnostics;

namespace Halite3.Hlt
{
    public sealed class CostField
    {
        public const byte GoalCost = 0;
        public const byte MinCost = 1;
        public const byte MaxCost = 254;
        public const byte WallCost = byte.MaxValue; // 255

        private readonly byte[] _cells;

        public int Width { get; }
        public int Height { get; }

        public byte this[int x, int y]
        {
            get
            {
                int index = Position.ToIndex(x, y, Width, Height);
                return _cells[index];
            }

            private set
            {
                int index = Position.ToIndex(x, y, Width, Height);
                _cells[index] = value;
            }
        }

        public CostField(Game game, int maxHalite, byte myDrop, byte theirDrop, bool richIsCheap)
        {
            Debug.Assert(game != null);

            Width = game.Map.Width;
            Height = game.Map.Height;

            _cells = new byte[Height * Width];

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
                    MapCell mapCell = game.Map[x, y];

                    byte cost = MinCost; // 1
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

                        // Normalize the amount of halite, with growth towards peaks
                        // being exponential instead of linear
                        double potential = Potential(h2);
                        double h3 = (potential - minPotential) / (maxPotential - minPotential); // 0..1
                        double h4 = h3 * (MaxCost - MinCost); // 0..253
                        double h5 = h4 + 1; // 1..254

                        double halite = h5;
                        if (richIsCheap)
                            halite = byte.MaxValue - halite; // 254..1

                        Debug.Assert(halite > 0 && halite < byte.MaxValue, $"{h1}, {h2}, {h3}, {h4}, {halite}, {minPotential}, {potential}, {maxPotential}");

                        cost = (byte)halite;
                    }

                    _cells[y * Width + x] = cost;
                }
            }
        }
    }
}
