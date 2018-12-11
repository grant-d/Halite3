using System.Diagnostics;

namespace Halite3.Hlt
{
    public static class MapExtensions
    {
        public static Position GetLocalRichestMine(this Map map, Ship ship, byte radius)
        {
            Debug.Assert(map != null);
            Debug.Assert(ship != null);

            Position mine = ship.Position;

            if (radius == 0)
                return mine;

            int halite = map.At(mine).Halite;
            for (int x = ship.Position.X - radius; x <= ship.Position.X + radius; x++)
            {
                for (int y = ship.Position.Y - radius; y <= ship.Position.Y + radius; y++)
                {
                    var pos = new Position(x, y);
                    MapCell cell = map.At(pos);

                    if (cell.IsEmpty
                        && cell.Halite > halite)
                    {
                        halite = map.At(pos).Halite;
                        mine = pos;
                    }
                }
            }

            return mine;
        }
    }
}
