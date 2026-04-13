using MCSync.Core;

namespace MCSync.UI;

public sealed class SetupForm : Form
{
    private static readonly Color SurfaceColor = Color.FromArgb(14, 18, 24);
    private static readonly Color CardColor = Color.FromArgb(24, 30, 39);
    private static readonly Color InputColor = Color.FromArgb(31, 39, 50);
    private static readonly Color TextColor = Color.FromArgb(236, 241, 248);
    private static readonly Color MutedTextColor = Color.FromArgb(162, 174, 190);
    private static readonly Color AccentColor = Color.FromArgb(90, 170, 255);
    private static readonly Color ButtonColor = Color.FromArgb(38, 47, 60);

    private readonly UserConfig _originalConfig;
    private readonly Dictionary<string, Control> _fields = new();

    public SetupForm(UserConfig config)
    {
        _originalConfig = config.Clone();

        Text = "Configuracion de MCSync";
        Width = 900;
        Height = 610;
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(860, 560);
        BackColor = SurfaceColor;
        ForeColor = TextColor;
        Font = new Font("Segoe UI", 9F);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(20, 18, 20, 18),
            BackColor = SurfaceColor
        };

        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var headerPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 88,
            Padding = new Padding(0, 0, 0, 12)
        };

        var titleLabel = new Label
        {
            Text = "Configuracion",
            Dock = DockStyle.Top,
            AutoSize = false,
            Height = 38,
            Font = new Font("Segoe UI Semibold", 17F),
            ForeColor = TextColor,
            TextAlign = ContentAlignment.BottomLeft
        };

        var subtitleLabel = new Label
        {
            Text = "Solo campos esenciales para sincronizar, hostear y recuperar estado remoto.",
            Dock = DockStyle.Top,
            AutoSize = false,
            Height = 28,
            Font = new Font("Segoe UI", 9.5F),
            ForeColor = MutedTextColor,
            TextAlign = ContentAlignment.BottomLeft
        };

        headerPanel.Controls.Add(subtitleLabel);
        headerPanel.Controls.Add(titleLabel);
        root.Controls.Add(headerPanel, 0, 0);

        var scrollPanel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(0, 0, 8, 0),
            BackColor = SurfaceColor
        };

        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 3,
            BackColor = CardColor,
            Padding = new Padding(18, 10, 18, 16)
        };

        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 188));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 106));

        AddSectionRow(table, "GitHub");

        AddTextRow(table, "GitHub owner", "GitHubOwner", _originalConfig.GitHubOwner);
        AddTextRow(table, "GitHub repo", "GitHubRepo", _originalConfig.GitHubRepo);
        AddTextRow(table, "GitHub branch", "GitHubBranch", _originalConfig.GitHubBranch);
        AddTextRow(table, "GitHub token", "GitHubToken", _originalConfig.GetGitHubToken(), password: true);

        AddSectionRow(table, "Servidor");

        AddFileRow(table, "Server jar", "ServerJarPath", _originalConfig.ServerJarPath, "JAR (*.jar)|*.jar|Todos (*.*)|*.*");
        AddTextRow(table, "playit.gg URL", "PlayitGGUrl", _originalConfig.PlayitGGUrl);
        AddTextRow(table, "RAM minima MB", "JavaMinMemoryMb", _originalConfig.JavaMinMemoryMb.ToString());
        AddTextRow(table, "RAM maxima MB", "JavaMaxMemoryMb", _originalConfig.JavaMaxMemoryMb.ToString());

        scrollPanel.Controls.Add(table);
        root.Controls.Add(scrollPanel, 0, 1);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            Padding = new Padding(0, 12, 0, 0),
            BackColor = SurfaceColor
        };

        var saveButton = new Button
        {
            Text = "Guardar",
            AutoSize = false,
            Width = 120,
            Height = 36
        };
        StyleButton(saveButton, AccentColor, TextColor);
        saveButton.Click += OnSaveClicked;

        var cancelButton = new Button
        {
            Text = "Cancelar",
            AutoSize = false,
            Width = 120,
            Height = 36,
            DialogResult = DialogResult.Cancel
        };
        StyleButton(cancelButton, ButtonColor, TextColor);

        buttons.Controls.Add(saveButton);
        buttons.Controls.Add(cancelButton);
        root.Controls.Add(buttons, 0, 2);

        AcceptButton = saveButton;
        CancelButton = cancelButton;
        Controls.Add(root);
    }

    public UserConfig? SavedConfig { get; private set; }

    private void AddSectionRow(TableLayoutPanel table, string title)
    {
        var row = table.RowCount++;
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var sectionLabel = new Label
        {
            Text = title.ToUpperInvariant(),
            Dock = DockStyle.Fill,
            AutoSize = true,
            ForeColor = MutedTextColor,
            Font = new Font("Segoe UI Semibold", 9F),
            Padding = new Padding(0, 16, 0, 8),
            Margin = new Padding(0)
        };

        table.Controls.Add(sectionLabel, 0, row);
        table.SetColumnSpan(sectionLabel, 3);
    }

    private void AddTextRow(TableLayoutPanel table, string label, string key, string value, bool password = false)
    {
        var row = table.RowCount++;
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var labelControl = CreateFieldLabel(label);
        var textBox = CreateFieldTextBox(value, password);

        table.Controls.Add(labelControl, 0, row);
        table.Controls.Add(textBox, 1, row);
        table.Controls.Add(new Label { AutoSize = true }, 2, row);
        _fields[key] = textBox;
    }

    private void AddFileRow(TableLayoutPanel table, string label, string key, string value, string filter)
    {
        var row = table.RowCount++;
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var labelControl = CreateFieldLabel(label);
        var textBox = CreateFieldTextBox(value, password: false);

        var button = new Button
        {
            Text = "Buscar",
            AutoSize = false,
            Width = 92,
            Height = 32,
            Margin = new Padding(8, 2, 0, 8)
        };
        StyleButton(button, ButtonColor, TextColor);

        button.Click += (_, _) =>
        {
            using var dialog = new OpenFileDialog
            {
                Filter = filter,
                FileName = textBox.Text
            };

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                textBox.Text = dialog.FileName;
            }
        };

        table.Controls.Add(labelControl, 0, row);
        table.Controls.Add(textBox, 1, row);
        table.Controls.Add(button, 2, row);
        _fields[key] = textBox;
    }

    private Label CreateFieldLabel(string text)
    {
        return new Label
        {
            Text = text,
            Dock = DockStyle.Fill,
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = TextColor,
            Font = new Font("Segoe UI", 9.5F),
            Padding = new Padding(0, 8, 0, 0),
            Margin = new Padding(0, 2, 8, 8)
        };
    }

    private TextBox CreateFieldTextBox(string value, bool password)
    {
        return new TextBox
        {
            Text = value,
            Dock = DockStyle.Fill,
            UseSystemPasswordChar = password,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = InputColor,
            ForeColor = TextColor,
            Font = new Font("Segoe UI", 9.5F),
            Margin = new Padding(0, 2, 0, 8)
        };
    }

    private static void StyleButton(Button button, Color backColor, Color foreColor)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(backColor, 0.12f);
        button.FlatAppearance.MouseOverBackColor = ControlPaint.Light(backColor, 0.06f);
        button.BackColor = backColor;
        button.ForeColor = foreColor;
        button.Font = new Font("Segoe UI Semibold", 9F);
        button.Cursor = Cursors.Hand;
    }

    private void OnSaveClicked(object? sender, EventArgs e)
    {
        if (!int.TryParse(GetText("JavaMinMemoryMb"), out var minMb) || minMb <= 0)
        {
            MessageBox.Show(this, "La RAM minima debe ser un entero positivo.", "MCSync", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!int.TryParse(GetText("JavaMaxMemoryMb"), out var maxMb) || maxMb < minMb)
        {
            MessageBox.Show(this, "La RAM maxima debe ser un entero y no puede ser menor que la minima.", "MCSync", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var config = _originalConfig.Clone();
        config.HostDisplayName = Environment.UserName;//GetText("HostDisplayName");
        config.GitHubOwner = GetText("GitHubOwner");
        config.GitHubRepo = GetText("GitHubRepo");
        config.GitHubBranch = GetText("GitHubBranch");
        config.StateFilePath = "state.json"; //GetText("StateFilePath");
        config.SnapshotFolderPath = "snapshots"; //GetText("SnapshotFolderPath");
        config.WorldId = "survival-main";//GetText("WorldId");
        config.ServerJarPath = GetText("ServerJarPath");
        config.PlayitGGUrl = GetText("PlayitGGUrl");
        config.JavaMinMemoryMb = minMb;
        config.JavaMaxMemoryMb = maxMb;

        var token = GetText("GitHubToken");
        if (!string.IsNullOrWhiteSpace(token))
        {
            config.SetGitHubToken(token);
        }

        if (!config.IsValid(out var error))
        {
            MessageBox.Show(this, error, "MCSync", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        SavedConfig = config;
        DialogResult = DialogResult.OK;
        Close();
    }

    private string GetText(string key) => (_fields[key] as TextBox)?.Text.Trim() ?? string.Empty;
}
