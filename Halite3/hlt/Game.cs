using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Halite3.Hlt
{
    /// <summary>
    /// The game object holds all metadata pertinent to the game and all its contents
    /// </summary>
    /// <see cref="https://halite.io/learn-programming-challenge/api-docs#game"/>
    public sealed class Game : IDisposable
    {
        public PlayerId MyId { get; }
        public List<Player> Players { get; } = new List<Player>();
        public Player Me { get; }
        public Map Map { get; }

        public int TurnNumber { get; set; }

        /// <summary>
        /// Initiates a game object collecting all start-state instances for the contained items for pre-game.
        /// Also sets up basic logging.
        /// </summary>
        public Game()
        {
            Constants.LoadConstants(GameInput.ReadLine());

            var inputs = GameInput.ReadInput();
            int numPlayers = inputs.GetInt();
            MyId = new PlayerId(inputs.GetInt());

            Log.Initialize(new StreamWriter(String.Format(CultureInfo.InvariantCulture, "bot-{0}.log", MyId)));

            for (int i = 0; i < numPlayers; i++)
            {
                Players.Add(Player._generate());
            }

            Me = Players[MyId.Id];
            Map = Map._generate();
        }

        /// <summary>
        /// Signals to the Halite engine that you are ready to begin.
        /// </summary>
        /// <param name="name"></param>
        public static void Ready(string name)
        {
            Console.WriteLine(name);
        }

        /// <summary>
        /// Reads in the information about the new turn from the Halite engine,
        /// and then updates the GameMap and the Players.
        /// </summary>
        public void UpdateFrame()
        {
            TurnNumber = GameInput.ReadInput().GetInt();
            Log.LogMessage("=============== TURN " + TurnNumber + " ================");

            for (int i = 0; i < Players.Count; ++i)
            {
                var input = GameInput.ReadInput();

                var currentPlayerId = new PlayerId(input.GetInt());
                int numShips = input.GetInt();
                int numDropoffs = input.GetInt();
                int halite = input.GetInt();

                Players[currentPlayerId.Id]._update(numShips, numDropoffs, halite);
            }

            Map._update();

            foreach (Player player in Players)
            {
                foreach (Ship ship in player.Ships.Values)
                {
                    Map[ship].MarkUnsafe(ship);
                }

                Map[player.Shipyard].Structure = player.Shipyard;

                foreach (Dropoff dropoff in player.Dropoffs.Values)
                {
                    Map[dropoff].Structure = dropoff;
                }
            }
        }

        /// <summary>
        /// Converts instances of Command into strings and sends them to the Halite engine.
        /// </summary>
        /// <param name="commands">An IEnumerable such as an array or List of commands.</param>
        public static void EndTurn(IEnumerable<Command> commands)
        {
            foreach (Command command in commands)
            {
                Console.Write(command.Info);
                Console.Write(' ');
            }
            Console.WriteLine();
        }

        #region IDisposable

        private bool _disposed = false;

        void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Log.Dispose();
                }

                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion
    }
}
