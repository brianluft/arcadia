using System;
using System.Drawing;
using System.Windows.Forms;

namespace ComputerUse
{
    public partial class SafetyRectangleForm : Form
    {
        private readonly System.Windows.Forms.Timer _blinkTimer;
        private readonly Rectangle _targetRectangle;
        private bool _isVisible = true;

        public SafetyRectangleForm(Rectangle rectangle)
        {
            _targetRectangle = rectangle;

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

            // Set form size and position to match target rectangle
            Bounds = _targetRectangle;

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

        private void BlinkTimer_Tick(object sender, EventArgs e)
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
                // Fill the entire client area with magenta
                graphics.FillRectangle(brush, ClientRectangle);
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
}
