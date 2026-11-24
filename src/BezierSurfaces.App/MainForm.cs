using System.Numerics;
using BezierSurfaces.Core.Files;
using BezierSurfaces.Core.Geometry;
using BezierSurfaces.Core.Model;

namespace BezierSurfaces.App;

// główne okno
// rysuje siatkę trójkątów na płachcie i wielobok kontrolny
public sealed class MainForm : Form
{
    private readonly Scene _scene = new();
    private readonly PictureBox _drawing = new();
    private readonly CheckBox _polygonsBox = new() { Text = "Wielobok kontrolny", Checked = true };
    private readonly CheckBox _meshBox = new() { Text = "Siatka trójkątów", Checked = true };
    private readonly TrackBar _divisionsBar = new() { Minimum = 1, Maximum = 40, Value = 8, TickFrequency = 4 };
    private readonly Label _divisionsLabel = new();

    /// ustawia okno, wczytuje powierzchnie i tnie je na trójkąty
    public MainForm()
    {
        Text = "Powierzchnie Béziera";
        ClientSize = new Size(1180, 700);
        MinimumSize = new Size(860, 480);
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Font;

        BuildLayout();
        LoadSurfaces();
        _scene.Divisions = _divisionsBar.Value;
        _scene.RebuildMesh();
        RefreshDivisionLabel();
    }

    /// dokłada pasek z suwakiem obok obszaru rysowania
    private void BuildLayout()
    {
        var panel = new Panel { Dock = DockStyle.Right, Width = 250 };
        _drawing.Name = "drawingSurface";
        _drawing.Dock = DockStyle.Fill;
        _drawing.BackColor = Color.White;
        _drawing.Paint += OnPaint;
        _drawing.Resize += (_, _) => _drawing.Invalidate();
        // najpierw obszar, potem pasek: pasek zabiera prawą krawędź, reszta zostaje dla rysunku
        Controls.Add(_drawing);
        Controls.Add(panel);

        int y = 12;
        Add(panel, new Label { Text = "Dokładność siatki" }, ref y, 22);
        Add(panel, _divisionsLabel, ref y, 22);
        Add(panel, _divisionsBar, ref y, 42);
        Add(panel, _polygonsBox, ref y, 28);
        Add(panel, _meshBox, ref y, 28);

        _divisionsBar.ValueChanged += (_, _) =>
        {
            _scene.Divisions = _divisionsBar.Value;
            RefreshDivisionLabel();
            _scene.RebuildMesh();
            _drawing.Invalidate();
        };
        _polygonsBox.CheckedChanged += (_, _) =>
        {
            _scene.Draw.ControlPolygons = _polygonsBox.Checked;
            _drawing.Invalidate();
        };
        _meshBox.CheckedChanged += (_, _) =>
        {
            _scene.Draw.TriangleMesh = _meshBox.Checked;
            _drawing.Invalidate();
        };
    }

    /// ustawia kontrolkę w pasku i schodzi niżej
    private static void Add(Panel panel, Control control, ref int y, int height)
    {
        control.Left = 12;
        control.Top = y;
        control.Width = 220;
        control.Height = height;
        panel.Controls.Add(control);
        y += height + 6;
    }

    /// wpisuje na pasek, na ile części podzielony jest przedział 0–1
    private void RefreshDivisionLabel()
    {
        _divisionsLabel.Text = $"Podział: {_divisionsBar.Value}";
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

    /// rysuje osie, trójkąty płata i wieloboki kontrolne
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
        if (_scene.Draw.TriangleMesh)
        {
            DrawMesh(e.Graphics, map);
        }

        if (_scene.Draw.ControlPolygons)
        {
            for (int index = 0; index < _scene.Surfaces.Count; index++)
            {
                DrawControlPolygon(e.Graphics, map, _scene.Surfaces[index].ControlPoints, ColorFor(index));
            }
        }
    }

    /// rysuje boki trójkątów leżących na płachcie
    private void DrawMesh(Graphics graphics, ScreenMap map)
    {
        using var first = new Pen(CurveColorFor(0), 1f);
        using var second = new Pen(CurveColorFor(1), 1f);
        foreach (MeshTriangle triangle in _scene.Mesh.Triangles)
        {
            Pen pen = triangle.SurfaceIndex % 2 == 0 ? first : second;
            DrawSegment(graphics, pen, map, triangle.A.Position, triangle.B.Position);
            DrawSegment(graphics, pen, map, triangle.B.Position, triangle.C.Position);
            DrawSegment(graphics, pen, map, triangle.C.Position, triangle.A.Position);
        }
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

    /// wybiera jaśniejszy kolor boków trójkątów
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
