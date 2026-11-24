using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Numerics;
using BezierSurfaces.Core.Files;
using BezierSurfaces.Core.Geometry;
using BezierSurfaces.Core.Lighting;
using BezierSurfaces.Core.Model;
using BezierSurfaces.Core.Raster;

namespace BezierSurfaces.App;

// główne okno
// rysuje wypełnienie, siatkę trójkątów, wielobok kontrolny i obraca scenę myszą
public sealed class MainForm : Form
{
    private readonly Scene _scene = new();
    private readonly PictureBox _drawing = new SmoothCanvas();
    private readonly EdgeBucketRasterizer _rasterizer = new(800);
    private readonly FrameBuffer _frame = new();
    private Bitmap? _bitmap;
    private bool _turning;
    private Point _lastMouse;
    private readonly CheckBox _polygonsBox = new() { Text = "Wielobok kontrolny", Checked = true };
    private readonly CheckBox _meshBox = new() { Text = "Siatka trójkątów", Checked = true };
    private readonly CheckBox _filledBox = new() { Text = "Wypełnione trójkąty", Checked = true };
    private readonly TrackBar _divisionsBar = new()
    {
        Minimum = 1,
        Maximum = 40,
        Value = 8,
        TickFrequency = 4,
        TickStyle = TickStyle.None
    };
    private readonly Label _divisionsLabel = new();
    private readonly Label _diffuseLabel = new() { AutoSize = true };
    private readonly Label _specularLabel = new() { AutoSize = true };
    private readonly Label _shininessLabel = new() { AutoSize = true };
    private readonly TrackBar _diffuseBar = new() { Minimum = 0, Maximum = 100, Value = 75, TickStyle = TickStyle.None, AutoSize = true };
    private readonly TrackBar _specularBar = new() { Minimum = 0, Maximum = 100, Value = 35, TickStyle = TickStyle.None, AutoSize = true };
    private readonly TrackBar _shininessBar = new() { Minimum = 1, Maximum = 100, Value = 24, TickStyle = TickStyle.None, AutoSize = true };
    private readonly Button _resetButton = new() { Text = "Widok z góry" };

    private static readonly Color PageColor = Color.White;
    private static readonly Color PanelColor = Color.FromArgb(243, 244, 246);
    private static readonly Color LineColor = Color.FromArgb(209, 213, 219);
    private static readonly Color InkColor = Color.FromArgb(17, 24, 39);
    private static readonly Color MutedColor = Color.FromArgb(75, 85, 99);

    /// ustawia okno, wczytuje powierzchnie i tnie je na trójkąty
    public MainForm()
    {
        Text = "Powierzchnie Béziera";
        Font = new Font("Segoe UI", 10f);
        BackColor = PageColor;
        ClientSize = new Size(1100, 700);
        MinimumSize = new Size(860, 520);
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Font;

        BuildLayout();
        LoadSurfaces();
        _scene.Divisions = _divisionsBar.Value;
        ApplyShadingFromUi();
        _scene.RebuildMesh();
        RefreshDivisionLabel();
    }

    /// układa biały obszar rysowania i szary panel, jak w edytorze wielokątów
    private void BuildLayout()
    {
        var page = new Panel { Dock = DockStyle.Fill, BackColor = PageColor, Padding = new Padding(16) };
        var panel = new Panel { Dock = DockStyle.Right, Width = 270, BackColor = PanelColor };
        var stack = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            BackColor = PanelColor,
            Padding = new Padding(16, 16, 16, 16)
        };
        panel.Controls.Add(stack);
        var columnGap = new Panel { Dock = DockStyle.Right, Width = 12, BackColor = PageColor };
        var frame = new Panel { Dock = DockStyle.Fill, BackColor = LineColor, Padding = new Padding(1) };

        _drawing.Name = "drawingSurface";
        _drawing.Dock = DockStyle.Fill;
        _drawing.BackColor = PageColor;
        _drawing.Paint += OnPaint;
        _drawing.Resize += (_, _) => _drawing.Invalidate();
        _drawing.MouseDown += OnMouseDown;
        _drawing.MouseMove += OnMouseMove;
        _drawing.MouseUp += (_, _) => EndTurn();
        frame.Controls.Add(_drawing);

