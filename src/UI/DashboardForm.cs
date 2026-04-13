using MCSync.Core;

namespace MCSync.UI;

public sealed class DashboardForm : Form
{
    private static readonly Color SurfaceColor = Color.FromArgb(14, 18, 24);
    private static readonly Color CardColor = Color.FromArgb(24, 30, 39);
    private static readonly Color InputColor = Color.FromArgb(31, 39, 50);
    private static readonly Color TextColor = Color.FromArgb(236, 241, 248);
    private static readonly Color MutedTextColor = Color.FromArgb(162, 174, 190);
    private static readonly Color AccentColor = Color.FromArgb(90, 170, 255);
    private static readonly Color DangerColor = Color.FromArgb(242, 112, 112);
    private static readonly Color NeutralButtonColor = Color.FromArgb(38, 47, 60);

    private readonly SyncOrchestrator _orchestrator;
    private readonly ConfigStore _configStore;
    private readonly AppLogger _logger;
    private readonly Label _statusValueLabel;
    private readonly Label _statusDetailLabel;
    private readonly Label _remoteStateLabel;
    private readonly Label _versionLabel;
    private readonly Label _hostLabel;
    private readonly TextBox _addressTextBox;
    private readonly Button _hostActionButton;
    private readonly Button _copyAddressButton;
    private readonly Button _refreshButton;
    private readonly Button _settingsButton;
    private readonly Button _logsButton;
    private readonly System.Windows.Forms.Timer _refreshTimer;

    private LogWindow? _logWindow;
    private bool _isBusy;

