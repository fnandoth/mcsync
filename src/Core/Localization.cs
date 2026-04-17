using System.Globalization;

namespace MCSync.Core;

public enum UiLanguage
{
    Spanish,
    English
}

public static class Localizer
{
    private static UiLanguage _language = UiLanguage.Spanish;

    private static readonly IReadOnlyDictionary<string, string> Spanish = new Dictionary<string, string>
    {
        ["Common.Ready"] = "Listo",
        ["Setup.Title"] = "Configuracion de MCSync",
        ["Setup.Subtitle"] = "SOLO CAMPOS ESENCIALES PARA SINCRONIZAR, HOSTEAR Y RECUPERAR ESTADO REMOTO.",
        ["Setup.SectionGitHub"] = "GitHub",
        ["Setup.SectionServer"] = "Servidor",
        ["Setup.FieldGitHubOwner"] = "GitHub owner",
        ["Setup.FieldGitHubRepo"] = "GitHub repo",
        ["Setup.FieldGitHubBranch"] = "GitHub branch",
        ["Setup.FieldGitHubToken"] = "GitHub token",
        ["Setup.FieldLanguage"] = "Idioma",
        ["Setup.FieldServerJar"] = "Server jar",
        ["Setup.FieldPlayitUrl"] = "playit.gg URL",
        ["Setup.FieldJavaMinMb"] = "RAM minima MB",
        ["Setup.FieldJavaMaxMb"] = "RAM maxima MB",
        ["Setup.Save"] = "Guardar",
        ["Setup.Cancel"] = "Cancelar",
        ["Setup.Browse"] = "Buscar",
        ["Setup.FileDialogFilter"] = "JAR (*.jar)|*.jar|Todos (*.*)|*.*",
        ["Setup.MinRamValidation"] = "La RAM minima debe ser un entero positivo.",
        ["Setup.MaxRamValidation"] = "La RAM maxima debe ser un entero y no puede ser menor que la minima.",
        ["Dashboard.StatusTitle"] = "Estado actual",
        ["Dashboard.AddressTitle"] = "IP publica",
        ["Dashboard.ActionsTitle"] = "Acciones",
        ["Dashboard.Subtitle"] = "INICIA O DETIENE EL HOST Y REVISA EL ESTADO DEL MUNDO EN UNA SOLA VISTA.",
        ["Dashboard.ActionsSubtitle"] = "LA ACCION PRINCIPAL CAMBIA SEGUN EL ESTADO DEL HOST.",
        ["Dashboard.RemoteStateNoData"] = "ESTADO REMOTO: SIN DATOS",
        ["Dashboard.RemoteStateActiveHostFormat"] = "ESTADO REMOTO: HOST ACTIVO ({0})",
        ["Dashboard.RemoteStateNoActiveHost"] = "ESTADO REMOTO: SIN HOST ACTIVO",
        ["Dashboard.WorldVersionFormat"] = "VERSION MUNDO: {0}",
        ["Dashboard.ActiveHostFormat"] = "HOST ACTIVO: {0}",
        ["Dashboard.HostActionStart"] = "Iniciar host",
        ["Dashboard.HostActionStopSync"] = "Detener host y sincronizar",
        ["Dashboard.CopyIp"] = "Copiar IP",
        ["Dashboard.RefreshState"] = "Actualizar estado",
        ["Dashboard.Settings"] = "Configuracion",
        ["Dashboard.ViewLogs"] = "Ver logs",
        ["Dashboard.NoPublicIp"] = "No hay una IP publica disponible en este momento.",
        ["Tray.StatusReady"] = "ESTADO: LISTO",
        ["Tray.StatusFormat"] = "ESTADO: {0} - {1}",
        ["Tray.OpenDashboard"] = "ABRIR PANEL",
        ["Tray.StartHost"] = "INICIAR COMO HOST",
        ["Tray.StopHostUpload"] = "DETENER HOST Y SUBIR MUNDO",
        ["Tray.CopyCurrentIp"] = "COPIAR IP ACTUAL",
        ["Tray.RefreshState"] = "ACTUALIZAR ESTADO",
        ["Tray.Settings"] = "CONFIGURACION",
        ["Tray.ViewLogs"] = "VER LOGS",
        ["Tray.Exit"] = "SALIR",
        ["Tray.CompleteInitialConfig"] = "Completa la configuracion inicial para empezar.",
        ["Tray.WorldSyncedReleased"] = "Mundo sincronizado y host liberado.",
        ["Tray.NoPublicIp"] = "No hay una IP publica disponible en este momento.",
        ["Tray.AddressCopiedFormat"] = "Direccion copiada: {0}",
        ["Tray.ActiveHostSummaryFormat"] = "Host activo: {0} {1}",
        ["Tray.NoActiveHostSummaryFormat"] = "Sin host activo. Ultima version: {0}",
        ["Tray.ConfigurationSaved"] = "Configuracion guardada.",
        ["Tray.ExitConfirmHosting"] = "El host esta activo. Si sales ahora se intentara detener el servidor y subir el mundo. ¿Continuar?",
        ["LogWindow.Title"] = "Logs de MCSync",
        ["LogWindow.SystemActivity"] = "Actividad del sistema",
        ["LogWindow.Copy"] = "Copiar",
        ["LogWindow.Clear"] = "Limpiar",
        ["UserConfig.MissingGitHubOwner"] = "Falta el owner del repositorio de GitHub.",
        ["UserConfig.MissingGitHubRepo"] = "Falta el nombre del repositorio de GitHub.",
        ["UserConfig.MissingGitHubToken"] = "Falta el token de GitHub.",
        ["UserConfig.MissingServerJar"] = "No se encontro server.jar en la ruta configurada.",
        ["UserConfig.MissingPlayitUrl"] = "Falta la URL de playit.gg.",
        ["UserConfig.MissingJavaPath"] = "Falta la ruta del ejecutable de Java.",
        ["Sync.AlreadyHosting"] = "La app ya esta hospedando el mundo.",
        ["Sync.VerifyingRemoteState"] = "Verificando estado remoto...",
        ["Sync.CannotAcquireHostRole"] = "No fue posible tomar el rol de host.",
        ["Sync.PreparingServerFolder"] = "Preparando carpeta del servidor...",
        ["Sync.StartingServerJar"] = "Iniciando server.jar...",
        ["Sync.OpeningPlayitTunnel"] = "Abriendo tunel playit...",
        ["Sync.ServerReadyAwaitingTunnel"] = "Servidor listo. Esperando direccion de tunel...",
        ["Sync.ServerReadyAtFormat"] = "Servidor listo en {0}",
        ["Sync.StoppingHostAndBlocking"] = "Deteniendo host y bloqueando nuevos cambios...",
        ["Sync.CompressingWorld"] = "Comprimiendo mundo...",
        ["Sync.UploadingSnapshotVersionFormat"] = "Subiendo snapshot version {0}...",
        ["Sync.WorldSyncedHostReleased"] = "Mundo sincronizado. Host liberado.",
        ["Sync.HostReleasedLog"] = "Host liberado y mundo sincronizado correctamente.",
        ["Sync.NoRemoteSnapshot"] = "No existe snapshot remoto previo. Se iniciara un mundo nuevo.",
        ["Sync.LocalSnapshotUpToDate"] = "El snapshot local ya coincide con la ultima version remota.",
        ["Sync.DownloadingWorldVersionFormat"] = "Descargando mundo v{0}...",
        ["Sync.RemoteSnapshotAppliedFormat"] = "Snapshot remoto v{0} descargado y aplicado.",
        ["Sync.FailedStartCleanupWarningFormat"] = "La limpieza del arranque fallido reporto un error: {0}",
        ["Sync.ReleaseLeaseAfterFailureWarningFormat"] = "No fue posible liberar el lease tras un fallo: {0}",
        ["Sync.HostStartFailed"] = "El inicio como host fallo.",
        ["Sync.ServerProcessEndedAutoSync"] = "El proceso del servidor termino. Iniciando sincronizacion de cierre automatica.",
        ["Sync.AutoCloseFailedAfterServerEnded"] = "Fallo el cierre automatico luego de que el servidor termino.",
        ["Sync.ServerEndedAutoSyncFailed"] = "El servidor termino y la sincronizacion automatica fallo.",
        ["Sync.CannotStopHostOnAppClose"] = "No fue posible detener el host al cerrar la app."
    };

