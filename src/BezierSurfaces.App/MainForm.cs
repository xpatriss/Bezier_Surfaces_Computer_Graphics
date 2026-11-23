using System.Numerics;
using BezierSurfaces.Core.Files;
using BezierSurfaces.Core.Geometry;
using BezierSurfaces.Core.Model;

namespace BezierSurfaces.App;

// główne okno
// na tym etapie rysuje wieloboki kontrolne, bez obrotu i bez siatki trójkątów
public sealed class MainForm : Form
{
    private readonly Scene _scene = new();
    private readonly PictureBox _drawing = new();

    public MainForm()
    {
        Text = "Powierzchnie Béziera";
        ClientSize = new Size(1000, 700);
        MinimumSize = new Size(640, 480);
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Font;

        _drawing.Name = "drawingSurface";
        _drawing.Dock = DockStyle.Fill;
        _drawing.BackColor = Color.White;
        _drawing.Paint += OnPaint;
        _drawing.Resize += (_, _) => _drawing.Invalidate();
        Controls.Add(_drawing);

        LoadSurfaces();
    }

    private void LoadSurfaces()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "data", "surfaces.txt");
        try
        {
            foreach (ControlGrid grid in SurfaceFileReader.Read(path))
            {
                _scene.Surfaces.Add(new BezierSurface(grid));
            }
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, "Plik powierzchni");
        }
    }

    private void OnPaint(object? sender, PaintEventArgs e)
    {
        int width = _drawing.ClientSize.Width;
        int height = _drawing.ClientSize.Height;
        if (width < 2 || height < 2)
        {
            return;
        }

        ScreenMap map = ScreenMap.Fit(width, height);
        DrawAxes(e.Graphics, map);
        for (int index = 0; index < _scene.Surfaces.Count; index++)
        {
            DrawControlPolygon(e.Graphics, map, _scene.Surfaces[index].ControlPoints, ColorFor(index));
        }
    }

    // szary krzyż pokazuje, że (0, 0) leży w środku obszaru
    private static void DrawAxes(Graphics graphics, ScreenMap map)
    {
        using var pen = new Pen(Color.FromArgb(180, 190, 190, 190), 1f);
        graphics.DrawLine(pen, 0, map.CenterY, map.Width, map.CenterY);
        graphics.DrawLine(pen, map.CenterX, 0, map.CenterX, map.Height);
    }

    private static void DrawControlPolygon(Graphics graphics, ScreenMap map, ControlGrid grid, Color color)
    {
        using var pen = new Pen(color, 1.6f);
        using var brush = new SolidBrush(color);
        for (int row = 0; row < 4; row++)
        {
            for (int column = 0; column < 3; column++)
            {
                DrawSegment(graphics, pen, map, grid[row, column], grid[row, column + 1]);
            }
        }

        for (int column = 0; column < 4; column++)
        {
            for (int row = 0; row < 3; row++)
            {
                DrawSegment(graphics, pen, map, grid[row, column], grid[row + 1, column]);
            }
        }

        for (int row = 0; row < 4; row++)
        {
            for (int column = 0; column < 4; column++)
            {
                Vector2 screen = map.ToScreen(grid[row, column]);
                graphics.FillRectangle(brush, screen.X - 3f, screen.Y - 3f, 6f, 6f);
            }
        }
    }

    // rzut prostokątny: do rysowania bierzemy x i y, z zostaje w punkcie
    private static void DrawSegment(Graphics graphics, Pen pen, ScreenMap map, Vector3 start, Vector3 end)
    {
        Vector2 a = map.ToScreen(start);
        Vector2 b = map.ToScreen(end);
        graphics.DrawLine(pen, a.X, a.Y, b.X, b.Y);
    }

    private static Color ColorFor(int surfaceIndex)
    {
        return surfaceIndex % 2 == 0 ? Color.DarkGreen : Color.DarkBlue;
    }
}
