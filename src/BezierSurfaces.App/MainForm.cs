namespace BezierSurfaces.App;

// główne okno
// picturebox jest przyszłym obszarem rysowania, na tym etapie nic nie liczy
public sealed class MainForm : Form
{
    public MainForm()
    {
        Text = "Powierzchnie Béziera";
        ClientSize = new Size(1000, 700);
        MinimumSize = new Size(640, 480);
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Font;

        var drawingSurface = new PictureBox
        {
            Name = "drawingSurface",
            Dock = DockStyle.Fill,
            BackColor = Color.White
        };

        Controls.Add(drawingSurface);
    }
}