    private static readonly IReadOnlyDictionary<string, string> English = new Dictionary<string, string>
    {
        ["Common.Ready"] = "Ready",
        ["Setup.Title"] = "MCSync settings",
        ["Setup.Subtitle"] = "ONLY ESSENTIAL FIELDS TO SYNC, HOST, AND RECOVER REMOTE STATE.",
        ["Setup.SectionGitHub"] = "GitHub",
        ["Setup.SectionServer"] = "Server",
        ["Setup.FieldGitHubOwner"] = "GitHub owner",
        ["Setup.FieldGitHubRepo"] = "GitHub repo",
        ["Setup.FieldGitHubBranch"] = "GitHub branch",
        ["Setup.FieldGitHubToken"] = "GitHub token",
        ["Setup.FieldLanguage"] = "Language",
        ["Setup.FieldServerJar"] = "Server jar",
        ["Setup.FieldPlayitUrl"] = "playit.gg URL",
        ["Setup.FieldJavaMinMb"] = "Min RAM MB",
        ["Setup.FieldJavaMaxMb"] = "Max RAM MB",
        ["Setup.Save"] = "Save",
        ["Setup.Cancel"] = "Cancel",
        ["Setup.Browse"] = "Browse",
        ["Setup.FileDialogFilter"] = "JAR (*.jar)|*.jar|All files (*.*)|*.*",
        ["Setup.MinRamValidation"] = "Min RAM must be a positive integer.",
        ["Setup.MaxRamValidation"] = "Max RAM must be an integer and cannot be lower than min RAM.",
        ["Dashboard.StatusTitle"] = "Current status",
        ["Dashboard.AddressTitle"] = "Public IP",
        ["Dashboard.ActionsTitle"] = "Actions",
        ["Dashboard.Subtitle"] = "START OR STOP HOSTING AND CHECK WORLD STATUS IN A SINGLE VIEW.",
        ["Dashboard.ActionsSubtitle"] = "THE PRIMARY ACTION CHANGES BASED ON HOST STATE.",
        ["Dashboard.RemoteStateNoData"] = "REMOTE STATE: NO DATA",
        ["Dashboard.RemoteStateActiveHostFormat"] = "REMOTE STATE: ACTIVE HOST ({0})",
        ["Dashboard.RemoteStateNoActiveHost"] = "REMOTE STATE: NO ACTIVE HOST",
        ["Dashboard.WorldVersionFormat"] = "WORLD VERSION: {0}",
        ["Dashboard.ActiveHostFormat"] = "ACTIVE HOST: {0}",
        ["Dashboard.HostActionStart"] = "Start host",
        ["Dashboard.HostActionStopSync"] = "Stop host and sync",
        ["Dashboard.CopyIp"] = "Copy IP",
        ["Dashboard.RefreshState"] = "Refresh state",
        ["Dashboard.Settings"] = "Settings",
        ["Dashboard.ViewLogs"] = "View logs",
        ["Dashboard.NoPublicIp"] = "No public IP is available right now.",
        ["Tray.StatusReady"] = "STATUS: READY",
        ["Tray.StatusFormat"] = "STATUS: {0} - {1}",
        ["Tray.OpenDashboard"] = "OPEN DASHBOARD",
        ["Tray.StartHost"] = "START AS HOST",
        ["Tray.StopHostUpload"] = "STOP HOST AND UPLOAD WORLD",
        ["Tray.CopyCurrentIp"] = "COPY CURRENT IP",
        ["Tray.RefreshState"] = "REFRESH STATE",
        ["Tray.Settings"] = "SETTINGS",
        ["Tray.ViewLogs"] = "VIEW LOGS",
        ["Tray.Exit"] = "EXIT",
        ["Tray.CompleteInitialConfig"] = "Complete the initial setup to get started.",
        ["Tray.WorldSyncedReleased"] = "World synced and host released.",
        ["Tray.NoPublicIp"] = "No public IP is available right now.",
        ["Tray.AddressCopiedFormat"] = "Address copied: {0}",
        ["Tray.ActiveHostSummaryFormat"] = "Active host: {0} {1}",
        ["Tray.NoActiveHostSummaryFormat"] = "No active host. Latest version: {0}",
        ["Tray.ConfigurationSaved"] = "Configuration saved.",
        ["Tray.ExitConfirmHosting"] = "Host is active. Exiting now will try to stop the server and upload the world. Continue?",
        ["LogWindow.Title"] = "MCSync logs",
        ["LogWindow.SystemActivity"] = "System activity",
        ["LogWindow.Copy"] = "Copy",
        ["LogWindow.Clear"] = "Clear",
        ["UserConfig.MissingGitHubOwner"] = "Missing GitHub repository owner.",
        ["UserConfig.MissingGitHubRepo"] = "Missing GitHub repository name.",
        ["UserConfig.MissingGitHubToken"] = "Missing GitHub token.",
        ["UserConfig.MissingServerJar"] = "server.jar was not found in the configured path.",
        ["UserConfig.MissingPlayitUrl"] = "Missing playit.gg URL.",
        ["UserConfig.MissingJavaPath"] = "Missing Java executable path.",
        ["Sync.AlreadyHosting"] = "The app is already hosting the world.",
        ["Sync.VerifyingRemoteState"] = "Checking remote state...",
        ["Sync.CannotAcquireHostRole"] = "Could not acquire host role.",
        ["Sync.PreparingServerFolder"] = "Preparing server folder...",
        ["Sync.StartingServerJar"] = "Starting server.jar...",
        ["Sync.OpeningPlayitTunnel"] = "Opening playit tunnel...",
        ["Sync.ServerReadyAwaitingTunnel"] = "Server ready. Waiting for tunnel address...",
        ["Sync.ServerReadyAtFormat"] = "Server ready at {0}",
        ["Sync.StoppingHostAndBlocking"] = "Stopping host and blocking new changes...",
        ["Sync.CompressingWorld"] = "Compressing world...",
        ["Sync.UploadingSnapshotVersionFormat"] = "Uploading snapshot version {0}...",
        ["Sync.WorldSyncedHostReleased"] = "World synced. Host released.",
        ["Sync.HostReleasedLog"] = "Host released and world synced successfully.",
        ["Sync.NoRemoteSnapshot"] = "No previous remote snapshot exists. A new world will be started.",
        ["Sync.LocalSnapshotUpToDate"] = "Local snapshot already matches the latest remote version.",
        ["Sync.DownloadingWorldVersionFormat"] = "Downloading world v{0}...",
        ["Sync.RemoteSnapshotAppliedFormat"] = "Remote snapshot v{0} downloaded and applied.",
        ["Sync.FailedStartCleanupWarningFormat"] = "Failed-start cleanup reported an error: {0}",
        ["Sync.ReleaseLeaseAfterFailureWarningFormat"] = "Could not release lease after failure: {0}",
        ["Sync.HostStartFailed"] = "Starting as host failed.",
        ["Sync.ServerProcessEndedAutoSync"] = "Server process ended. Starting automatic shutdown sync.",
        ["Sync.AutoCloseFailedAfterServerEnded"] = "Automatic shutdown failed after server process ended.",
        ["Sync.ServerEndedAutoSyncFailed"] = "Server ended and automatic sync failed.",
        ["Sync.CannotStopHostOnAppClose"] = "Could not stop host while closing the app."
    };

    public static UiLanguage CurrentLanguage => _language;

    public static void SetLanguage(UiLanguage language)
    {
        _language = language;
    }

    public static string Get(string key)
    {
        var source = _language == UiLanguage.English ? English : Spanish;
        if (source.TryGetValue(key, out var value))
        {
            return value;
        }

        if (Spanish.TryGetValue(key, out var spanishValue))
        {
            return spanishValue;
        }

        return key;
    }

    public static string Format(string key, params object?[] args) =>
        string.Format(CultureInfo.CurrentCulture, Get(key), args);
}
