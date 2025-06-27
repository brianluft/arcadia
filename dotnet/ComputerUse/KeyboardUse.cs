using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ComputerUse;

public class KeyboardUse
{
    private readonly SafetyManager _safetyManager;

    public KeyboardUse(SafetyManager safetyManager)
    {
        _safetyManager = safetyManager;
    }

    public async Task Press(Keys keys)
    {
        _safetyManager.ConfirmType($"Press key: {keys}");

        await Task.Run(() =>
        {
            FormHider.Do(() =>
            {
                SendKeys.SendWait(ConvertKeysToSendKeysString(keys));
            });
        });
    }

    public async Task Type(string text)
    {
        _safetyManager.ConfirmType($"Type text: {text}");

        await Task.Run(() =>
        {
            FormHider.Do(() =>
            {
                SendKeys.SendWait(EscapeSendKeysString(text));
            });
        });
    }

    private string ConvertKeysToSendKeysString(Keys keys)
    {
        // Extract modifiers
        bool ctrl = (keys & Keys.Control) == Keys.Control;
        bool alt = (keys & Keys.Alt) == Keys.Alt;
        bool shift = (keys & Keys.Shift) == Keys.Shift;

        // Remove modifiers to get the base key
        Keys baseKey = keys & ~Keys.Modifiers;

        string keyString = baseKey switch
        {
            Keys.Enter => "{ENTER}",
            Keys.Escape => "{ESC}",
            Keys.Tab => "{TAB}",
            Keys.Back => "{BACKSPACE}",
            Keys.Delete => "{DELETE}",
            Keys.Insert => "{INSERT}",
            Keys.Home => "{HOME}",
            Keys.End => "{END}",
            Keys.PageUp => "{PGUP}",
            Keys.PageDown => "{PGDN}",
            Keys.Up => "{UP}",
            Keys.Down => "{DOWN}",
            Keys.Left => "{LEFT}",
            Keys.Right => "{RIGHT}",
            Keys.F1 => "{F1}",
            Keys.F2 => "{F2}",
            Keys.F3 => "{F3}",
            Keys.F4 => "{F4}",
            Keys.F5 => "{F5}",
            Keys.F6 => "{F6}",
            Keys.F7 => "{F7}",
            Keys.F8 => "{F8}",
            Keys.F9 => "{F9}",
            Keys.F10 => "{F10}",
            Keys.F11 => "{F11}",
            Keys.F12 => "{F12}",
            Keys.Space => " ",
            Keys.LWin or Keys.RWin => "^{ESC}", // Windows key approximation
            _ => baseKey.ToString().ToLower(),
        };

        // Apply modifiers
        string result = keyString;
        if (ctrl)
            result = "^" + result;
        if (alt)
            result = "%" + result;
        if (shift)
            result = "+" + result;

        return result;
    }

    private string EscapeSendKeysString(string text)
    {
        // Escape special characters for SendKeys
        return text.Replace("+", "{+}")
            .Replace("^", "{^}")
            .Replace("%", "{%}")
            .Replace("~", "{~}")
            .Replace("(", "{(}")
            .Replace(")", "{)}")
            .Replace("{", "{{}")
            .Replace("}", "{}}")
            .Replace("[", "{[}")
            .Replace("]", "{]}");
    }
}
