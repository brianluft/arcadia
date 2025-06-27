using System;
using System.Collections.Generic;
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
        // Create a user-friendly description of the key combination
        string keyDescription = GetKeyDescription(keys);
        _safetyManager.ConfirmKeyPress(keyDescription);

        await Task.Run(() =>
        {
            FormHider.Do(() =>
            {
                SendKeysCombination(keys);
            });
        });
    }

    public async Task Press(Keys keys, string keyDescription)
    {
        // Use the provided key description for the safety confirmation
        _safetyManager.ConfirmKeyPress(keyDescription);

        await Task.Run(() =>
        {
            FormHider.Do(() =>
            {
                SendKeysCombination(keys);
            });
        });
    }

    public async Task Type(string text)
    {
        _safetyManager.ConfirmType(text);

        await Task.Run(() =>
        {
            FormHider.Do(() =>
            {
                SendKeys.SendWait(EscapeSendKeysString(text));
            });
        });
    }

    private string GetKeyDescription(Keys keys)
    {
        // Extract modifiers
        bool ctrl = (keys & Keys.Control) == Keys.Control;
        bool alt = (keys & Keys.Alt) == Keys.Alt;
        bool shift = (keys & Keys.Shift) == Keys.Shift;

        // Extract the base key by removing modifier flags
        // Use KeyCode property which returns the key code without modifiers
        Keys baseKey = keys & Keys.KeyCode;

        // Build description
        var parts = new List<string>();
        if (ctrl)
            parts.Add("Ctrl");
        if (alt)
            parts.Add("Alt");
        if (shift)
            parts.Add("Shift");

        // Only add the base key if it's not None/0
        if (baseKey != Keys.None)
        {
            string keyName = GetKeyName(baseKey);
            parts.Add(keyName);
        }

        // If we have no parts (shouldn't happen in normal use), return a default
        if (parts.Count == 0)
        {
            return "Unknown Key";
        }

        return string.Join(" + ", parts);
    }

    private string GetKeyName(Keys key)
    {
        return key switch
        {
            Keys.Enter => "Enter",
            Keys.Escape => "Escape",
            Keys.Tab => "Tab",
            Keys.Back => "Backspace",
            Keys.Delete => "Delete",
            Keys.Insert => "Insert",
            Keys.Home => "Home",
            Keys.End => "End",
            Keys.PageUp => "Page Up",
            Keys.PageDown => "Page Down",
            Keys.Up => "Up Arrow",
            Keys.Down => "Down Arrow",
            Keys.Left => "Left Arrow",
            Keys.Right => "Right Arrow",
            Keys.F1 => "F1",
            Keys.F2 => "F2",
            Keys.F3 => "F3",
            Keys.F4 => "F4",
            Keys.F5 => "F5",
            Keys.F6 => "F6",
            Keys.F7 => "F7",
            Keys.F8 => "F8",
            Keys.F9 => "F9",
            Keys.F10 => "F10",
            Keys.F11 => "F11",
            Keys.F12 => "F12",
            Keys.Space => "Space",
            Keys.LWin => "Left Windows Key",
            Keys.RWin => "Right Windows Key",
            _ => key.ToString(),
        };
    }

    private void SendKeysCombination(Keys keys)
    {
        // Use SendKeys for all key combinations
        string sendKeysString = ConvertKeysToSendKeysString(keys);
        SendKeys.SendWait(sendKeysString);
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
            Keys.LWin => "^{ESC}", // Windows key approximation using Ctrl+Esc
            Keys.RWin => "^{ESC}", // Windows key approximation using Ctrl+Esc
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
