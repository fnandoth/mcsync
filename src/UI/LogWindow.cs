using MCSync.Core;

namespace MCSync.UI;

public sealed class LogWindow : Form
{
    private readonly AppLogger _logger;
    private readonly RichTextBox _logTextBox;

    public LogWindow(AppLogger logger)
    {
        _logger = logger;
        Text = "MCSync Logs";
        Width = 920;
        Height = 600;
        StartPosition = FormStartPosition.CenterScreen;

        _logTextBox = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            Font = new Font("Consolas", 10),
            BackColor = Color.FromArgb(20, 24, 28),
            ForeColor = Color.Gainsboro
        };

        Controls.Add(_logTextBox);
        Load += OnLoad;
        FormClosing += OnFormClosing;
    }

    private void OnLoad(object? sender, EventArgs e)
    {
        _logTextBox.Lines = _logger.Snapshot().Select(x => x.ToString()).ToArray();
        _logTextBox.SelectionStart = _logTextBox.TextLength;
        _logTextBox.ScrollToCaret();
        _logger.EntryWritten += OnEntryWritten;
    }

    private void OnEntryWritten(object? sender, LogEntry entry)
    {
        if (IsDisposed)
        {
            return;
        }

        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => AppendEntry(entry)));
            return;
        }

        AppendEntry(entry);
    }

    private void AppendEntry(LogEntry entry)
    {
        _logTextBox.AppendText(entry + Environment.NewLine);
        _logTextBox.SelectionStart = _logTextBox.TextLength;
        _logTextBox.ScrollToCaret();
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        e.Cancel = true;
        Hide();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _logger.EntryWritten -= OnEntryWritten;
        }

        base.Dispose(disposing);
    }
}
