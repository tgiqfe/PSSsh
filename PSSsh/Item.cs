using System.Diagnostics;

namespace PSSsh
{
    internal class Item
    {
        public static readonly string ProcessDirectory =
            Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
    }
}
