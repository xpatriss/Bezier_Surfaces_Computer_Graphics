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

        var box = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Font = Font,
            Text = HelpText.Body
        };
        Controls.Add(box);
    }
}
