using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;

namespace Halite3.Hlt
{
    public static class Constants
    {
        /// <summary>
        /// The cost to build a single ship.
        /// </summary>
        public static int ShipCost { get; private set; }

        /// <summary>
        /// The cost to build a dropoff.
        /// </summary>
        public static int DropOffCost { get; private set; }

        /// <summary>
        /// The maximum amount of halite a ship can carry.
        /// </summary>
        public static int MaxHalite { get; private set; }

        /// <summary>
        /// The maximum number of turns a game can last. This reflects the fact
        /// that smaller maps play for fewer turns.
        /// </summary>
        public static int MaxTurns { get; private set; }

        /// <summary>
        /// 1/EXTRACT_RATIO halite (truncated) is collected from a square per turn.
        /// </summary>
        public static int ExtractRatio { get; private set; }

        /// <summary>
        /// 1/MOVE_COST_RATIO halite (truncated) is needed to move off a cell.
        /// </summary>
        public static int MoveCostRatio { get; private set; }

        /// <summary>
        /// Whether inspiration is enabled.
        /// </summary>
        public static bool InspirationEnabled { get; private set; }

        /// <summary>
        /// A ship is inspired if at least INSPIRATION_SHIP_COUNT opponent
        /// ships are within this Manhattan distance.
        /// </summary>
        public static int InspirationRadius { get; private set; }

        /// <summary>
        /// A ship is inspired if at least this many opponent ships are within
        /// INSPIRATION_RADIUS distance.
        /// </summary>
        public static int InspirationShipCount { get; private set; }

        /// <summary>
        /// An inspired ship mines 1/X halite from a cell per turn instead.
        /// </summary>
        public static int InspiredExtractRatio { get; private set; }

        /// <summary>
        /// An inspired ship that removes Y halite from a cell collects X*Y additional halite.
        /// </summary>
        public static double InspiredBonusMultiplier { get; private set; }

        /// <summary>
        /// An inspired ship instead spends 1/X% halite to move.
        /// </summary>
        public static double InspiredMoveCostRatio { get; private set; }

        /// <summary>
        /// Deserializes the JSON string of constants and stores it as a collection
        /// of static variables.
        /// </summary>
        /// <param name="constantsStr">A JSON string containing the game constants</param>
        internal static void LoadConstants(string constantsStr)
        {
            Dictionary<string, string> constantsDict =
                JsonConvert.DeserializeObject<Dictionary<string, string>>(constantsStr);

            ShipCost = int.Parse(constantsDict["NEW_ENTITY_ENERGY_COST"], CultureInfo.InvariantCulture);
            DropOffCost = int.Parse(constantsDict["DROPOFF_COST"], CultureInfo.InvariantCulture);
            MaxHalite = int.Parse(constantsDict["MAX_ENERGY"], CultureInfo.InvariantCulture);
            MaxTurns = int.Parse(constantsDict["MAX_TURNS"], CultureInfo.InvariantCulture);
            ExtractRatio = int.Parse(constantsDict["EXTRACT_RATIO"], CultureInfo.InvariantCulture);
            MoveCostRatio = int.Parse(constantsDict["MOVE_COST_RATIO"], CultureInfo.InvariantCulture);
            InspirationEnabled = bool.Parse(constantsDict["INSPIRATION_ENABLED"]);
            InspirationRadius = int.Parse(constantsDict["INSPIRATION_RADIUS"], CultureInfo.InvariantCulture);
            InspirationShipCount = int.Parse(constantsDict["INSPIRATION_SHIP_COUNT"], CultureInfo.InvariantCulture);
            InspiredExtractRatio = int.Parse(constantsDict["INSPIRED_EXTRACT_RATIO"], CultureInfo.InvariantCulture);
            InspiredBonusMultiplier = double.Parse(constantsDict["INSPIRED_BONUS_MULTIPLIER"], CultureInfo.InvariantCulture);
            InspiredMoveCostRatio = int.Parse(constantsDict["INSPIRED_MOVE_COST_RATIO"], CultureInfo.InvariantCulture);
        }
    }
}
