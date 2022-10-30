namespace Altium.Algo;

public class FileMerger
{
    public string MergeFiles(string[] filesNames)
    {
        if (filesNames.Length == 1)
            return filesNames[0];

        var streams = new StreamReader?[filesNames.Length];
        var resultFileName = Path.Combine(
            Path.GetDirectoryName(filesNames[0])!,
            Path.GetRandomFileName());
        try
        {
            for (int i = 0; i < filesNames.Length; i++)
                streams[i] = File.OpenText(filesNames[i]);

            using var resultStream = File.CreateText(resultFileName);
            var pQueue = new PriorityQueue<SortableString, SortableString>();
            for (var i = 0; i < filesNames.Length; i++)
            {
                var item = new SortableString(streams[i]!.ReadLine()!, i);
                pQueue.Enqueue(item, item);
            }

            while (pQueue.Count != 0)
            {
                var item = pQueue.Dequeue();
                resultStream.WriteLine(item.Value);
                if (streams[item.FileNumber]!.EndOfStream) continue;
                var newItem = new SortableString(streams[item.FileNumber]!.ReadLine()!, item.FileNumber);
                pQueue.Enqueue(newItem, newItem);
            }
        }
        finally
        {
            foreach (var stream in streams)
                stream?.Close();

            foreach (var fileName in filesNames)
                File.Delete(fileName);
        }

        return resultFileName;
    }
    
    public string MergeFiles(string path1, string path2)
    {
        StreamReader? stream1 = null;
        StreamReader? stream2 = null;
        var resultFileName = Path.Combine(Path.GetDirectoryName(path1)!, Path.GetRandomFileName());
        try
        {
            stream1 = File.OpenText(path1);
            stream2 = File.OpenText(path2);
            using var resultStream = File.CreateText(resultFileName);
            var stringFromF1 = new SortableString(stream1.ReadLine()!);
            var stringFromF2 = new SortableString(stream2.ReadLine()!);
            while (!stream1.EndOfStream && !stream2.EndOfStream)
            {
                var cmpResult = stringFromF1.CompareTo(stringFromF2);
                if (cmpResult == -1)
                {
                    resultStream.WriteLine(stringFromF1.Value);
                    stringFromF1 = new SortableString(stream1.ReadLine()!);
                }
                else if (cmpResult == 1)
                {
                    resultStream.WriteLine(stringFromF2.Value);
                    stringFromF2 = new SortableString(stream2.ReadLine()!);
                }
                else
                {
                    resultStream.WriteLine(stringFromF1.Value);
                    resultStream.WriteLine(stringFromF1.Value);
                    stringFromF1 = new SortableString(stream1.ReadLine()!);
                    stringFromF2 = new SortableString(stream2.ReadLine()!);
                }
            }
            WriteLastCompareStrings(stringFromF1, stringFromF2, resultStream);
            var nonEmptyStream = stream1.EndOfStream ? stream2 : stream1;
            while (!nonEmptyStream.EndOfStream)
                resultStream.WriteLine(nonEmptyStream.ReadLine()!);
        }
        finally
        {
            stream1?.Close();
            stream2?.Close();
            File.Delete(path1);
            File.Delete(path2);
        }

        return resultFileName;
    }

    private static void WriteLastCompareStrings(SortableString first, SortableString second, StreamWriter writer)
    {
        if (first.CompareTo(second) < 1)
        {
            writer.WriteLine(first.Value);
            writer.WriteLine(second.Value);
        }
        else
        {
            writer.WriteLine(second.Value);
            writer.WriteLine(first.Value);
        }
    }
}