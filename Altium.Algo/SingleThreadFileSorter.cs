using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Channels;
using FileInfo = System.IO.FileInfo;

namespace Altium.Algo;

public class SingleThreadFileSorter : IFileSorter
{
    private const int DefaultChunkSize = 512 * 1024 * 1024;

    private readonly int _chunkSize;
    private readonly int _parallelLevel;

    public SingleThreadFileSorter(int chunkSize, int parallelLevel)
    {
        _chunkSize = chunkSize;
        _parallelLevel = parallelLevel;
    }

    public SingleThreadFileSorter() : this(DefaultChunkSize, 4)
    {
        
    }
    
    public async Task SortFileAsync(string path, CancellationToken token)
    {
        var fileSize = new FileInfo(path).Length;
        var channel = Channel.CreateBounded<(List<SortableString>, string)>(
            new BoundedChannelOptions(_parallelLevel)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleWriter = true,
                SingleReader = true,
                Capacity = _parallelLevel
            });
        var folder = Path.GetDirectoryName(path);
        var queue = new ConcurrentQueue<string>();
        try {
            var writerTask = WriterTask(path, channel);
            var readerTask = ReaderTask(channel, queue, _parallelLevel);
            var t1 = Stopwatch.StartNew();
            await Task.WhenAll(writerTask, readerTask);
            t1.Stop();
            var mergerTask = MergerTask(queue, fileSize);
            var timer = new Stopwatch();
            timer.Restart();
            var resultFileName = await mergerTask;
            File.Move(resultFileName, Path.Combine(folder, "sorted.txt"));
            Console.WriteLine($"Split time is {t1.Elapsed}");
            Console.WriteLine($"Merge is {timer.Elapsed}");
        }
        finally
        {
//dfs
        }
    }

    private async Task DumpToFile(List<SortableString> buffer,
        ConcurrentQueue<string> createdFiles,
        string? folder)
    {
        await Task.Yield();
        var timer = new Stopwatch();
        timer.Start();
        buffer.Sort();
        timer.Stop();
        var created = Path.Combine(folder, Path.GetRandomFileName());
        var bufferFile = File.CreateText(created);
        Console.WriteLine($"Sort chunk {created} time {timer.Elapsed}");
        timer.Restart();
        try
        {
            foreach (var fileItem in buffer)
                await bufferFile.WriteLineAsync(fileItem.Value);
        }
        finally
        {
            await bufferFile.FlushAsync();
            bufferFile.Close();
        }
        Console.WriteLine($"Write data to {created} time {timer.Elapsed}");
        createdFiles.Enqueue(created);
        buffer.Clear();
    }

    private async Task<int> WriterTask(string path, Channel<(List<SortableString>,string)> channel)
    {
        var writer = channel.Writer;
        using var file = File.OpenText(path);
        var folder = Path.GetDirectoryName(path);
        var chunk = 0;
        var startPos = 0L;
        var buffer = new List<SortableString>(10_000);
        var timer = new Stopwatch();
        while (!file.EndOfStream)
        {
            var line = await file.ReadLineAsync();
            if (line == null)
                continue;
            buffer.Add(new SortableString(line));
            if (file.BaseStream.Position - startPos <= _chunkSize) continue;
            timer.Stop();
            Console.WriteLine($"Get buffer for chunk {chunk}. Time = {timer.Elapsed}");
            await writer.WriteAsync((buffer, folder!));
            chunk++;
            startPos = file.BaseStream.Position;
            timer.Restart();
            buffer = new List<SortableString>(10_000);
        }

        if (buffer.Count != 0)
        {
            await writer.WriteAsync((buffer, folder!));
            chunk++;
        }
        writer.Complete();
        return chunk;
    }
    private async Task ReaderTask(Channel<(List<SortableString>, string)> channel,
        ConcurrentQueue<string> filesForMerge,
        int parallelLevel)
    {
        var tasks = new List<Task>(parallelLevel);
        var reader = channel.Reader;
        await foreach (var item in reader.ReadAllAsync(CancellationToken.None))
        {
            if (tasks.Count == parallelLevel)
            {
                await Task.WhenAny(tasks);
                tasks.RemoveAll(x => x.IsCompleted);
            }
            tasks.Add(DumpToFile(item.Item1, filesForMerge, item.Item2));
        }

        await Task.WhenAll(tasks);
    }

    private async Task<string> MergerTask(ConcurrentQueue<string> filesToProcess, long expectedFileSize)
    {
        string fileName1 = null;
        string fileName2 = null;
        var merger = new FileMerger();
        var files = filesToProcess.ToArray();
        var batchSize = 6;
        while (files.Length != 1)
        {
            var timer = new Stopwatch();
            timer.Start();
            var tasks = new List<Task<string>>();
            var batch = new List<string>();
            foreach (var t in files)
            {
                batch.Add(t);
                if (batch.Count != batchSize) continue;
                var cf = batch.ToArray();
                tasks.Add(Task.Run(async () =>
                {
                    await Task.Yield();
                    return merger.MergeFiles(cf);
                }));
                batch = new List<string>();
            }
            if (batch.Count != 0)
                tasks.Add(Task.Run(async () =>
                {
                    await Task.Yield();
                    return merger.MergeFiles(batch.ToArray());
                }));
            files = await Task.WhenAll(tasks);
            timer.Stop();
            Console.WriteLine($"Merge files {timer.Elapsed}");
        }

        return files.First();
    }
}