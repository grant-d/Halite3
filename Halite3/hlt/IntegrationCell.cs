namespace Halite3.Hlt
{
    // https://leifnode.com/2013/12/flow-field-pathfinding/

    public sealed class IntegrationCell
    {
        public const ushort Wall = ushort.MaxValue;

        public ushort Mine { get; set; }

        public ushort Home { get; set; }

        public IntegrationCell(ushort mine, ushort home)
        {
            Mine = mine;
            Home = home;
        }
    }
}
