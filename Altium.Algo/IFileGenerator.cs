namespace Altium.Algo;

public interface IFileGenerator
{
    Task GenerateFileAsync(string path, long byteSize, CancellationToken token);
}