using System.Collections.Generic;

namespace Halite3.Hlt
{
    /// <summary>
    /// Players have an id, a shipyard, halite and dictionaries of ships and dropoffs as member variables.
    /// </summary>
    /// <see cref="https://halite.io/learn-programming-challenge/api-docs#player"/>
    public sealed class Player
    {
        public PlayerId Id { get; }
        public Shipyard Shipyard { get; }
        public int Halite { get; private set; }


        private readonly Dictionary<int, Ship> _ships = new Dictionary<int, Ship>();
        public IReadOnlyDictionary<int, Ship> Ships => _ships;


        private readonly Dictionary<int, Dropoff> _dropoffs = new Dictionary<int, Dropoff>();
        public IReadOnlyDictionary<int, Dropoff> Dropoffs => _dropoffs;

        private Player(PlayerId playerId, Shipyard shipyard, int halite = 0)
        {
            Id = playerId;
            Shipyard = shipyard;
            Halite = halite;
        }

        /// <summary>
        /// Update each ship and dropoff for the player.
        /// </summary>
        internal void _update(int numShips, int numDropoffs, int halite)
        {
            Halite = halite;

            _ships.Clear();
            for (int i = 0; i < numShips; ++i)
            {
                var ship = Ship._generate(Id);
                _ships[ship.Id.Id] = ship;
            }

            _dropoffs.Clear();
            for (int i = 0; i < numDropoffs; ++i)
            {
                var dropoff = Dropoff._generate(Id);
                _dropoffs[dropoff.Id.Id] = dropoff;
            }
        }

        /// <summary>
        /// Create a new Player by reading from the Halite engine.
        /// </summary>
        /// <returns></returns>
        internal static Player _generate()
        {
            var input = GameInput.ReadInput();

            var playerId = new PlayerId(input.GetInt());
            int shipyard_x = input.GetInt();
            int shipyard_y = input.GetInt();

            return new Player(playerId, new Shipyard(playerId, new Position(shipyard_x, shipyard_y)));
        }
    }
}
