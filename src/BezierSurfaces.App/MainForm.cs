using System.Numerics;
using BezierSurfaces.Core.Bezier;
using BezierSurfaces.Core.Files;
using BezierSurfaces.Core.Geometry;
using BezierSurfaces.Core.Model;

namespace BezierSurfaces.App;

// główne okno
// rysuje wielobok kontrolny i linie stałego u oraz v na powierzchni
public sealed class MainForm : Form
{
    private readonly Scene _scene = new();
    private readonly PictureBox _drawing = new();

    // ile linii w każdym kierunku i z ilu odcinków składa się jedna linia
    private const int ParameterLineCount = 8;
    private const int SamplesPerLine = 24;

    /// ustawia okno i wczytuje powierzchnie
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

    /// czyta plik startowy i dokłada siatki do sceny
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

    /// rysuje osie, linie powierzchni i wieloboki kontrolne
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
            ControlGrid grid = _scene.Surfaces[index].ControlPoints;
            DrawParameterLines(e.Graphics, map, grid, CurveColorFor(index));
            DrawControlPolygon(e.Graphics, map, grid, ColorFor(index));
        }
    }

    /// rysuje linie stałego u i stałego v, złożone z punktów powierzchni
    private static void DrawParameterLines(Graphics graphics, ScreenMap map, ControlGrid grid, Color color)
    {
        using var pen = new Pen(color, 1.2f);
        for (int line = 0; line <= ParameterLineCount; line++)
        {
            float fixedParameter = line / (float)ParameterLineCount;
            DrawCurve(graphics, pen, map, grid, fixedParameter, constantU: true);
            DrawCurve(graphics, pen, map, grid, fixedParameter, constantU: false);
        }
    }

    /// idzie wzdłuż jednej linii parametru i łączy kolejne punkty odcinkami
    private static void DrawCurve(
        Graphics graphics,
        Pen pen,
        ScreenMap map,
        ControlGrid grid,
        float fixedParameter,
        bool constantU)
    {
        Vector3 previous = Sample(grid, fixedParameter, 0f, constantU);
        for (int sample = 1; sample <= SamplesPerLine; sample++)
        {
            float moving = sample / (float)SamplesPerLine;
            Vector3 next = Sample(grid, fixedParameter, moving, constantU);
            DrawSegment(graphics, pen, map, previous, next);
            previous = next;
        }
    }

    /// bierze punkt płata przy ustalonym u albo przy ustalonym v
    private static Vector3 Sample(ControlGrid grid, float fixedParameter, float movingParameter, bool constantU)
    {
        if (constantU)
        {
            return BezierPatch.Point(grid, fixedParameter, movingParameter);
        }

        return BezierPatch.Point(grid, movingParameter, fixedParameter);
    }
    /// rysuje szary krzyż osi przez środek okna
    private static void DrawAxes(Graphics graphics, ScreenMap map)
    {
        using var pen = new Pen(Color.FromArgb(180, 190, 190, 190), 1f);
        graphics.DrawLine(pen, 0, map.CenterY, map.Width, map.CenterY);
        graphics.DrawLine(pen, map.CenterX, 0, map.CenterX, map.Height);
    }

    /// łączy sąsiednie punkty kontrolne i zaznacza same punkty
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

    /// rysuje jeden odcinek między dwoma punktami sceny
    // rzut prostokątny: do rysowania bierzemy x i y, z zostaje w punkcie
    private static void DrawSegment(Graphics graphics, Pen pen, ScreenMap map, Vector3 start, Vector3 end)
    {
        Vector2 a = map.ToScreen(start);
        Vector2 b = map.ToScreen(end);
        graphics.DrawLine(pen, a.X, a.Y, b.X, b.Y);
    }

    /// wybiera jaśniejszy kolor linii powierzchni
    private static Color CurveColorFor(int surfaceIndex)
    {
        return surfaceIndex % 2 == 0 ? Color.SeaGreen : Color.RoyalBlue;
    }

    /// wybiera kolor wieloboku: zielony dla pierwszej powierzchni, niebieski dla drugiej
    private static Color ColorFor(int surfaceIndex)
    {
        return surfaceIndex % 2 == 0 ? Color.DarkGreen : Color.DarkBlue;
    }
}
