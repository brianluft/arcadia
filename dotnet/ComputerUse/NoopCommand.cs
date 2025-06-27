namespace ComputerUse;

/// <summary>
/// A no-operation command that does nothing and exits immediately.
/// Used for testing the command infrastructure.
/// </summary>
public class NoopCommand : ICommand
{
    public Task ExecuteAsync(StatusReporter statusReporter)
    {
        return Task.CompletedTask;
    }
}
