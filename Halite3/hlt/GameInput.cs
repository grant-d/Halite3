using System;
using System.Globalization;
using System.Text;

namespace Halite3.Hlt
{
    /// <summary>
    /// A class that wraps Console.Readline() and int.Parse() with exception handling.
    /// </summary>
    public sealed class GameInput
    {
        private readonly string[] _input;
        private int _current;

        public GameInput(string line)
        {
            _input = line.Split(" ");
        }

        public int GetInt()
            => int.Parse(_input[_current++], CultureInfo.InvariantCulture);

        public static GameInput ReadInput()
            => new GameInput(ReadLine());

        public static String ReadLine()
        {
            try
            {
                var builder = new StringBuilder();

                int buffer;
                for (; (buffer = Console.Read()) >= 0;)
                {
                    if (buffer == '\n')
                    {
                        break;
                    }
                    if (buffer == '\r')
                    {
                        // Ignore carriage return if on windows for manual testing.
                        continue;
                    }
                    builder.Append((char)buffer);
                }

                return builder.ToString();
            }
            catch (Exception e)
            {
                Log.Message("Input connection from server closed. Exiting...");
                throw new InvalidOperationException(e.Message);
            }
        }
    }
}
