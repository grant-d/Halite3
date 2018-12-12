using System.Collections.Generic;
using System.Diagnostics;

namespace Halite3.Hlt
{
    public sealed class CostCell
    {
        public const byte Wall = byte.MaxValue;

        public byte Mine { get; set; }
        public byte Home { get; set; }

        public CostCell(byte mine, byte home)
        {
            Mine = mine;
            Home = home;
        }
    }

    public sealed class CostField
    {
        private readonly CostCell[][] _cells;

        public int Width { get; }
        public int Height { get; }

        private CostField(int width, int height)
        {
            Width = width;
            Height = height;

            _cells = new CostCell[Height][];

            for (int y = 0; y < Height; y++)
            {
                _cells[y] = new CostCell[Width];

                for (int x = 0; x < Width; x++)
                {
                    _cells[y][x] = new CostCell(1, 1);
                }
            }
        }

        public static CostField Build(Game game)
        {
            Debug.Assert(game != null);
            int maxHalite = game.Map.GetMaxHalite();

            var costField = new CostField(game.Map.Width, game.Map.Height);

            for (int y = 0; y < costField.Height; y++)
            {
                for (int x = 0; x < costField.Width; x++)
                {
                    int mine = 1;
                    int home = 1;

                    MapCell mapCell = game.Map.At(new Position(x, y));

                    if (mapCell.HasStructure)
                    {
                        if (mapCell.Structure.Owner.Id == game.MyId.Id)
                        {
                            mine = CostCell.Wall;
                            home = 1;
                        }
                        else
                        {
                            mine = CostCell.Wall;
                            home = CostCell.Wall;
                        }
                    }
                    else
                    {
                        int norm = mapCell.Halite * 253 / maxHalite; // 0-253

                        mine = 254 - norm;
                        home = 1 + norm;
                    }

                    costField._cells[y][x] = new CostCell((byte)mine, (byte)home);
                }
            }

            return costField;
        }

        public CostCell At(Position position)
        {
            Position normalized = Normalize(position);
            return _cells[normalized.Y][normalized.X];
        }

        private Position Normalize(Position position)
        {
            int x = ((position.X % Width) + Width) % Width;
            int y = ((position.Y % Height) + Height) % Height;
            return new Position(x, y);
        }
    }

    public sealed class FlowCell
    {
        public ushort Mine { get; set; }

        public ushort Home { get; set; }

        public FlowCell(ushort mine, ushort home)
        {
            Mine = mine;
            Home = home;
        }
    }

    public sealed class FlowField
    {
        private readonly FlowCell[][] _cells;

        public int Width { get; }
        public int Height { get; }

        private FlowField(int width, int height)
        {
            Width = width;
            Height = height;

            _cells = new FlowCell[Height][];
            for (int y = 0; y < Height; y++)
            {
                _cells[y] = new FlowCell[Width];

                for (int x = 0; x < Width; x++)
                {
                    _cells[y][x] = new FlowCell(ushort.MaxValue, ushort.MaxValue);
                }
            }
        }

        public static FlowField Build(CostField costField)
        {
            Debug.Assert(costField != null);

            // https://leifnode.com/2013/12/flow-field-pathfinding/

            // 1 - The algorithm starts by resetting the value of all cells to a large value (I use 65535).
            var flowField = new FlowField(costField.Width, costField.Height);

            // 2 - The goal node then gets its total path cost set to zero and gets added to the open list.
            // From this point the goal node is treated like a normal node.
            var goalPos = new Position(0, 0);
            flowField.At(goalPos).Mine = 0;

            var openList = new Queue<Position>();
            openList.Enqueue(goalPos);

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

                for (var i = 0; i < neighbors.Length; i++)
                {
                    Position neighbor = neighbors[i];

                    // 4- If the neighbor has a cost of 255 then it gets ignored completely.
                    var neighborCost = costField.At(neighbor).Mine;
                    if (neighborCost == CostCell.Wall)
                        continue;

                    // 4 - All of the current node’s neighbors get their total cost set to the current node’s cost
                    // plus their cost read from the cost field,
                    ushort mineCost = (ushort)(costField.At(currentPos).Mine + neighborCost);

                    // 4 - This happens if and only if the new calculated cost is lower than the old cost.
                    if (mineCost < flowField.At(neighbor).Mine)
                    {
                        flowField.At(neighbor).Mine = mineCost;

                        // 4 - Then they get added to the back of the open list.
                        if (!openList.Contains(neighbor))
                            openList.Enqueue(neighbor);
                    }
                }
            }

