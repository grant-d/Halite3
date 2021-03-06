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

        public static CostField CreateMine(Game game, IDictionary<Position, byte> customCosts)
        {
            Debug.Assert(game != null);

            var maxHalite = game.Map.MaxHalite;

            // If mine <= 9, then moveCost = 9/10 == 0. So we must always keep mine >= 10.
            // Then add enough for one more dig: 10 * 4/3 = 13.33. 13.33 * 0.75 == 10.
            double ratio = (Constants.ExtractRatio - 1.0) / Constants.ExtractRatio; // 0.75
            double extra = Constants.MoveCostRatio / ratio; // 13.33
            double log75 = Math.Log10(ratio); // -0.125

            // Normalize the amount of halite, with exponential growth towards peaks
            // halite * 0.75^p == 13.33, so p = Log(13.33 / halite) / Log(0.75)
            const double flattness = 25; // Higher is more flat. Must be > 0 else div-by-zero
            double Potential(double halite) => Math.Log10(extra / (maxHalite + flattness - halite)) / log75;

            double minPotential = Potential(1); // 2.321
            double maxPotential = Potential(maxHalite); // 940 -> 14.884
            double potentialRange = maxPotential - minPotential; // 12.563

            var cells = new byte[game.Map.Width * game.Map.Height];

            SetCustomCosts(game, customCosts, cells, true);

            for (int y = 0; y < game.Map.Height; y++)
            {
                for (int x = 0; x < game.Map.Width; x++)
                {
                    int ix = y * game.Map.Width + x;
                    if (cells[ix] != Wall) // Skip placeholders
                    {
                        // By default all cells are 1 (0 is goal, 255 is wall)
                        double h1 = Math.Max(1, game.Map[x, y].Halite);

                        // Normalize
                        double potential = Potential(h1);

                        // Scale to 0..1
                        double h2 = (potential - minPotential) / potentialRange;

                        // Scale to 0..253
                        double h3 = h2 * (Peak - Valley); // 254-1

                        // Shift to 1..254
                        double h4 = h3 + 1;

                        // Invert since we want rich valleys. So 1->254, 254->1
                        byte halite = (byte)(byte.MaxValue - h4);

                        Debug.Assert(halite > 0 && halite < byte.MaxValue, $"MINE {h1}, {h2}, {h3}, _{halite}_, {maxHalite}, {minPotential}, {potential}, {maxPotential}");

                        cells[ix] = halite;
                    }
                }
            }

            SetCustomCosts(game, customCosts, cells, false);

            var field = new CostField(game.Map.Width, game.Map.Height, cells);
            return field;
        }

        public static CostField CreateHome(Game game, IDictionary<Position, byte> customCosts)
        {
            Debug.Assert(game != null);

            var maxHalite = game.Map.MaxHalite;

            // If mine <= 9, then moveCost = 9/10 == 0. So we must always keep mine >= 10.
            // Then add enough for one more dig: 10 * 4/3 = 13.33. 13.33 * 0.75 == 10.
            double ratio = (Constants.ExtractRatio - 1.0) / Constants.ExtractRatio; // 0.75
            double extra = Constants.MoveCostRatio / ratio; // 13.33
            double log75 = Math.Log10(ratio); // -0.125

            double minPotential = Potential(1); // 2.321
            double maxPotential = Potential(maxHalite); // 940 -> 14.884
            double potentialRange = maxPotential - minPotential; // 12.563

            // halite * 0.75^p == 13.33, so p = Log(13.33 / halite) / Log(0.75)
            const double flattness = 25; // Higher is more flat. Must be > 0 else div-by-zero
            double Potential(double halite) => Math.Log10(extra / (halite + flattness)) / log75;

            var cells = new byte[game.Map.Width * game.Map.Height];

            SetCustomCosts(game, customCosts, cells, true);

            for (int y = 0; y < game.Map.Height; y++)
            {
                for (int x = 0; x < game.Map.Width; x++)
                {
                    int ix = y * game.Map.Width + x;
                    if (cells[ix] != Wall) // Skip placeholders
                    {
                        // By default all cells are 1 (0 is goal, 255 is wall)
                        double h1 = Math.Max(1, game.Map[x, y].Halite);

                        // Normalize
                        double potential = Potential(h1);

                        // Scale to 0..1
                        double h2 = (potential - minPotential) / potentialRange;

                        // Scale to 0..253
                        double h3 = h2 * (Peak - Valley); // 254-1

                        // Shift to 1..254
                        double h4 = h3 + 1;

                        // Do not invert since we want barren valleys.
                        byte halite = (byte)h4;

                        Debug.Assert(halite > 0 && halite < byte.MaxValue, $"HOME {h1}, {h2}, {h3}, _{halite}_, {maxHalite}, {minPotential}, {potential}, {maxPotential}");

                        cells[ix] = halite;
                    }
                }
            }

            SetCustomCosts(game, customCosts, cells, false);

            var field = new CostField(game.Map.Width, game.Map.Height, cells);
            return field;
        }

        private static void SetCustomCosts(Game game, IDictionary<Position, byte> customCosts, byte[] cells, bool isPlaceholder)
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

        public static CostField Compress(Map map, int xRadius, int yRadius)
        {
            Debug.Assert(map != null);
            Debug.Assert(xRadius > 0);
            Debug.Assert(yRadius > 0);

            int xLen = xRadius * 2 + 1; // 1->3, 2->5, 3->7
            int yLen = yRadius * 2 + 1;

            int width = 1 + map.Width / xLen; // (32) 1->11, 2->7, 3->5
            int height = 1 + map.Height / yLen; // (64) 1->22, 2->13, 3->10

            var cells = new byte[width * height];
            var maxVal = xLen * yLen * byte.MaxValue;

            for (int x = xRadius, xx = 0; x < map.Width; x += xLen, xx++)
            {
                for (int y = yRadius, yy = 0; y < map.Height; y += yLen, yy++)
                {
                    int sum = 0;
                    for (int x1 = x - xRadius; x1 < x + xRadius; x1++)
                    {
                        for (int y1 = y - yRadius; y1 < y + yRadius; y1++)
                        {
                            sum += map[x1, y1].Halite;
                        }
                    }

                    // Normalize
                    cells[yy * width + xx] = (byte)(byte.MaxValue * sum / maxVal);
                }
            }

            var field = new CostField(width, height, cells);
            return field;
        }
    }
}
