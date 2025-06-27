namespace ComputerUse;

public readonly record struct Coord(int RowIndex, int ColumnIndex)
{
    public const int NUM_COLUMNS = 16;
    public const int NUM_ROWS = 9;

    public override string ToString() => $"{'A' + ColumnIndex}{RowIndex}";

    public static Coord Parse(string s)
    {
        if (s.Length != 2)
        {
            throw new ArgumentException($"Invalid coord string: {s}", nameof(s));
        }

        var columnIndex = s[0] - 'A';
        var rowIndex = int.Parse(s[1..]);
        return new Coord(rowIndex, columnIndex);
    }
}
