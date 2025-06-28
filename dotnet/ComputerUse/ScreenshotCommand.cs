namespace ComputerUse;

public class ScreenshotCommand : ICommand
{
    private readonly ScreenUse _screenUse;

    public ScreenshotCommand(ScreenUse screenUse)
    {
        _screenUse = screenUse;
    }

    public string? ZoomPathString { get; set; }
    public string OutputFile { get; set; } = string.Empty;

    public Task ExecuteAsync()
    {
        if (string.IsNullOrEmpty(OutputFile))
        {
            throw new InvalidOperationException("OutputFile is required");
        }

        // Parse zoom path if provided
        ZoomPath? zoomPath = null;
        if (!string.IsNullOrEmpty(ZoomPathString))
        {
            var coordStrings = ZoomPathString.Split(',', StringSplitOptions.RemoveEmptyEntries);
            var coords = new List<Coord>();

            foreach (var coordStr in coordStrings)
            {
                coords.Add(Coord.Parse(coordStr.Trim()));
            }

            zoomPath = new ZoomPath(coords);
        }

        var outputFileInfo = new FileInfo(OutputFile);

        // Create directory if it doesn't exist
        outputFileInfo.Directory?.Create();

        // Take screenshots
        var screenshots = _screenUse.TakeScreenshots(zoomPath);

        try
        {
            // Save the primary (zoomed) screenshot
            screenshots.Primary.Save(outputFileInfo.FullName, System.Drawing.Imaging.ImageFormat.Png);

            // If there's an overview screenshot, save it too
            if (screenshots.Overview != null)
            {
                var overviewFileName =
                    Path.GetFileNameWithoutExtension(outputFileInfo.Name)
                    + "_overview"
                    + Path.GetExtension(outputFileInfo.Name);
                var overviewPath = Path.Combine(outputFileInfo.DirectoryName!, overviewFileName);
                screenshots.Overview.Save(overviewPath, System.Drawing.Imaging.ImageFormat.Png);
            }
        }
        finally
        {
            // Dispose of images
            screenshots.Primary.Dispose();
            screenshots.Overview?.Dispose();
        }

        return Task.CompletedTask;
    }
}
