using System.Diagnostics;

namespace ComputerUse;

public partial class MainForm : Form
{
    private readonly StatusReporter _statusReporter;
    private readonly TextBox _statusTextBox;
    private ICommand? _command;
    private bool _firstShow = true;

    public MainForm(StatusReporter statusReporter)
    {
        _statusReporter = statusReporter;

        // Subscribe to status updates
        _statusReporter.StatusUpdate += OnStatusUpdate;

        // Get DPI scaling factor
        using var g = CreateGraphics();
        var dpiScaling = g.DpiX / 96.0f;

        // Create main layout panel
        var tableLayoutPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding((int)(10 * dpiScaling)),
            AutoSize = true,
        };

        // Configure row styles
        tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Title label
        tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Status textbox
        tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Stop link

        // Configure column style
        tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        // Create controls
        var titleLabel = new Label
        {
            Text = "Your computer is being controlled by AI.",
            AutoSize = true,
            Margin = new Padding(0, 0, 0, (int)(10 * dpiScaling)),
        };

        _statusTextBox = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Size = new Size((int)(300 * dpiScaling), (int)(100 * dpiScaling)),
            Margin = new Padding(0, 0, 0, (int)(10 * dpiScaling)),
        };

        var stopButton = new Button
        {
            Text = "Stop",
            AutoSize = true,
            Margin = new Padding(0),
            Padding = new Padding(
                (int)(10 * dpiScaling),
                (int)(4 * dpiScaling),
                (int)(10 * dpiScaling),
                (int)(4 * dpiScaling)
            ),
        };
        stopButton.Click += (_, _) => Process.GetCurrentProcess().Kill();

        // Add controls to layout panel
        tableLayoutPanel.Controls.Add(titleLabel, 0, 0);
        tableLayoutPanel.Controls.Add(_statusTextBox, 0, 1);
        tableLayoutPanel.Controls.Add(stopButton, 0, 2);

        // Configure form
        Text = "Arcadia Computer Use";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;

        // Add layout panel to form
        Controls.Add(tableLayoutPanel);

        // Position in lower right corner after form is laid out
        Load += (_, _) => PositionForm();
    }

    private void PositionForm()
    {
        // Get DPI scaling factor
        using var g = CreateGraphics();
        var dpiScaling = g.DpiX / 96.0f;

        // Position in lower right corner, inset slightly
        var workingArea = Screen.PrimaryScreen?.WorkingArea ?? Screen.AllScreens[0].WorkingArea;
        var insetX = (int)(workingArea.Width * 0.01);
        var insetY = (int)(workingArea.Height * 0.01);
        Location = new Point(workingArea.Right - Width - insetX, workingArea.Bottom - Height - insetY);
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

        if (value && _command != null && _firstShow)
        {
            // Execute the command when the form first becomes visible
            _firstShow = false;
            try
            {
                _command.Execute(_statusReporter);
                // After command execution, close the form
                await Task.Delay(1000); // Give user time to see final status
                Close();
            }
            catch (Exception ex)
            {
                _statusReporter.Report($"Command execution failed: {ex.Message}");
                MessageBox.Show(
                    $"Command execution failed: {ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
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

        _statusTextBox.Text = e.Message;
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
