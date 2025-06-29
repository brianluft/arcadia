using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace ComputerUse;

public record ScreenshotResult(Bitmap Primary, Bitmap? Overview = null);

public class ScreenUse
{
    private const int GRID_LINE_WIDTH = 2;
    private const int CENTER_DOT_SIZE = 5;
    private const int FONT_SIZE = 9;

    private readonly SafetyManager _safetyManager;

    public ScreenUse(SafetyManager safetyManager)
    {
        _safetyManager = safetyManager;
    }

    public void TakeScreenshot(FileInfo outputFile, ZoomPath? zoomPath = null)
    {
        var screenshots = TakeScreenshots(zoomPath);

        // Save the primary (zoomed) screenshot
        screenshots.Primary.Save(outputFile.FullName, ImageFormat.Png);

        // Dispose of images
        screenshots.Primary.Dispose();
        screenshots.Overview?.Dispose();
    }

    public ScreenshotResult TakeScreenshots(ZoomPath? zoomPath = null)
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
            Bitmap primaryImage;
            Bitmap? overviewImage = null;

            if (zoomPath == null)
            {
                // No zoom path - just process the full screenshot
                using (var scaledImage = ScaleImage(screenshot, 1080))
                {
                    primaryImage = DrawGridAndCoordinates(scaledImage);
                }
            }
            else
            {
                // Zoom path specified - create both zoomed and overview images

                // Create the zoomed image with fake cursor in the center
                using (var croppedImage = CropImage(screenshot, targetRectangle, screenBounds))
                {
                    using (var scaledImage = ScaleImage(croppedImage, 1080))
                    {
                        primaryImage = DrawGridAndCoordinates(scaledImage, drawFakeCursor: true);
                    }
                }

                // Create the overview image with highlighted target rectangle
                using (var scaledFullscreen = ScaleImage(screenshot, 1080))
                {
                    overviewImage = DrawGridAndCoordinatesWithHighlight(
                        scaledFullscreen,
                        targetRectangle,
                        screenBounds
                    );
                }
            }

            return new ScreenshotResult(primaryImage, overviewImage);
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
            var cursorInfo = new NativeMethods.CURSORINFO();
            cursorInfo.cbSize = Marshal.SizeOf(cursorInfo);

