using System;
using System.Threading.Tasks;

namespace ComputerUse
{
    public class ConfirmTypeCommand : ICommand
    {
        private readonly SafetyManager _safetyManager;
        public string Text { get; set; } = string.Empty;

        public ConfirmTypeCommand(SafetyManager safetyManager)
        {
            _safetyManager = safetyManager;
        }

        public async Task ExecuteAsync(StatusReporter statusReporter)
        {
            try
            {
                statusReporter.Report($"Confirming type operation for text: \"{Text}\"");

                _safetyManager.ConfirmType(Text);

                statusReporter.Report("Type operation confirmed successfully");

                // Small delay to show the completion message
                await Task.Delay(1000);
            }
            catch (OperationCanceledException)
            {
                statusReporter.Report("Type operation was canceled by user");
                throw;
            }
            catch (Exception ex)
            {
                statusReporter.Report($"Error during type confirmation: {ex.Message}");
                throw;
            }
        }
    }
}
