using System;
using System.Threading;

namespace StreamCatcherConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Enter stream name");
            var streamName = Console.ReadLine() ?? "".Trim();
            Console.WriteLine("Enter video folder path");
            var path = Console.ReadLine() ?? "".Trim();

            var streamCatcher = new StreamCatcher.StreamCatcher(streamName, path);
            streamCatcher.Run(new CancellationToken()).Wait();
        }
    }
}
