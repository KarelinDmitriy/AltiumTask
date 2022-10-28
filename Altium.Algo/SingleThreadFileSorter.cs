using Microsoft.VisualBasic;

namespace Altium.Algo;

public class SingleThreadFileSorter : IFileSorter
{
    private const int DefaultChunkSize = 512 * 1024 * 1024;

    private readonly int _chunkSize;

    public SingleThreadFileSorter(int chunkSize)
    {
        _chunkSize = chunkSize;
    }

    public SingleThreadFileSorter() : this(DefaultChunkSize)
    {
        
    }


    public async Task SortFileAsync(string path, CancellationToken token)
    {
        using var file = File.OpenText(path);
        var folder = Path.GetDirectoryName(path);
        var chunk = 0;
        var startPos = 0L;
        var buffer = new List<FileItem>(500_000);
        try
        {
            while (!file.EndOfStream)
            {
                var line = await file.ReadLineAsync();
                if (line == null)
                    continue;
                buffer.Add(FileItem.Parse(line));
                if (file.BaseStream.Position - startPos <= _chunkSize) continue;
                await DumpToFile(buffer, folder, chunk);
                chunk++;
                startPos = file.BaseStream.Position;
            }

            if (buffer.Count != 0)
            {
                await DumpToFile(buffer, folder, chunk);
                chunk++;
            }

            await MergeFiles(path, chunk);
        }
        finally
        {
            for (int i=0; i<chunk; i++)
                if (File.Exists(Path.Combine(folder, $"{i}.txt")))
                    File.Delete(Path.Combine(folder, $"{i}.txt")); 
        }
    }

    private static async Task DumpToFile(List<FileItem> buffer,
        string? folder,
        int chunk)
    {
        buffer.Sort();
        var bufferFile = File.CreateText(Path.Combine(folder, $"{chunk}.txt"));
        try
        {
            foreach (var fileItem in buffer)
                await bufferFile.WriteLineAsync($"{fileItem.IntPart}:{fileItem.StringPart}");
        }
        finally
        {
            await bufferFile.FlushAsync();
            bufferFile.Close();
        }

        buffer.Clear();
    }

    private async Task MergeFiles(string file, int chunks)
    {
        var folder = Path.GetDirectoryName(file);
        var streams = new StreamReader?[chunks];
        try
        {
            for (var i = 0; i < chunks; i++)
                streams[i] = File.OpenText(Path.Combine(folder, $"{i}.txt"));
            await using var resultFile = File.CreateText(file);
            var pQueue = new PriorityQueue<FileItem, FileItem>();
            for (var i = 0; i < chunks; i++)
            {
                var item = FileItem.Parse(await streams[i].ReadLineAsync(), i);
                pQueue.Enqueue(item, item);
            }

            while (pQueue.Count != 0)
            {
                var item = pQueue.Dequeue();
                await resultFile.WriteLineAsync($"{item.IntPart}:{item.StringPart}");
                if (!streams[item.FileIndex].EndOfStream)
                {
                    var newItem = FileItem.Parse(await streams[item.FileIndex].ReadLineAsync(), item.FileIndex);
                    pQueue.Enqueue(newItem, newItem);
                }
            }
        }
        finally
        {
            foreach (var stream in streams)
            {
                stream?.Close();
            }
        }
    }

    private class FileItem : IComparable<FileItem>
    {
        public string StringPart { get; set; }
        public int IntPart { get; set; }
        public int FileIndex { get; set; }

        public int CompareTo(FileItem? other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;
            var stringPartComparison = string.Compare(StringPart, other.StringPart, StringComparison.Ordinal);
            return stringPartComparison != 0 
                ? stringPartComparison
                : IntPart.CompareTo(other.IntPart);
        }

        public static FileItem Parse(string line)
        {
            var splitResult = line.Split(':');
            return new FileItem
            {
                StringPart = splitResult[1],
                IntPart = int.Parse(splitResult[0])
            };
        }

        public static FileItem Parse(string line, int fileIndex)
        {
            var result = Parse(line);
            result.FileIndex = fileIndex;
            return result;
        }
    }
}