    public DashboardForm(SyncOrchestrator orchestrator, ConfigStore configStore, AppLogger logger)
    {
        _orchestrator = orchestrator;
        _configStore = configStore;
        _logger = logger;

        Text = "MCSync";
        Width = 980;
        Height = 660;
        MinimumSize = new Size(940, 600);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = SurfaceColor;
        ForeColor = TextColor;
        Font = new Font("Segoe UI", 9F);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(20, 18, 20, 18),
            BackColor = SurfaceColor
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var header = CreateCardPanel();
        header.Height = 94;

        var title = new Label
        {
            Text = "Panel principal",
            Dock = DockStyle.Top,
            Height = 40,
            Font = new Font("Segoe UI Semibold", 19F),
            ForeColor = TextColor,
            TextAlign = ContentAlignment.BottomLeft
        };
        var subtitle = new Label
        {
            Text = "Inicia o detiene el host y revisa el estado del mundo en una sola vista.",
            Dock = DockStyle.Top,
            Height = 26,
            Font = new Font("Segoe UI", 9.5F),
            ForeColor = MutedTextColor,
            TextAlign = ContentAlignment.BottomLeft
        };
        header.Controls.Add(subtitle);
        header.Controls.Add(title);
        root.Controls.Add(header, 0, 0);

        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0),
            BackColor = SurfaceColor
        };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));

        var statusCard = CreateCardPanel();
        statusCard.Dock = DockStyle.Fill;
        statusCard.Margin = new Padding(0, 12, 10, 0);

        var statusTitle = new Label
        {
            Text = "Estado actual",
            Dock = DockStyle.Top,
            Height = 28,
            Font = new Font("Segoe UI Semibold", 11.5F),
            ForeColor = TextColor
        };
        _statusValueLabel = new Label
        {
            Text = "IDLE",
            Dock = DockStyle.Top,
            Height = 42,
            Font = new Font("Segoe UI Semibold", 18F),
            ForeColor = AccentColor
        };
        _statusDetailLabel = new Label
        {
            Text = "Listo",
            Dock = DockStyle.Top,
            Height = 30,
            Font = new Font("Segoe UI", 9.5F),
            ForeColor = TextColor
        };
        _remoteStateLabel = CreateDetailLabel("Estado remoto: sin datos", TextColor);
        _versionLabel = CreateDetailLabel("Version mundo: -", MutedTextColor);
        _hostLabel = CreateDetailLabel("Host activo: -", MutedTextColor);

        var addressTitle = new Label
        {
            Text = "IP publica",
            Dock = DockStyle.Top,
            Height = 24,
            Font = new Font("Segoe UI Semibold", 9F),
            ForeColor = MutedTextColor,
            Padding = new Padding(0, 4, 0, 0)
        };
        _addressTextBox = new TextBox
        {
            Dock = DockStyle.Top,
            Height = 32,
            ReadOnly = true,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = InputColor,
            ForeColor = TextColor,
            Font = new Font("Segoe UI", 10F),
            Text = "-"
        };

        statusCard.Controls.Add(_addressTextBox);
        statusCard.Controls.Add(addressTitle);
        statusCard.Controls.Add(_hostLabel);
        statusCard.Controls.Add(_versionLabel);
        statusCard.Controls.Add(_remoteStateLabel);
        statusCard.Controls.Add(_statusDetailLabel);
        statusCard.Controls.Add(_statusValueLabel);
        statusCard.Controls.Add(statusTitle);
        content.Controls.Add(statusCard, 0, 0);

        var actionsCard = CreateCardPanel();
        actionsCard.Dock = DockStyle.Fill;
        actionsCard.Margin = new Padding(10, 12, 0, 0);

        var actionsTitle = new Label
        {
            Text = "Acciones",
            Dock = DockStyle.Top,
            Height = 28,
            Font = new Font("Segoe UI Semibold", 11.5F),
            ForeColor = TextColor
        };
        var actionsSubtitle = new Label
        {
            Text = "La accion principal cambia automaticamente segun el estado del host.",
            Dock = DockStyle.Top,
            Height = 40,
            Font = new Font("Segoe UI", 9F),
            ForeColor = MutedTextColor
        };

        var actionsTable = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 1,
            RowCount = 5,
            AutoSize = true,
            Margin = new Padding(0),
            Padding = new Padding(0, 6, 0, 0),
            BackColor = CardColor
        };
        actionsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var i = 0; i < 5; i++)
        {
            actionsTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }

        _hostActionButton = CreateActionButton("Iniciar host", AccentColor);
        _copyAddressButton = CreateActionButton("Copiar IP", NeutralButtonColor);
        _refreshButton = CreateActionButton("Actualizar estado", NeutralButtonColor);
        _settingsButton = CreateActionButton("Configuracion", NeutralButtonColor);
        _logsButton = CreateActionButton("Ver logs", NeutralButtonColor);

        _hostActionButton.Click += async (_, _) => await ToggleHostingAsync();
        _copyAddressButton.Click += async (_, _) => await CopyAddressAsync();
        _refreshButton.Click += async (_, _) => await RefreshRemoteStateAsync();
        _settingsButton.Click += async (_, _) => await OpenSettingsAsync();
        _logsButton.Click += (_, _) => ShowLogs();

        actionsTable.Controls.Add(_hostActionButton, 0, 0);
        actionsTable.Controls.Add(_copyAddressButton, 0, 1);
        actionsTable.Controls.Add(_refreshButton, 0, 2);
        actionsTable.Controls.Add(_settingsButton, 0, 3);
        actionsTable.Controls.Add(_logsButton, 0, 4);

        actionsCard.Controls.Add(actionsTable);
        actionsCard.Controls.Add(actionsSubtitle);
        actionsCard.Controls.Add(actionsTitle);
        content.Controls.Add(actionsCard, 1, 0);

        root.Controls.Add(content, 0, 1);
        Controls.Add(root);

        _orchestrator.StatusChanged += OnOrchestratorStatusChanged;
        _refreshTimer = new System.Windows.Forms.Timer { Interval = 10000 };
        _refreshTimer.Tick += async (_, _) => await RefreshRemoteStateAsync(showErrors: false);

        Shown += async (_, _) => await InitializeAsync();
        Resize += (_, _) =>
        {
            if (WindowState == FormWindowState.Minimized)
            {
                Hide();
            }
        };
        FormClosing += OnFormClosing;
    }

    public void ShowDashboard()
    {
        if (!Visible)
        {
            Show();
        }

        WindowState = FormWindowState.Normal;
        Activate();
        BringToFront();
    }

    private async Task InitializeAsync()
    {
        _refreshTimer.Start();
        await RefreshRemoteStateAsync(showErrors: false);
        UpdateDashboardState();
    }

    private async Task ToggleHostingAsync()
    {
        if (_orchestrator.IsHosting)
        {
            await StopHostingAsync();
            return;
        }

        await StartHostingAsync();
    }

    private async Task StartHostingAsync()
    {
        if (_isBusy)
        {
            return;
        }

        try
        {
            _isBusy = true;
            UpdateActionButtons();
            await _orchestrator.StartHostingAsync();
            await RefreshRemoteStateAsync(showErrors: false);
        }
        catch (Exception ex)
        {
            _logger.Error("No se pudo iniciar el host desde el dashboard.", ex);
            MessageBox.Show(this, ex.Message, "MCSync", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _isBusy = false;
            UpdateDashboardState();
        }
    }

    private async Task StopHostingAsync()
    {
        if (_isBusy)
        {
            return;
        }

        try
        {
            _isBusy = true;
            UpdateActionButtons();
            await _orchestrator.StopHostingAsync();
            await RefreshRemoteStateAsync(showErrors: false);
        }
        catch (Exception ex)
        {
            _logger.Error("No se pudo detener el host desde el dashboard.", ex);
            MessageBox.Show(this, ex.Message, "MCSync", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _isBusy = false;
            UpdateDashboardState();
        }
    }

    private async Task CopyAddressAsync()
    {
        if (_isBusy)
        {
            return;
        }

        try
        {
            var address = await _orchestrator.GetCurrentAddressAsync();
            if (string.IsNullOrWhiteSpace(address))
            {
                MessageBox.Show(this, "No hay una IP publica disponible en este momento.", "MCSync", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            Clipboard.SetText(address);
        }
        catch (Exception ex)
        {
            _logger.Error("No se pudo copiar la direccion actual.", ex);
            MessageBox.Show(this, ex.Message, "MCSync", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task RefreshRemoteStateAsync(bool showErrors = true)
    {
        if (_isBusy)
        {
            return;
        }

        try
        {
            await _orchestrator.RefreshStateAsync();
        }
        catch (Exception ex)
        {
            _logger.Error("No se pudo actualizar el estado remoto desde el dashboard.", ex);
            if (showErrors)
            {
                MessageBox.Show(this, ex.Message, "MCSync", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        finally
        {
            UpdateDashboardState();
        }
    }

    private async Task OpenSettingsAsync()
    {
        if (_isBusy)
        {
            return;
        }

        var config = await _orchestrator.LoadConfigAsync();
        using var form = new SetupForm(config);

        if (form.ShowDialog(this) == DialogResult.OK && form.SavedConfig is not null)
        {
            await _configStore.SaveAsync(form.SavedConfig);
            await RefreshRemoteStateAsync(showErrors: false);
        }
    }

    private void ShowLogs()
    {
        _logWindow ??= new LogWindow(_logger);

        if (!_logWindow.Visible)
        {
            _logWindow.Show(this);
        }

        _logWindow.WindowState = FormWindowState.Normal;
        _logWindow.BringToFront();
    }

    private void OnOrchestratorStatusChanged(object? sender, SyncStatusChangedEventArgs e)
    {
        if (IsDisposed)
        {
            return;
        }

        if (InvokeRequired)
        {
            BeginInvoke(new Action(UpdateDashboardState));
            return;
        }

        UpdateDashboardState();
    }

    private void UpdateDashboardState()
    {
        var remoteState = _orchestrator.LastKnownRemoteState;
        var address = _orchestrator.CurrentTunnelAddress ?? remoteState?.Host?.TunnelAddress;
        var hasActiveHost = remoteState?.HasActiveHost(DateTimeOffset.UtcNow) ?? false;

        _statusValueLabel.Text = _orchestrator.Status.ToString().ToUpperInvariant();
        _statusValueLabel.ForeColor = _orchestrator.Status switch
        {
            SyncLifecycleStatus.Hosting => AccentColor,
            SyncLifecycleStatus.Error => DangerColor,
            _ => TextColor
        };

        _statusDetailLabel.Text = _orchestrator.StatusMessage;
        _addressTextBox.Text = string.IsNullOrWhiteSpace(address) ? "-" : address;
        _remoteStateLabel.Text = hasActiveHost
            ? $"Estado remoto: host activo ({remoteState?.Host?.DisplayName})"
            : "Estado remoto: sin host activo";
        _versionLabel.Text = $"Version mundo: {remoteState?.WorldVersion ?? 0}";
        _hostLabel.Text = $"Host activo: {(string.IsNullOrWhiteSpace(address) ? "-" : address)}";

        UpdateActionButtons();
    }

    private void UpdateActionButtons()
    {
        var address = _orchestrator.CurrentTunnelAddress ?? _orchestrator.LastKnownRemoteState?.Host?.TunnelAddress;
        var isHosting = _orchestrator.IsHosting;
        var interactive = !_isBusy;

        _hostActionButton.Enabled = interactive;
        if (isHosting)
        {
            _hostActionButton.Text = "Detener host y sincronizar";
            ApplyButtonStyle(_hostActionButton, DangerColor);
        }
        else
        {
            _hostActionButton.Text = "Iniciar host";
            ApplyButtonStyle(_hostActionButton, AccentColor);
        }

        _copyAddressButton.Enabled = interactive && !string.IsNullOrWhiteSpace(address);
        _refreshButton.Enabled = interactive;
        _settingsButton.Enabled = interactive;
        _logsButton.Enabled = true;
    }

    private static Label CreateDetailLabel(string text, Color color)
    {
        return new Label
        {
            Text = text,
            Dock = DockStyle.Top,
            Height = 26,
            ForeColor = color,
            Font = new Font("Segoe UI", 9.5F)
        };
    }

    private static Panel CreateCardPanel()
    {
        return new Panel
        {
            Dock = DockStyle.Top,
            Padding = new Padding(16, 14, 16, 14),
            Margin = new Padding(0),
            BackColor = CardColor
        };
    }

    private static Button CreateActionButton(string text, Color backColor)
    {
        var button = new Button
        {
            Text = text,
            Dock = DockStyle.Top,
            Height = 38,
            Margin = new Padding(0, 0, 0, 8)
        };

        ApplyButtonStyle(button, backColor);
        return button;
    }

    private static void ApplyButtonStyle(Button button, Color backColor)
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
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _refreshTimer.Stop();
            _refreshTimer.Dispose();
            _orchestrator.StatusChanged -= OnOrchestratorStatusChanged;
            _logWindow?.Dispose();
        }

        base.Dispose(disposing);
    }
}
