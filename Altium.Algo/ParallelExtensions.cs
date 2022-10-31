namespace Altium.Algo;

public static class ParallelExtensions
{
    public static async Task<List<TOut>> ProcessParallelAsync<TIn, TOut>(
        this IEnumerable<TIn> data, Func<TIn,Task<TOut>> processor,
        int maxTaskCount,
        Func<List<TOut>, Exception, Task>? fallback = null)
    {
        var tasks = new List<Task<TOut>>();
        var result = new List<TOut>();
        try
        {
            foreach (var t in data)
            {
                tasks.Add(processor(t));
                if (tasks.Count != maxTaskCount) continue;
                var completedTask = await Task.WhenAny(tasks);
                result.Add(completedTask.Result);
                tasks.Remove(completedTask);
            }

            if (tasks.Count != 0)
                result.AddRange(await Task.WhenAll(tasks));
        }
        catch (Exception e)
        {
            if (fallback != null)
                await fallback(result, e);
            throw;
        }

        return result;
    }

    public static async Task<List<TOut>> ProcessParallelAsync<TIn, TOut>(
        this IAsyncEnumerable<TIn> data, Func<TIn, Task<TOut>> processor,
        int maxTaskCount,
        Func<List<TOut>, Exception, Task>? fallback = null)
    {
        var tasks = new List<Task<TOut>>();
        var result = new List<TOut>();
        try
        {
            await foreach (var t in data)
            {
                tasks.Add(processor(t));
                if (tasks.Count != maxTaskCount) continue;
                var completedTask = await Task.WhenAny(tasks);
                result.Add(completedTask.Result);
                tasks.Remove(completedTask);
            }

            if (tasks.Count != 0)
                result.AddRange(await Task.WhenAll(tasks));
        }
        catch (Exception e)
        {
            if (fallback != null)
                await fallback(result, e);
            throw;
        }
        
        return result;
    }
}