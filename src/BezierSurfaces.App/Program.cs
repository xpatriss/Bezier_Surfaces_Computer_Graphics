namespace BezierSurfaces.App;

internal static class Program
{
    // punkt startowy aplikacji
    [STAThread]
    private static void Main()
    {
        // dpi i domyślna czcionka winforms
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}
