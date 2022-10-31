using System.Threading.Channels;

namespace Altium.Algo;

public class MultiThreadFileSorter : IFileSorter
{
    private readonly long _blockSize;
    private readonly int _parallelLevel;

    public MultiThreadFileSorter(long blockSize, int parallelLevel)
    {
        _blockSize = blockSize;
        _parallelLevel = parallelLevel;
    }

    public async Task SortFileAsync(string pathToFileForSort, string pathToResultFile, CancellationToken token)
    {
        var channel = Channel.CreateBounded<List<StringWrapperForSorting>>(
            new BoundedChannelOptions(_parallelLevel)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleWriter = true,
                SingleReader = true,
                Capacity = _parallelLevel
            });
        var folder = Path.GetDirectoryName(pathToFileForSort);
        var writerTask = SplitFileAsync(pathToFileForSort, channel);
        var readerTask = SaveBlocksAsync(channel, folder!);
        await Task.WhenAll(writerTask, readerTask);
        var resultFileName = await MergeFilesAsync((await readerTask).ToArray());
        File.Move(resultFileName, Path.Combine(folder!, pathToResultFile));
    }

    private async Task SplitFileAsync(string path, ChannelWriter<List<StringWrapperForSorting>> writer)
    {
        using var file = File.OpenText(path);
        var blockStartPosition = 0L;
        var buffer = new List<StringWrapperForSorting>(5_000);
        while (!file.EndOfStream)
        {
            var line = await file.ReadLineAsync();
            if (line == null)
                continue;
            buffer.Add(new StringWrapperForSorting(line));
            if (file.BaseStream.Position - blockStartPosition <= _blockSize) continue;
            await writer.WriteAsync(buffer);
            blockStartPosition = file.BaseStream.Position;
            buffer = new List<StringWrapperForSorting>(5_000);
        }

        if (buffer.Count != 0)
        {
            await writer.WriteAsync(buffer);
        }

        writer.Complete();
    }

    private Task<List<string>> SaveBlocksAsync(ChannelReader<List<StringWrapperForSorting>> reader,
        string folder)
    {
        return reader.ReadAllAsync()
            .ProcessParallelAsync(
                p => SortAndSaveFileBlockAsync(p, folder),
                _parallelLevel,
                DeleteFilesFallback);
    }

    private async Task<string> SortAndSaveFileBlockAsync(List<StringWrapperForSorting> buffer,
        string folder)
    {
        await Task.Yield();
        buffer.Sort();
        var createdFileName = Path.Combine(folder, Path.GetRandomFileName());
        var bufferFile = File.CreateText(createdFileName);
        var isThrowException = false;
        try
        {
            bufferFile.AutoFlush = false;
            foreach (var fileItem in buffer)
                await bufferFile.WriteLineAsync(fileItem.Value);
        }
        catch (Exception)
        {
            isThrowException = true;
            throw;
        }
        finally
        {
            if (!isThrowException)
                await bufferFile.FlushAsync();
            bufferFile.Close();
            if (isThrowException)
                File.Delete(createdFileName);
        }

        return createdFileName;
    }
    private async Task<string> MergeFilesAsync(string[] files)
    {
        const int batchSize = 8;
        var merger = new FileMerger();
        while (files.Length != 1)
        {
            var batches = new List<List<string>>();
            var batch = new List<string>();
            foreach (var t in files)
            {
                batch.Add(t);
                if (batch.Count != batchSize) continue;
                batches.Add(batch);
                batch = new List<string>();
            }

            if (batch.Count != 0)
                batches.Add(batch);
            string[] mergedFiles = {};
            try
            {
                mergedFiles = (await batches
                        .ProcessParallelAsync(b => merger.MergeFiles(b),
                            _parallelLevel,
                            DeleteFilesFallback)
                    ).ToArray();
            }
            finally
            {
                foreach (var file in files.Where(x => !mergedFiles.Contains(x)))
                    File.Delete(file);
            }
            files = mergedFiles;
        }

        return files.First();
    }

    private static Task DeleteFilesFallback(List<string> files, Exception ex)
    {
        foreach (var file in files)
            File.Delete(file);
        return Task.CompletedTask;
    }
}