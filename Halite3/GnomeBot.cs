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
                // At this point "game" variable is populated with initial map data.
                // This is a good place to do computationally expensive start-up pre-processing.
                // As soon as you call "ready" function below, the 2 second per turn timer will start.
                Game.Ready("MyCSharpBot");
                Log.Message("Successfully created bot! My Player ID is " + game.MyId + ". Bot rng seed is " + rngSeed + ".");

                (int minHalite, int maxHalite, int totalHalite, int meanHalite) = game.Map.GetHaliteStatistics();
                Log.Message($"Min={minHalite}, Max={maxHalite}, Mean = {meanHalite}, Total={totalHalite}");

                double maxShips1 = game.Map.Height * game.Map.Width; // 32->1024, 40->1600, 48->2304, 56->3136, 64->4096
                maxShips1 = maxShips1 / 160; // 32->6, 40->10, 48->13, 56->19, 64->27
                int maxShips = (int)(maxShips1 + (12 * meanHalite * 2 / maxHalite)); // 32->12, 40->16, 48->19, 56->25, 64->33
                Log.Message($"Ships={maxShips}");

                int maxDropOffs = -1 + game.Map.Width / 20; // 32->0, 40->1, 48->1, 64->2
                int minBuildTurn = Constants.MaxTurns * 5 / 10;
                int maxBuildTurn = Constants.MaxTurns * 8 / 10;
                var states = new Dictionary<EntityId, ShipState>(maxShips);

                while (true)
                {
                    game.UpdateFrame();
                    int turnsRemaining = Constants.MaxTurns - game.TurnNumber;

                    var commandQueue = new List<Command>();

                    var requests = new Dictionary<EntityId, ShipRequest>(game.Me.Ships.Count);
                    foreach (Ship ship in game.Me.Ships.Values)
                    {
                        (_, maxHalite, _, _) = game.Map.GetHaliteStatistics();

                        var goalMine = game.Map.GetRichestLocalSquare(ship.Position, game.Map.Width / 8 + ship.Id.Id % 3);
                        var costMine = new CostField(game, maxHalite, CostCell.Max, CostCell.Wall, true);
                        var waveMine = new WaveField(costMine, goalMine.Position);
                        var flowMine = new FlowField(waveMine);
                        //LogFields(game.Map, "MINE", costMine, waveMine, flowMine);

                        var costHome = new CostField(game, maxHalite, CostCell.Min, CostCell.Wall, false);
                        var waveHome = new WaveField(costHome, game.Me.Shipyard.Position);
                        var flowHome = new FlowField(waveHome);
                        //LogFields(game.Map, "HOME", costHome, waveHome, flowHome);

                        if (!states.TryGetValue(ship.Id, out ShipState status))
                        {
                            states[ship.Id] = ShipState.Mining;
                        }

                        (Position Position, int Distance) closestBase = game.GetClosestDrop(ship);
                        if (turnsRemaining - game.Me.Ships.Count <= closestBase.Distance)
                        {
                            Log.Message($"Setting {ship} to Ending");
                            status = ShipState.Ending;
                            states[ship.Id] = ShipState.Ending;
                        }

                        switch (status)
                        {
                            case ShipState.Mining:
                                {
                                    states[ship.Id] = ShipState.Mining;
                                    Log.Message($"Analyzing {ship} in Mining");

                                    // Follow the flowfield out
                                    FlowCell flow = flowMine[ship.Position];
                                    FlowDirection flowDir = flow.Direction;
                                    Position target = flowDir.FromPosition(ship.Position);

                                    // If ship is full, go back to base
                                    if (ship.IsFull)
                                    {
                                        Log.Message($"{ship} is full; returning");
                                        goto case ShipState.Returning;
                                    }

                                    // If ship is on a drop
                                    if (game.IsOnDrop(ship.Position))
                                    {
                                        Log.Message($"{ship} is on drop");

                                        if (!game.Map[target].IsEmpty)
                                        {
                                            Log.Message($"{ship} has non-empty target {target}");

                                            // Move in the direction with the maximum halite
                                            int halite = 0;
                                            foreach (Direction dir1 in DirectionExtensions.AllCardinals)
                                            {
                                                if (game.Map[ship.Position.DirectionalOffset(dir1)].Halite > halite)
                                                {
                                                    halite = game.Map[ship.Position.DirectionalOffset(dir1)].Halite;
                                                    target = ship.Position.DirectionalOffset(dir1);
                                                    Log.Message($"{ship} has max target {target}");
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
                                                        Log.Message($"{ship} has empty target {target}");
                                                        break;
                                                    }
                                                }
                                            }
                                        }

                                        // Queue the request
                                        requests[ship.Id] = new ShipRequest(ShipState.Mining, target);
                                        Log.Message($"{ship} has flow target {target}");
                                        continue;
                                    }

                                    // If current mine is not depleted
                                    if (IsWorthMining(game.Map[ship.Position].Halite))//, ship.Halite))
                                    {
                                        states[ship.Id] = ShipState.Mining;

                                        // Stay in same mine
                                        Log.Message($"{ship} is staying");
                                        requests[ship.Id] = new ShipRequest(ShipState.Mining, ship.Position);
                                        continue;
                                    }
                                    // If mine is depleted, but ship is nearly full
                                    //else if (ship.Halite > 0.97 * Constants.MaxHalite)
                                    //{
                                    //    Log.Message($"{ship} is full; returning");
                                    //    goto case ShipState.Returning;
                                    //}

                                    // Queue the request
                                    Log.Message($"{ship} has target {target}");
                                    requests[ship.Id] = new ShipRequest(ShipState.Mining, target);
                                }
                                break;

                            case ShipState.Returning:
                                {
                                    states[ship.Id] = ShipState.Returning;
                                    Log.Message($"Analyzing {ship} in Returning");

                                    // If ship is on the drop, go mine
                                    if (game.IsOnDrop(ship.Position))
                                    {
                                        Log.Message($"{ship} is on drop");
                                        goto case ShipState.Mining;
                                    }

                                    // If ship is next to the drop
                                    if (game.IsNextToDrop(ship.Position, out Position drop))
                                    {
                                        Log.Message($"{ship} is next to drop");
                                        // Queue the request
                                        requests[ship.Id] = new ShipRequest(ShipState.Returning, drop);
                                        continue;
                                    }

                                    // Else follow the flowfield home
                                    FlowCell flow = flowHome[ship.Position];
                                    FlowDirection flowDir = flow.Direction;

                                    // Queue the request
                                    Position target = flowDir.FromPosition(ship.Position);
                                    requests[ship.Id] = new ShipRequest(ShipState.Returning, target);
                                    Log.Message($"{ship} is returning to {target}");
                                }
                                break;

                            case ShipState.Ending:
                                {
                                    states[ship.Id] = ShipState.Ending;
                                    Log.Message($"Analyzing {ship} in Ending");

                                    // Else follow the flowfield home
                                    FlowCell flow = flowHome[ship.Position];
                                    FlowDirection flowDir = flow.Direction;

                                    // Queue the request
                                    Position target = flowDir.FromPosition(ship.Position);
                                    requests[ship.Id] = new ShipRequest(ShipState.Ending, target);
                                }
                                break;

                            default: break;
                        }
                    }

                    // Take care of swaps
                    var done = new Dictionary<EntityId, bool>();
                    foreach (KeyValuePair<EntityId, ShipRequest> kvp1 in requests)
                    {
                        Ship ship1 = game.Me.Ships[kvp1.Key.Id];
                        Position target1 = kvp1.Value.Target;

                        foreach (KeyValuePair<EntityId, ShipRequest> kvp2 in requests)
                        {
                            if (kvp1.Key == kvp2.Key)
                                continue;

                            if (done.TryGetValue(kvp2.Key, out bool isDone) && isDone)
                                continue;

                            Ship ship2 = game.Me.Ships[kvp2.Key.Id];
                            Position target2 = kvp2.Value.Target;

                            // Swap needed
                            if (target1 == ship2.Position
                                && target2 == ship1.Position)
                            {
                                game.Map[ship2.Position].MarkSafe();
                                Direction dir = game.Map.NaiveNavigate(ship1, ship2.Position);
                                commandQueue.Add(Command.Move(ship1.Id, dir));
                                done[kvp1.Key] = true;

                                game.Map[ship1.Position].MarkSafe();
                                dir = game.Map.NaiveNavigate(ship2, ship1.Position);
                                commandQueue.Add(Command.Move(ship2.Id, dir));
                                done[kvp2.Key] = true;

                                Log.Message($"Swapped {ship1} and {ship2}");
                                continue;
                            }

                            // Wiggle needed
                            if (target2 == ship1.Position)
                            {
                                Position pos1, pos2;
                                if (ship1.Position.Y != ship2.Position.Y)
                                {
                                    pos1 = ship2.Position.DirectionalOffset(Direction.West);
                                    pos2 = ship2.Position.DirectionalOffset(Direction.East);
                                }
                                else
                                {
                                    pos1 = ship2.Position.DirectionalOffset(Direction.North);
                                    pos2 = ship2.Position.DirectionalOffset(Direction.South);
                                }

                                Position target = target2;

                                int best = 0;
                                MapCell cell = game.Map[pos1];
                                if (cell.IsEmpty)
                                {
                                    target = pos1;
                                    best = cell.Halite;
                                }

                                cell = game.Map[pos2];
                                if (cell.IsEmpty
                                    && cell.Halite > best)
                                    target = pos2;

                                if (target != target2)
                                {
                                    Direction dir = game.Map.NaiveNavigate(ship2, target);
                                    commandQueue.Add(Command.Move(ship2.Id, dir));
                                    done[kvp2.Key] = true;

                                    Log.Message($"Wiggled {ship2} from behind {ship1} to {target}");
                                }
                            }
                        }
                    }

                    // Transfer remaining requests to command queue
                    foreach (KeyValuePair<EntityId, ShipRequest> kvp in requests)
                    {
                        if (done.TryGetValue(kvp.Key, out bool isDone) && isDone)
                            continue;

                        Ship ship = game.Me.Ships[kvp.Key.Id];
                        Position target = kvp.Value.Target;

                        // If ship is next to the drop
                        if (kvp.Value.State == ShipState.Ending
                            && game.IsNextToDrop(ship.Position, out Position drop))
                        {
                            target = drop;
                            game.Map[target].MarkSafe();
                        }

                        Direction dir = game.Map.NaiveNavigate(ship, kvp.Value.Target);
                        commandQueue.Add(Command.Move(ship.Id, dir));
                        Log.Message($"Queued {ship} to move {dir}");
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

        private static bool IsWorthMining(int halite, double factor = 1.0)
           => halite / Constants.ExtractRatio >= factor * halite / Constants.MoveCostRatio;

        //private static bool IsWorthMining(int mine, int ship)
        //{
        //    int profit = GetProfit(mine);
        //    int cost = GetCost(mine);

        //    if (mine - cost <= 0) return false;

        //    // Cannot move if out of fuel, so may as well keep mining
        //    if (ship - cost <= 0) return true;

        //    // If mine has enough reserves to fill ship, keep going
        //    if (ship + mine >= Constants.MaxHalite) return true;

        //    var ship2 = ship + profit - GetCost(mine - profit);
        //    if (ship2 > ship - cost) return true;

        //    Log.Message($"Mine={mine}, Ship={ship}, Profit={profit}, Cost={cost}, Ship2={ship2}");

        //    return false;

        //    // 25% of halite available in cell, rounded up to the nearest whole number.
        //    int GetProfit(int halite)
        //        => (int)Math.Ceiling(halite * 1.0 / Constants.ExtractRatio);

        //    // 10% of halite available at turn origin cell is deducted from ship’s current halite.
        //    int GetCost(int halite)
        //        => (int)Math.Floor(halite * 1.0 / Constants.MoveCostRatio);
        //}

        private static void LogFields(Map map, string title, CostField costField, WaveField waveField, FlowField flowField)
        {
            Debug.Assert(map != null);

            var sb = new StringBuilder();

            Log.Message("MAP FIELD " + title);
            ShowColNos();
            for (int y = 0; y < map.Height; y++)
            {
                sb.Clear();
                sb.Append($"{y:000}|");
                for (int x = 0; x < map.Width; x++)
                {
                    var pos = new Position(x, y);
                    if (map[pos].HasStructure)
                        sb.Append($"■{map[pos].Halite:000}■|");
                    else
                        sb.Append($" {map[pos].Halite:000} |");
                }
                Log.Message(sb.ToString());
            }

            if (costField != null)
            {
                Log.Message("COST FIELD " + title);
                ShowColNos();
                for (int y = 0; y < costField.Height; y++)
                {
                    sb.Clear();
                    sb.Append($"{y:000}|");
                    for (int x = 0; x < costField.Width; x++)
                    {
                        var pos = new Position(x, y);
                        if (map[pos].HasStructure)
                            sb.Append($"■{costField[pos].Cost:000}■|");
                        else
                            sb.Append($" {costField[pos].Cost:000} |");
                    }
                    Log.Message(sb.ToString());
                }
            }

            if (waveField != null)
            {
                Log.Message("WAVE FIELD " + title);
                ShowColNos();
                for (int y = 0; y < waveField.Height; y++)
                {
                    sb.Clear();
                    sb.Append($"{y:000}|");
                    for (int x = 0; x < waveField.Width; x++)
                    {
                        var pos = new Position(x, y);
                        if (map[pos].HasStructure)
                            sb.Append($"{waveField[pos].Cost:00000}■");
                        else
                            sb.Append($"{waveField[pos].Cost:00000}|");
                    }
                    Log.Message(sb.ToString());
                }
            }

            if (flowField != null)
            {
                Log.Message("FLOW FIELD " + title);
                ShowColNos();
                for (int y = 0; y < flowField.Height; y++)
                {
                    sb.Clear();
                    sb.Append($"{y:000}|");
                    for (int x = 0; x < flowField.Width; x++)
                    {
                        var pos = new Position(x, y);
                        if (map[pos].HasStructure)
                            sb.Append($"■ {flowField[pos].Direction.ToSymbol()} ■|");
                        else
                            sb.Append($"  {flowField[pos].Direction.ToSymbol()}  |");
                    }
                    Log.Message(sb.ToString());
                }
            }

            void ShowColNos()
            {
                sb.Clear().Append("   |");
                for (int x = 0; x < map.Width; x++)
                    sb.Append($" {x:000} |");
                Log.Message(sb.ToString());
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
