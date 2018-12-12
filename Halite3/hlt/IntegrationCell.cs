namespace Halite3.Hlt
{
    // https://leifnode.com/2013/12/flow-field-pathfinding/

    public sealed class IntegrationCell
    {
        public const ushort Wall = ushort.MaxValue;

        public ushort Cost { get; set; }

        public IntegrationCell(ushort cost)
        {
            Cost = cost;
        }
    }
}
