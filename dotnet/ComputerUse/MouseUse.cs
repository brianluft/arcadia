using System.Drawing;

namespace ComputerUse;

public class MouseUse
{
    private readonly SafetyManager _safetyManager;

    public MouseUse(SafetyManager safetyManager)
    {
        _safetyManager = safetyManager;
    }

    public void Click(ZoomPath zoomPath, MouseButtons button, bool doubleClick)
    {
        var primaryScreen = Screen.PrimaryScreen ?? throw new InvalidOperationException("No primary screen found");
        var screenBounds = primaryScreen.Bounds;

        // Calculate the target rectangle from the zoom path
        var targetRectangle = zoomPath.GetRectangle(screenBounds.Size);

        // Adjust for screen offset
        targetRectangle.Offset(screenBounds.X, screenBounds.Y);

        // Calculate center point of the target rectangle
        var centerPoint = new Point(
            targetRectangle.X + targetRectangle.Width / 2,
            targetRectangle.Y + targetRectangle.Height / 2
        );

        // Confirm with user
        _safetyManager.ConfirmClick(centerPoint);

        // Perform click while hiding forms
        FormHider.Do(() =>
        {
            // Move cursor to the center point
            NativeMethods.SetCursorPos(centerPoint.X, centerPoint.Y);

            // Small delay to ensure cursor position is set
            Thread.Sleep(10);

            // Perform the click(s)
            if (doubleClick)
            {
                PerformSingleClick(button);
                Thread.Sleep(50); // Standard double-click interval
                PerformSingleClick(button);
            }
            else
            {
                PerformSingleClick(button);
            }
        });
    }

    private static void PerformSingleClick(MouseButtons button)
    {
        uint downFlag,
            upFlag;

        switch (button)
        {
            case MouseButtons.Left:
                downFlag = NativeMethods.MOUSEEVENTF_LEFTDOWN;
                upFlag = NativeMethods.MOUSEEVENTF_LEFTUP;
                break;
            case MouseButtons.Right:
                downFlag = NativeMethods.MOUSEEVENTF_RIGHTDOWN;
                upFlag = NativeMethods.MOUSEEVENTF_RIGHTUP;
                break;
            case MouseButtons.Middle:
                downFlag = NativeMethods.MOUSEEVENTF_MIDDLEDOWN;
                upFlag = NativeMethods.MOUSEEVENTF_MIDDLEUP;
                break;
            default:
                throw new ArgumentException($"Unsupported mouse button: {button}");
        }

        // Perform mouse down
        NativeMethods.mouse_event(downFlag, 0, 0, 0, UIntPtr.Zero);

        // Small delay between down and up
        Thread.Sleep(10);

        // Perform mouse up
        NativeMethods.mouse_event(upFlag, 0, 0, 0, UIntPtr.Zero);
    }
}
