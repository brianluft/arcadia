using System;
using System.Drawing;
using System.Windows.Forms;

namespace ComputerUse;

public partial class SafetyCrosshairForm : Form
{
    private readonly System.Windows.Forms.Timer _blinkTimer;
    private readonly Point _crosshairCenter;
    private readonly int _crosshairLength;
    private readonly int _crosshairThickness;
    private bool _isVisible = true;

    public SafetyCrosshairForm(Point crosshairCenter)
    {
        _crosshairCenter = crosshairCenter;

        // Calculate DPI scaling factor
        using (var g = CreateGraphics())
        {
            float dpiScaling = g.DpiX / 96f;
            _crosshairLength = (int)(64 * dpiScaling);
            _crosshairThickness = (int)(3 * dpiScaling);
        }

        InitializeComponent();

        _blinkTimer = new System.Windows.Forms.Timer
        {
            Interval = 250, // 250ms blink interval
        };
        _blinkTimer.Tick += BlinkTimer_Tick;
        _blinkTimer.Start();
    }

    private void InitializeComponent()
    {
        SuspendLayout();

        // Form properties for transparent overlay
        FormBorderStyle = FormBorderStyle.None;
        WindowState = FormWindowState.Normal;
        TopMost = true;
        ShowInTaskbar = false;
        BackColor = Color.Lime; // Will be made transparent
        TransparencyKey = Color.Lime;

        // Set form size and position to cover crosshair area
        int formSize = _crosshairLength + _crosshairThickness;
        Size = new Size(formSize, formSize);
        Location = new Point(_crosshairCenter.X - formSize / 2, _crosshairCenter.Y - formSize / 2);

        // Enable double buffering to reduce flicker
        SetStyle(
            ControlStyles.AllPaintingInWmPaint
                | ControlStyles.UserPaint
                | ControlStyles.DoubleBuffer
                | ControlStyles.ResizeRedraw,
            true
        );

        ResumeLayout(false);
    }

    private void BlinkTimer_Tick(object? sender, EventArgs e)
    {
        _isVisible = !_isVisible;
        Invalidate(); // Trigger repaint
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        if (!_isVisible)
            return;

        var graphics = e.Graphics;
        using (var brush = new SolidBrush(Color.Magenta))
        {
            // Calculate crosshair position relative to form
            int centerX = Width / 2;
            int centerY = Height / 2;
            int halfLength = _crosshairLength / 2;
            int halfThickness = _crosshairThickness / 2;

            // Draw horizontal line
            graphics.FillRectangle(
                brush,
                centerX - halfLength,
                centerY - halfThickness,
                _crosshairLength,
                _crosshairThickness
            );

            // Draw vertical line
            graphics.FillRectangle(
                brush,
                centerX - halfThickness,
                centerY - halfLength,
                _crosshairThickness,
                _crosshairLength
            );
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _blinkTimer?.Stop();
        base.OnFormClosing(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _blinkTimer?.Dispose();
        }
        base.Dispose(disposing);
    }

    // Prevent user from closing via Alt+F4 or other means
    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _blinkTimer?.Stop();
        base.OnFormClosed(e);
    }
}
