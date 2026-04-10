using MCSync.Core;

namespace MCSync.UI;

public sealed class SetupForm : Form
{
    private readonly UserConfig _originalConfig;
    private readonly Dictionary<string, Control> _fields = new();

    public SetupForm(UserConfig config)
    {
        _originalConfig = config.Clone();

        Text = "Configuracion inicial de MCSync";
        Width = 760;
        Height = 390;
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(760, 390);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(12)
        };

        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var scrollPanel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true
        };

        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 3
        };

        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));

        AddTextRow(table, "GitHub owner", "GitHubOwner", _originalConfig.GitHubOwner);
        AddTextRow(table, "GitHub repo", "GitHubRepo", _originalConfig.GitHubRepo);
        AddTextRow(table, "GitHub branch", "GitHubBranch", _originalConfig.GitHubBranch);
        AddTextRow(table, "GitHub token", "GitHubToken", _originalConfig.GetGitHubToken(), password: true);
        AddFileRow(table, "Server jar", "ServerJarPath", _originalConfig.ServerJarPath, "JAR (*.jar)|*.jar|Todos (*.*)|*.*");
        AddFolderRow(table, "Server folder", "ServerFolderPath", _originalConfig.ServerFolderPath);
        AddTextRow(table, "playit.gg URL", "PlayitGGUrl", _originalConfig.PlayitGGUrl);
        AddTextRow(table, "RAM minima MB", "JavaMinMemoryMb", _originalConfig.JavaMinMemoryMb.ToString());
        AddTextRow(table, "RAM maxima MB", "JavaMaxMemoryMb", _originalConfig.JavaMaxMemoryMb.ToString());

        scrollPanel.Controls.Add(table);
        root.Controls.Add(scrollPanel, 0, 0);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true
        };

        var saveButton = new Button
        {
            Text = "Guardar",
            AutoSize = true
        };
        saveButton.Click += OnSaveClicked;

        var cancelButton = new Button
        {
            Text = "Cancelar",
            AutoSize = true,
            DialogResult = DialogResult.Cancel
        };

        buttons.Controls.Add(saveButton);
        buttons.Controls.Add(cancelButton);
        root.Controls.Add(buttons, 0, 1);

        AcceptButton = saveButton;
        CancelButton = cancelButton;
        Controls.Add(root);
    }

    public UserConfig? SavedConfig { get; private set; }

    private void AddTextRow(TableLayoutPanel table, string label, string key, string value, bool password = false)
    {
        var row = table.RowCount++;
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var labelControl = new Label
        {
            Text = label,
            Dock = DockStyle.Fill,
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(0, 8, 0, 0)
        };

        var textBox = new TextBox
        {
            Text = value,
            Dock = DockStyle.Fill,
            UseSystemPasswordChar = password
        };

        table.Controls.Add(labelControl, 0, row);
        table.Controls.Add(textBox, 1, row);
        table.Controls.Add(new Label { AutoSize = true }, 2, row);
        _fields[key] = textBox;
    }

    private void AddFileRow(TableLayoutPanel table, string label, string key, string value, string filter)
    {
        var row = table.RowCount++;
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var labelControl = new Label
        {
            Text = label,
            Dock = DockStyle.Fill,
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(0, 8, 0, 0)
        };

        var textBox = new TextBox
        {
            Text = value,
            Dock = DockStyle.Fill
        };

        var button = new Button
        {
            Text = "Buscar",
            AutoSize = true
        };

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

    private void AddFolderRow(TableLayoutPanel table, string label, string key, string value)
    {
        var row = table.RowCount++;
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var labelControl = new Label
        {
            Text = label,
            Dock = DockStyle.Fill,
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(0, 8, 0, 0)
        };

        var textBox = new TextBox
        {
            Text = value,
            Dock = DockStyle.Fill
        };

        var button = new Button
        {
            Text = "Buscar",
            AutoSize = true
        };

        button.Click += (_, _) =>
        {
            using var dialog = new FolderBrowserDialog
            {
                InitialDirectory = Directory.Exists(textBox.Text) ? textBox.Text : AppContext.BaseDirectory
            };

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                textBox.Text = dialog.SelectedPath;
            }
        };

        table.Controls.Add(labelControl, 0, row);
        table.Controls.Add(textBox, 1, row);
        table.Controls.Add(button, 2, row);
        _fields[key] = textBox;
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
        config.ServerFolderPath = GetText("ServerFolderPath");
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
