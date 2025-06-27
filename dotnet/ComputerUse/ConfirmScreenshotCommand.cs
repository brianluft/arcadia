using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

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

        public Task ExecuteAsync(StatusReporter statusReporter)
        {
            try
            {
                var rectangle = new Rectangle(X, Y, Width, Height);
                _safetyManager.ConfirmScreenshot(rectangle);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error during screenshot confirmation: {ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                throw;
            }

            return Task.CompletedTask;
        }
    }
}
