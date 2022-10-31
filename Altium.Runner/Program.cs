using System.Diagnostics;
using Altium.Algo;
using Altium.Runner;

const int DefaultThreadCount = 8;
const long InitialBlockSize = 128 * 1024;

try
{
    if (args[0] == "-s")
    {
        var threadCount = DefaultThreadCount;
        var initialBlockSize = InitialBlockSize;
        if (args.Length < 2)
            throw new ConsoleArgumentException("Sort mode should contains 2-4 arguments. See help -h");
        if (!File.Exists(args[1]))
            throw new ConsoleArgumentException($"File {args[1]} not exists");
        if (args.Length > 3)
        {
            if (!int.TryParse(args[3], out var newThreadCount) || newThreadCount < 1)
                throw new ConsoleArgumentException($"Invalid thread count value {args[3]}");
            threadCount = newThreadCount;
        }

        if (args.Length > 4)
        {
            if (!long.TryParse(args[4], out var newBlockSize) || newBlockSize < 1)
                throw new ConsoleArgumentException($"Invalid initial block size value {args[4]}");
            initialBlockSize = newBlockSize;
        }
        var sorter = new MultiThreadFileSorter(initialBlockSize, threadCount);
        var timer = new Stopwatch();
        timer.Start();
        await sorter.SortFileAsync(args[1], args[2], CancellationToken.None);
        timer.Stop();
        Console.WriteLine($"Total time {timer.Elapsed}");
    }
    else if (args[0] == "-g")
    {
        if (args.Length != 3)
            throw new ConsoleArgumentException("Generate mode should contains 3 arguments. See help -h");
        var generator = new FileGenerator();
        if (!long.TryParse(args[1], out var size) || size < 1)
            throw new ConsoleArgumentException($"Incorrect size({args[1]} for file");
        await generator.GenerateFileAsync(args[2], size, CancellationToken.None);
        Console.WriteLine("Complete");
    }
    else if (args[0] == "-h")
    {
        Console.WriteLine(
@"-g = Generate new file with lines like '<random number>: <random string>'.
       Second parameter is file size in bytes.
       Third parameter is path to file.
       For example generate random file size of 1Mb to path 'C:/data/randomfile.txt':
       -g 1048576 C:/data/randomfile.txt

-s = Sort file
     Second parameter is path to file.
     Third parameter is path to save result file
     Fourth parameter is level of parallelism sort operation. Optional. Default 8
     fifth parameter is size of initial split block size. Optional. Default 128kb
     For example sort file 'C:\data\randomfile.txt' to 'C:\data\sorted.txt' in 4 thread with 1Mb initial block size:
     -s C:\data\randomfile.txt С:\data\sorted.txt 4 1048576
");
    }
    else throw new ConsoleArgumentException("Unknown mode type");
}
catch (ConsoleArgumentException e)
{
    Console.WriteLine(e.Message);
}
