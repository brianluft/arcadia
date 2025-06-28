using System;
using System.Drawing;
using System.Windows.Forms;

namespace ComputerUse;

public partial class SafetyPromptForm : Form
{
    private readonly System.Windows.Forms.Timer _timer;
    private readonly int _totalSeconds;
    private float _remainingSeconds;
    private readonly Label _messageLabel;
    private readonly ProgressBar _progressBar;
    private readonly Button _cancelButton;
    private readonly TableLayoutPanel _tableLayout;

    public SafetyPromptForm(string message, int countdownSeconds)
    {
        _totalSeconds = countdownSeconds;
        _remainingSeconds = countdownSeconds;

        // Get DPI scaling factor
        using var g = CreateGraphics();
        var dpiScaling = g.DpiX / 96.0f;

        SuspendLayout();

        // Form properties
        Text = "AI Action Confirmation";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        TopMost = true;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Size = new Size((int)(400 * dpiScaling), (int)(200 * dpiScaling));

        // Create table layout
        _tableLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding((int)(20 * dpiScaling)),
        };

        _tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _tableLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _tableLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _tableLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        // Message label
        _messageLabel = new Label
        {
            Text = message,
            AutoSize = true,
            MaximumSize = new Size((int)(350 * dpiScaling), 0),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Margin = new Padding(0, 0, 0, (int)(10 * dpiScaling)),
        };

        // Progress bar (starts full, goes to empty)
        _progressBar = new ProgressBar
        {
            Minimum = 0,
            Maximum = _totalSeconds * 10, // 10 updates per second
            Value = _totalSeconds * 10, // Start full
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Height = (int)(20 * dpiScaling),
            Margin = new Padding(0, 0, 0, (int)(10 * dpiScaling)),
        };

        // Cancel button
        _cancelButton = new Button
        {
            Text = "Cancel",
            DialogResult = DialogResult.Cancel,
            AutoSize = true,
            Anchor = AnchorStyles.Top,
            Margin = new Padding(0),
            Padding = new Padding(
                (int)(10 * dpiScaling),
                (int)(4 * dpiScaling),
                (int)(10 * dpiScaling),
                (int)(4 * dpiScaling)
            ),
        };

        // Add controls to table layout
        _tableLayout.Controls.Add(_messageLabel, 0, 0);
        _tableLayout.Controls.Add(_progressBar, 0, 1);
        _tableLayout.Controls.Add(_cancelButton, 0, 2);

        Controls.Add(_tableLayout);

        // Set cancel button as cancel button for the form
        CancelButton = _cancelButton;

        ResumeLayout(false);
        PerformLayout();

        _timer = new System.Windows.Forms.Timer
        {
            Interval = 100, // 100ms updates
        };
        _timer.Tick += Timer_Tick;
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        _timer.Start();
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        _remainingSeconds -= 0.1f;

        if (_remainingSeconds <= 0)
        {
            _timer.Stop();
            DialogResult = DialogResult.OK;
            Close();
            return;
        }

        // Update progress bar (countdown from full to empty)
        int newValue = (int)(_remainingSeconds * 10);
        if (newValue >= _progressBar.Minimum && newValue <= _progressBar.Maximum)
        {
            _progressBar.Value = newValue;
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _timer?.Stop();
        base.OnFormClosing(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer?.Dispose();
        }
        base.Dispose(disposing);
    }
}
