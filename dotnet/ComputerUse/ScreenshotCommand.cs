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

    public Task ExecuteAsync(StatusReporter statusReporter)
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

        _screenUse.TakeScreenshot(outputFileInfo, zoomPath);

        return Task.CompletedTask;
    }
}
