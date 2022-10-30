namespace Altium.Algo;

internal class SortableString : IComparable<SortableString>
{
    public SortableString(string value, int fileNumber=0)
    {
        Value = value;
        FileNumber = fileNumber;
        for (int i=0; i<value.Length; i++)
            if (value[i] == ':')
            {
                DelimerPos = i;
                break;
            }
    }
    public string Value { get; }
    
    public int FileNumber { get; }

    private int DelimerPos;

    public int CompareTo(SortableString? other)
    {
        var r = Value.Split(":");
        var r2 = other.Value.Split(":");
        var strCmp = r[1].CompareTo(r2[1]);
        if (strCmp != 0)
            return strCmp;
        return int.Parse(r[0]).CompareTo(int.Parse(r2[0]));
    }
    
    // public int CompareTo(SortableString? other)
    // {
    //     var i = DelimerPos;
    //     var j = other!.DelimerPos;
    //     while (i < Value.Length && j < other.Value.Length && Value[i] == other.Value[j])
    //     {
    //         i++;
    //         j++;
    //     }
    //
    //     if (i < Value.Length && j < other.Value.Length)
    //         return Value[i].CompareTo(other.Value[j]);
    //
    //     if (i == Value.Length && j == other.Value.Length)
    //     {
    //         var n1 = 0;
    //         for (int k = 0; k < DelimerPos; k++)
    //             n1 = n1 * 10 + (Value[k] - '0');
    //         var n2 = 0;
    //         for (int k = 0; k < other.DelimerPos; k++)
    //             n2 = n2 * 10 + (other.Value[k] - '0');
    //         return n1.CompareTo(n2);
    //     }
    //
    //     if (i == Value.Length)
    //         return -1;
    //     return 1;
    // }
}