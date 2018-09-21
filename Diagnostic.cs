using System;
using System.Diagnostics;

namespace Archiver
{
    public static class Diagnostic
    {
        public static void Timing(string message, Action action)
        {
            var watch = Stopwatch.StartNew();
            action();
            watch.Stop();
            Console.WriteLine(message + " [" + watch.ElapsedMilliseconds + " ms]");
        }
    }
}
