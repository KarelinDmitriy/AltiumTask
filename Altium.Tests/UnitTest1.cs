using Altium.Algo;

namespace Altium.Tests;

public class Tests
{
    private string PathToFile = @"C:\Users\dkarelin\ForTests\testFile.txt";
    
    [Test]
    public async Task Test1()
    {
        var generator = new FileGenerator();
        await generator.GenerateFileAsync(PathToFile, 1*1024 * 1024 * 1024L, CancellationToken.None);
    }

    [Test]
    public async Task Test2()
    {
        var sorter = new SingleThreadFileSorter(10 * 1024 * 1024, 4);
        
        await sorter.SortFileAsync(PathToFile, CancellationToken.None);
    }
}