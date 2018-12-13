using System.Collections.Generic;
using System.Diagnostics;

namespace Halite3.Hlt
{
    public sealed class WaveField
    {
        private readonly WaveCell[] _cells;

        public int Width { get; }
        public int Height { get; }

        public WaveCell this[int x, int y]
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

        public WaveField(CostField costField, Position goal)
        {
            Debug.Assert(costField != null);
            Debug.Assert(goal != null);

            Width = costField.Width;
            Height = costField.Height;

            _cells = new WaveCell[Height * Width];
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    // 1 - The algorithm starts by resetting the value of all cells to a large value (65535).
                    _cells[y * Width + x] = WaveCell.Max;
                }
            }

            // 2 - The goal node then gets its total path cost set to zero.
            this[goal.X, goal.Y] = WaveCell.Goal;

            // 2 - And gets added to the open list.
            // From this point the goal node is treated like a normal node.
            var openList = new Queue<Position>();
            openList.Enqueue(goal);

            // 5. This algorithm continues until the open list is empty.
            while (openList.Count > 0)
            {
                // 3 - The current node is made equal to the node at the beginning of the open list
                // and gets removed from the list.
                Position currentPos = openList.Dequeue();

                var neighbors = new Position[4]
                {
                    new Position(currentPos.X, currentPos.Y - 1), // N
                    new Position(currentPos.X - 1, currentPos.Y), // W
                    new Position(currentPos.X + 1, currentPos.Y), // E
                    new Position(currentPos.X, currentPos.Y + 1), // S
                };

                for (int i = 0; i < neighbors.Length; i++)
                {
                    Position neighbor = neighbors[i];

                    // 4- If the neighbor has a cost of 255 then it gets ignored completely.
                    byte neighborCell = costField[neighbor.X, neighbor.Y];
                    if (neighborCell == CostField.WallCost)
                        continue;

                    // 4 - All of the current node’s neighbors get their total cost set to the current node’s cost
                    // plus their cost read from the cost field,
                    ushort cost = (ushort)(this[currentPos.X, currentPos.Y].Cost + neighborCell);

                    // 4 - This happens if and only if the new calculated cost is lower than the old cost.
                    if (cost < this[neighbor.X, neighbor.Y].Cost)
                    {
                        this[neighbor.X, neighbor.Y] = new WaveCell(cost);

                        // 4 - Then they get added to the back of the open list.
                        if (!openList.Contains(neighbor))
                            openList.Enqueue(neighbor);
                    }
                }
            }
        }
    }
}
