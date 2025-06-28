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

    public Task ExecuteAsync()
    {
        if (string.IsNullOrEmpty(Text))
        {
            throw new InvalidOperationException("Text is required");
        }

        _keyboardUse.Type(Text);

        return Task.CompletedTask;
    }
}
