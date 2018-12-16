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
        private const double CostFactor = 1.0;
        private const double EndFactor = 1.25;
        private const int CompressRatio = 20;

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

                //double maxShips1 = game.Map.Height * game.Map.Width; // 32->1024, 40->1600, 48->2304, 56->3136, 64->4096
                //maxShips1 = maxShips1 / 160; // 32->6, 40->10, 48->13, 56->19, 64->27
                //double pf = game.Players.Count + 40.0 / game.Players.Count; // 2->22, 4->14
                //maxShips1 = maxShips1 + (pf * meanHalite * 2 / maxHalite); // 32->12, 40->16, 48->19, 56->25, 64->33
                double maxShips1 = game.Map.Height * game.Map.Width * 4 / game.Players.Count; // 32->1024 x 2|1, 64->4096 x 2|1
                maxShips1 = Math.Sqrt(maxShips1); // 32->45|32, 64->90|64
                maxShips1 = maxShips1 / 2; // 32->24|16, 64->45|32
                int maxShips = (int)maxShips1;
                Log.Message($"Ships={maxShips}");

                int maxDropOffs = -1 + game.Map.Width / 20; // 32->0, 40->1, 48->1, 64->2
                int minBuildTurn = Constants.MaxTurns * 5 / 10;
                int maxBuildTurn = Constants.MaxTurns * 8 / 10;
                var states = new Dictionary<EntityId, ShipState>(maxShips);

                // Set static custom costs
                (IReadOnlyDictionary<Position, byte> mineBaseCosts, IReadOnlyDictionary<Position, byte> homeBaseCosts) = SetCustomCosts(game);

                while (true)
                {
                    game.UpdateFrame();
                    int turnsRemaining = Constants.MaxTurns - game.TurnNumber;

                    // Some data may change so update it iteratively throughout gameplay
                    (IDictionary<Position, byte> mineCosts, IDictionary<Position, byte> homeCosts) = UpdateCustomCosts(game, mineBaseCosts, homeBaseCosts);

                    var costHome = CostField.CreateHome(game, homeCosts);
                    var waveHome = new WaveField(costHome, new[] { game.Me.Shipyard.Position });
                    var flowHome = new FlowField(waveHome);
                    //LogFields(game, "HOME", costHome, waveHome, flowHome);

                    var xRadius = game.Map.Width / CompressRatio; // 32->1, 64->3
                    var yRadius = game.Map.Height / CompressRatio;
                    var xLen = xRadius * 2 + 1; // 32->3, 64->7
                    var yLen = yRadius * 2 + 1;
                    var costRich = CostField.Compress(game.Map, xRadius, yRadius);

                    var sy = new Position(game.Me.Shipyard.Position.X / xLen, game.Me.Shipyard.Position.Y / yLen);
                    var richest = new List<(Position, ushort)>(costRich.Width * costRich.Height);
                    for (int x = 0; x < costRich.Width; x++)
                    {
                        for (int y = 0; y < costRich.Height; y++)
                        {
                            var pos = new Position(x, y);
                            var waveRich = new WaveField(costRich, new[] { pos });
                            //LogFields(game, "RICH", costRich, waveRich, null);
                            richest.Add((pos, waveRich[sy.X, sy.Y]));
                        }
                    }
                    IEnumerable<Position> topN = richest
                        .OrderByDescending(n => n.Item2)
                        .Take(3)
                        .Select(n => new Position(n.Item1.X * xLen + 1, n.Item1.Y * yLen + 1));

                    var requests = new Dictionary<EntityId, ShipRequest>(game.Me.Ships.Count);
                    foreach (Ship ship in game.Me.Ships.Values)
                    {
                        Log.Message($"------- FLOURINE TURN {game.TurnNumber - 1} ------- ");

                        int mineRadius = Math.Min(4, game.TurnNumber / 70 + 1);
                        var goalMine = game.Map.GetRichestLocalSquare(ship.Position, mineRadius);
                        var costMine = CostField.CreateMine(game, mineCosts);
                        var waveMine = new WaveField(costMine, topN);
                        var flowMine = new FlowField(waveMine);
                        LogFields(game, "MINE", costMine, waveMine, flowMine);

                        if (!states.TryGetValue(ship.Id, out ShipState status))
                        {
                            states[ship.Id] = ShipState.Mining;
                        }

                        (Position Position, int Distance) closestBase = game.GetClosestDrop(ship);
                        if (turnsRemaining - game.Me.Ships.Count - 1 <= closestBase.Distance * EndFactor)
                        {
                            status = ShipState.Ending;
                        }

                        Log.Message($"Analyzing {ship} in {status}");
                        switch (status)
                        {
                            case ShipState.Mining:
                                {
                                    states[ship.Id] = ShipState.Mining;

                                    // If ship is full, go back to base
                                    if (ship.IsFull)
                                    {
                                        Log.Message($"{ship} is full");
                                        goto case ShipState.Homing;
                                    }

                                    Position target = flowMine.GetTarget(ship.Position);

                                    // If current mine is not depleted, stay
                                    if (IsWorthStaying(game.Map, ship, target, out _, out _))
                                    {
                                        Log.Message($"{ship} is staying");
                                        requests[ship.Id] = new ShipRequest(ShipState.Mining, ship.Position);
                                        break;
                                    }

                                    // If mine is depleted, but ship is nearly full, go back to base
                                    else if (ship.Halite > 0.97 * Constants.MaxHalite)
                                    {
                                        Log.Message($"{ship} is nearly full");
                                        goto case ShipState.Homing;
                                    }

                                    Log.Message($"{ship} has target {target}");
                                    requests[ship.Id] = new ShipRequest(ShipState.Mining, target);
                                }
                                break;

                            case ShipState.Homing:
                                {
                                    if (game.IsOnDrop(ship.Position))
                                    {
                                        Log.Message($"{ship} is emptied");
                                        goto case ShipState.Mining;
                                    }

                                    states[ship.Id] = ShipState.Homing;

                                    Position target = flowHome.GetTarget(ship.Position);
                                    Log.Message($"{ship} has target {target}");
                                    requests[ship.Id] = new ShipRequest(ShipState.Homing, target);
                                }
                                break;

                            case ShipState.Ending:
                                {
                                    states[ship.Id] = ShipState.Ending;

                                    if (game.IsOnDrop(ship.Position))
                                    {
                                        Log.Message($"{ship} is home");
                                        requests[ship.Id] = new ShipRequest(ShipState.Ending, ship.Position);
                                        break;
                                    }

                                    Position target = flowHome.GetTarget(ship.Position);
                                    Log.Message($"{ship} has target {target}");
                                    requests[ship.Id] = new ShipRequest(ShipState.Ending, target);
                                }
                                break;
                        }
                    }

                    var commandDict = new Dictionary<EntityId, Command>();
                    foreach (Ship ship in game.Me.Ships.Values)
                    {
                        commandDict[ship.Id] = ship.Stay();
                    }

                    // Take care of conflicts
                    var done = new Dictionary<EntityId, bool>();
                    foreach (KeyValuePair<EntityId, ShipRequest> kvp1 in requests)
                    {
                        if (done.TryGetValue(kvp1.Key, out bool isDone) && isDone)
                            continue;

                        Ship ship1 = game.Me.Ships[kvp1.Key.Id];
                        Position target1 = kvp1.Value.Target;
                        //Log.Message($"Resolving commands for {ship1}");

                        foreach (KeyValuePair<EntityId, ShipRequest> kvp2 in requests)
                        {
                            if (kvp1.Key == kvp2.Key)
                                continue;

                            if (done.TryGetValue(kvp1.Key, out isDone) && isDone)
                                continue;

                            if (done.TryGetValue(kvp2.Key, out isDone) && isDone)
                                continue;

                            Ship ship2 = game.Me.Ships[kvp2.Key.Id];
                            Position target2 = kvp2.Value.Target;
                            //Log.Message($"  Resolving commands for {ship2}");

                            // Swap needed
                            if (target1 == ship2.Position
                                && target2 == ship1.Position)
                            {
                                game.Map[ship2.Position.X, ship2.Position.Y].MarkSafe();
                                Direction dir = game.Map.NaiveNavigate(ship1, ship2.Position);
                                commandDict[ship1.Id] = Command.Move(ship1.Id, dir);

                                game.Map[ship1.Position.X, ship1.Position.Y].MarkSafe();
                                dir = game.Map.NaiveNavigate(ship2, ship1.Position);
                                commandDict[ship2.Id] = Command.Move(ship2.Id, dir);

                                Log.Message($"Swapped {ship1} and {ship2}");
                                done[kvp1.Key] = true;
                                done[kvp2.Key] = true;
                                break;
                            }

                            // Wiggle needed
                            if (target2 == ship1.Position)
                            {
                                if (kvp1.Value.State == ShipState.Ending
                                    && game.IsOnDrop(ship1.Position))
                                {
                                    game.Map[target2.X, target2.Y].MarkSafe();
                                    Direction dir3 = game.Map.NaiveNavigate(ship2, target2);
                                    commandDict[ship2.Id] = Command.Move(ship2.Id, dir3);

                                    Log.Message($"Ended {ship2} over {ship1}");
                                    done[kvp1.Key] = true;
                                    done[kvp2.Key] = true;
                                    continue;
                                }

                                Direction dir1, dir2;
                                if (ship1.Position.X == ship2.Position.X)
                                {
                                    dir1 = Direction.North;
                                    dir2 = Direction.South;
                                }
                                else
                                {
                                    dir1 = Direction.West;
                                    dir2 = Direction.East;
                                }

                                Position target = new Direction[] { dir1, dir2 }
                                    .Select(d => ship2.Position.DirectionalOffset(d))
                                    .Except(requests.Where(n => n.Key != kvp2.Key).Select(n => n.Value.Target))
                                    //.OrderBy(_ => rng.NextDouble())
                                    .FirstOrDefault();

                                if (target == default)
                                {
                                    target = DirectionExtensions.NSEW
                                        .Select(d => ship2.Position.DirectionalOffset(d))
                                        .Except(requests.Where(n => n.Key != kvp2.Key).Select(n => n.Value.Target))
                                        //.OrderBy(_ => rng.NextDouble())
                                        .FirstOrDefault();
                                }

                                Direction dir = target == default ? Direction.Stay : game.Map.NaiveNavigate(ship2, target);
                                commandDict[ship2.Id] = Command.Move(ship2.Id, dir);

                                Log.Message($"Wiggled {ship2} from behind {ship1} to {target}");
                                done[kvp2.Key] = true;
                                continue;
                            }

                            // Avoid crash
                            if (target1 == target2)
                            {
                                Position target = DirectionExtensions.NSEW
                                    .Select(d => ship2.Position.DirectionalOffset(d))
                                    .Except(requests.Where(n => n.Key != kvp2.Key).Select(n => n.Value.Target))
                                    //.OrderBy(_ => rng.NextDouble())
                                    .FirstOrDefault();

                                Direction dir = target == default ? Direction.Stay : game.Map.NaiveNavigate(ship2, target);
                                commandDict[ship2.Id] = Command.Move(ship2.Id, dir);

                                Log.Message($"Swerved {ship2} from {ship1} to {target}");
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
                        Log.Message($"Transferring commands for {ship}");

                        // If mining
                        if (ship.Position == target)
                        {
                            commandDict[ship.Id] = ship.Stay();
                            Log.Message($"Queued {ship} to stay");
                            continue;
                        };

                        // If ship is next to the drop
                        if (game.IsNextToDrop(ship.Position, out Position drop))
                        {
                            if (kvp.Value.State == ShipState.Homing
                                || kvp.Value.State == ShipState.Ending)
                            {
                                target = drop;

                                if (kvp.Value.State == ShipState.Ending
                                    || game.IsShipyardHijacked())
                                {
                                    game.Map[target.X, target.Y].MarkSafe();
                                    Log.Message($"Aiming {ship} at drop {target}");
                                }
                            }
                        }

                        Direction dir = game.Map.NaiveNavigate(ship, target);
                        commandDict[ship.Id] = Command.Move(ship.Id, dir);
                        Log.Message($"Queued {ship} to move {dir}");
                    }

                    // 
                    var commandQueue = commandDict.Select(n => n.Value).ToList();
                    foreach (Command cmd in commandQueue)
                    {
                        Log.Message($"{cmd.Info}");
                    }

                    // Mitigate hijack
                    if (game.IsShipyardHijacked()
                        && game.Me.Halite >= Constants.ShipCost)
                    {
                        commandQueue.Add(Command.SpawnShip());
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

        private static (IReadOnlyDictionary<Position, byte> MineCosts, IReadOnlyDictionary<Position, byte> HomeCosts) SetCustomCosts(Game game)
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
                // But don't affect the halite contour
                mineCosts.Add(pos, CostField.Wall);

                // When homing, attract ships towards my shipyard
                homeCosts.Add(pos, CostField.Valley);
            }

            // Their shipyards
            IEnumerable<Player> players = game.Players.Where(n => n.Id != game.MyId);
            foreach (Position pos in players.Select(n => n.Shipyard.Position))
            {
                // When mining, don't crash into their shipyard ships
                // But don't affect the halite contour
                mineCosts.Add(pos, CostField.Wall);

                // When homing, accentuate my valleys and avoid collisions
                homeCosts.Add(pos, CostField.Peak);
            }

            return (mineCosts, homeCosts);
        }

        private static (IDictionary<Position, byte> MineCosts, IDictionary<Position, byte> HomeCosts) UpdateCustomCosts(Game game, IReadOnlyDictionary<Position, byte> mineBaseCosts, IReadOnlyDictionary<Position, byte> homeBaseCosts)
        {
            var mineCosts = new Dictionary<Position, byte>(mineBaseCosts);
            var homeCosts = new Dictionary<Position, byte>(homeBaseCosts);

            // My dropoffs
            foreach (Position pos in game.Me.Dropoffs.Select(n => n.Value.Position))
            {
                // When mining, don't attract ships to my dropoffs
                // But don't affect the halite contour
                mineCosts[pos] = CostField.Wall;

                // When homing, attract ships towards my dropoffs
                homeCosts[pos] = CostField.Valley;
            }

            // Their dropoffs
            IEnumerable<Player> players = game.Players.Where(n => n.Id != game.MyId);
            foreach (Position pos in players.SelectMany(n => n.Dropoffs).Select(n => n.Value.Position))
            {
                // When mining, don't crash into their dropoff ships
                // But don't affect the halite contour
                mineCosts[pos] = CostField.Wall;

                // When homing, accentuate my valleys and avoid collisions
                homeCosts[pos] = CostField.Peak;
            }

            return (mineCosts, homeCosts);
        }

        private static bool IsWorthStaying(Map map, Ship ship, Position target, out int shipStay, out int shipLeave)
        {
            var stay = IsWorthStaying(map, ship.Position, ship.Halite, target, out shipStay, out shipLeave);

            Log.Message($"STAY?: {ship}, stay={shipStay}, leave={shipLeave}, decision={stay}");

            return stay;
        }

        private static bool IsWorthStaying(Map map, Position shipPosition, in int shipHalite, Position target, out int shipStay, out int shipLeave)
        {
            double mineHalite = map[shipPosition.X, shipPosition.Y].Halite;
            bool barren = mineHalite <= 0;

            // Calculate ship's bounty if it leaves
            shipLeave = shipHalite - Move(mineHalite);

            // Calculate ship's bounty if it stays
            int mined = Mine(mineHalite);
            shipStay = shipHalite + mined;

            if (barren) return false;

            // Calculate opportunity cost, by estimating next steps in a mini game tree
            mineHalite -= mined;
            int shipStayStay = shipStay + Mine(mineHalite);
            int shipStayLeave = shipStay - Move(mineHalite);
            shipStay = Math.Max(shipStayStay, shipStayLeave);

            mineHalite = map[target.X, target.Y].Halite;
            int shipLeaveLeave = shipLeave - Move(mineHalite);
            int shipLeaveStay = shipLeave + Mine(mineHalite);
            shipLeave = Math.Max(shipLeaveLeave, shipLeaveStay);

            return shipStay > shipLeave;

            // 25% of halite available in cell, rounded up to the nearest whole number.
            int Mine(double halite)
                => (int)Math.Ceiling(halite / Constants.ExtractRatio); // 4

            // 10% of halite available at turn origin cell is deducted from ship’s current halite.
            int Move(double halite)
                => (int)Math.Floor(halite / Constants.MoveCostRatio); // 10
        }

        private static void LogFields(Game game, string title, CostField costField, WaveField waveField, FlowField flowField)
        {
            Debug.Assert(game != null);
            Debug.Assert(costField != null);
            Debug.Assert(waveField != null);
            Debug.Assert(flowField != null);

            var map = game.Map;
            var sb = new StringBuilder();

            Log.Message($"{title}: MAP");
            ColHeader(map.Width);
            for (int y = 0; y < map.Height; y++)
            {
                sb.Clear();
                sb.Append($"{y:000}|");
                for (int x = 0; x < map.Width; x++)
                {
                    (char l, char r) = Symbol(x, y);
                    sb.Append($"{l}{map[x, y].Halite:000}{r}|");
                }
                Log.Message(sb.ToString());
            }

            if (costField != null)
            {
                Log.Message($"{title}: COST");
                ColHeader(costField.Width);
                for (int y = 0; y < costField.Height; y++)
                {
                    sb.Clear();
                    sb.Append($"{y:000}|");
                    for (int x = 0; x < costField.Width; x++)
                    {
                        (char l, char r) = Symbol(x, y);
                        sb.Append($"{l}{costField[x, y]:000}{r}|");
                    }
                    Log.Message(sb.ToString());
                }
            }

            if (waveField != null)
            {
                Log.Message($"{title}: WAVE");
                ColHeader(waveField.Width);
                for (int y = 0; y < waveField.Height; y++)
                {
                    sb.Clear();
                    sb.Append($"{y:000}|");
                    for (int x = 0; x < waveField.Width; x++)
                    {
                        (char l, char r) = Symbol(x, y);
                        sb.Append($"{l}{waveField[x, y]:0000}{r}");
                    }
                    Log.Message(sb.ToString());
                }
            }

            if (flowField != null)
            {
                Log.Message($"{title}: FLOW");
                ColHeader(flowField.Width);
                for (int y = 0; y < flowField.Height; y++)
                {
                    sb.Clear();
                    sb.Append($"{y:000}|");
                    for (int x = 0; x < flowField.Width; x++)
                    {
                        (char l, char r) = Symbol(x, y);
                        sb.Append($"{l} {flowField[x, y].Direction.ToSymbol()} {r}|");
                    }
                    Log.Message(sb.ToString());
                }
            }

            (char l, char r) Symbol(int x, int y)
            {
                char l = ' ';
                if (map[x, y].HasStructure)
                {
                    l = '■';
                }
                else if (waveField[x, y] == WaveField.Goal)
                {
                    l = '◉';
                }
                else switch (costField[x, y])
                {
                    case CostField.Valley: l = '▼'; break; // 1
                    case CostField.Peak: l = '▲'; break; // 254
                    case CostField.Wall: l = '⎕'; break; // 255
                }

                char r = ' ';
                if (map[x, y].IsOccupied)
                    r = map[x, y].Ship.Owner.Id == game.MyId.Id ? '+' : '-';

                return (l, r);
            }

            void ColHeader(int len)
            {
                sb.Clear().Append("   |");
                for (int x = 0; x < len; x++)
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
