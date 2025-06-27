using System.Diagnostics;

namespace ComputerUse;

public partial class MainForm : Form
{
    private readonly StatusReporter _statusReporter;
    private readonly TextBox _statusTextBox;
    private ICommand? _command;

    public MainForm(StatusReporter statusReporter)
    {
        _statusReporter = statusReporter;

        // Subscribe to status updates
        _statusReporter.StatusUpdate += OnStatusUpdate;

        // Get DPI scaling factor
        using var g = CreateGraphics();
        var dpiScaling = g.DpiX / 96.0f;

        // Create controls
        var titleLabel = new Label
        {
            Text = "Your computer is being controlled by AI.",
            AutoSize = true,
            Location = new Point(10, 10),
        };

        _statusTextBox = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Width = (int)(500 * dpiScaling),
            Height = (int)(200 * dpiScaling),
            Location = new Point(10, titleLabel.Bottom + 10),
        };

        var stopLinkLabel = new LinkLabel
        {
            Text = "Stop",
            AutoSize = true,
            Location = new Point(10, _statusTextBox.Bottom + 10),
        };
        stopLinkLabel.LinkClicked += (_, _) => Process.GetCurrentProcess().Kill();

        // Configure form
        Text = "Arcadia Computer Use";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;

        // Calculate form size
        var formWidth = _statusTextBox.Right + 20;
        var formHeight = stopLinkLabel.Bottom + 20;
        Size = new Size(formWidth, formHeight);

        // Position in lower right corner, inset by 5% of workspace
        var workingArea = Screen.PrimaryScreen?.WorkingArea ?? Screen.AllScreens[0].WorkingArea;
        var insetX = (int)(workingArea.Width * 0.05);
        var insetY = (int)(workingArea.Height * 0.05);
        Location = new Point(workingArea.Right - Width - insetX, workingArea.Bottom - Height - insetY);

        // Add controls to form
        Controls.AddRange(new Control[] { titleLabel, _statusTextBox, stopLinkLabel });
    }

    /// <summary>
    /// Sets the command to be executed by this form.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    public void SetCommand(ICommand command)
    {
        _command = command;
    }

    protected override async void SetVisibleCore(bool value)
    {
        base.SetVisibleCore(value);

        if (value && _command != null)
        {
            // Execute the command when the form becomes visible
            try
            {
                await _command.ExecuteAsync(_statusReporter);
                // After command execution, close the form
                await Task.Delay(1000); // Give user time to see final status
                Close();
            }
            catch (Exception ex)
            {
                _statusReporter.Report($"Command execution failed: {ex.Message}");
                await Task.Delay(2000); // Give user time to see error
                Close();
            }
        }
    }

    private void OnStatusUpdate(object? sender, StatusUpdateEventArgs e)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => OnStatusUpdate(sender, e));
            return;
        }

        _statusTextBox.AppendText($"{DateTime.Now:HH:mm:ss} - {e.Message}\r\n");
        _statusTextBox.SelectionStart = _statusTextBox.Text.Length;
        _statusTextBox.ScrollToCaret();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _statusReporter.StatusUpdate -= OnStatusUpdate;
        }
        base.Dispose(disposing);
    }
}
