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
        bool win = (keys & Keys.LWin) == Keys.LWin || (keys & Keys.RWin) == Keys.RWin;

        // Extract the base key by removing modifier flags
        // Use KeyCode property which returns the key code without modifiers
        Keys baseKey = keys & Keys.KeyCode;

        // Build description
        var parts = new List<string>();
        if (win)
            parts.Add("Win");
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
            _ => key.ToString(),
        };
    }

    private void SendKeysCombination(Keys keys)
    {
        // Extract modifiers
        bool ctrl = (keys & Keys.Control) == Keys.Control;
        bool alt = (keys & Keys.Alt) == Keys.Alt;
        bool shift = (keys & Keys.Shift) == Keys.Shift;
        bool win = (keys & Keys.LWin) == Keys.LWin;

        // Remove modifiers to get the base key
        Keys baseKey = keys & ~Keys.Modifiers & ~Keys.LWin;

        // Handle Windows key combinations using P/Invoke
        if (win)
        {
            SendWindowsKeyCombo(baseKey, ctrl, alt, shift);
        }
        else
        {
            // Use SendKeys for non-Windows key combinations
            string sendKeysString = ConvertKeysToSendKeysString(keys);
            SendKeys.SendWait(sendKeysString);
        }
    }

    private void SendWindowsKeyCombo(Keys baseKey, bool ctrl, bool alt, bool shift)
    {
        // Press modifiers first
        if (shift)
            NativeMethods.keybd_event(0x10, 0, 0, UIntPtr.Zero); // VK_SHIFT
        if (ctrl)
            NativeMethods.keybd_event(0x11, 0, 0, UIntPtr.Zero); // VK_CONTROL
        if (alt)
            NativeMethods.keybd_event(0x12, 0, 0, UIntPtr.Zero); // VK_MENU

        // Press Windows key
        NativeMethods.keybd_event(0x5B, 0, 0, UIntPtr.Zero); // VK_LWIN

        // Press the base key
        byte vkCode = GetVirtualKeyCode(baseKey);
        if (vkCode != 0)
        {
            NativeMethods.keybd_event(vkCode, 0, 0, UIntPtr.Zero);
            NativeMethods.keybd_event(vkCode, 0, NativeMethods.KEYEVENTF_KEYUP, UIntPtr.Zero);
        }

        // Release Windows key
        NativeMethods.keybd_event(0x5B, 0, NativeMethods.KEYEVENTF_KEYUP, UIntPtr.Zero);

        // Release modifiers in reverse order
        if (alt)
            NativeMethods.keybd_event(0x12, 0, NativeMethods.KEYEVENTF_KEYUP, UIntPtr.Zero);
        if (ctrl)
            NativeMethods.keybd_event(0x11, 0, NativeMethods.KEYEVENTF_KEYUP, UIntPtr.Zero);
        if (shift)
            NativeMethods.keybd_event(0x10, 0, NativeMethods.KEYEVENTF_KEYUP, UIntPtr.Zero);
    }

    private byte GetVirtualKeyCode(Keys key)
    {
        return key switch
        {
            Keys.A => 0x41,
            Keys.B => 0x42,
            Keys.C => 0x43,
            Keys.D => 0x44,
            Keys.E => 0x45,
            Keys.F => 0x46,
            Keys.G => 0x47,
            Keys.H => 0x48,
            Keys.I => 0x49,
            Keys.J => 0x4A,
            Keys.K => 0x4B,
            Keys.L => 0x4C,
            Keys.M => 0x4D,
            Keys.N => 0x4E,
            Keys.O => 0x4F,
            Keys.P => 0x50,
            Keys.Q => 0x51,
            Keys.R => 0x52,
            Keys.S => 0x53,
            Keys.T => 0x54,
            Keys.U => 0x55,
            Keys.V => 0x56,
            Keys.W => 0x57,
            Keys.X => 0x58,
            Keys.Y => 0x59,
            Keys.Z => 0x5A,
            Keys.D0 => 0x30,
            Keys.D1 => 0x31,
            Keys.D2 => 0x32,
            Keys.D3 => 0x33,
            Keys.D4 => 0x34,
            Keys.D5 => 0x35,
            Keys.D6 => 0x36,
            Keys.D7 => 0x37,
            Keys.D8 => 0x38,
            Keys.D9 => 0x39,
            Keys.F1 => 0x70,
            Keys.F2 => 0x71,
            Keys.F3 => 0x72,
            Keys.F4 => 0x73,
            Keys.F5 => 0x74,
            Keys.F6 => 0x75,
            Keys.F7 => 0x76,
            Keys.F8 => 0x77,
            Keys.F9 => 0x78,
            Keys.F10 => 0x79,
            Keys.F11 => 0x7A,
            Keys.F12 => 0x7B,
            Keys.Enter => 0x0D,
            Keys.Escape => 0x1B,
            Keys.Space => 0x20,
            Keys.Tab => 0x09,
            Keys.Back => 0x08,
            Keys.Delete => 0x2E,
            Keys.Insert => 0x2D,
            Keys.Home => 0x24,
            Keys.End => 0x23,
            Keys.PageUp => 0x21,
            Keys.PageDown => 0x22,
            Keys.Up => 0x26,
            Keys.Down => 0x28,
            Keys.Left => 0x25,
            Keys.Right => 0x27,
            _ => 0,
        };
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
