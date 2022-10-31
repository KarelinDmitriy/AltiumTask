namespace Altium.Algo;

public class FileMerger
{
    public async Task<string> MergeFiles(IList<string> filesNames)
    {
        if (filesNames.Count == 1)
            return filesNames[0];
        var streams = new StreamReader?[filesNames.Count];
        var resultFileName = Path.Combine(
            Path.GetDirectoryName(filesNames[0])!,
            Path.GetRandomFileName());
        var isExceptionThrow = false;
        try
        {
            for (int i = 0; i < filesNames.Count; i++)
                streams[i] = File.OpenText(filesNames[i]);

            await using var resultStream = File.CreateText(resultFileName);
            var pQueue = new PriorityQueue<StringWrapperForSorting, StringWrapperForSorting>();
            for (var i = 0; i < filesNames.Count; i++)
            {
                var item = new StringWrapperForSorting((await streams[i]!.ReadLineAsync())!, i);
                pQueue.Enqueue(item, item);
            }

            while (pQueue.Count != 0)
            {
                var item = pQueue.Dequeue();
                await resultStream.WriteLineAsync(item.Value);

                if (streams[item.FileNumber]!.EndOfStream) continue;
                var newItem = new StringWrapperForSorting((await streams[item.FileNumber]!.ReadLineAsync())!, item.FileNumber);
                pQueue.Enqueue(newItem, newItem);
            }
        }
        catch (Exception)
        {
            isExceptionThrow = true;
            throw;
        }
        finally
        {
            foreach (var stream in streams)
                stream?.Close();
            if (isExceptionThrow)
                File.Delete(resultFileName);
        }

        return resultFileName;
    }
}