using System.Text;

namespace ComputerUse;

public class WindowInfo
{
    public IntPtr Handle { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsFocused { get; set; }
}

public class WindowWalker
{
    public WindowInfo? GetFocusedWindow()
    {
        var foregroundWindow = NativeMethods.GetForegroundWindow();
        if (foregroundWindow == IntPtr.Zero)
            return null;

        var title = GetWindowTitle(foregroundWindow);
        if (string.IsNullOrEmpty(title))
            return null;

        return new WindowInfo
        {
            Handle = foregroundWindow,
            Title = title,
            IsFocused = true,
        };
    }

    public List<WindowInfo> GetUnfocusedWindows()
    {
        var windows = new List<WindowInfo>();
        var foregroundWindow = NativeMethods.GetForegroundWindow();

        NativeMethods.EnumWindows(
            (hWnd, lParam) =>
            {
                // Skip if not visible
                if (!NativeMethods.IsWindowVisible(hWnd))
                    return true;

                // Skip if this is the foreground window
                if (hWnd == foregroundWindow)
                    return true;

                var title = GetWindowTitle(hWnd);
                if (!string.IsNullOrEmpty(title))
                {
                    windows.Add(
                        new WindowInfo
                        {
                            Handle = hWnd,
                            Title = title,
                            IsFocused = false,
                        }
                    );
                }

                return true; // Continue enumeration
            },
            IntPtr.Zero
        );

        return windows;
    }

    private static string GetWindowTitle(IntPtr hWnd)
    {
        var length = NativeMethods.GetWindowTextLength(hWnd);
        if (length == 0)
            return string.Empty;

        var builder = new StringBuilder(length + 1);
        NativeMethods.GetWindowText(hWnd, builder, builder.Capacity);
        return builder.ToString();
    }
}
