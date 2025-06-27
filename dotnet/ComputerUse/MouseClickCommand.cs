namespace ComputerUse;

public class MouseClickCommand : ICommand
{
    private readonly MouseUse _mouseUse;

    public string? ZoomPathString { get; set; }
    public string? Button { get; set; }
    public bool Double { get; set; }

    public MouseClickCommand(MouseUse mouseUse)
    {
        _mouseUse = mouseUse;
    }

    public Task ExecuteAsync(StatusReporter statusReporter)
    {
        if (string.IsNullOrEmpty(ZoomPathString))
            throw new InvalidOperationException("ZoomPath is required");

        if (string.IsNullOrEmpty(Button))
            throw new InvalidOperationException("Button is required");

        // Parse zoom path
        var coordStrings = ZoomPathString.Split(',', StringSplitOptions.RemoveEmptyEntries);
        var coords = new List<Coord>();

        foreach (var coordStr in coordStrings)
        {
            coords.Add(Coord.Parse(coordStr.Trim()));
        }

        var zoomPath = new ZoomPath(coords);

        // Parse button
        MouseButtons mouseButton = Button.ToLowerInvariant() switch
        {
            "left" => MouseButtons.Left,
            "right" => MouseButtons.Right,
            "middle" => MouseButtons.Middle,
            _ => throw new ArgumentException($"Invalid button: {Button}. Supported values: left, right, middle"),
        };

        // Perform the click
        _mouseUse.Click(zoomPath, mouseButton, Double);

        return Task.CompletedTask;
    }
}
