namespace MCSync.UI;

internal enum NothingButtonVariant
{
    Primary,
    Secondary,
    Destructive,
    Ghost
}

internal static class NothingTheme
{
    public static readonly Color Black = Color.FromArgb(0, 0, 0);
    public static readonly Color Surface = Color.FromArgb(17, 17, 17);
    public static readonly Color SurfaceRaised = Color.FromArgb(26, 26, 26);
    public static readonly Color Border = Color.FromArgb(34, 34, 34);
    public static readonly Color BorderVisible = Color.FromArgb(51, 51, 51);
    public static readonly Color TextDisabled = Color.FromArgb(102, 102, 102);
    public static readonly Color TextSecondary = Color.FromArgb(153, 153, 153);
    public static readonly Color TextPrimary = Color.FromArgb(232, 232, 232);
    public static readonly Color TextDisplay = Color.FromArgb(255, 255, 255);
    public static readonly Color Accent = Color.FromArgb(215, 25, 33);
    public static readonly Color Success = Color.FromArgb(74, 158, 92);
    public static readonly Color Warning = Color.FromArgb(212, 168, 67);

    private static readonly string UiFontFamily = ResolveFontFamily("Space Grotesk", "Segoe UI", FontFamily.GenericSansSerif.Name);
    private static readonly string MonoFontFamily = ResolveFontFamily("Space Mono", "Consolas", FontFamily.GenericMonospace.Name);
    private static readonly string DisplayFontFamily = ResolveFontFamily("Doto", "Space Mono", "Consolas");

    public static Font Ui(float size, FontStyle style = FontStyle.Regular) =>
        new(UiFontFamily, size, style, GraphicsUnit.Point);

    public static Font Mono(float size, FontStyle style = FontStyle.Regular) =>
        new(MonoFontFamily, size, style, GraphicsUnit.Point);

    public static Font Display(float size, FontStyle style = FontStyle.Regular) =>
        new(DisplayFontFamily, size, style, GraphicsUnit.Point);

    public static void StyleForm(Form form)
    {
        form.BackColor = Black;
        form.ForeColor = TextPrimary;
        form.Font = Ui(10F);
        form.Icon = AppIconProvider.Icon;
    }

    public static void StyleCard(Control control, int padding = 16)
    {
        control.BackColor = Surface;
        control.Padding = new Padding(padding);
        control.Paint += (_, e) =>
        {
            var rect = control.ClientRectangle;
            rect.Width -= 1;
            rect.Height -= 1;
            using var pen = new Pen(BorderVisible);
            e.Graphics.DrawRectangle(pen, rect);
        };
    }

    public static void StyleInput(TextBox textBox, bool useMono = false)
    {
        textBox.BorderStyle = BorderStyle.FixedSingle;
        textBox.BackColor = SurfaceRaised;
        textBox.ForeColor = TextDisplay;
        textBox.Font = useMono ? Mono(10F) : Ui(10F);
    }

    public static void StyleButton(Button button, NothingButtonVariant variant)
    {
        button.Text = button.Text.ToUpperInvariant();
        button.Height = Math.Max(button.Height, 44);
        button.FlatStyle = FlatStyle.Flat;
        button.Cursor = Cursors.Hand;
        button.Font = Mono(10F);

        switch (variant)
        {
            case NothingButtonVariant.Primary:
                button.BackColor = TextDisplay;
                button.ForeColor = Black;
                button.FlatAppearance.BorderSize = 0;
                button.FlatAppearance.MouseOverBackColor = Color.FromArgb(236, 236, 236);
                button.FlatAppearance.MouseDownBackColor = Color.FromArgb(210, 210, 210);
                break;
            case NothingButtonVariant.Destructive:
                button.BackColor = Black;
                button.ForeColor = Accent;
                button.FlatAppearance.BorderSize = 1;
                button.FlatAppearance.BorderColor = Accent;
                button.FlatAppearance.MouseOverBackColor = Color.FromArgb(22, 0, 0);
                button.FlatAppearance.MouseDownBackColor = Color.FromArgb(40, 0, 0);
                break;
            case NothingButtonVariant.Ghost:
                button.BackColor = Black;
                button.ForeColor = TextSecondary;
                button.FlatAppearance.BorderSize = 0;
                button.FlatAppearance.MouseOverBackColor = SurfaceRaised;
                button.FlatAppearance.MouseDownBackColor = Border;
                break;
            default:
                button.BackColor = Black;
                button.ForeColor = TextPrimary;
                button.FlatAppearance.BorderSize = 1;
                button.FlatAppearance.BorderColor = BorderVisible;
                button.FlatAppearance.MouseOverBackColor = SurfaceRaised;
                button.FlatAppearance.MouseDownBackColor = Border;
                break;
        }
    }

    public static Label CreateMetaLabel(string text, int height = 20)
    {
        return new Label
        {
            Text = text.ToUpperInvariant(),
            Dock = DockStyle.Top,
            Height = height,
            Font = Mono(9F),
            ForeColor = TextSecondary
        };
    }

    private static string ResolveFontFamily(params string[] candidates)
    {
        foreach (var candidate in candidates)
        {
            if (FontFamily.Families.Any(f => string.Equals(f.Name, candidate, StringComparison.OrdinalIgnoreCase)))
            {
                return candidate;
            }
        }

        return FontFamily.GenericSansSerif.Name;
    }
}
