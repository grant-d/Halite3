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

            const byte range = 253;

            // Precompute exponent table
            if (s_exp == null)
            {
                CacheExponents(maxHalite, range);
            }

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
                        int halite = s_exp[mapCell.Halite]; // 0..253

                        if (richIsCheap)
                            halite = range + 2 - halite; // 254..1

                        Debug.Assert(halite > 0 && halite < byte.MaxValue, $"{mapCell.Halite}, {halite}"); // 1..254
                        cost = new CostCell((byte)halite);
                    }

                    _cells[y * Width + x] = cost;
                }
            }
        }

        private static byte[] s_exp;

        private static void CacheExponents(int maxHalite, byte range)
        {
            double exp = Constants.ExtractRatio / (Constants.ExtractRatio - 1.0); // 1.33
            double max = Math.Pow(maxHalite * 1.0 / range, exp);
            double ratio = range / max;

            s_exp = new byte[1024];
            for (int i = 0; i < s_exp.Length; i++)
            {
                // Normalize the amount of halite favoring higher values
                double hal = ratio * Math.Pow(i * 1.0 / range, exp); // 0..253

                s_exp[i] = (byte)(1 + hal); // 1..254
            }
        }
    }
}
