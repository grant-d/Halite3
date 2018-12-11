using System.IO;

namespace Halite3.Hlt
{
    /// <summary>
    /// A class that can be used to log messages to a file.
    /// </summary>
    public sealed class Log
    {
        private static Log s_instance;
        private readonly TextWriter _file;

        private Log(TextWriter f)
        {
            _file = f;
        }

        public static void Initialize(TextWriter f)
        {
            s_instance = new Log(f);
        }

        public static void LogMessage(string message)
        {
            try
            {
                s_instance._file.WriteLine(message);
                s_instance._file.Flush();
            }
            catch (IOException)
            {
            }
        }

        public static void Dispose()
        {
            s_instance._file.Flush();
            s_instance._file.Dispose();
        }
    }
}
