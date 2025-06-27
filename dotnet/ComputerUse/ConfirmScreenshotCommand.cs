using System;
using System.Drawing;
using System.Threading.Tasks;

namespace ComputerUse
{
    public class ConfirmScreenshotCommand : ICommand
    {
        private readonly SafetyManager _safetyManager;
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public ConfirmScreenshotCommand(SafetyManager safetyManager)
        {
            _safetyManager = safetyManager;
        }

        public async Task ExecuteAsync(StatusReporter statusReporter)
        {
            try
            {
                var rectangle = new Rectangle(X, Y, Width, Height);

                statusReporter.Report(
                    $"Confirming screenshot for region: X={X}, Y={Y}, Width={Width}, Height={Height}"
                );

                _safetyManager.ConfirmScreenshot(rectangle);

                statusReporter.Report("Screenshot confirmed successfully");

                // Small delay to show the completion message
                await Task.Delay(1000);
            }
            catch (OperationCanceledException)
            {
                statusReporter.Report("Screenshot was canceled by user");
                throw;
            }
            catch (Exception ex)
            {
                statusReporter.Report($"Error during screenshot confirmation: {ex.Message}");
                throw;
            }
        }
    }
}
