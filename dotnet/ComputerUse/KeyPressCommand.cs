using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ComputerUse;

public class KeyPressCommand : ICommand
{
    private readonly KeyboardUse _keyboardUse;

    public string? Key { get; set; }
    public bool Shift { get; set; }
    public bool Ctrl { get; set; }
    public bool Alt { get; set; }
    public bool Win { get; set; }

    public KeyPressCommand(KeyboardUse keyboardUse)
    {
        _keyboardUse = keyboardUse;
    }

    public async Task ExecuteAsync(StatusReporter statusReporter)
    {
        if (string.IsNullOrEmpty(Key))
        {
            throw new InvalidOperationException("Key is required");
        }

        statusReporter.Report($"Pressing key: {Key} (Shift={Shift}, Ctrl={Ctrl}, Alt={Alt}, Win={Win})");

        // Parse the key string to Keys enum
        if (!Enum.TryParse<Keys>(Key, true, out Keys baseKey))
        {
            throw new ArgumentException($"Invalid key: {Key}");
        }

        // Apply modifiers
        Keys keys = baseKey;
        if (Shift)
            keys |= Keys.Shift;
        if (Ctrl)
            keys |= Keys.Control;
        if (Alt)
            keys |= Keys.Alt;
        if (Win)
            keys |= Keys.LWin; // Use LWin for Windows key

        await _keyboardUse.Press(keys);
        statusReporter.Report("Key press completed");
    }
}