            if (NativeMethods.GetCursorInfo(out cursorInfo) && cursorInfo.flags == NativeMethods.CURSOR_SHOWING)
            {
                var cursorPosition = new Point(
                    cursorInfo.ptScreenPos.x - screenBounds.X,
                    cursorInfo.ptScreenPos.y - screenBounds.Y
                );

                if (cursorInfo.hCursor != IntPtr.Zero)
                {
                    NativeMethods.DrawIcon(graphics.GetHdc(), cursorPosition.X, cursorPosition.Y, cursorInfo.hCursor);
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

    private static Bitmap DrawGridAndCoordinates(Bitmap source, bool drawFakeCursor = false)
    {
        var result = new Bitmap(source);

        // Calculate grid dimensions dynamically based on image aspect ratio
        var aspectRatio = (double)source.Width / source.Height;
        var numColumns = Coord.CalculateColumns(aspectRatio);
        var numRows = Coord.NUM_ROWS;

        // Create an off-screen bitmap for the overlay (grid and text)
        using (var overlay = new Bitmap(source.Width, source.Height))
        {
            using (var overlayGraphics = Graphics.FromImage(overlay))
            {
                // Clear overlay to black (transparent areas)
                overlayGraphics.Clear(Color.Black);

                var cellWidth = (float)source.Width / numColumns;
                var cellHeight = (float)source.Height / numRows;

                // Draw grid lines in white on the overlay
                using (var pen = new Pen(Color.White, GRID_LINE_WIDTH))
                {
                    pen.DashStyle = DashStyle.Solid;

                    // Draw vertical lines
                    for (int col = 1; col < numColumns; col++)
                    {
                        var x = col * cellWidth;
                        overlayGraphics.DrawLine(pen, x, 0, x, source.Height);
                    }

                    // Draw horizontal lines
                    for (int row = 1; row < numRows; row++)
                    {
                        var y = row * cellHeight;
                        overlayGraphics.DrawLine(pen, 0, y, source.Width, y);
                    }
                }

                // Draw center dots and coordinate labels in white on the overlay
                using (var font = new Font("Consolas", FONT_SIZE, FontStyle.Bold))
                using (var textBrush = new SolidBrush(Color.White))
                using (var dotBrush = new SolidBrush(Color.White))
                {
                    for (int row = 0; row < numRows; row++)
                    {
                        for (int col = 0; col < numColumns; col++)
                        {
                            var coord = new Coord(row, col);

                            // Calculate center of cell
                            var centerX = (col + 0.5f) * cellWidth;
                            var centerY = (row + 0.5f) * cellHeight;

                            // Draw 3x3 center dot
                            var dotRect = new Rectangle(
                                (int)(centerX - CENTER_DOT_SIZE / 2f),
                                (int)(centerY - CENTER_DOT_SIZE / 2f),
                                CENTER_DOT_SIZE,
                                CENTER_DOT_SIZE
                            );
                            overlayGraphics.FillRectangle(dotBrush, dotRect);

                            // Draw coordinate label to the right of center, properly vertically centered
                            var labelText = coord.ToString();
                            var textSize = overlayGraphics.MeasureString(labelText, font);
                            var labelX = centerX + CENTER_DOT_SIZE / 2f + 2;
                            var labelY = centerY - textSize.Height / 2f;
                            overlayGraphics.DrawString(labelText, font, textBrush, labelX, labelY);
                        }
                    }
                }

                // Draw fake mouse cursor in the center of the image if requested
                if (drawFakeCursor)
                {
                    DrawFakeMouseCursor(overlayGraphics, source.Width / 2, source.Height / 2);
                }
            }

            // Apply the inversion magic: wherever the overlay has white pixels,
            // invert the corresponding pixels in the result
            ApplyInversionMask(result, overlay);
        }

        return result;
    }

    private static void DrawFakeMouseCursor(Graphics graphics, int centerX, int centerY)
    {
        // Draw a simple arrow-shaped cursor pointing to the center
        // Make it large enough to be visible but not too intrusive
        const int cursorSize = 20;

        using (var pen = new Pen(Color.White, 3))
        using (var brush = new SolidBrush(Color.White))
        {
            // Draw arrow shape pointing to center
            Point[] arrowPoints = new Point[]
            {
                new Point(centerX - cursorSize / 2, centerY - cursorSize / 2), // Top left
                new Point(centerX - cursorSize / 4, centerY - cursorSize / 2), // Top middle
                new Point(centerX, centerY - cursorSize / 4), // Right middle
                new Point(centerX + cursorSize / 4, centerY), // Bottom right
                new Point(centerX, centerY + cursorSize / 4), // Bottom middle
                new Point(centerX - cursorSize / 4, centerY), // Left middle
                new Point(centerX - cursorSize / 2, centerY - cursorSize / 4), // Left top
            };

            graphics.FillPolygon(brush, arrowPoints);
            graphics.DrawPolygon(pen, arrowPoints);
        }
    }

    private static Bitmap DrawGridAndCoordinatesWithHighlight(
        Bitmap source,
        Rectangle targetRectangle,
        Rectangle screenBounds
    )
    {
        // First draw the regular grid and coordinates
        var result = DrawGridAndCoordinates(source);

        // Calculate the target rectangle relative to the scaled image
        var scaleFactorX = (double)source.Width / screenBounds.Width;
        var scaleFactorY = (double)source.Height / screenBounds.Height;

        var scaledTargetRect = new Rectangle(
            (int)((targetRectangle.X - screenBounds.X) * scaleFactorX),
            (int)((targetRectangle.Y - screenBounds.Y) * scaleFactorY),
            (int)(targetRectangle.Width * scaleFactorX),
            (int)(targetRectangle.Height * scaleFactorY)
        );

        // Draw highlight on the result
        using (var graphics = Graphics.FromImage(result))
        {
            // Draw thick magenta border
            using (var borderPen = new Pen(Color.Magenta, 6))
            {
                graphics.DrawRectangle(borderPen, scaledTargetRect);
            }

            // Add magenta tint to the interior
            using (var tintBrush = new SolidBrush(Color.FromArgb(64, Color.Magenta))) // Semi-transparent magenta
            {
                graphics.FillRectangle(tintBrush, scaledTargetRect);
            }
        }

        return result;
    }

    private static void ApplyInversionMask(Bitmap result, Bitmap overlay)
    {
        // Use unsafe code for performance when processing pixels
        var resultData = result.LockBits(
            new Rectangle(0, 0, result.Width, result.Height),
            ImageLockMode.ReadWrite,
            PixelFormat.Format32bppArgb
        );

        var overlayData = overlay.LockBits(
            new Rectangle(0, 0, overlay.Width, overlay.Height),
            ImageLockMode.ReadOnly,
            PixelFormat.Format32bppArgb
        );

        try
        {
            unsafe
            {
                byte* resultPtr = (byte*)resultData.Scan0;
                byte* overlayPtr = (byte*)overlayData.Scan0;

                int bytesPerPixel = 4; // ARGB = 4 bytes per pixel
                int stride = resultData.Stride;

                for (int y = 0; y < result.Height; y++)
                {
                    for (int x = 0; x < result.Width; x++)
                    {
                        int offset = y * stride + x * bytesPerPixel;

                        // Get overlay pixel (BGRA format)
                        byte overlayB = overlayPtr[offset];
                        byte overlayG = overlayPtr[offset + 1];
                        byte overlayR = overlayPtr[offset + 2];

                        // Check if overlay pixel is white (or close to white)
                        // White pixels in overlay indicate where to apply inversion
                        if (overlayR > 128 && overlayG > 128 && overlayB > 128)
                        {
                            // Invert the corresponding pixel in the result
                            byte invertedB = (byte)(255 - resultPtr[offset]);
                            byte invertedG = (byte)(255 - resultPtr[offset + 1]);
                            byte invertedR = (byte)(255 - resultPtr[offset + 2]);

                            // Add green tint to cope with middle-gray inversion issues
                            // Boost the green component by 64 to ensure visibility
                            invertedG = (byte)Math.Min(255, invertedG + 64);

                            resultPtr[offset] = invertedB; // Blue
                            resultPtr[offset + 1] = invertedG; // Green
                            resultPtr[offset + 2] = invertedR; // Red
                            // Alpha channel (offset + 3) remains unchanged
                        }
                    }
                }
            }
        }
        finally
        {
            result.UnlockBits(resultData);
            overlay.UnlockBits(overlayData);
        }
    }
}
