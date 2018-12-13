using System.Diagnostics;

namespace Halite3.Hlt
{
    public static class GameExtensions
    {
        public static (int Min, int Max, int Total, int Mean) GetHaliteStatistics(this Map map)
        {
            Debug.Assert(map != null);

            int min = int.MaxValue;
            int max = 0;
            int sum = 0;

            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    MapCell cell = map[x, y];

                    sum += cell.Halite;

                    if (cell.Halite < min || min == int.MaxValue)
                        min = cell.Halite;

                    if (cell.Halite > max)
                        max = cell.Halite;
                }
            }

            return (min, max, sum, sum / (map.Height * map.Width));
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
                    int dist = game.Map.GetManhattanDistance(ship.Position, dropoff.Position);

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

            int dist = game.Map.GetManhattanDistance(ship.Position, game.Me.Shipyard.Position);

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

            MapCell cell = game.Map[position.X, position.Y];

            if (cell.HasStructure
                && cell.Structure is Dropoff drop
                && drop.Owner == game.MyId)
            {
                return true;
            }

            return false;
        }

        public static bool IsNextToDrop(this Game game, Position position, out Position target)
        {
            Debug.Assert(game != null);
            Debug.Assert(position != null);

            target = position;

            foreach (Direction dir in DirectionExtensions.AllCardinals)
            {
                Position pos = position.DirectionalOffset(dir);

                if (IsOnDrop(game, pos))
                {
                    target = pos;
                    return true;
                }
            }

            return false;
        }

        public static bool IsShipyardHijacked(this Game game)
        {
            Debug.Assert(game != null);

            if (!game.Map[game.Me.Shipyard].IsOccupied)
                return false;

            if (game.Map[game.Me.Shipyard].Ship.Owner == game.MyId)
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