        page.Controls.Add(frame);
        page.Controls.Add(columnGap);
        page.Controls.Add(panel);
        Controls.Add(page);

        _divisionsLabel.ForeColor = MutedColor;
        _divisionsLabel.BackColor = PanelColor;
        _divisionsLabel.AutoSize = true;
        _diffuseLabel.ForeColor = MutedColor;
        _diffuseLabel.BackColor = PanelColor;
        _specularLabel.ForeColor = MutedColor;
        _specularLabel.BackColor = PanelColor;
        _shininessLabel.ForeColor = MutedColor;
        _shininessLabel.BackColor = PanelColor;
        _divisionsBar.BackColor = PanelColor;
        _divisionsBar.AutoSize = true;
        _diffuseBar.BackColor = PanelColor;
        _specularBar.BackColor = PanelColor;
        _shininessBar.BackColor = PanelColor;
        StyleOption(_polygonsBox);
        StyleOption(_meshBox);
        StyleOption(_filledBox);
        _resetButton.Font = Font;
        _resetButton.FlatStyle = FlatStyle.Flat;
        _resetButton.BackColor = PageColor;
        _resetButton.ForeColor = InkColor;
        _resetButton.TextAlign = ContentAlignment.MiddleCenter;
        _resetButton.Padding = new Padding(8, 10, 8, 14);
        _resetButton.Height = ButtonHeight(_resetButton);
        _resetButton.FlatAppearance.BorderColor = LineColor;
        _resetButton.FlatAppearance.BorderSize = 1;

