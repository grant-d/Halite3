using Halite3.Hlt;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Diagnostics;
using System.IO;

namespace Halite3
{
    public enum ShipState
    {
        None = 0,
        Headed,
        Mining,
        Returning,
        Converting,
        Ending
    }

    public sealed class ShipStatus
    {
        public ShipState State { get; set; }
    }

    public sealed class GnomeBot
    {
        public static void Main(string[] args)
        {
            //while (!Debugger.IsAttached);

            int rngSeed = args.Length > 1 ? int.Parse(args[1], CultureInfo.InvariantCulture) : DateTime.Now.Millisecond;
            var rng = new Random(rngSeed);

            using (var game = new Game())
            {
                // At this point "game" variable is populated with initial map data.
                // This is a good place to do computationally expensive start-up pre-processing.
                // As soon as you call "ready" function below, the 2 second per turn timer will start.
                Game.Ready("MyCSharpBot");
                Log.LogMessage("Successfully created bot! My Player ID is " + game.MyId + ". Bot rng seed is " + rngSeed + ".");

                int maxShips = 8 + game.Map.Width / 8;

                var states = new Dictionary<EntityId, ShipStatus>();

                while (true)
                {
                    game.UpdateFrame();

                    if (InitialSpawn(game))
                        continue;

                    var commandQueue = new List<Command>();

                    if (game.Me.Ships.Count >= maxShips
                        && game.Me.Dropoffs.Count < game.TurnNumber / 120
                        && game.TurnNumber <= 350
                        && game.Me.Halite > Constants.DropOffCost * 2)
                    {
                        int best = 0;
                        Ship sh = default;
                        foreach (Ship ship in game.Me.Ships.Values)
                        {
                            if (!states.TryGetValue(ship.Id, out ShipStatus status))
                                states.Add(ship.Id, status = new ShipStatus { State = ShipState.None });

                            if (status.State != ShipState.Mining)
                                continue;

                            int dist = GetTotalManhattanDistanceFromBases(ship, game);

                            if (dist > best)
                            {
                                best = dist;
                                sh = ship;
                            }
                        }

                        if (sh != default)
                        {
                            states[sh.Id].State = ShipState.Converting;
                            commandQueue.Add(sh.MakeDropoff());
                        }
                    }

                    foreach (Ship ship in game.Me.Ships.Values)
                    {
                        if (!states.TryGetValue(ship.Id, out ShipStatus status))
                            states.Add(ship.Id, status = new ShipStatus { State = ShipState.None });

                        // Keep track of closest base
                        (Position Position, int Steps) closestBase = GetClosestBase(ship, game);
                        var turnsRemaining = Constants.MaxTurns - game.TurnNumber;

                        Log.LogMessage($"Remaining {turnsRemaining} of {Constants.MaxTurns}");

                        if (turnsRemaining - game.Me.Ships.Count <= closestBase.Steps)
                        {
                            states[ship.Id].State = ShipState.Ending;
                        }

                        switch (status.State)
                        {
                            case ShipState.Converting:
                                continue;

                            case ShipState.None:
                                {
                                    states[ship.Id].State = ShipState.None;
                                    goto case ShipState.Mining;
                                }

                            case ShipState.Mining:
                                {
                                    if (ship.IsFull)
                                    {
                                        goto case ShipState.Returning;
                                    }

                                    if (game.Map.At(ship.Position).Halite > Constants.BarrenHalite)
                                    {
                                        states[ship.Id].State = ShipState.Mining;
                                        commandQueue.Add(ship.Stay());
                                        continue;
                                    }

                                    Position mine = GetBestMine(ship, game, 2);
                                    if (mine != ship.Position)
                                    {
                                        Direction dir = game.Map.NaiveNavigate(ship, mine);
                                        commandQueue.Add(ship.Move(dir));
                                    }
                                }
                                break;

                            case ShipState.Returning:
                                {
                                    states[ship.Id].State = ShipState.Returning;

                                    if (IsOnBase(ship, game))
                                    {
                                        goto case ShipState.None;
                                    }

                                    Direction dir = game.Map.NaiveNavigate(ship, closestBase.Position);
                                    commandQueue.Add(ship.Move(dir));
                                }
                                break;

                            case ShipState.Ending:
                                {
                                    states[ship.Id].State = ShipState.Ending;

                                    Direction dir = GetEndingDirection(ship, game, closestBase.Position);

                                    commandQueue.Add(ship.Move(dir));
                                }
                                break;
                        }
                    }

                    if (game.TurnNumber <= 350
                        && game.Me.Ships.Count <= maxShips
                        && game.Me.Halite > 1.5 * Constants.ShipCost
                        && !game.Map.At(game.Me.Shipyard).IsOccupied)
                    {
                        commandQueue.Add(Shipyard.Spawn());
                    }

                    Game.EndTurn(commandQueue);
                }
            }
        }

        private static int GetTotalManhattanDistanceFromBases(Ship ship, Game game)
        {
            var dist = game.Map.GetManhattanDistance(ship.Position, game.Me.Shipyard.Position);

            foreach (Dropoff dropoff in game.Me.Dropoffs.Values)
            {
                dist += game.Map.GetManhattanDistance(ship.Position, dropoff.Position);
            }

            return dist;
        }

        private static Direction GetEndingDirection(Ship ship, Game game, Position closestBase)
        {
            Direction dir = game.Map.NaiveNavigate(ship, closestBase);

            foreach (Direction direction in DirectionExtensions.AllCardinals)
            {
                if (ship.Position.DirectionalOffset(direction) == closestBase)
                {
                    dir = direction;
                    break;
                }
            }

            return dir;
        }

        private static bool InitialSpawn(Game game)
        {
            if (game.TurnNumber >= 6)
                return false;

            var commands = new List<Command>(5);
            foreach (Ship ship in game.Me.Ships.Values)
            {
                if (ship.Position == game.Me.Shipyard.Position)
                {
                    var dir = (Direction)"nesw"[game.TurnNumber - 2];
                    commands.Add(ship.Move(dir));
                }
            }

            if (game.TurnNumber <= 4)
                commands.Add(Shipyard.Spawn());

            Game.EndTurn(commands);
            return true;
        }

        private static (Position Position, int Steps) GetClosestBase(Ship ship, Game game)
        {
            Position pos = game.Me.Shipyard.Position;
            int steps = game.Map.GetManhattanDistance(ship.Position, pos);
            foreach (Dropoff dropoff in game.Me.Dropoffs.Values)
            {
                var dist = game.Map.GetManhattanDistance(ship.Position, dropoff.Position);
                if (dist <= steps)
                {
                    pos = dropoff.Position;
                    steps = dist;
                }
            }

            return (pos, steps);
        }

        private static Position GetBestMine(Ship ship, Game game, byte radius)
        {
            Position mine = ship.Position;
            int halite = 0;

            while (halite == 0)
            {
                for (int x = ship.Position.X - radius; x <= ship.Position.X + radius; x++)
                {
                    for (int y = ship.Position.Y - radius; y <= ship.Position.Y + radius; y++)
                    {
                        var card = new Position(x, y);
                        MapCell cell = game.Map.At(card);

                        if (cell.IsEmpty
                            && cell.Halite > halite)
                        {
                            halite = game.Map.At(card).Halite;
                            mine = card;
                        }
                    }
                }
            }

            return mine;
        }

        private static bool IsOnBase(Ship ship, Game game)
        {
            if (ship.Position == game.Me.Shipyard.Position)
            {
                return true;
            }

            foreach (Dropoff drop in game.Me.Dropoffs.Values)
            {
                if (ship.Position == drop.Position)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
