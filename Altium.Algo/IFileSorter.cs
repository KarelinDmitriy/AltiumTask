namespace Altium.Algo;

public interface IFileSorter
{
    Task SortFileAsync(string path, CancellationToken token);
}