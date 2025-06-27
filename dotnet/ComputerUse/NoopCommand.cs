namespace ComputerUse;

/// <summary>
/// A no-operation command that does nothing and exits immediately.
/// Used for testing the command infrastructure.
/// </summary>
public class NoopCommand : ICommand
{
    public void Execute(StatusReporter statusReporter)
    {
        statusReporter.Report("Executing noop command...");
        Thread.Sleep(100); // Brief delay to show the status message
        statusReporter.Report("Noop command completed.");
    }
}
