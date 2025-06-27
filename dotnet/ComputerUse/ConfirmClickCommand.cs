using System;
using System.Drawing;
using System.Threading.Tasks;

namespace ComputerUse
{
    public class ConfirmClickCommand : ICommand
    {
        private readonly SafetyManager _safetyManager;
        public int X { get; set; }
        public int Y { get; set; }

        public ConfirmClickCommand(SafetyManager safetyManager)
        {
            _safetyManager = safetyManager;
        }

        public void Execute(StatusReporter statusReporter)
        {
            try
            {
                var point = new Point(X, Y);

                statusReporter.Report($"Confirming click at position: X={X}, Y={Y}");

                _safetyManager.ConfirmClick(point);

                statusReporter.Report("Click confirmed successfully");

                // Small delay to show the completion message
                Thread.Sleep(1000);
            }
            catch (OperationCanceledException)
            {
                statusReporter.Report("Click was canceled by user");
                throw;
            }
            catch (Exception ex)
            {
                statusReporter.Report($"Error during click confirmation: {ex.Message}");
                throw;
            }
        }
    }
}
