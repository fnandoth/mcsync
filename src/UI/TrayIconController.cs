using MCSync.Core;

namespace MCSync.UI;

public sealed class TrayIconController : IDisposable
{
    private readonly SyncOrchestrator _orchestrator;
    private readonly ConfigStore _configStore;
    private readonly AppLogger _logger;
    private readonly Action _exitAction;
    private readonly Action _showDashboardAction;
    private readonly NotifyIcon _notifyIcon;
    private readonly ContextMenuStrip _menu;
    private readonly ToolStripMenuItem _statusItem;
    private readonly ToolStripMenuItem _openDashboardItem;
    private readonly ToolStripMenuItem _startHostItem;
    private readonly ToolStripMenuItem _stopHostItem;
    private readonly ToolStripMenuItem _copyAddressItem;
    private readonly ToolStripMenuItem _refreshStateItem;
    private readonly ToolStripMenuItem _settingsItem;
    private readonly ToolStripMenuItem _logsItem;
    private readonly ToolStripMenuItem _exitItem;
    private readonly Control _uiInvoker;

    private LogWindow? _logWindow;

    public TrayIconController(
        SyncOrchestrator orchestrator,
        ConfigStore configStore,
        AppLogger logger,
        Action showDashboardAction,
        Action exitAction)
    {
        _orchestrator = orchestrator;
        _configStore = configStore;
        _logger = logger;
        _showDashboardAction = showDashboardAction;
        _exitAction = exitAction;
        _uiInvoker = new Control();
        _ = _uiInvoker.Handle;

        _statusItem = new ToolStripMenuItem("ESTADO: LISTO") { Enabled = false };
        _openDashboardItem = new ToolStripMenuItem("ABRIR PANEL");
        _startHostItem = new ToolStripMenuItem("INICIAR COMO HOST");
        _stopHostItem = new ToolStripMenuItem("DETENER HOST Y SUBIR MUNDO");
        _copyAddressItem = new ToolStripMenuItem("COPIAR IP ACTUAL");
        _refreshStateItem = new ToolStripMenuItem("ACTUALIZAR ESTADO");
        _settingsItem = new ToolStripMenuItem("CONFIGURACION");
        _logsItem = new ToolStripMenuItem("VER LOGS");
        _exitItem = new ToolStripMenuItem("SALIR");

        _openDashboardItem.Click += (_, _) => _showDashboardAction();
        _startHostItem.Click += async (_, _) => await StartHostingAsync();
        _stopHostItem.Click += async (_, _) => await StopHostingAsync();
        _copyAddressItem.Click += async (_, _) => await CopyAddressAsync();
        _refreshStateItem.Click += async (_, _) => await RefreshStateAsync();
        _settingsItem.Click += async (_, _) => await ShowSettingsAsync();
        _logsItem.Click += (_, _) => ShowLogs();
        _exitItem.Click += async (_, _) => await ExitAsync();

        _menu = new ContextMenuStrip();
        _menu.BackColor = NothingTheme.Surface;
        _menu.ForeColor = NothingTheme.TextPrimary;
        _menu.Font = NothingTheme.Mono(9F);
        _menu.Renderer = new ToolStripProfessionalRenderer(new NothingColorTable());
        _menu.Opening += (_, _) => UpdateMenuState();
        _menu.Items.AddRange(
        [
            _statusItem,
            new ToolStripSeparator(),
            _openDashboardItem,
            new ToolStripSeparator(),
            _startHostItem,
            _stopHostItem,
            _copyAddressItem,
            _refreshStateItem,
            new ToolStripSeparator(),
            _settingsItem,
            _logsItem,
            new ToolStripSeparator(),
            _exitItem
        ]);

        _notifyIcon = new NotifyIcon
        {
            Text = "MCSync",
            Visible = true,
            Icon = SystemIcons.Application,
            ContextMenuStrip = _menu
        };

        _notifyIcon.DoubleClick += (_, _) => _showDashboardAction();
        _orchestrator.StatusChanged += OnStatusChanged;
        _ = CheckConfigurationOnStartupAsync();
        UpdateMenuState();
    }

    private async Task CheckConfigurationOnStartupAsync()
    {
        var config = await _orchestrator.LoadConfigAsync();
        if (!config.IsValid(out _))
        {
            ShowBalloon("MCSync", "Completa la configuracion inicial para empezar.");
            await ShowSettingsAsync();
        }
    }

