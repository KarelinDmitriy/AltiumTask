namespace Altium.Algo;

public interface IFileSorter
{
    Task SortFileAsync(string pathToFileForSort, string pathToResultFile, CancellationToken token);
}