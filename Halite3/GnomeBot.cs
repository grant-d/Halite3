using Halite3.Hlt;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

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
                // At this point "game" variable is populated with initial map data.
                // This is a good place to do computationally expensive start-up pre-processing.
                // As soon as you call "ready" function below, the 2 second per turn timer will start.
                Game.Ready("MyCSharpBot");
                Log.LogMessage("Successfully created bot! My Player ID is " + game.MyId + ". Bot rng seed is " + rngSeed + ".");

                while (true)
                {
                    game.UpdateFrame();

                    var commandQueue = new List<Command>();

                    foreach (Ship ship in game.Me.Ships.Values)
                    {
                    }

                    Game.EndTurn(commandQueue);
                }
            }
        }

    }
}
