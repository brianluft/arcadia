namespace ComputerUse;

public class StatusUpdateEventArgs : EventArgs
{
    public string Message { get; }

    public StatusUpdateEventArgs(string message)
    {
        Message = message;
    }
}

public class StatusReporter
{
    public event EventHandler<StatusUpdateEventArgs>? StatusUpdate;

    public void Report(string message)
    {
        StatusUpdate?.Invoke(this, new StatusUpdateEventArgs(message));
    }
}
