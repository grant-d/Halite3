using System.Diagnostics;

namespace Halite3.Hlt
{
    public static class GameExtensions
    {
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

        public static bool IsShipOnDrop(this Game game, Ship ship)
        {
            Debug.Assert(game != null);
            Debug.Assert(ship != null);

            if (ship.Position == game.Me.Shipyard.Position)
            {
                return true;
            }

            if (game.Me.Dropoffs != null)
            {
                foreach (Dropoff drop in game.Me.Dropoffs.Values)
                {
                    if (ship.Position == drop.Position)
                    {
                        return true;
                    }
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
    }
}
