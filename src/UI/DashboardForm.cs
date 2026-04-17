using MCSync.Core;

namespace MCSync.UI;

public sealed class DashboardForm : Form
{
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
        Width = 1020;
        Height = 680;
        MinimumSize = new Size(960, 620);
        StartPosition = FormStartPosition.CenterScreen;
        NothingTheme.StyleForm(this);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(32),
            BackColor = NothingTheme.Black
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var header = CreateCardPanel();
        header.Height = 138;

        var badge = NothingTheme.CreateMetaLabel("[ MCSYNC CONTROL ]", 24);
        var title = new Label
        {
            Text = "HOST SYNC",
            Dock = DockStyle.Top,
            Height = 56,
            Font = NothingTheme.Display(42F),
            ForeColor = NothingTheme.TextDisplay,
            TextAlign = ContentAlignment.BottomLeft
        };
        header.Controls.Add(title);
        header.Controls.Add(badge);
        root.Controls.Add(header, 0, 0);

        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0),
            BackColor = NothingTheme.Black
        };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65));
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));

        var statusCard = CreateCardPanel();
        statusCard.Dock = DockStyle.Fill;
        statusCard.Margin = new Padding(0, 16, 12, 0);

        var statusTitle = NothingTheme.CreateMetaLabel(Localizer.Get("Dashboard.StatusTitle"), 22);
        _statusValueLabel = new Label
        {
            Text = "IDLE",
            Dock = DockStyle.Top,
            Height = 76,
            Font = NothingTheme.Display(58F),
            ForeColor = NothingTheme.TextDisplay
        };
        _statusDetailLabel = new Label
        {
            Text = Localizer.Get("Common.Ready"),
            Dock = DockStyle.Top,
            Height = 30,
            Font = NothingTheme.Ui(12F),
            ForeColor = NothingTheme.TextPrimary
        };
        _remoteStateLabel = CreateDetailLabel(Localizer.Get("Dashboard.RemoteStateNoData"), NothingTheme.TextPrimary);
        _versionLabel = CreateDetailLabel(Localizer.Format("Dashboard.WorldVersionFormat", "-"), NothingTheme.TextSecondary);
        _hostLabel = CreateDetailLabel(Localizer.Format("Dashboard.ActiveHostFormat", "-"), NothingTheme.TextSecondary);

        var addressTitle = NothingTheme.CreateMetaLabel(Localizer.Get("Dashboard.AddressTitle"), 22);
        addressTitle.Padding = new Padding(0, 6, 0, 0);
        _addressTextBox = new TextBox
        {
            Dock = DockStyle.Top,
            Height = 38,
            ReadOnly = true,
            Text = "-"
        };
        NothingTheme.StyleInput(_addressTextBox, useMono: true);

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
        actionsCard.Margin = new Padding(12, 16, 0, 0);

        var actionsTable = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 1,
            RowCount = 5,
            AutoSize = true,
            Margin = new Padding(0),
            Padding = new Padding(0, 10, 0, 0),
            BackColor = NothingTheme.Surface
        };
        actionsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var i = 0; i < 5; i++)
        {
            actionsTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }

        _hostActionButton = CreateActionButton(Localizer.Get("Dashboard.HostActionStart"), NothingButtonVariant.Primary);
        _copyAddressButton = CreateActionButton(Localizer.Get("Dashboard.CopyIp"), NothingButtonVariant.Secondary);
        _refreshButton = CreateActionButton(Localizer.Get("Dashboard.RefreshState"), NothingButtonVariant.Secondary);
        _settingsButton = CreateActionButton(Localizer.Get("Dashboard.Settings"), NothingButtonVariant.Secondary);
        _logsButton = CreateActionButton(Localizer.Get("Dashboard.ViewLogs"), NothingButtonVariant.Ghost);

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
                MessageBox.Show(this, Localizer.Get("Dashboard.NoPublicIp"), "MCSync", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            Localizer.SetLanguage(form.SavedConfig.UiLanguage);
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
            SyncLifecycleStatus.Hosting => NothingTheme.Success,
            SyncLifecycleStatus.SyncingDown or SyncLifecycleStatus.SyncingUp => NothingTheme.Warning,
            SyncLifecycleStatus.Error => NothingTheme.Accent,
            _ => NothingTheme.TextDisplay
        };

        var statusDetail = _orchestrator.Status == SyncLifecycleStatus.Idle
            ? Localizer.Get("Common.Ready")
            : _orchestrator.StatusMessage;
        _statusDetailLabel.Text = statusDetail;
        _addressTextBox.Text = string.IsNullOrWhiteSpace(address) ? "-" : address;
        _remoteStateLabel.Text = hasActiveHost
            ? Localizer.Format("Dashboard.RemoteStateActiveHostFormat", remoteState?.Host?.DisplayName?.ToUpperInvariant())
            : Localizer.Get("Dashboard.RemoteStateNoActiveHost");
        _versionLabel.Text = Localizer.Format("Dashboard.WorldVersionFormat", remoteState?.WorldVersion ?? 0);
        _hostLabel.Text = Localizer.Format("Dashboard.ActiveHostFormat", string.IsNullOrWhiteSpace(address) ? "-" : address);

        UpdateActionButtons();
    }

    private void UpdateActionButtons()
    {
        var address = _orchestrator.CurrentTunnelAddress ?? _orchestrator.LastKnownRemoteState?.Host?.TunnelAddress;
        var isHosting = _orchestrator.IsHosting;
        var interactive = !_isBusy;

        _copyAddressButton.Text = Localizer.Get("Dashboard.CopyIp");
        _refreshButton.Text = Localizer.Get("Dashboard.RefreshState");
        _settingsButton.Text = Localizer.Get("Dashboard.Settings");
        _logsButton.Text = Localizer.Get("Dashboard.ViewLogs");
        _hostActionButton.Enabled = interactive;
        if (isHosting)
        {
            _hostActionButton.Text = Localizer.Get("Dashboard.HostActionStopSync");
            ApplyButtonStyle(_hostActionButton, NothingButtonVariant.Destructive);
        }
        else
        {
            _hostActionButton.Text = Localizer.Get("Dashboard.HostActionStart");
            ApplyButtonStyle(_hostActionButton, NothingButtonVariant.Primary);
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
            Height = 24,
            ForeColor = color,
            Font = NothingTheme.Mono(9F)
        };
    }

    private static Panel CreateCardPanel()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Top,
            Margin = new Padding(0)
        };
        NothingTheme.StyleCard(panel, 18);
        return panel;
    }

    private static Button CreateActionButton(string text, NothingButtonVariant variant)
    {
        var button = new Button
        {
            Text = text,
            Dock = DockStyle.Top,
            Height = 44,
            Margin = new Padding(0, 0, 0, 10)
        };

        ApplyButtonStyle(button, variant);
        return button;
    }

    private static void ApplyButtonStyle(Button button, NothingButtonVariant variant)
    {
        NothingTheme.StyleButton(button, variant);
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