    private async Task StartHostingAsync()
    {
        try
        {
            await _orchestrator.StartHostingAsync();
            ShowBalloon("MCSync", _orchestrator.StatusMessage);
        }
        catch (Exception ex)
        {
            _logger.Error("No se pudo iniciar el host.", ex);
            MessageBox.Show(ex.Message, "MCSync", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            UpdateMenuState();
        }
    }

    private async Task StopHostingAsync()
    {
        try
        {
            await _orchestrator.StopHostingAsync();
            ShowBalloon("MCSync", "Mundo sincronizado y host liberado.");
        }
        catch (Exception ex)
        {
            _logger.Error("No se pudo detener el host.", ex);
            MessageBox.Show(ex.Message, "MCSync", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            UpdateMenuState();
        }
    }

    private async Task CopyAddressAsync()
    {
        try
        {
            var address = await _orchestrator.GetCurrentAddressAsync();
            if (string.IsNullOrWhiteSpace(address))
            {
                MessageBox.Show("No hay una IP publica disponible en este momento.", "MCSync", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            Clipboard.SetText(address);
            ShowBalloon("MCSync", $"Direccion copiada: {address}");
        }
        catch (Exception ex)
        {
            _logger.Error("No se pudo copiar la direccion actual.", ex);
            MessageBox.Show(ex.Message, "MCSync", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task RefreshStateAsync()
    {
        try
        {
            var state = await _orchestrator.RefreshStateAsync();
            var summary = state.HasActiveHost(DateTimeOffset.UtcNow)
                ? $"Host activo: {state.Host?.DisplayName} {state.Host?.TunnelAddress}"
                : $"Sin host activo. Ultima version: {state.WorldVersion}";

            ShowBalloon("MCSync", summary);
        }
        catch (Exception ex)
        {
            _logger.Error("No se pudo refrescar el estado remoto.", ex);
            MessageBox.Show(ex.Message, "MCSync", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            UpdateMenuState();
        }
    }

    private async Task ShowSettingsAsync()
    {
        var config = await _orchestrator.LoadConfigAsync();
        using var form = new SetupForm(config);

        if (form.ShowDialog() == DialogResult.OK && form.SavedConfig is not null)
        {
            await _configStore.SaveAsync(form.SavedConfig);
            ShowBalloon("MCSync", "Configuracion guardada.");
        }

        UpdateMenuState();
    }

    private void ShowLogs()
    {
        _logWindow ??= new LogWindow(_logger);

        if (!_logWindow.Visible)
        {
            _logWindow.Show();
        }

        _logWindow.WindowState = FormWindowState.Normal;
        _logWindow.BringToFront();
    }

    private async Task ExitAsync()
    {
        if (_orchestrator.IsHosting)
        {
            var confirm = MessageBox.Show(
                "El host esta activo. Si sales ahora se intentara detener el servidor y subir el mundo. ¿Continuar?",
                "MCSync",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes)
            {
                return;
            }
        }

        if (_orchestrator.IsHosting)
        {
            try
            {
                await _orchestrator.StopHostingAsync();
            }
            catch (Exception ex)
            {
                _logger.Error("El cierre del host antes de salir fallo.", ex);
            }
        }

        _exitAction();
    }

    private void OnStatusChanged(object? sender, SyncStatusChangedEventArgs e)
    {
        if (_uiInvoker.IsDisposed)
        {
            return;
        }

        if (_uiInvoker.InvokeRequired)
        {
            _uiInvoker.BeginInvoke(new Action(UpdateMenuState));
            return;
        }

        UpdateMenuState();
    }

    private void UpdateMenuState()
    {
        var activeAddress = _orchestrator.CurrentTunnelAddress ?? _orchestrator.LastKnownRemoteState?.Host?.TunnelAddress;
        _statusItem.Text = $"ESTADO: {_orchestrator.Status.ToString().ToUpperInvariant()} - {_orchestrator.StatusMessage.ToUpperInvariant()}";
        _startHostItem.Enabled = !_orchestrator.IsHosting;
        _stopHostItem.Enabled = _orchestrator.IsHosting;
        _copyAddressItem.Enabled = !string.IsNullOrWhiteSpace(activeAddress);
    }

    private void ShowBalloon(string title, string message)
    {
        _notifyIcon.BalloonTipTitle = title;
        _notifyIcon.BalloonTipText = message;
        _notifyIcon.ShowBalloonTip(4000);
    }

    public void Dispose()
    {
        _orchestrator.StatusChanged -= OnStatusChanged;
        _logWindow?.Dispose();
        _menu.Dispose();
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _uiInvoker.Dispose();
    }

    private sealed class NothingColorTable : ProfessionalColorTable
    {
        public override Color MenuBorder => NothingTheme.BorderVisible;
        public override Color ToolStripDropDownBackground => NothingTheme.Surface;
        public override Color ImageMarginGradientBegin => NothingTheme.Surface;
        public override Color ImageMarginGradientMiddle => NothingTheme.Surface;
        public override Color ImageMarginGradientEnd => NothingTheme.Surface;
        public override Color MenuItemSelected => NothingTheme.SurfaceRaised;
        public override Color MenuItemBorder => NothingTheme.BorderVisible;
    }
}
