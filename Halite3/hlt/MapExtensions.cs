using System.Diagnostics;

namespace Halite3.Hlt
{
    public static class MapExtensions
    {
        public static Position GetRichestLocalTriangle(this Map map, Position position)
        {
            Debug.Assert(map != null);
            Debug.Assert(position != null);

            // North
            Position north = position.DirectionalOffset(Direction.North);
            int n = Sum(north, Direction.North, Direction.East, Direction.West);
            int best = n;
            Position mine = north;

            // East
            Position east = position.DirectionalOffset(Direction.East);
            int e = Sum(east, Direction.North, Direction.East, Direction.South);
            if (e > best) { mine = east; best = e; }

            // South
            Position south = position.DirectionalOffset(Direction.South);
            int s = Sum(south, Direction.East, Direction.South, Direction.West);
            if (s > best) { mine = south; best = s; }

            // West
            Position west = position.DirectionalOffset(Direction.West);
            int w = Sum(west, Direction.North, Direction.South, Direction.West);
            if (w > best) { mine = west; }

            return mine;

            int Sum(Position pos, Direction dir1, Direction dir2, Direction dir3)
            {
                int sum = map.At(pos).Halite;

                Position p1 = north.DirectionalOffset(dir1);
                sum += map.At(p1).Halite;

                Position p2 = north.DirectionalOffset(dir2);
                sum += map.At(p2).Halite;

                Position p3 = north.DirectionalOffset(dir3);
                sum += map.At(p3).Halite;

                return sum;
            }
        }

        public static Position GetRichestLocalSquare(this Map map, Position position, int radius = 1)
        {
            Debug.Assert(map != null);
            Debug.Assert(position != null);
            Debug.Assert(radius >= 0 && radius <= map.Width && radius <= map.Height);

            if (radius == 0)
                return position;

            // North
            var north = new Position(position.X, position.Y - radius);
            int n = Sum(north);
            int best = n;
            Position mine = north;

            // East
            var east = new Position(position.X + radius, position.Y);
            int e = Sum(east);
            if (e > best) { mine = east; best = e; }

            // South
            var south = new Position(position.X, position.Y + radius);
            int s = Sum(south);
            if (s > best) { mine = south; best = s; }

            // West
            var west = new Position(position.X - radius, position.Y);
            int w = Sum(west);
            if (w > best) { mine = west; }

            return mine;

            int Sum(Position pos)
            {
                int sum = map.At(pos).Halite;

                if (radius == 0)
                    return sum;

                for (int x = pos.X - radius; x <= pos.X + radius; x++)
                {
                    for (int y = pos.Y - radius; y <= pos.Y + radius; y++)
                    {
                        sum += map.At(new Position(x, y)).Halite;
                    }
                }

                return sum;
            }
        }

        public static Position GetRichestLocalRadius(this Map map, Position position, int radius)
        {
            Debug.Assert(map != null);
            Debug.Assert(position != null);
            Debug.Assert(radius >= 0 && radius <= map.Width && radius <= map.Height);

            if (radius == 0)
                return position;

            Position mine = position;
            int halite = map.At(mine).Halite;
            for (int x = position.X - radius; x <= position.X + radius; x++)
            {
                for (int y = position.Y - radius; y <= position.Y + radius; y++)
                {
                    var pos = new Position(x, y);
                    MapCell cell = map.At(pos);

                    if (cell.IsEmpty
                        && cell.Halite > halite)
                    {
                        halite = map.At(pos).Halite;
                        mine = pos;
                    }
                }
            }

            return mine;
        }
    }
}
