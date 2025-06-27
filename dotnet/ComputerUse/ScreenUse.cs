using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace ComputerUse;

public class ScreenUse
{
    private const int GRID_LINE_WIDTH = 2;
    private const int CENTER_DOT_SIZE = 3;
    private const int FONT_SIZE = 12;

    private readonly SafetyManager _safetyManager;

    public ScreenUse(SafetyManager safetyManager)
    {
        _safetyManager = safetyManager;
    }

    public void TakeScreenshot(FileInfo outputFile, ZoomPath? zoomPath = null)
    {
        var primaryScreen = Screen.PrimaryScreen ?? throw new InvalidOperationException("No primary screen found");
        var screenBounds = primaryScreen.Bounds;

        // Determine target rectangle
        Rectangle targetRectangle;
        if (zoomPath == null)
        {
            targetRectangle = screenBounds;
        }
        else
        {
            targetRectangle = zoomPath.Value.GetRectangle(screenBounds.Size);
            // Adjust for screen offset
            targetRectangle.Offset(screenBounds.X, screenBounds.Y);
        }

        // Confirm with user
        _safetyManager.ConfirmScreenshot(targetRectangle);

        // Take screenshot while hiding forms
        Bitmap screenshot = null!;
        FormHider.Do(() =>
        {
            screenshot = CaptureScreenWithCursor(screenBounds);
        });

        using (screenshot)
        {
            // Crop to target rectangle
            using (var croppedImage = CropImage(screenshot, targetRectangle, screenBounds))
            {
                // Scale to 1080px height
                using (var scaledImage = ScaleImage(croppedImage, 1080))
                {
                    // Draw grid and coordinates
                    using (var finalImage = DrawGridAndCoordinates(scaledImage))
                    {
                        // Save as PNG
                        finalImage.Save(outputFile.FullName, ImageFormat.Png);
                    }
                }
            }
        }
    }

