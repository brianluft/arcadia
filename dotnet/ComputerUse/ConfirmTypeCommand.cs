using System;
using System.Threading.Tasks;
using System.Windows.Forms;

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

        public Task ExecuteAsync(StatusReporter statusReporter)
        {
            try
            {
                _safetyManager.ConfirmType(Text);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error during type confirmation: {ex.Message}",
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
