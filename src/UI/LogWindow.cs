using MCSync.Core;

namespace MCSync.UI;

public sealed class LogWindow : Form
{
    private readonly AppLogger _logger;
    private readonly RichTextBox _logTextBox;
    private readonly Button _copyButton;
    private readonly Button _clearButton;

    public LogWindow(AppLogger logger)
    {
        _logger = logger;
        Text = Localizer.Get("LogWindow.Title");
        Width = 920;
        Height = 620;
        StartPosition = FormStartPosition.CenterScreen;
        NothingTheme.StyleForm(this);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 1,
            Padding = new Padding(24),
            BackColor = NothingTheme.Black
        };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var card = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = NothingTheme.Surface,
            Padding = new Padding(14, 12, 14, 14)
        };
        NothingTheme.StyleCard(card, 14);
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
            Text = Localizer.Get("LogWindow.SystemActivity"),
            Dock = DockStyle.Fill,
            AutoSize = true,
            Font = NothingTheme.Display(30F),
            ForeColor = NothingTheme.TextDisplay,
            TextAlign = ContentAlignment.BottomLeft
        };
        var subtitle = new Label
        {
            Text = "",
            Dock = DockStyle.Fill,
            AutoSize = true,
            Font = NothingTheme.Mono(9F),
            ForeColor = NothingTheme.TextSecondary,
            TextAlign = ContentAlignment.TopLeft
        };

        titleStack.Controls.Add(title, 0, 0);
        titleStack.Controls.Add(subtitle, 0, 1);

        _logTextBox = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            Font = NothingTheme.Mono(10F),
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = NothingTheme.Black,
            ForeColor = NothingTheme.TextPrimary
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
            Text = Localizer.Get("LogWindow.Copy"),
            Width = 96,
            Height = 32,
            Margin = new Padding(8, 0, 0, 0)
        };
        StyleButton(_copyButton, NothingButtonVariant.Secondary);
        _copyButton.Click += (_, _) => Clipboard.SetText(_logTextBox.Text);

        _clearButton = new Button
        {
            Text = Localizer.Get("LogWindow.Clear"),
            Width = 96,
            Height = 32,
            Margin = new Padding(8, 0, 0, 0)
        };
        StyleButton(_clearButton, NothingButtonVariant.Ghost);
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
            "ERROR" => NothingTheme.Accent,
            "WARN" => NothingTheme.Warning,
            _ => NothingTheme.TextPrimary
        };

        _logTextBox.SelectionStart = _logTextBox.TextLength;
        _logTextBox.SelectionLength = 0;
        _logTextBox.SelectionColor = levelColor;
        _logTextBox.AppendText(entry + Environment.NewLine);
        _logTextBox.SelectionColor = _logTextBox.ForeColor;
        _logTextBox.SelectionStart = _logTextBox.TextLength;
        _logTextBox.ScrollToCaret();
    }

    private static void StyleButton(Button button, NothingButtonVariant variant)
    {
        NothingTheme.StyleButton(button, variant);
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
