using Halite3.Hlt;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace Halite3
{
    public enum ShipState
    {
        None = 0,
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

                int maxShips = 8 + game.Map.Width / 8; // 32->12, 40->13, 48->14, 64->16
                int maxDropOffs = 0 + game.Map.Width / 16; // 32->2, 40->2, 48->3, 64->4

                var states = new Dictionary<EntityId, ShipStatus>();

                while (true)
                {
                    game.UpdateFrame();

                    if (InitialSpawn(game))
                        continue;

                    var commandQueue = new List<Command>();

                    if (game.Me.Ships.Count >= maxShips
                        && game.Me.Dropoffs.Count < maxDropOffs
                        && game.TurnNumber <= 350
                        && game.Me.Halite > Constants.DropOffCost * 2)
                    {
                        Ship ship = GetFurthestShip(game.Me, game, states);

                        if (ship != default)
                        {
                            states[ship.Id].State = ShipState.Converting;
                            commandQueue.Add(ship.MakeDropoff());
                        }
                    }

                    foreach (Ship ship in game.Me.Ships.Values)
                    {
                        if (!states.TryGetValue(ship.Id, out ShipStatus status))
                            states.Add(ship.Id, status = new ShipStatus { State = ShipState.None });

                        // Keep track of closest base
                        (Position Position, int Distance) closestBase = game.GetClosestBase(ship);
                        var turnsRemaining = Constants.MaxTurns - game.TurnNumber;

                        Log.LogMessage($"Remaining {turnsRemaining} of {Constants.MaxTurns}");

                        if (turnsRemaining - game.Me.Ships.Count <= closestBase.Distance)
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

                                    Position mine = game.Map.GetLocalRichestMine(ship, 2);
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

                                    if (game.IsOnBase(ship))
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

        private static Ship GetFurthestShip(Player player, Game game, IReadOnlyDictionary<EntityId, ShipStatus> shipStatus)
        {
            Debug.Assert(player != null);
            Debug.Assert(game != null);
            Debug.Assert(shipStatus != null);

            Ship sh = default;

            int best = 0;
            foreach (Ship ship in player.Ships.Values)
            {
                if (!shipStatus.TryGetValue(ship.Id, out ShipStatus status))
                    continue;

                if (status.State != ShipState.Mining)
                    continue;

                int dist = game.GetManhattanDistanceFromAllBases(ship);
                if (dist > best)
                {
                    best = dist;
                    sh = ship;
                }
            }

            return sh;
        }

        private static Direction GetEndingDirection(Ship ship, Game game, Position closestBase)
        {
            Direction dir = game.Map.NaiveNavigate(ship, closestBase);

            var dist = game.Map.GetManhattanDistance(ship.Position, closestBase);
            if (dist == 0)
                return Direction.Still;

            if (dist == 1)
            {
                foreach (Direction direction in DirectionExtensions.AllCardinals)
                {
                    if (ship.Position.DirectionalOffset(direction) == closestBase)
                    {
                        dir = direction;
                        break;
                    }
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
    }
}
