namespace Altium.Algo;

/// <summary>
/// Support class for sorting string by rule from task.
/// Additionally store FileNumber for processing after priority queue enqueue
/// </summary>
internal class StringWrapperForSorting : IComparable<StringWrapperForSorting>
{
    public StringWrapperForSorting(string value, int fileNumber=0)
    {
        Value = value;
        FileNumber = fileNumber;
        for (int i=0; i<value.Length; i++)
            if (value[i] == ':')
                _delimiterPos = i;
    }
    public string Value { get; }
    public int FileNumber { get; }

    private readonly int _delimiterPos;
    
    public int CompareTo(StringWrapperForSorting? other)
    {
        var cmpResult = string.Compare(Value, _delimiterPos + 1, other.Value, other._delimiterPos + 1,
            Value.Length + other.Value.Length, StringComparison.Ordinal);
        if (cmpResult != 0)
            return cmpResult;
        var n1 = 0;
        for (int k = 0; k < _delimiterPos; k++)
            n1 = n1 * 10 + (Value[k] - '0');
        var n2 = 0;
        for (int k = 0; k < other._delimiterPos; k++)
            n2 = n2 * 10 + (other.Value[k] - '0');
        return n1.CompareTo(n2);
    }
}