            return flowField;
        }

        public FlowCell At(Position position)
        {
            Position normalized = Normalize(position);
            return _cells[normalized.Y][normalized.X];
        }

        private Position Normalize(Position position)
        {
            int x = ((position.X % Width) + Width) % Width;
            int y = ((position.Y % Height) + Height) % Height;
            return new Position(x, y);
        }
    }

    public static class GameExtensions
    {
        public static int GetMaxHalite(this Map map)
        {
            Debug.Assert(map != null);

            int maxHalite = 0;
            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    MapCell mapCell = map.At(new Position(x, y));

                    if (mapCell.Halite > maxHalite)
                        maxHalite = mapCell.Halite;
                }
            }

            return maxHalite;
        }

        public static (Position Position, int Distance) GetClosestDrop(this Game game, Ship ship)
        {
            Debug.Assert(game != null);
            Debug.Assert(ship != null);

            Position pos = game.Me.Shipyard.Position;
            int steps = game.Map.GetManhattanDistance(ship.Position, pos);

            if (game.Me.Dropoffs != null)
            {
                foreach (Dropoff dropoff in game.Me.Dropoffs.Values)
                {
                    var dist = game.Map.GetManhattanDistance(ship.Position, dropoff.Position);

                    if (dist <= steps)
                    {
                        pos = dropoff.Position;
                        steps = dist;
                    }
                }
            }

            return (pos, steps);
        }

        public static int GetAggregateDistanceFromAllDrops(this Game game, Ship ship)
        {
            Debug.Assert(game != null);
            Debug.Assert(ship != null);

            var dist = game.Map.GetManhattanDistance(ship.Position, game.Me.Shipyard.Position);

            if (game.Me.Dropoffs != null)
            {
                foreach (Dropoff dropoff in game.Me.Dropoffs.Values)
                {
                    dist += game.Map.GetManhattanDistance(ship.Position, dropoff.Position);
                }
            }

            return dist;
        }

        public static bool IsOnDrop(this Game game, Position position)
        {
            Debug.Assert(game != null);
            Debug.Assert(position != null);

            if (position == game.Me.Shipyard.Position)
            {
                return true;
            }

            MapCell cell = game.Map.At(position);

            if (cell.HasStructure
                && cell.Structure is Dropoff drop
                && drop.Owner.Id == game.MyId.Id)
            {
                return true;
            }

            return false;
        }

        public static bool IsNextToDrop(this Game game, Position position, out Direction direction, out Position target)
        {
            Debug.Assert(game != null);
            Debug.Assert(position != null);

            direction = Direction.Still;
            target = position;

            foreach (Direction dir in DirectionExtensions.AllCardinals)
            {
                Position pos = position.DirectionalOffset(dir);

                if (IsOnDrop(game, pos))
                {
                    direction = dir;
                    target = pos;
                    return true;
                }
            }

            return false;
        }

        public static bool IsShipyardHijacked(this Game game)
        {
            Debug.Assert(game != null);

            if (!game.Map.At(game.Me.Shipyard).IsOccupied)
                return false;

            if (game.Map.At(game.Me.Shipyard).Ship.Owner.Id == game.MyId.Id)
                return false;

            return true;
        }

        public static (Position Position, int Halite) GetRichestDropSquare(this Game game, int radius)
        {
            Debug.Assert(game != null);
            Debug.Assert(radius >= 0 && radius <= game.Map.Width && radius <= game.Map.Height);

            (Position pos, int halite) = game.Map.GetRichestLocalSquare(game.Me.Shipyard.Position, radius);

            foreach (Dropoff drop in game.Me.Dropoffs.Values)
            {
                (Position Position, int Halite) mine = game.Map.GetRichestLocalSquare(drop.Position, radius);
                if (mine.Halite > halite)
                {
                    pos = mine.Position;
                    halite = mine.Halite;
                }
            }

            return (pos, halite);
        }
    }
}
