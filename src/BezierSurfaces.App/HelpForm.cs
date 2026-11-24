namespace BezierSurfaces.App;

/// okno z instrukcją obsługi
sealed class HelpForm : Form
{
    /// pokazuje tekst pomocy tylko do czytania
    public HelpForm()
    {
        Text = "Dokumentacja";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(720, 640);
        MinimizeBox = false;
        Font = new Font("Segoe UI", 10f);
        Padding = new Padding(12);

        var box = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Font = Font,
            BackColor = SystemColors.Window,
            Text = HelpText.Body.ReplaceLineEndings()
        };
        Controls.Add(box);
        Shown += (_, _) =>
        {
            box.Select(0, 0);
            box.ScrollToCaret();
        };
    }
}
