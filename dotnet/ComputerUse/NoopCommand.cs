namespace ComputerUse;

/// <summary>
/// A no-operation command that does nothing and exits immediately.
/// Used for testing the command infrastructure.
/// </summary>
public class NoopCommand : ICommand
{
    public async Task ExecuteAsync(StatusReporter statusReporter)
    {
        statusReporter.Report("Executing noop command...");
        await Task.Delay(100); // Brief delay to show the status message
        statusReporter.Report("Noop command completed.");
    }
}
