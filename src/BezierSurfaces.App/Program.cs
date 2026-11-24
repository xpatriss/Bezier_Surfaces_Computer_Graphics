using BezierSurfaces.Core.Files;
using BezierSurfaces.Core.Model;

namespace BezierSurfaces.App;

internal static class Program
{
    /// uruchamia okno aplikacji
    [STAThread]
    private static void Main()
    {
        // dpi i domyślna czcionka winforms
        ApplicationConfiguration.Initialize();
        string path = Path.Combine(AppContext.BaseDirectory, "data", "surfaces.txt");
        List<ControlGrid> grids;
        try
        {
            grids = SurfaceFileReader.Read(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or FormatException)
        {
            MessageBox.Show($"Nie udało się wczytać powierzchni.\n{path}\n\n{exception.Message}",
                "Powierzchnie Béziera", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        Application.Run(new MainForm(grids));
    }
}
