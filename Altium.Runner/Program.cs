using System.Diagnostics;
using Altium.Algo;

if (args[0] == "-s")
{
    var sorter = new SingleThreadFileSorter(5 * 1024 * 1024, 1);
    var timer = new Stopwatch();
    timer.Start();
    await sorter.SortFileAsync(args[1], CancellationToken.None);
    timer.Stop();
    Console.WriteLine($"Total {timer.Elapsed}");
}