        var hint = Hint("Lewy przycisk przechyla. Prawy kręci w płaszczyźnie ekranu.");
        AddRow(stack, Heading("Dokładność siatki"));
        AddRow(stack, _divisionsLabel);
        AddRow(stack, _divisionsBar);
        AddRow(stack, Rule());
        AddRow(stack, Heading("Widok"));
        AddRow(stack, _polygonsBox);
        AddRow(stack, _meshBox);
        AddRow(stack, _filledBox);
        AddRow(stack, Rule());
        AddRow(stack, Heading("Światło"));
        AddRow(stack, _diffuseLabel);
        AddRow(stack, _diffuseBar);
        AddRow(stack, _specularLabel);
        AddRow(stack, _specularBar);
        AddRow(stack, _shininessLabel);
        AddRow(stack, _shininessBar);
        AddRow(stack, _resetButton);
        AddRow(stack, Rule());
        AddRow(stack, hint);
        panel.Resize += (_, _) => FitStack(stack, hint);
        FitStack(stack, hint);

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
        _filledBox.CheckedChanged += (_, _) =>
        {
            _scene.Draw.FilledTriangles = _filledBox.Checked;
            _drawing.Invalidate();
        };
        _diffuseBar.ValueChanged += (_, _) =>
        {
            ApplyShadingFromUi();
            _drawing.Invalidate();
        };
        _specularBar.ValueChanged += (_, _) =>
        {
            ApplyShadingFromUi();
            _drawing.Invalidate();
        };
        _shininessBar.ValueChanged += (_, _) =>
        {
            ApplyShadingFromUi();
            _drawing.Invalidate();
        };
        _resetButton.Click += (_, _) =>
        {
            _scene.ReplaceOrientation(Matrix3.Identity);
            _drawing.Invalidate();
        };
    }

    /// dokłada kontrolkę pod poprzednią, z odstępem
    private static void AddRow(FlowLayoutPanel stack, Control control)
    {
        control.Margin = new Padding(0, 0, 0, 12);
        stack.Controls.Add(control);
    }

    /// rozciąga wiersze na szerokość panelu, bez zmiany ich wysokości
    private static void FitStack(FlowLayoutPanel stack, Label hint)
    {
        int width = stack.ClientSize.Width - stack.Padding.Horizontal;
        if (width < 40)
        {
            return;
        }

        hint.MaximumSize = new Size(width, 0);
        foreach (Control control in stack.Controls)
        {
            if (control is Label { AutoSize: true })
            {
                continue;
            }

            control.Width = width;
            if (control is Button button)
            {
                button.Height = ButtonHeight(button);
            }
        }
    }

    /// wysokość przycisku z zapasem pod ogonki liter
    // sam pomiar tekstu obcina dół g i y na płaskim przycisku
    private static int ButtonHeight(Button button)
    {
        int textHeight = TextRenderer.MeasureText(button.Text, button.Font).Height;
        return textHeight + button.Padding.Vertical + 16;
    }

    /// nagłówek sekcji w panelu
    private static Label Heading(string text)
    {
        return new Label
        {
            Text = text,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            ForeColor = InkColor,
            BackColor = PanelColor,
            AutoSize = true
        };
    }

    /// szary tekst podpowiedzi
    private static Label Hint(string text)
    {
        return new Label
        {
            Text = text,
            Font = new Font("Segoe UI", 9f),
            ForeColor = MutedColor,
            BackColor = PanelColor,
            AutoSize = true
        };
    }

    /// cienka linia między sekcjami
    private static Panel Rule()
    {
        return new Panel { BackColor = LineColor, Height = 1 };
    }

    /// ustawia pole wyboru na kolor panelu
    private static void StyleOption(CheckBox box)
    {
        box.BackColor = PanelColor;
        box.ForeColor = InkColor;
        box.AutoSize = true;
    }

    /// przepisuje suwaki kd, ks i m na scenę
    private void ApplyShadingFromUi()
    {
        _scene.DiffuseWeight = _diffuseBar.Value / 100f;
        _scene.SpecularWeight = _specularBar.Value / 100f;
        _scene.Shininess = _shininessBar.Value;
        _diffuseLabel.Text = $"kd: {_scene.DiffuseWeight:0.00}";
        _specularLabel.Text = $"ks: {_scene.SpecularWeight:0.00}";
        _shininessLabel.Text = $"m: {_shininessBar.Value}";
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
            List<ControlGrid> grids = SurfaceFileReader.Read(path);
            var colors = new[]
            {
                new Vector3Color(0.22f, 0.55f, 0.38f),
                new Vector3Color(0.25f, 0.40f, 0.75f)
            };
            for (int index = 0; index < grids.Count; index++)
            {
                _scene.Surfaces.Add(new BezierSurface(grids[index], colors[index % colors.Length]));
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
        if (_scene.Draw.FilledTriangles)
        {
            DrawFilled(e.Graphics, map, width, height);
        }

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        DrawAxes(e.Graphics, map);
        if (_scene.Draw.TriangleMesh)
        {
            DrawMesh(e.Graphics, map);
        }

        if (_scene.Draw.ControlPolygons)
        {
            for (int index = 0; index < _scene.Surfaces.Count; index++)
            {
                DrawControlPolygon(
                    e.Graphics,
                    map,
                    _scene.Surfaces[index].ControlPoints,
                    _scene.Orientation,
                    ColorFor(index));
            }
        }
    }

    /// wypełnia trójkąty do bitmapy i kładzie ją pod liniami
    // kolor liczy wzór światła w każdym pikselu, o wygranej decyduje większe z
    private void DrawFilled(Graphics graphics, ScreenMap map, int width, int height)
    {
        _frame.Resize(width, height);
        _frame.Clear(unchecked((int)0xFFFFFFFF));
        _rasterizer.DrawMesh(_scene, map, _frame, _scene.LightPosition);
        if (_bitmap is null || _bitmap.Width != width || _bitmap.Height != height)
        {
            _bitmap?.Dispose();
            _bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        }

        var bits = _bitmap.LockBits(
            new Rectangle(0, 0, width, height),
            ImageLockMode.WriteOnly,
            PixelFormat.Format32bppArgb);
        try
        {
            _frame.CopyTo(bits.Scan0, bits.Stride);
        }
        finally
        {
            _bitmap.UnlockBits(bits);
        }

        graphics.DrawImageUnscaled(_bitmap, 0, 0);
    }

    /// rysuje boki trójkątów leżących na płachcie
    private void DrawMesh(Graphics graphics, ScreenMap map)
    {
        using var first = new Pen(CurveColorFor(0), 1f);
        using var second = new Pen(CurveColorFor(1), 1f);
        foreach (MeshTriangle triangle in _scene.Mesh.Triangles)
        {
            Pen pen = triangle.SurfaceIndex % 2 == 0 ? first : second;
            DrawSegment(graphics, pen, map, triangle.A.PositionView, triangle.B.PositionView);
            DrawSegment(graphics, pen, map, triangle.B.PositionView, triangle.C.PositionView);
            DrawSegment(graphics, pen, map, triangle.C.PositionView, triangle.A.PositionView);
        }
    }

    /// rysuje szary krzyż osi przez środek okna
    private static void DrawAxes(Graphics graphics, ScreenMap map)
    {
        using var pen = new Pen(Color.FromArgb(180, 190, 190, 190), 1f);
        graphics.DrawLine(pen, 0, map.CenterY, map.Width, map.CenterY);
        graphics.DrawLine(pen, map.CenterX, 0, map.CenterX, map.Height);
    }

    /// zaczyna obrót, gdy lewy albo prawy przycisk idzie w dół
    private void OnMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button is not (MouseButtons.Left or MouseButtons.Right))
        {
            return;
        }

        _turning = true;
        _lastMouse = e.Location;
        _drawing.Capture = true;
    }

    /// dokłada mały obrót od przesunięcia myszy i odrysowuje scenę
    private void OnMouseMove(object? sender, MouseEventArgs e)
    {
        if (!_turning)
        {
            return;
        }

        float dx = e.X - _lastMouse.X;
        float dy = e.Y - _lastMouse.Y;
        _lastMouse = e.Location;
        if (dx == 0f && dy == 0f)
        {
            return;
        }

        TurnAxis axis = (Control.MouseButtons & MouseButtons.Right) != 0
            ? TurnAxis.ScreenZ
            : TurnAxis.ScreenXAndY;
        Matrix3 increment = TurntableRotation.Increment(dx, dy, axis);
        // z lewej, bo nowy ruch jest względem ekranu, a nie względem już obróconej bryły
        _scene.ReplaceOrientation(Matrix3.Multiply(increment, _scene.Orientation));
        _drawing.Invalidate();
    }

    /// kończy obrót po puszczeniu przycisku
    private void EndTurn()
    {
        _turning = false;
        _drawing.Capture = false;
    }

    /// łączy sąsiednie punkty kontrolne i zaznacza same punkty
    private static void DrawControlPolygon(
        Graphics graphics,
        ScreenMap map,
        ControlGrid grid,
        Matrix3 orientation,
        Color color)
    {
        using var pen = new Pen(color, 1.6f);
        using var brush = new SolidBrush(color);
        for (int row = 0; row < 4; row++)
        {
            for (int column = 0; column < 3; column++)
            {
                DrawSegment(
                    graphics,
                    pen,
                    map,
                    orientation.Transform(grid[row, column]),
                    orientation.Transform(grid[row, column + 1]));
            }
        }

        for (int column = 0; column < 4; column++)
        {
            for (int row = 0; row < 3; row++)
            {
                DrawSegment(
                    graphics,
                    pen,
                    map,
                    orientation.Transform(grid[row, column]),
                    orientation.Transform(grid[row + 1, column]));
            }
        }

        for (int row = 0; row < 4; row++)
        {
            for (int column = 0; column < 4; column++)
            {
                Vector2 screen = map.ToScreen(orientation.Transform(grid[row, column]));
                graphics.FillRectangle(brush, screen.X - 3f, screen.Y - 3f, 6f, 6f);
            }
        }
    }

    /// rysuje jeden odcinek między dwoma już obróconymi punktami
    // rzut prostokątny: na ekran idą x i y, z po obrocie już siedzi w tych dwóch liczbach
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

    /// obszar rysowania bez migotania przy zmianie rozmiaru
    private sealed class SmoothCanvas : PictureBox
    {
        public SmoothCanvas()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
        }
    }

    /// zwalnia bitmapę wypełnienia razem z oknem
    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _bitmap?.Dispose();
        base.OnFormClosed(e);
    }
}
