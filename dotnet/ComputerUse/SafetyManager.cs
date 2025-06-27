using System;
using System.Drawing;
using System.Windows.Forms;

namespace ComputerUse
{
    public class SafetyManager
    {
        public void ConfirmScreenshot(Rectangle rectangle)
        {
            SafetyRectangleForm? rectangleForm = null;
            try
            {
                // Check if this is a full-screen screenshot
                var primaryScreen = Screen.PrimaryScreen;
                bool isFullScreen = rectangle == primaryScreen?.Bounds;

                // Show rectangle form only if not full-screen
                if (!isFullScreen)
                {
                    rectangleForm = new SafetyRectangleForm(rectangle);
                    rectangleForm.Show();
                }

                // Create prompt message
                string message = isFullScreen
                    ? "AI is about to take a full-screen screenshot."
                    : $"AI is about to take a screenshot of region:\nX: {rectangle.X}, Y: {rectangle.Y}\nWidth: {rectangle.Width}, Height: {rectangle.Height}";

                // Show prompt form with 2 second countdown
                using (var promptForm = new SafetyPromptForm(message, 2))
                {
                    // Position prompt near target rectangle but on-screen
                    if (!isFullScreen)
                    {
                        PositionFormNearRectangle(promptForm, rectangle);
                    }

                    var result = promptForm.ShowDialog();
                    if (result != DialogResult.OK)
                    {
                        throw new OperationCanceledException("Screenshot operation was canceled by user.");
                    }
                }
            }
            finally
            {
                rectangleForm?.Close();
                rectangleForm?.Dispose();
            }
        }

        public void ConfirmClick(Point point)
        {
            SafetyCrosshairForm? crosshairForm = null;
            try
            {
                // Show crosshair form at target point
                crosshairForm = new SafetyCrosshairForm(point);
                crosshairForm.Show();

                // Create prompt message
                string message = $"AI is about to click at position:\nX: {point.X}, Y: {point.Y}";

                // Show prompt form with 5 second countdown
                using (var promptForm = new SafetyPromptForm(message, 5))
                {
                    // Position prompt near target point but on-screen
                    PositionFormNearPoint(promptForm, point);

                    var result = promptForm.ShowDialog();
                    if (result != DialogResult.OK)
                    {
                        throw new OperationCanceledException("Click operation was canceled by user.");
                    }
                }
            }
            finally
            {
                crosshairForm?.Close();
                crosshairForm?.Dispose();
            }
        }

        public void ConfirmType(string text)
        {
            // Create prompt message
            string message = $"AI is about to type the following text:\n\"{text}\"";

            // Show prompt form with 5 second countdown, centered on screen
            using (var promptForm = new SafetyPromptForm(message, 5))
            {
                var result = promptForm.ShowDialog();
                if (result != DialogResult.OK)
                {
                    throw new OperationCanceledException("Type operation was canceled by user.");
                }
            }
        }

        private void PositionFormNearRectangle(Form form, Rectangle targetRect)
        {
            var screen = Screen.FromRectangle(targetRect);
            var workingArea = screen.WorkingArea;

            // Try positioning to the right of the rectangle
            int preferredX = targetRect.Right + 10;
            int preferredY = targetRect.Top;

            // Ensure form stays within screen bounds
            if (preferredX + form.Width > workingArea.Right)
            {
                // Try left side
                preferredX = targetRect.Left - form.Width - 10;
                if (preferredX < workingArea.Left)
                {
                    // Try below
                    preferredX = targetRect.Left;
                    preferredY = targetRect.Bottom + 10;

                    if (preferredY + form.Height > workingArea.Bottom)
                    {
                        // Try above
                        preferredY = targetRect.Top - form.Height - 10;
                        if (preferredY < workingArea.Top)
                        {
                            // Default to center of working area
                            preferredX = workingArea.Left + (workingArea.Width - form.Width) / 2;
                            preferredY = workingArea.Top + (workingArea.Height - form.Height) / 2;
                        }
                    }
                }
            }

            // Clamp to working area bounds
            preferredX = Math.Max(workingArea.Left, Math.Min(preferredX, workingArea.Right - form.Width));
            preferredY = Math.Max(workingArea.Top, Math.Min(preferredY, workingArea.Bottom - form.Height));

            form.StartPosition = FormStartPosition.Manual;
            form.Location = new Point(preferredX, preferredY);
        }

        private void PositionFormNearPoint(Form form, Point targetPoint)
        {
            var screen = Screen.FromPoint(targetPoint);
            var workingArea = screen.WorkingArea;

            // Try positioning to the right of the point
            int preferredX = targetPoint.X + 20;
            int preferredY = targetPoint.Y - form.Height / 2;

            // Ensure form stays within screen bounds
            if (preferredX + form.Width > workingArea.Right)
            {
                // Try left side
                preferredX = targetPoint.X - form.Width - 20;
                if (preferredX < workingArea.Left)
                {
                    // Try below
                    preferredX = targetPoint.X - form.Width / 2;
                    preferredY = targetPoint.Y + 20;

                    if (preferredY + form.Height > workingArea.Bottom)
                    {
                        // Try above
                        preferredY = targetPoint.Y - form.Height - 20;
                        if (preferredY < workingArea.Top)
                        {
                            // Default to center of working area
                            preferredX = workingArea.Left + (workingArea.Width - form.Width) / 2;
                            preferredY = workingArea.Top + (workingArea.Height - form.Height) / 2;
                        }
                    }
                }
            }

            // Clamp to working area bounds
            preferredX = Math.Max(workingArea.Left, Math.Min(preferredX, workingArea.Right - form.Width));
            preferredY = Math.Max(workingArea.Top, Math.Min(preferredY, workingArea.Bottom - form.Height));

            form.StartPosition = FormStartPosition.Manual;
            form.Location = new Point(preferredX, preferredY);
        }
    }
}
