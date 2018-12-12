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
        public static void Main(string[] args)
        {
            //while (!Debugger.IsAttached);

            int rngSeed = args.Length > 1 ? int.Parse(args[1], CultureInfo.InvariantCulture) : DateTime.Now.Millisecond;
            var rng = new Random(rngSeed);

            using (var game = new Game())
            {
                Log.LogMessage(game.Map.GetMinMaxHalite().ToString());

                int maxShips = 20;

                // At this point "game" variable is populated with initial map data.
                // This is a good place to do computationally expensive start-up pre-processing.
                // As soon as you call "ready" function below, the 2 second per turn timer will start.
                Game.Ready("MyCSharpBot");
                Log.LogMessage("Successfully created bot! My Player ID is " + game.MyId + ". Bot rng seed is " + rngSeed + ".");

                while (true)
                {
                    game.UpdateFrame();

                    var richMine = game.Map.GetRichestLocalSquare(game.Me.Shipyard.Position, 4);
                    var costMine = new CostField(game, new CostCell(254), CostCell.Wall);
                    var intgMine = new IntegrationField(costMine, richMine.Position);
                    var flowMine = new FlowField(intgMine);
                    //LogFields(game, "MINE", costMine, intgMine, flowMine);

                    var costHome = new CostField(game, new CostCell(1), new CostCell(254));
                    var intgHome = new IntegrationField(costHome, game.Me.Shipyard.Position);
                    var flowHome = new FlowField(intgHome);
                    //LogFields(game, "HOME", costHome, intgHome, flowHome);

                    var commandQueue = new List<Command>();

                    if (game.Me.Ships.Count < maxShips
                        && game.Me.Halite > Constants.ShipCost)
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

                    foreach (Ship ship in game.Me.Ships.Values)
                    {
                        if (ship.IsFull)
                        {
                            FlowCell flow = flowHome[ship.Position];
                            FlowDirection flowDir = flow.Direction;
                            Direction dir = game.Map.NaiveNavigate(ship, flowDir.FromPosition(ship.Position));
                            commandQueue.Add(Command.Move(ship.Id, dir));
                        }
                        else
                        {
                            if (IsWorthMining(game.Map[ship.Position].Halite))
                            {
                                commandQueue.Add(ship.Stay());
                            }
                            else
                            {
                                FlowCell flow = flowMine[ship.Position];
                                FlowDirection flowDir = flow.Direction;
                                flowDir = flowDir.Invert();
                                Direction dir = game.Map.NaiveNavigate(ship, flowDir.FromPosition(ship.Position));
                                commandQueue.Add(Command.Move(ship.Id, dir));
                            }
                        }
                    }

                    Game.EndTurn(commandQueue);
                }
            }
        }

        private static bool IsWorthMining(int halite)
            => halite / Constants.ExtractRatio > halite / Constants.MoveCostRatio;

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
}
