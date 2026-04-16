namespace MCSync.UI;

internal static class AppIconProvider
{
    private static readonly Icon CachedIcon = LoadIcon();

    public static Icon Icon => CachedIcon;

    private static Icon LoadIcon()
    {
        using var extracted = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        return extracted is null ? SystemIcons.Application : (Icon)extracted.Clone();
    }
}
