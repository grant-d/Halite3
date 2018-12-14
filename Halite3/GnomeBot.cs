using Halite3.Hlt;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
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

                // Set static custom costs
                (IDictionary<Position, byte> mineCosts, IDictionary<Position, byte> homeCosts) = SetCustomCosts(game);

                while (true)
                {
                    game.UpdateFrame();
                    int turnsRemaining = Constants.MaxTurns - game.TurnNumber;

                    (_, maxHalite, _, _) = game.Map.GetHaliteStatistics();

                    // Some data may change so update it iteratively throughout gameplay
                    UpdateCustomCosts(game, mineCosts, homeCosts);

                    var costHome = CostField.CreateHome(game, maxHalite, homeCosts);
                    var waveHome = new WaveField(costHome, game.Me.Shipyard.Position);
                    var flowHome = new FlowField(waveHome);
                    //LogFields(game.Map, "HOME", costHome, waveHome, flowHome);

                    var requests = new Dictionary<EntityId, ShipRequest>(game.Me.Ships.Count);
                    foreach (Ship ship in game.Me.Ships.Values)
                    {
                        (Position Position, int Halite) goalMine = game.Map.GetRichestLocalSquare(ship.Position, game.Map.Width / 10 + ship.Id.Id % 3);

                        var costMine = CostField.CreateMine(game, maxHalite, mineCosts);
                        var waveMine = new WaveField(costMine, goalMine.Position);
                        var flowMine = new FlowField(waveMine);
                        //LogFields(game.Map, "MINE", costMine, waveMine, flowMine);

                        if (!states.TryGetValue(ship.Id, out ShipState status))
                        {
                            states[ship.Id] = ShipState.Mining;
                        }

                        (Position Position, int Distance) closestBase = game.GetClosestDrop(ship);
                        if (turnsRemaining - game.Me.Ships.Count <= closestBase.Distance * 1.05)
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
                                    FlowCell flow = flowMine[ship.Position.X, ship.Position.Y];
                                    FlowDirection flowDir = flow.Direction;
                                    Position target = flowDir.FromPosition(ship.Position);

                                    // If ship is full, go back to base
                                    if (ship.IsFull)
                                    {
                                        Log.Message($"{ship} is full; returning");
                                        goto case ShipState.Homing;
                                    }

                                    // If ship is on a drop
                                    if (game.IsOnDrop(ship.Position))
                                    {
                                        Log.Message($"{ship} is on drop");

                                        if (!game.Map[target.X, target.Y].IsEmpty)
                                        {
                                            Log.Message($"{ship} has non-empty target {target}");

                                            // Move in the direction with the maximum halite
                                            int halite = 0;
                                            foreach (Direction dir1 in DirectionExtensions.AllCardinals)
                                            {
                                                Position p = ship.Position.DirectionalOffset(dir1);
                                                int h = game.Map[p.X, p.Y].Halite;
                                                if (h > halite)
                                                {
                                                    halite = h;
                                                    target = ship.Position.DirectionalOffset(dir1);
                                                    Log.Message($"{ship} has max target {target}");
                                                }
                                            }

                                            // If no halite available, move in any empty direction
                                            if (target == ship.Position)
                                            {
                                                foreach (Direction dir1 in DirectionExtensions.AllCardinals)
                                                {
                                                    Position p = ship.Position.DirectionalOffset(dir1);
                                                    if (game.Map[p.X, p.Y].IsEmpty)
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
                                    if (IsWorthMining(game.Map[ship.Position.X, ship.Position.Y].Halite, ship.Halite))
                                    {
                                        states[ship.Id] = ShipState.Mining;

                                        // Stay in same mine
                                        Log.Message($"{ship} is staying");
                                        requests[ship.Id] = new ShipRequest(ShipState.Mining, ship.Position);
                                        continue;
                                    }
                                    // If mine is depleted, but ship is nearly full
                                    else if (ship.Halite > 0.97 * Constants.MaxHalite)
                                    {
                                        Log.Message($"{ship} is full; returning");
                                        goto case ShipState.Homing;
                                    }

                                    // Queue the request
                                    Log.Message($"{ship} has target {target}");
                                    requests[ship.Id] = new ShipRequest(ShipState.Mining, target);
                                }
                                break;

                            case ShipState.Homing:
                                {
                                    states[ship.Id] = ShipState.Homing;
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
                                        requests[ship.Id] = new ShipRequest(ShipState.Homing, drop);
                                        continue;
                                    }

                                    // Else follow the flowfield home
                                    FlowCell flow = flowHome[ship.Position.X, ship.Position.Y];
                                    FlowDirection flowDir = flow.Direction;

                                    // Queue the request
                                    Position target = flowDir.FromPosition(ship.Position);
                                    requests[ship.Id] = new ShipRequest(ShipState.Homing, target);
                                    Log.Message($"{ship} is returning to {target}");
                                }
                                break;

                            case ShipState.Ending:
                                {
                                    states[ship.Id] = ShipState.Ending;
                                    Log.Message($"Analyzing {ship} in Ending");

                                    // Else follow the flowfield home
                                    FlowCell flow = flowHome[ship.Position.X, ship.Position.Y];
                                    FlowDirection flowDir = flow.Direction;

                                    // Queue the request
                                    Position target = flowDir.FromPosition(ship.Position);
                                    requests[ship.Id] = new ShipRequest(ShipState.Ending, target);
                                }
                                break;

                            default: break;
                        }
                    }

                    var commandDict = new Dictionary<EntityId, Command>();

                    // Take care of swaps
                    var done = new Dictionary<EntityId, bool>();
                    foreach (KeyValuePair<EntityId, ShipRequest> kvp1 in requests)
                    {
                        if (done.TryGetValue(kvp1.Key, out bool isDone) && isDone)
                            continue;

                        Ship ship1 = game.Me.Ships[kvp1.Key.Id];
                        Position target1 = kvp1.Value.Target;

                        foreach (KeyValuePair<EntityId, ShipRequest> kvp2 in requests)
                        {
                            if (kvp1.Key == kvp2.Key)
                                continue;

                            if (done.TryGetValue(kvp2.Key, out isDone) && isDone)
                                continue;

                            Ship ship2 = game.Me.Ships[kvp2.Key.Id];
                            Position target2 = kvp2.Value.Target;

                            // Swap needed
                            if (target1 == ship2.Position
                                && target2 == ship1.Position)
                            {
                                game.Map[ship2.Position.X, ship2.Position.Y].MarkSafe();
                                Direction dir = game.Map.NaiveNavigate(ship1, ship2.Position);
                                commandDict[ship1.Id] = Command.Move(ship1.Id, dir);
                                done[kvp1.Key] = true;

                                game.Map[ship1.Position.X, ship1.Position.Y].MarkSafe();
                                dir = game.Map.NaiveNavigate(ship2, ship1.Position);
                                commandDict[ship2.Id] = Command.Move(ship2.Id, dir);
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
                                MapCell cell = game.Map[pos1.X, pos1.Y];
                                if (cell.IsEmpty)
                                {
                                    target = pos1;
                                    best = cell.Halite;
                                }

                                cell = game.Map[pos2.X, pos2.Y];
                                if (cell.IsEmpty
                                    && cell.Halite > best)
                                    target = pos2;

                                if (target != target2)
                                {
                                    Direction dir = game.Map.NaiveNavigate(ship2, target);
                                    commandDict[ship2.Id] = Command.Move(ship2.Id, dir);
                                    done[kvp2.Key] = true;

                                    Log.Message($"Wiggled {ship2} from behind {ship1} to {target}");
                                }
                                continue;
                            }

                            // More than 1 ship picked the same target
                            if (target1 == target2)
                            {
                                // If mine1 is richer than mine2
                                if (game.Map[ship1.Position.X, ship1.Position.Y].Halite > game.Map[ship2.Position.X, ship2.Position.Y].Halite)
                                {
                                    // Move ship2
                                    Direction dir = game.Map.NaiveNavigate(ship2, target2);
                                    commandDict[ship2.Id] = Command.Move(ship2.Id, dir);
                                    commandDict[ship1.Id] = ship1.Stay();

                                    Log.Message($"Stopped {ship1} so {ship2} can move to {target2}");
                                }
                                else
                                {
                                    // Else move ship1
                                    Direction dir = game.Map.NaiveNavigate(ship1, target1);
                                    commandDict[ship1.Id] = Command.Move(ship1.Id, dir);
                                    commandDict[ship2.Id] = ship2.Stay();

                                    Log.Message($"Stopped {ship2} so {ship1} can move to {target1}");
                                }

                                done[kvp1.Key] = true;
                                done[kvp2.Key] = true;
                                continue;
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
                            game.Map[target.X, target.Y].MarkSafe();
                        }

                        Direction dir = game.Map.NaiveNavigate(ship, kvp.Value.Target);
                        commandDict[ship.Id] = Command.Move(ship.Id, dir);
                        Log.Message($"Queued {ship} to move {dir}");
                    }

                    // 
                    var commandQueue = commandDict.Select(n => n.Value).ToList();

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

        private static (IDictionary<Position, byte> MineCosts, IDictionary<Position, byte> HomeCosts) SetCustomCosts(Game game)
        {
            // Each cell in the cost field is represented by a single byte that will normally be set to some value in between 1 and 255.
            // By default all cells are set to a value of 1.
            // Any values between 2 and 254 represent cells that are passable but should be avoided if possible.
            // The value 255 represents impassable walls that units must path around.

            var mineCosts = new Dictionary<Position, byte>();
            var homeCosts = new Dictionary<Position, byte>();

            // My shipyard
            {
                Position pos = game.Me.Shipyard.Position;

                // When mining, don't attract ships to my shipyard
                // But don't affect the halite countour
                mineCosts.Add(pos, CostField.Wall);

                // When homing, attract ships towards my shipyard
                homeCosts.Add(pos, CostField.Valley);
            }

            // Their shipyards
            IEnumerable<Player> players = game.Players.Where(n => n.Id != game.MyId);
            foreach (Position pos in players.Select(n => n.Shipyard.Position))
            {
                // When mining, don't crash into their shipyard ships
                // But don't affect the halite countour
                mineCosts.Add(pos, CostField.Wall);

                // When homing, accentuate my valleys and avoid collisions
                homeCosts.Add(pos, CostField.Peak);
            }

            return (mineCosts, homeCosts);
        }

        private static void UpdateCustomCosts(Game game, IDictionary<Position, byte> mineCosts, IDictionary<Position, byte> homeCosts)
        {
            // My dropoffs
            foreach (Position pos in game.Me.Dropoffs.Select(n => n.Value.Position))
            {
                // When mining, don't attract ships to my dropoffs
                // But don't affect the halite countour
                mineCosts[pos] = CostField.Wall;

                // When homing, attract ships towards my dropoffs
                homeCosts[pos] = CostField.Valley;
            }

            // Their dropoffs
            IEnumerable<Player> players = game.Players.Where(n => n.Id != game.MyId);
            foreach (Position pos in players.SelectMany(n => n.Dropoffs).Select(n => n.Value.Position))
            {
                // When mining, don't crash into their dropoff ships
                // But don't affect the halite countour
                mineCosts[pos] = CostField.Wall;

                // When homing, accentuate my valleys and avoid collisions
                homeCosts[pos] = CostField.Peak;
            }
        }

        private static bool IsWorthMining(int mine, int ship)
        {
            // Calculate ship's bounty if it leaves now
            double leaveNow = Math.Max(0, ship - MoveCost(mine));

            // If mine <= 9, then moveCost = 9/10 == 0. So we must always keep mine >= 10.
            // Then add enough for one more dig: 10 * 4/3 = 13.33. 13.33 * 0.75 == 10.
            leaveNow += Constants.MoveCostRatio * Constants.ExtractRatio / (Constants.ExtractRatio - 1.0); // 13.333

            // Calculate ship's bounty if it leaves next turn
            double profit = Profit(mine);
            double leaveNext = Math.Max(0, ship + profit - MoveCost(mine - profit));

            return leaveNow < leaveNext;

            // 25% of halite available in cell, rounded up to the nearest whole number.
            double Profit(double halite)
                => halite / Constants.ExtractRatio; // 4

            // 10% of halite available at turn origin cell is deducted from ship’s current halite.
            double MoveCost(double halite)
                => halite / Constants.MoveCostRatio; // 10
        }

        private static void LogFields(Map map, string title, CostField costField, WaveField waveField, FlowField flowField)
        {
            Debug.Assert(map != null);

            var sb = new StringBuilder();

            Log.Message($"{title}: MAP");
            ShowColNos();
            for (int y = 0; y < map.Height; y++)
            {
                sb.Clear();
                sb.Append($"{y:000}|");
                for (int x = 0; x < map.Width; x++)
                {
                    if (map[x, y].HasStructure)
                        sb.Append($"■{map[x, y].Halite:000}■|");
                    else
                        sb.Append($" {map[x, y].Halite:000} |");
                }
                Log.Message(sb.ToString());
            }

            if (costField != null)
            {
                Log.Message($"{title}: COST");
                ShowColNos();
                for (int y = 0; y < costField.Height; y++)
                {
                    sb.Clear();
                    sb.Append($"{y:000}|");
                    for (int x = 0; x < costField.Width; x++)
                    {
                        if (map[x, y].HasStructure)
                            sb.Append($"■{costField[x, y]:000}■|");
                        else
                            sb.Append($" {costField[x, y]:000} |");
                    }
                    Log.Message(sb.ToString());
                }
            }

            if (waveField != null)
            {
                Log.Message($"{title}: WAVE");
                ShowColNos();
                for (int y = 0; y < waveField.Height; y++)
                {
                    sb.Clear();
                    sb.Append($"{y:000}|");
                    for (int x = 0; x < waveField.Width; x++)
                    {
                        if (map[x, y].HasStructure)
                            sb.Append($"{waveField[x, y].Cost:00000}■");
                        else
                            sb.Append($"{waveField[x, y].Cost:00000}|");
                    }
                    Log.Message(sb.ToString());
                }
            }

            if (flowField != null)
            {
                Log.Message($"{title}: FLOW");
                ShowColNos();
                for (int y = 0; y < flowField.Height; y++)
                {
                    sb.Clear();
                    sb.Append($"{y:000}|");
                    for (int x = 0; x < flowField.Width; x++)
                    {
                        if (map[x, y].HasStructure)
                            sb.Append($"■ {flowField[x, y].Direction.ToSymbol()} ■|");
                        else
                            sb.Append($"  {flowField[x, y].Direction.ToSymbol()}  |");
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
        Homing,
        //Converting,
        //Inspired,
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
