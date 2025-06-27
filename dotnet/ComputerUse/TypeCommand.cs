using System;
using System.Threading.Tasks;

namespace ComputerUse;

public class TypeCommand : ICommand
{
    private readonly KeyboardUse _keyboardUse;

    public string? Text { get; set; }

    public TypeCommand(KeyboardUse keyboardUse)
    {
        _keyboardUse = keyboardUse;
    }

    public void Execute(StatusReporter statusReporter)
    {
        if (string.IsNullOrEmpty(Text))
        {
            throw new InvalidOperationException("Text is required");
        }

        statusReporter.Report($"Typing text: {Text}");
        _keyboardUse.Type(Text);
        statusReporter.Report("Text typing completed");
    }
}
