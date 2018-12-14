using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Halite3.Hlt
{
    public sealed class CostField
    {
        public const byte Goal = 0;
        public const byte Valley = 1;
        public const byte Peak = 254;
        public const byte Wall = byte.MaxValue; // 255

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

        private CostField(int width, int height, Byte[] cells)
        {
            Debug.Assert(width > 0);
            Debug.Assert(height > 0);
            Debug.Assert(cells != null);
            Debug.Assert(cells.Length == width * height);

            Width = width;
            Height = height;

            _cells = cells;
        }

        public static CostField CreateMine(Game game, int maxHalite, IReadOnlyDictionary<Position, byte> customCosts)
        {
            Debug.Assert(game != null);

            // If mine <= 9, then moveCost = 9/10 == 0. So we must always keep mine >= 10.
            // Then add enough for one more dig: 10 * 4/3 = 13.33. 13.33 * 0.75 == 10.
            double ratio = (Constants.ExtractRatio - 1.0) / Constants.ExtractRatio; // 0.75
            double extra = Constants.MoveCostRatio / ratio; // 13.33
            double log75 = Math.Log(ratio);

            // halite * 0.75^p == 13.33
            double Potential(double halite) => Math.Log(extra / (maxHalite + 1 - halite)) / log75; // +1 else div-by-zero
            double minPotential = Potential(0);
            double maxPotential = Potential(maxHalite);

            var cells = new byte[game.Map.Width * game.Map.Height];

            SetCustomCosts(game, customCosts, cells, true);

            for (int y = 0; y < game.Map.Height; y++)
            {
                for (int x = 0; x < game.Map.Width; x++)
                {
                    int ix = y * game.Map.Width + x;
                    if (cells[ix] != Wall) // We used wall as a placeholder
                    {
                        double h1 = game.Map[x, y].Halite;
                        double h2 = Math.Max(1, h1); // Else div-by-zero

                        // Normalize the amount of halite, with exponential growth towards peaks
                        double potential = Potential(h2);
                        double h3 = (potential - minPotential) / (maxPotential - minPotential); // 0..1
                        double h4 = h3 * (Peak - Valley); // 0..253
                        double h5 = h4 + 1; // 1..254

                        double halite = byte.MaxValue - h5; // 254..1

                        Debug.Assert(halite > 0 && halite < byte.MaxValue, $"MINE {h1}, {h2}, {h3}, {h4}, {halite}, {minPotential}, {potential}, {maxPotential}");

                        cells[ix] = (byte)halite;
                    }
                }
            }

            SetCustomCosts(game, customCosts, cells, false);

            var field = new CostField(game.Map.Width, game.Map.Height, cells);
            return field;
        }

        public static CostField CreateHome(Game game, int maxHalite, IReadOnlyDictionary<Position, byte> customCosts)
        {
            Debug.Assert(game != null);

            // If mine <= 9, then moveCost = 9/10 == 0. So we must always keep mine >= 10.
            // Then add enough for one more dig: 10 * 4/3 = 13.33. 13.33 * 0.75 == 10.
            double ratio = (Constants.ExtractRatio - 1.0) / Constants.ExtractRatio; // 0.75
            double extra = Constants.MoveCostRatio / ratio; // 13.33
            double log75 = Math.Log(ratio);

            // halite * 0.75^p == 13.33
            double Potential(double halite) => Math.Log(extra / (maxHalite + 1 - halite)) / log75; // +1 else div-by-zero
            double minPotential = Potential(0);
            double maxPotential = Potential(maxHalite);

            var cells = new byte[game.Map.Width * game.Map.Height];

            SetCustomCosts(game, customCosts, cells, true);

            for (int y = 0; y < game.Map.Height; y++)
            {
                for (int x = 0; x < game.Map.Width; x++)
                {
                    int ix = y * game.Map.Width + x;
                    if (cells[ix] != Wall) // We used wall as a placeholder
                    {
                        double h1 = game.Map[x, y].Halite;
                        double h2 = Math.Max(1, h1); // Else div-by-zero

                        // Normalize the amount of halite, with exponential growth towards peaks
                        double potential = Potential(h2);
                        double h3 = (potential - minPotential) / (maxPotential - minPotential); // 0..1
                        double h4 = h3 * (Peak - Valley); // 0..253
                        double h5 = h4 + 1; // 1..254

                        double halite = byte.MaxValue - h5; // 254..1

                        Debug.Assert(halite > 0 && halite < byte.MaxValue, $"MINE {h1}, {h2}, {h3}, {h4}, {halite}, {minPotential}, {potential}, {maxPotential}");

                        cells[ix] = (byte)halite;
                    }
                }
            }

            SetCustomCosts(game, customCosts, cells, false);

            var field = new CostField(game.Map.Width, game.Map.Height, cells);
            return field;
        }

        private static void SetCustomCosts(Game game, IReadOnlyDictionary<Position, byte> customCosts, byte[] cells, bool isPlaceholder)
        {
            if (customCosts != null)
            {
                foreach (KeyValuePair<Position, byte> kvp in customCosts)
                {
                    int ix = Position.ToIndex(kvp.Key.X, kvp.Key.Y, game.Map.Width, game.Map.Height);
                    cells[ix] = isPlaceholder ? Wall : kvp.Value; // Use wall as a placeholder
                }
            }
        }
    }
}
