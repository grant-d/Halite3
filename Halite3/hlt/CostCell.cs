namespace Halite3.Hlt
{
    // https://leifnode.com/2013/12/flow-field-pathfinding/

    public sealed class CostCell
    {
        public const byte Wall = byte.MaxValue;

        public byte Mine { get; set; }
        public byte Home { get; set; }

        public CostCell(byte mine, byte home)
        {
            Mine = mine;
            Home = home;
        }
    }
}
