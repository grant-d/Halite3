namespace Halite3.Hlt
{
    // https://leifnode.com/2013/12/flow-field-pathfinding/

    public sealed class CostCell
    {
        public const byte Wall = byte.MaxValue;

        public byte Cost { get; set; }

        public CostCell(byte cost)
        {
            Cost = cost;
        }
    }
}
