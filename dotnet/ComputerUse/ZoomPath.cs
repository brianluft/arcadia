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
            var x = coord.ColumnIndex * rectangle.Width / Coord.NUM_COLUMNS;
            var y = coord.RowIndex * rectangle.Height / Coord.NUM_ROWS;
            var width = rectangle.Width / Coord.NUM_COLUMNS;
            var height = rectangle.Height / Coord.NUM_ROWS;
            rectangle = new Rectangle(x, y, width, height);
        }

        return rectangle;
    }
}
