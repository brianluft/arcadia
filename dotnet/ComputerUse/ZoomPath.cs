using System.Drawing;

namespace ComputerUse;

public readonly record struct ZoomPath(List<Coord> Coords)
{
    public Rectangle GetRectangle(Size screenSize)
    {
        var rectangle = new Rectangle(0, 0, screenSize.Width, screenSize.Height);

        // Iteratively zoom in.
        foreach (var coord in Coords)
        {
            // Calculate grid dimensions based on current rectangle's aspect ratio
            var aspectRatio = (double)rectangle.Width / rectangle.Height;
            var numColumns = Coord.CalculateColumns(aspectRatio);
            var numRows = Coord.NUM_ROWS;

            var x = rectangle.X + coord.ColumnIndex * rectangle.Width / numColumns;
            var y = rectangle.Y + coord.RowIndex * rectangle.Height / numRows;
            var width = rectangle.Width / numColumns;
            var height = rectangle.Height / numRows;
            rectangle = new Rectangle(x, y, width, height);
        }

        return rectangle;
    }
}
