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
                var costField = new CostField(game);
                var intgField = new IntegrationField(costField, game.Me.Shipyard.Position);
                var flowField = new FlowField(intgField);

                Log.LogMessage(game.Map.GetMinMaxHalite().ToString());

                var sb = new StringBuilder();

                Log.LogMessage("COST FIELD");
                for (var y = 0; y < flowField.Height; y++)
                {
                    sb.Clear();
                    for (var x = 0; x < flowField.Width; x++)
                    {
                        var pos = new Position(x, y);
                        if (pos == game.Me.Shipyard.Position)
                            sb.Append((costField.At(pos).Home.ToString() + "**").PadRight(6));
                        else
                            sb.Append((costField.At(pos).Home.ToString() + " |").PadRight(6));
                    }
                    sb.AppendLine();
                    Log.LogMessage(sb.ToString());
                }

                Log.LogMessage("INTEGRATION FIELD");
                for (var y = 0; y < flowField.Height; y++)
                {
                    sb.Clear();
                    for (var x = 0; x < flowField.Width; x++)
                    {
                        var pos = new Position(x, y);
                        if (pos == game.Me.Shipyard.Position)
                            sb.Append((intgField.At(pos).Home.ToString() + "**").PadRight(6));
                        else
                            sb.Append((intgField.At(pos).Home.ToString() + " |").PadRight(6));
                    }
                    sb.AppendLine();
                    Log.LogMessage(sb.ToString());
                }

                Log.LogMessage("FLOW FIELD");
                for (var y = 0; y < flowField.Height; y++)
                {
                    sb.Clear();
                    for (var x = 0; x < flowField.Width; x++)
                    {
                        var pos = new Position(x, y);
                        if (pos == game.Me.Shipyard.Position)
                            sb.Append((flowField.At(pos).Home.ToSymbol() + "**").PadRight(6));
                        else
                            sb.Append((flowField.At(pos).Home.ToSymbol() + "  ").PadRight(6));
                    }
                    sb.AppendLine();
                    Log.LogMessage(sb.ToString());
                }

                // At this point "game" variable is populated with initial map data.
                // This is a good place to do computationally expensive start-up pre-processing.
                // As soon as you call "ready" function below, the 2 second per turn timer will start.
                Game.Ready("MyCSharpBot");
                Log.LogMessage("Successfully created bot! My Player ID is " + game.MyId + ". Bot rng seed is " + rngSeed + ".");

                while (true)
                {
                    game.UpdateFrame(); 

                    var commandQueue = new List<Command>();

                    if (game.Me.Ships.Count == 0)
                    {
                        commandQueue.Add(Command.SpawnShip());
                    }

                    foreach (Ship ship in game.Me.Ships.Values)
                    {
                        FlowCell flow = flowField.At(ship.Position);

                        if (ship.IsFull)
                        {
                            FlowDirection flowDir = flow.Home;
                            Direction dir = game.Map.NaiveNavigate(ship, flowDir.ToPosition(ship.Position));
                            commandQueue.Add(Command.Move(ship.Id, dir));
                        }
                        else
                        {
                            FlowDirection flowDir = flow.Mine;
                            Direction dir = game.Map.NaiveNavigate(ship, flowDir.ToPosition(ship.Position));
                            commandQueue.Add(Command.Move(ship.Id, dir));
                        }
                    }

                    Game.EndTurn(commandQueue);
                }
            }
        }
    }
}
