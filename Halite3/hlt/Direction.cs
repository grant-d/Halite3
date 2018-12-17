namespace Halite3.Hlt
{
    /// <summary>
    /// A Direction is one of the 4 cardinal directions or STILL.
    /// </summary>
    /// <see cref="https://halite.io/learn-programming-challenge/api-docs#direction"/>
#pragma warning disable CA1028 // Enum Storage should be Int32
    public enum Direction : byte
#pragma warning restore CA1028 // Enum Storage should be Int32
    {
        X = (byte)'o',

        N = (byte)'n',
        E = (byte)'e',
        S = (byte)'s',
        W = (byte)'w',
    }
}
