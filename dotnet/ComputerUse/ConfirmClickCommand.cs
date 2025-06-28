using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ComputerUse;

public class ConfirmClickCommand : ICommand
{
    private readonly SafetyManager _safetyManager;
    public int X { get; set; }
    public int Y { get; set; }

    public ConfirmClickCommand(SafetyManager safetyManager)
    {
        _safetyManager = safetyManager;
    }

    public Task ExecuteAsync()
    {
        try
        {
            var point = new Point(X, Y);
            _safetyManager.ConfirmClick(point);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Error during click confirmation: {ex.Message}",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
            throw;
        }

        return Task.CompletedTask;
    }
}
