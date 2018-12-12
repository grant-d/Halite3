using System.Diagnostics;

namespace Halite3.Hlt
{
    public static class MapExtensions
    {
        public static (Position Position, int Halite) GetRichestLocalTriangle(this Map map, Position position)
        {
            Debug.Assert(map != null);
            Debug.Assert(position != null);

            // North
            Position north = position.DirectionalOffset(Direction.North);
            int n = Sum(north, Direction.North, Direction.East, Direction.West);
            int halite = n;
            Position mine = north;

            // East
            Position east = position.DirectionalOffset(Direction.East);
            int e = Sum(east, Direction.North, Direction.East, Direction.South);
            if (e > halite) { mine = east; halite = e; }

            // South
            Position south = position.DirectionalOffset(Direction.South);
            int s = Sum(south, Direction.East, Direction.South, Direction.West);
            if (s > halite) { mine = south; halite = s; }

            // West
            Position west = position.DirectionalOffset(Direction.West);
            int w = Sum(west, Direction.North, Direction.South, Direction.West);
            if (w > halite) { mine = west; halite = w; }

            return (mine, halite);

            int Sum(Position pos, Direction dir1, Direction dir2, Direction dir3)
            {
                int sum = map[pos].Halite;

                Position p1 = north.DirectionalOffset(dir1);
                sum += map[p1].Halite;

                Position p2 = north.DirectionalOffset(dir2);
                sum += map[p2].Halite;

                Position p3 = north.DirectionalOffset(dir3);
                sum += map[p3].Halite;

                return sum;
            }
        }

        public static (Position Position, int Halite) GetRichestLocalSquare(this Map map, Position position, int radius = 1)
        {
            Debug.Assert(map != null);
            Debug.Assert(position != null);
            Debug.Assert(radius >= 0 && radius <= map.Width && radius <= map.Height);

            if (radius == 0)
                return (position, Sum(position).Sum);

            // North
            var north = new Position(position.X, position.Y - radius);
            var n = Sum(north);
            int halite = n.Sum;
            Position mine = n.Position;

            // East
            var east = new Position(position.X + radius, position.Y);
            var e = Sum(east);
            if (e.Sum > halite) { mine = e.Position; halite = e.Sum; }

            // South
            var south = new Position(position.X, position.Y + radius);
            var s = Sum(south);
            if (s.Sum > halite) { mine = s.Position; halite = s.Sum; }

            // West
            var west = new Position(position.X - radius, position.Y);
            var w = Sum(west);
            if (w.Sum > halite) { mine = w.Position; halite = w.Sum; }

            return (mine, halite);

            (int Sum, Position Position) Sum(Position center)
            {
                int sum = map[center].Halite;

                if (radius == 0)
                    return (sum, center);

                int bestHal = 0;
                Position bestPol = center;

                for (int x = center.X - radius; x <= center.X + radius; x++)
                {
                    for (int y = center.Y - radius; y <= center.Y + radius; y++)
                    {
                        var pos = new Position(x, y);

                        int hal = map[pos].Halite;
                        if (hal > bestHal)
                        {
                            bestHal = hal;
                            bestPol = pos;
                        }

                        sum += hal;
                    }
                }

                return (sum, bestPol);
            }
        }

        public static (Position Position, int Halite) GetRichestLocalRadius(this Map map, Position position, int radius)
        {
            Debug.Assert(map != null);
            Debug.Assert(position != null);
            Debug.Assert(radius >= 0 && radius <= map.Width && radius <= map.Height);

            Position mine = position;
            int halite = map[mine].Halite;

            if (radius == 0)
                return (mine, halite);

            for (int x = position.X - radius; x <= position.X + radius; x++)
            {
                for (int y = position.Y - radius; y <= position.Y + radius; y++)
                {
                    var pos = new Position(x, y);
                    MapCell cell = map[pos];

                    if (cell.IsEmpty
                        && cell.Halite > halite)
                    {
                        mine = pos;
                        halite = map[pos].Halite;
                    }
                }
            }

            return (mine, halite);
        }
    }
}
