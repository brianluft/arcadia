namespace ComputerUse;

/// <summary>
/// Represents a command to be executed by the main form.
/// Each CLI command will be an implementation of this interface.
/// </summary>
public interface ICommand
{
    /// <summary>
    /// Executes the command.
    /// </summary>
    Task ExecuteAsync();
}