    private static Bitmap CaptureScreenWithCursor(Rectangle screenBounds)
    {
        var bitmap = new Bitmap(screenBounds.Width, screenBounds.Height);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            // Capture screen
            graphics.CopyFromScreen(screenBounds.Location, Point.Empty, screenBounds.Size);

            // Draw cursor
            var cursorInfo = new CURSORINFO();
            cursorInfo.cbSize = Marshal.SizeOf(cursorInfo);

            if (GetCursorInfo(out cursorInfo) && cursorInfo.flags == CURSOR_SHOWING)
            {
                var cursorPosition = new Point(
                    cursorInfo.ptScreenPos.x - screenBounds.X,
                    cursorInfo.ptScreenPos.y - screenBounds.Y
                );

                if (cursorInfo.hCursor != IntPtr.Zero)
                {
                    DrawIcon(graphics.GetHdc(), cursorPosition.X, cursorPosition.Y, cursorInfo.hCursor);
                    graphics.ReleaseHdc();
                }
            }
        }
        return bitmap;
    }

    private static Bitmap CropImage(Bitmap source, Rectangle targetRect, Rectangle screenBounds)
    {
        // Calculate crop rectangle relative to source image
        var cropRect = new Rectangle(
            targetRect.X - screenBounds.X,
            targetRect.Y - screenBounds.Y,
            targetRect.Width,
            targetRect.Height
        );

        // Ensure crop rectangle is within source bounds
        cropRect.Intersect(new Rectangle(0, 0, source.Width, source.Height));

        var croppedBitmap = new Bitmap(cropRect.Width, cropRect.Height);
        using (var graphics = Graphics.FromImage(croppedBitmap))
        {
            graphics.DrawImage(
                source,
                new Rectangle(0, 0, cropRect.Width, cropRect.Height),
                cropRect,
                GraphicsUnit.Pixel
            );
        }
        return croppedBitmap;
    }

    private static Bitmap ScaleImage(Bitmap source, int targetHeight)
    {
        // Calculate scaled dimensions maintaining aspect ratio
        var aspectRatio = (double)source.Width / source.Height;
        var scaledWidth = (int)(targetHeight * aspectRatio);

        var scaledBitmap = new Bitmap(scaledWidth, targetHeight);
        using (var graphics = Graphics.FromImage(scaledBitmap))
        {
            // Use high quality scaling when scaling down, nearest neighbor when scaling up
            if (targetHeight < source.Height)
            {
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            }
            else
            {
                graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                graphics.SmoothingMode = SmoothingMode.None;
                graphics.PixelOffsetMode = PixelOffsetMode.None;
            }

            graphics.DrawImage(source, 0, 0, scaledWidth, targetHeight);
        }
        return scaledBitmap;
    }

    private static Bitmap DrawGridAndCoordinates(Bitmap source)
    {
        var result = new Bitmap(source);
        using (var graphics = Graphics.FromImage(result))
        {
            var cellWidth = (float)source.Width / Coord.NUM_COLUMNS;
            var cellHeight = (float)source.Height / Coord.NUM_ROWS;

            // Draw grid lines with inverted colors
            using (var pen = new Pen(Color.White, GRID_LINE_WIDTH))
            {
                pen.DashStyle = DashStyle.Solid;

                // Draw vertical lines
                for (int col = 1; col < Coord.NUM_COLUMNS; col++)
                {
                    var x = col * cellWidth;
                    DrawInvertedLine(graphics, pen, x, 0, x, source.Height);
                }

                // Draw horizontal lines
                for (int row = 1; row < Coord.NUM_ROWS; row++)
                {
                    var y = row * cellHeight;
                    DrawInvertedLine(graphics, pen, 0, y, source.Width, y);
                }
            }

            // Draw center dots and coordinate labels
            using (var font = new Font("Arial", FONT_SIZE, FontStyle.Bold))
            using (var textBrush = new SolidBrush(Color.White))
            using (var dotBrush = new SolidBrush(Color.White))
            {
                for (int row = 0; row < Coord.NUM_ROWS; row++)
                {
                    for (int col = 0; col < Coord.NUM_COLUMNS; col++)
                    {
                        var coord = new Coord(row, col);

                        // Calculate center of cell
                        var centerX = (col + 0.5f) * cellWidth;
                        var centerY = (row + 0.5f) * cellHeight;

                        // Draw 3x3 inverted color center dot
                        var dotRect = new Rectangle(
                            (int)(centerX - CENTER_DOT_SIZE / 2f),
                            (int)(centerY - CENTER_DOT_SIZE / 2f),
                            CENTER_DOT_SIZE,
                            CENTER_DOT_SIZE
                        );
                        DrawInvertedRectangle(graphics, dotBrush, dotRect);

                        // Draw coordinate label to the right of center
                        var labelX = centerX + CENTER_DOT_SIZE / 2f + 2;
                        var labelY = centerY - FONT_SIZE / 2f;
                        DrawInvertedText(graphics, coord.ToString(), font, textBrush, labelX, labelY);
                    }
                }
            }
        }
        return result;
    }

    private static void DrawInvertedLine(Graphics graphics, Pen pen, float x1, float y1, float x2, float y2)
    {
        // This is a simplified inversion - in a real implementation you'd need to
        // sample the background colors and invert them
        graphics.DrawLine(pen, x1, y1, x2, y2);
    }

    private static void DrawInvertedRectangle(Graphics graphics, Brush brush, Rectangle rect)
    {
        // This is a simplified inversion - in a real implementation you'd need to
        // sample the background colors and invert them
        graphics.FillRectangle(brush, rect);
    }

    private static void DrawInvertedText(Graphics graphics, string text, Font font, Brush brush, float x, float y)
    {
        // This is a simplified inversion - in a real implementation you'd need to
        // sample the background colors and invert them
        graphics.DrawString(text, font, brush, x, y);
    }

    // P/Invoke declarations for cursor capture
    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct CURSORINFO
    {
        public int cbSize;
        public int flags;
        public IntPtr hCursor;
        public POINT ptScreenPos;
    }

    private const int CURSOR_SHOWING = 0x00000001;

    [DllImport("user32.dll")]
    private static extern bool GetCursorInfo(out CURSORINFO pci);

    [DllImport("user32.dll")]
    private static extern bool DrawIcon(IntPtr hDC, int X, int Y, IntPtr hIcon);
}
