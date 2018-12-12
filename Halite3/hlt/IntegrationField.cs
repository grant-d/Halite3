using System.Collections.Generic;
using System.Diagnostics;

namespace Halite3.Hlt
{
    // https://leifnode.com/2013/12/flow-field-pathfinding/

    public sealed class IntegrationField
    {
        private readonly IntegrationCell[][] _cells;

        public int Width { get; }
        public int Height { get; }

#pragma warning disable CA1043 // Use Integral Or String Argument For Indexers
        public IntegrationCell this[Position position]
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

        public IntegrationField(CostField costField, Position root)
        {
            Debug.Assert(costField != null);
            Debug.Assert(root != null);

            Width = costField.Width;
            Height = costField.Height;

            _cells = new IntegrationCell[Height][];
            for (int y = 0; y < Height; y++)
            {
                _cells[y] = new IntegrationCell[Width];

                for (int x = 0; x < Width; x++)
                {
                    // 1 - The algorithm starts by resetting the value of all cells to a large value (65535).
                    _cells[y][x] = IntegrationCell.Max;
                }
            }

            Build(costField, root);
        }

        private void Build(CostField costField, Position goal)
        {
            Debug.Assert(costField != null);
            Debug.Assert(goal != null);

            // 2 - The goal node then gets its total path cost set to zero.
            this[goal] = IntegrationCell.Zero;

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
                    new Position(currentPos.X + 1, currentPos.Y), // E
                    new Position(currentPos.X, currentPos.Y + 1), // S
                    new Position(currentPos.X - 1, currentPos.Y), // W
                };

                for (int i = 0; i < neighbors.Length; i++)
                {
                    Position neighbor = neighbors[i];

                    // 4- If the neighbor has a cost of 255 then it gets ignored completely.
                    CostCell neighborCell = costField[neighbor];
                    if (neighborCell == CostCell.Wall)
                        continue;

                    // 4 - All of the current node’s neighbors get their total cost set to the current node’s cost
                    // plus their cost read from the cost field,
                    ushort cost = (ushort)(this[currentPos].Cost + neighborCell.Cost);

                    // 4 - This happens if and only if the new calculated cost is lower than the old cost.
                    if (cost < this[neighbor].Cost)
                    {
                        this[neighbor] = new IntegrationCell(cost);

                        // 4 - Then they get added to the back of the open list.
                        if (!openList.Contains(neighbor))
                            openList.Enqueue(neighbor);
                    }
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
