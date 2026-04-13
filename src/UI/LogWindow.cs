using MCSync.Core;

namespace MCSync.UI;

public sealed class LogWindow : Form
{
    private static readonly Color SurfaceColor = Color.FromArgb(14, 18, 24);
    private static readonly Color CardColor = Color.FromArgb(24, 30, 39);
    private static readonly Color TextColor = Color.FromArgb(236, 241, 248);
    private static readonly Color MutedTextColor = Color.FromArgb(162, 174, 190);

    private readonly AppLogger _logger;
    private readonly RichTextBox _logTextBox;
    private readonly Button _copyButton;
    private readonly Button _clearButton;

    public LogWindow(AppLogger logger)
    {
        _logger = logger;
        Text = "Logs de MCSync";
        Width = 920;
        Height = 620;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = SurfaceColor;
        ForeColor = TextColor;
        Font = new Font("Segoe UI", 9F);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 1,
            Padding = new Padding(16, 14, 16, 16),
            BackColor = SurfaceColor
        };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var card = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = CardColor,
            Padding = new Padding(14, 12, 14, 14)
        };
        card.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        card.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var headerPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            RowCount = 1,
            Height = 52,
            Margin = new Padding(0, 0, 0, 10)
        };
        headerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        headerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var titleStack = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(0)
        };
        titleStack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        titleStack.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var title = new Label
        {
            Text = "Actividad del sistema",
            Dock = DockStyle.Fill,
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 12.5F),
            ForeColor = TextColor,
            TextAlign = ContentAlignment.BottomLeft
        };
        var subtitle = new Label
        {
            Text = "Vista cronologica de eventos para diagnostico rapido.",
            Dock = DockStyle.Fill,
            AutoSize = true,
            Font = new Font("Segoe UI", 9F),
            ForeColor = MutedTextColor,
            TextAlign = ContentAlignment.TopLeft
        };

        titleStack.Controls.Add(title, 0, 0);
        titleStack.Controls.Add(subtitle, 0, 1);

        _logTextBox = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            Font = new Font("Consolas", 10),
            BorderStyle = BorderStyle.None,
            BackColor = Color.FromArgb(19, 24, 32),
            ForeColor = Color.Gainsboro
        };

        var actions = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Right,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Margin = new Padding(0, 8, 0, 0)
        };

        _copyButton = new Button
        {
            Text = "Copiar",
            Width = 96,
            Height = 32,
            Margin = new Padding(8, 0, 0, 0)
        };
        StyleButton(_copyButton, Color.FromArgb(90, 170, 255));
        _copyButton.Click += (_, _) => Clipboard.SetText(_logTextBox.Text);

        _clearButton = new Button
        {
            Text = "Limpiar",
            Width = 96,
            Height = 32,
            Margin = new Padding(8, 0, 0, 0)
        };
        StyleButton(_clearButton, Color.FromArgb(38, 47, 60));
        _clearButton.Click += (_, _) => _logTextBox.Clear();

        actions.Controls.Add(_copyButton);
        actions.Controls.Add(_clearButton);
        headerPanel.Controls.Add(titleStack, 0, 0);
        headerPanel.Controls.Add(actions, 1, 0);

        card.Controls.Add(headerPanel, 0, 0);
        card.Controls.Add(_logTextBox, 0, 1);
        root.Controls.Add(card, 0, 0);
        Controls.Add(root);
        Load += OnLoad;
        FormClosing += OnFormClosing;
    }

    private void OnLoad(object? sender, EventArgs e)
    {
        _logTextBox.Clear();
        foreach (var entry in _logger.Snapshot())
        {
            AppendEntry(entry);
        }

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
        var levelColor = entry.Level switch
        {
            "ERROR" => Color.FromArgb(255, 122, 122),
            "WARN" => Color.FromArgb(255, 210, 122),
            _ => Color.Gainsboro
        };

        _logTextBox.SelectionStart = _logTextBox.TextLength;
        _logTextBox.SelectionLength = 0;
        _logTextBox.SelectionColor = levelColor;
        _logTextBox.AppendText(entry + Environment.NewLine);
        _logTextBox.SelectionColor = _logTextBox.ForeColor;
        _logTextBox.SelectionStart = _logTextBox.TextLength;
        _logTextBox.ScrollToCaret();
    }

    private static void StyleButton(Button button, Color backColor)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(backColor, 0.12f);
        button.FlatAppearance.MouseOverBackColor = ControlPaint.Light(backColor, 0.06f);
        button.BackColor = backColor;
        button.ForeColor = TextColor;
        button.Font = new Font("Segoe UI Semibold", 9F);
        button.Cursor = Cursors.Hand;
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
