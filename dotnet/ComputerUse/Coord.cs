namespace ComputerUse;

public readonly record struct Coord(int RowIndex, int ColumnIndex)
{
    public const int NUM_ROWS = 4;

    public override string ToString() => $"{(char)('A' + ColumnIndex)}{RowIndex}";

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

    /// <summary>
    /// Calculate the number of columns based on the aspect ratio of the image.
    /// Grid height is constant (NUM_ROWS), width varies based on aspect ratio.
    /// </summary>
    /// <param name="aspectRatio">Width / Height of the image</param>
    /// <returns>Number of columns (rounded to nearest integer)</returns>
    public static int CalculateColumns(double aspectRatio)
    {
        return (int)Math.Round(NUM_ROWS * aspectRatio);
    }
}
