using Halite3.Hlt;
using System;
using System.Collections.Generic;
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
                var states = new Dictionary<EntityId, ShipState>(maxShips);

                // At this point "game" variable is populated with initial map data.
                // This is a good place to do computationally expensive start-up pre-processing.
                // As soon as you call "ready" function below, the 2 second per turn timer will start.
                Game.Ready("MyCSharpBot");
                Log.LogMessage("Successfully created bot! My Player ID is " + game.MyId + ". Bot rng seed is " + rngSeed + ".");

                while (true)
                {
                    game.UpdateFrame();
                    int turnsRemaining = Constants.MaxTurns - game.TurnNumber;

                    //(Position richMine, _) = game.Map.GetRichestLocalSquare(game.Me.Shipyard.Position, game.Map.Width / 2);
                    //var costMine = new CostField(game, CostCell.Max, CostCell.Wall);
                    //var waveMine = new IntegrationField(costMine, richMine);
                    //var flowMine = new FlowField(waveMine);
                    //LogFields(game, "MINE", costMine, intgMine, flowMine);

                    var costHome = new CostField(game, CostCell.Zero, CostCell.Max);
                    var waveHome = new WaveField(costHome, game.Me.Shipyard.Position);
                    var flowHome = new FlowField(waveHome);
                    //LogFields(game, "HOME", costHome, waveHome, flowHome);

                    var commandQueue = new List<Command>();

                    var requests = new Dictionary<EntityId, ShipRequest>(game.Me.Ships.Count);
                    foreach (Ship ship in game.Me.Ships.Values)
                    {
                        if (!states.TryGetValue(ship.Id, out ShipState status))
                        {
                            states.Add(ship.Id, status = ShipState.Mining);
                        }

                        (Position Position, int Distance) closestBase = game.GetClosestDrop(ship);
                        if (turnsRemaining - game.Me.Ships.Count <= closestBase.Distance)
                        {
                            states[ship.Id] = ShipState.Ending;
                        }

                        switch (status)
                        {
                            case ShipState.Mining:
                                {
                                    // If ship is full, go back to base
                                    if (ship.IsFull)
                                    {
                                        goto case ShipState.Returning;
                                    }

                                    // If ship is on a drop
                                    if (game.IsOnDrop(ship.Position))
                                    {
                                        // Move in the direction with the maximum halite
                                        int halite = 0;
                                        Position target = ship.Position;
                                        foreach (Direction dir1 in DirectionExtensions.AllCardinals)
                                        {
                                            if (game.Map[ship.Position.DirectionalOffset(dir1)].Halite > halite)
                                            {
                                                halite = game.Map[ship.Position.DirectionalOffset(dir1)].Halite;
                                                target = ship.Position.DirectionalOffset(dir1);
                                            }
                                        }

                                        // If no halite available, move in any empty direction
                                        if (target == ship.Position)
                                        {
                                            foreach (Direction dir1 in DirectionExtensions.AllCardinals)
                                            {
                                                if (game.Map[ship.Position.DirectionalOffset(dir1)].IsEmpty)
                                                {
                                                    target = ship.Position.DirectionalOffset(dir1);
                                                    break;
                                                }
                                            }
                                        }

                                        // Queue the request
                                        requests[ship.Id] = new ShipRequest(ShipState.Mining, target);
                                        break;
                                    }

                                    // If current mine is not depleted
                                    if (IsWorthMining(game.Map[ship.Position].Halite))
                                    {
                                        states[ship.Id] = ShipState.Mining;

                                        // Stay in same mine
                                        requests[ship.Id] = new ShipRequest(ShipState.Mining, ship.Position);
                                        break;
                                    }

                                    (Position richMine, _) = game.Map.GetRichestLocalSquare(ship.Position, game.Map.Width / 4);
                                    var costMine = new CostField(game, CostCell.Max, CostCell.Wall);
                                    var waveMine = new WaveField(costMine, richMine);
                                    var flowMine = new FlowField(waveMine);

                                    // Else follow the flowfield out
                                    FlowCell flow = flowMine[ship.Position];
                                    FlowDirection flowDir = flow.Direction;

                                    // Follow the most expensive route
                                    flowDir = flowDir.Invert();

                                    // Queue the request
                                    Position target1 = flowDir.FromPosition(ship.Position);
                                    requests[ship.Id] = new ShipRequest(ShipState.Mining, target1);
                                }
                                break;

                            case ShipState.Returning:
                                {
                                    states[ship.Id] = ShipState.Returning;

                                    // If ship is on the drop, go mine
                                    if (game.IsOnDrop(ship.Position))
                                    {
                                        goto case ShipState.Mining;
                                    }

                                    // If ship is next to the drop
                                    if (game.IsNextToDrop(ship.Position, out Direction dir, out Position drop))
                                    {
                                        // Queue the request
                                        requests[ship.Id] = new ShipRequest(ShipState.Returning, drop);
                                        break;
                                    }

                                    // Else follow the flowfield home
                                    FlowCell flow = flowHome[ship.Position];
                                    FlowDirection flowDir = flow.Direction;

                                    // Queue the request
                                    Position target = flowDir.FromPosition(ship.Position);
                                    requests[ship.Id] = new ShipRequest(ShipState.Returning, target);
                                }
                                break;

                            case ShipState.Ending:
                                {
                                    states[ship.Id] = ShipState.Ending;

                                    // If ship is next to the drop
                                    if (game.IsNextToDrop(ship.Position, out Direction dir, out Position drop))
                                    {
                                        // Queue the request
                                        requests[ship.Id] = new ShipRequest(ShipState.Ending, drop);
                                        break;
                                    }

                                    // Else follow the flowfield home
                                    FlowCell flow = flowHome[ship.Position];
                                    FlowDirection flowDir = flow.Direction;

                                    // Queue the request
                                    Position target = flowDir.FromPosition(ship.Position);
                                    requests[ship.Id] = new ShipRequest(ShipState.Ending, target);
                                }
                                break;
                        }
                    }

                    // Take care of swaps
                    var done = new Dictionary<EntityId, bool>();
                    foreach (KeyValuePair<EntityId, ShipRequest> kvp1 in requests)
                    {
                        done[kvp1.Key] = false;

                        Ship ship1 = game.Me.Ships[kvp1.Key.Id];
                        Position pos1 = game.Map[ship1].Position;

                        foreach (KeyValuePair<EntityId, ShipRequest> kvp2 in requests)
                        {
                            if (kvp1.Key == kvp2.Key)
                                continue;

                            if (done.TryGetValue(kvp2.Key, out bool isDone) && isDone)
                                continue;

                            Ship ship2 = game.Me.Ships[kvp2.Key.Id];
                            Position pos2 = game.Map[ship2].Position;

                            if (kvp1.Value.Target == pos2 && kvp2.Value.Target == pos1)
                            {
                                game.Map[pos2].MarkSafe();
                                Direction dir = game.Map.NaiveNavigate(ship1, pos2);
                                commandQueue.Add(Command.Move(ship1.Id, dir));

                                game.Map[pos1].MarkSafe();
                                dir = game.Map.NaiveNavigate(ship2, pos1);
                                commandQueue.Add(Command.Move(ship2.Id, dir));

                                Log.LogMessage($"Swapped {ship1.Id.Id}({pos1}) and {ship2.Id.Id}({pos2})");

                                done[kvp1.Key] = true;
                                done[kvp2.Key] = true;
                            }
                        }
                    }

                    // Transfer remaining requests to command queue
                    foreach (KeyValuePair<EntityId, ShipRequest> kvp1 in requests)
                    {
                        if (done.TryGetValue(kvp1.Key, out bool isDone) && isDone)
                            continue;

                        Ship ship1 = game.Me.Ships[kvp1.Key.Id];
                        Position pos1 = game.Map[ship1].Position;

                        Direction dir = game.Map.NaiveNavigate(ship1, kvp1.Value.Target);
                        if (dir != Direction.Still)
                        {
                            commandQueue.Add(Command.Move(ship1.Id, dir));
                        }
                    }

                    // Spawn new ships as necessary
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

        private static void LogFields(Game game, string title, CostField costField, WaveField intgField, FlowField flowField)
        {
            var sb = new StringBuilder();

            Log.LogMessage("MAP FIELD " + title);
            for (int y = 0; y < game.Map.Height; y++)
            {
                sb.Clear();
                for (int x = 0; x < game.Map.Width; x++)
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
            for (int y = 0; y < costField.Height; y++)
            {
                sb.Clear();
                for (int x = 0; x < costField.Width; x++)
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
            for (int y = 0; y < intgField.Height; y++)
            {
                sb.Clear();
                for (int x = 0; x < intgField.Width; x++)
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
            for (int y = 0; y < flowField.Height; y++)
            {
                sb.Clear();
                for (int x = 0; x < flowField.Width; x++)
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

    public sealed class ShipRequest
    {
        public ShipState State { get; }

        public Position Target { get; }

        public ShipRequest(ShipState state, Position target)
        {
            State = state;
            Target = target;
        }
    }
}
