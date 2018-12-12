using Halite3.Hlt;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace Halite3
{
    public sealed class GnomeBot
    {
        private const double CostFactor = 1.1;

        public static void Main(string[] args)
        {
            //while (!Debugger.IsAttached);

            int rngSeed = args.Length > 1 ? int.Parse(args[1], CultureInfo.InvariantCulture) : DateTime.Now.Millisecond;
            var rng = new Random(rngSeed);

            using (var game = new Game())
            {
                Log.LogMessage(game.Map.GetMinMaxHalite().ToString());

                int maxShips = 12 + game.Map.Width / 8; // 32->20, 40->21, 48->22, 56->23, 64->24
                int maxDropOffs = -1 + game.Map.Width / 20; // 32->0, 40->1, 48->1, 64->2
                int minBuildTurn = Constants.MaxTurns * 5 / 10;
                int maxBuildTurn = Constants.MaxTurns * 8 / 10;
                var states = new Dictionary<EntityId, ShipStatus>();

                // At this point "game" variable is populated with initial map data.
                // This is a good place to do computationally expensive start-up pre-processing.
                // As soon as you call "ready" function below, the 2 second per turn timer will start.
                Game.Ready("MyCSharpBot");
                Log.LogMessage("Successfully created bot! My Player ID is " + game.MyId + ". Bot rng seed is " + rngSeed + ".");

                while (true)
                {
                    game.UpdateFrame();

                    (Position richMine, _) = game.Map.GetRichestLocalRadius(game.Me.Shipyard.Position, game.Map.Width / 4);
                    var costMine = new CostField(game, CostCell.Max, CostCell.Wall);
                    var intgMine = new IntegrationField(costMine, richMine);
                    var flowMine = new FlowField(intgMine);
                    LogFields(game, "MINE", costMine, intgMine, flowMine);

                    var costHome = new CostField(game, CostCell.Zero, CostCell.Max);
                    var intgHome = new IntegrationField(costHome, game.Me.Shipyard.Position);
                    var flowHome = new FlowField(intgHome);
                    LogFields(game, "HOME", costHome, intgHome, flowHome);

                    var commandQueue = new List<Command>();

                    foreach (Ship ship in game.Me.Ships.Values)
                    {
                        if (!states.TryGetValue(ship.Id, out ShipStatus status))
                        {
                            states.Add(ship.Id, status = new ShipStatus { State = ShipState.Mining });
                        }

                        (Position Position, int Distance) closestBase = game.GetClosestDrop(ship);
                        var turnsRemaining = Constants.MaxTurns - game.TurnNumber;

                        if (turnsRemaining - game.Me.Ships.Count <= closestBase.Distance)
                        {
                            states[ship.Id].State = ShipState.Ending;
                        }

                        switch (status.State)
                        {
                            case ShipState.Mining:
                                {
                                    if (ship.IsFull)
                                    {
                                        goto case ShipState.Returning;
                                    }

                                    if (game.IsOnDrop(ship.Position))
                                    {
                                        int halite = 0;
                                        Position pos = ship.Position;
                                        foreach (Direction dir1 in DirectionExtensions.AllCardinals)
                                        {
                                            if (game.Map[ship.Position.DirectionalOffset(dir1)].Halite > halite)
                                            {
                                                halite = game.Map[ship.Position.DirectionalOffset(dir1)].Halite;
                                                pos = ship.Position.DirectionalOffset(dir1);
                                            }
                                        }

                                        if (pos == ship.Position)
                                        {
                                            foreach (Direction dir1 in DirectionExtensions.AllCardinals)
                                            {
                                                if (game.Map[ship.Position.DirectionalOffset(dir1)].IsEmpty)
                                                {
                                                    pos = ship.Position.DirectionalOffset(dir1);
                                                    break;
                                                }
                                            }
                                        }

                                        Direction dir2 = game.Map.NaiveNavigate(ship, pos);
                                        commandQueue.Add(ship.Move(dir2));
                                        continue;
                                    }

                                    if (IsWorthMining(game.Map[ship.Position].Halite))
                                    {
                                        states[ship.Id].State = ShipState.Mining;
                                        commandQueue.Add(ship.Stay());
                                        continue;
                                    }

                                    FlowCell flow = flowMine[ship.Position];
                                    FlowDirection flowDir = flow.Direction;
                                    flowDir = flowDir.Invert();
                                    Position destination = flowDir.FromPosition(ship.Position);

                                    // HACK
                                    double dice = rng.NextDouble();
                                    if (dice < 0.1)
                                    {
                                        destination = Jiggle(game, ship, rng);
                                    }

                                    Direction dir = game.Map.NaiveNavigate(ship, destination);
                                    commandQueue.Add(Command.Move(ship.Id, dir));
                                }
                                break;

                            case ShipState.Returning:
                                {
                                    states[ship.Id].State = ShipState.Returning;

                                    if (game.IsOnDrop(ship.Position))
                                    {
                                        goto case ShipState.Mining;
                                    }

                                    if (game.IsNextToDrop(ship.Position, out Direction dir, out Position drop))
                                    {
                                        MapCell cell = game.Map[drop];
                                        if (cell.IsOccupied)
                                        {
                                            if (cell.Ship.Owner == game.MyId)
                                            {
                                                continue;
                                            }

                                            cell.MarkSafe();
                                        }

                                        dir = game.Map.NaiveNavigate(ship, drop);
                                        commandQueue.Add(ship.Move(dir));
                                        continue;
                                    }

                                    FlowCell flow = flowHome[ship.Position];
                                    FlowDirection flowDir = flow.Direction;
                                    Direction dir1 = game.Map.NaiveNavigate(ship, flowDir.FromPosition(ship.Position));
                                    commandQueue.Add(Command.Move(ship.Id, dir1));
                                }
                                break;

                            case ShipState.Ending:
                                {
                                    states[ship.Id].State = ShipState.Ending;

                                    if (game.IsNextToDrop(ship.Position, out Direction dir, out Position drop))
                                    {
                                        MapCell cell = game.Map[drop];
                                        cell.MarkSafe();

                                        dir = game.Map.NaiveNavigate(ship, drop);
                                        commandQueue.Add(ship.Move(dir));
                                        continue;
                                    }

                                    FlowCell flow = flowHome[ship.Position];
                                    FlowDirection flowDir = flow.Direction;
                                    Direction dir1 = game.Map.NaiveNavigate(ship, flowDir.FromPosition(ship.Position));
                                    commandQueue.Add(Command.Move(ship.Id, dir1));
                                }
                                break;
                        }
                    }

                    if (game.TurnNumber <= maxBuildTurn
                        && game.Me.Ships.Count < maxShips
                        && game.Me.Halite > Constants.ShipCost * CostFactor)
                    {
                        MapCell mapCell = game.Map[game.Me.Shipyard];
                        if (mapCell.IsOccupied)
                        {
                            if (mapCell.Ship.Owner != game.MyId)
                            {
                                commandQueue.Add(Command.SpawnShip());
                            }
                        }
                        else
                        {
                            commandQueue.Add(Command.SpawnShip());
                        }
                    }

                    Game.EndTurn(commandQueue);
                }
            }
        }

        private static bool IsWorthMining(int halite)
            => halite / Constants.ExtractRatio > halite / Constants.MoveCostRatio;

        private static Position Jiggle(Game game, Ship ship, Random rng)
        {
            var list = new List<Position>(5);
            list.Add(ship.Position); // Stay

            foreach (Direction direction in DirectionExtensions.AllCardinals)
            {
                Position pos = ship.Position.DirectionalOffset(direction);
                if (game.Map[pos].IsEmpty)
                {
                    list.Add(pos);
                }
            }

            if (list.Count == 1)
                return list[0];

            int i = rng.Next(0, list.Count);
            return list[i];
        }

        private static void LogFields(Game game, string title, CostField costField, IntegrationField intgField, FlowField flowField)
        {
            var sb = new StringBuilder();

            Log.LogMessage("MAP FIELD " + title);
            for (var y = 0; y < game.Map.Height; y++)
            {
                sb.Clear();
                for (var x = 0; x < game.Map.Width; x++)
                {
                    var pos = new Position(x, y);
                    if (game.Map[pos].HasStructure)
                        sb.Append((game.Map[pos].Halite.ToString() + "**").PadRight(6));
                    else
                        sb.Append((game.Map[pos].Halite.ToString() + " |").PadRight(6));
                }
                Log.LogMessage(sb.ToString());
            }

            Log.LogMessage("COST FIELD " + title);
            for (var y = 0; y < costField.Height; y++)
            {
                sb.Clear();
                for (var x = 0; x < costField.Width; x++)
                {
                    var pos = new Position(x, y);
                    if (game.Map[pos].HasStructure)
                        sb.Append((costField[pos].Cost.ToString() + "**").PadRight(6));
                    else
                        sb.Append((costField[pos].Cost.ToString() + " |").PadRight(6));
                }
                Log.LogMessage(sb.ToString());
            }

            Log.LogMessage("INTEGRATION FIELD " + title);
            for (var y = 0; y < intgField.Height; y++)
            {
                sb.Clear();
                for (var x = 0; x < intgField.Width; x++)
                {
                    var pos = new Position(x, y);
                    if (game.Map[pos].HasStructure)
                        sb.Append((intgField[pos].Cost.ToString() + "**").PadRight(6));
                    else
                        sb.Append((intgField[pos].Cost.ToString() + " |").PadRight(6));
                }
                Log.LogMessage(sb.ToString());
            }

            Log.LogMessage("FLOW FIELD " + title);
            for (var y = 0; y < flowField.Height; y++)
            {
                sb.Clear();
                for (var x = 0; x < flowField.Width; x++)
                {
                    var pos = new Position(x, y);
                    if (game.Map[pos].HasStructure)
                        sb.Append((flowField[pos].Direction.ToSymbol() + "**").PadRight(6));
                    else
                        sb.Append((flowField[pos].Direction.ToSymbol() + "  ").PadRight(6));
                }
                Log.LogMessage(sb.ToString());
            }
        }
    }

    public enum ShipState
    {
        Mining,
        Returning,
        //Converting,
        Ending
    }

    public sealed class ShipStatus
    {
        public ShipState State { get; set; }
    }
}
