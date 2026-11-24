using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Numerics;
using BezierSurfaces.Core.Bezier;
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
    private readonly System.Windows.Forms.Timer _clock = new();
    private Bitmap? _bitmap;
    private bool _updatingUi;
    private bool _turning;
    private bool _draggingLight;
    private int _lightPointIndex = -1;
    private int _selectedSurface;
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
    private readonly TrackBar _lightZBar = new() { Minimum = -200, Maximum = 200, Value = 100, TickStyle = TickStyle.None, AutoSize = true };
    private readonly Label _lightZLabel = new() { AutoSize = true };
    private readonly CheckBox _animateBox = new() { Text = "Animuj światło" };
    private readonly CheckBox _normalMapBox = new() { Text = "Mapa normalnych" };
    private readonly ComboBox _surfaceList = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly RadioButton _solidRadio = new() { Text = "Kolor jednolity", Checked = true };
    private readonly RadioButton _textureRadio = new() { Text = "Tekstura" };
    private readonly Button _resetButton = new() { Text = "Widok z góry" };
    private readonly Button _objectColorButton = new() { Text = "Kolor obiektu" };
    private readonly Button _textureButton = new() { Text = "Wczytaj teksturę" };
    private readonly Button _normalButton = new() { Text = "Wczytaj mapę" };
    private readonly Button _lightColorButton = new() { Text = "Kolor światła" };
    private readonly Button _helpButton = new() { Text = "Pomoc" };

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
        KeyPreview = true;

        BuildLayout();
        LoadSurfaces();
        _scene.Divisions = _divisionsBar.Value;
        ApplyShadingFromUi();
        ApplyLightZFromUi();
        _scene.RebuildMesh();
        RefreshDivisionLabel();
        _clock.Interval = 30;
        _clock.Tick += (_, _) => AdvanceLight();
        KeyDown += OnKeyDown;
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
        _lightZLabel.ForeColor = MutedColor;
        _lightZLabel.BackColor = PanelColor;
        _divisionsBar.BackColor = PanelColor;
        _divisionsBar.AutoSize = true;
        _diffuseBar.BackColor = PanelColor;
        _specularBar.BackColor = PanelColor;
        _shininessBar.BackColor = PanelColor;
        _lightZBar.BackColor = PanelColor;
        StyleOption(_polygonsBox);
        StyleOption(_meshBox);
        StyleOption(_filledBox);
        StyleOption(_animateBox);
        StyleOption(_normalMapBox);
        StyleOption(_solidRadio);
        StyleOption(_textureRadio);
        _surfaceList.Font = Font;
        _surfaceList.BackColor = PageColor;
        StyleFlat(_resetButton);
        StyleFlat(_objectColorButton);
        StyleFlat(_textureButton);
        StyleFlat(_normalButton);
        StyleFlat(_lightColorButton);
        StyleFlat(_helpButton);

        var hint = Hint("Lewy przycisk przechyla. Prawy kręci w płaszczyźnie ekranu. Pomarańczowe punkty da się ciągnąć. Spacja włącza animację. F1 otwiera pomoc.");
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
        AddRow(stack, _lightZLabel);
        AddRow(stack, _lightZBar);
        AddRow(stack, _animateBox);
        AddRow(stack, _lightColorButton);
        AddRow(stack, _resetButton);
        AddRow(stack, Rule());
        AddRow(stack, Heading("Powierzchnia"));
        AddRow(stack, _surfaceList);
        AddRow(stack, _solidRadio);
        AddRow(stack, _textureRadio);
        AddRow(stack, _objectColorButton);
        AddRow(stack, _textureButton);
        AddRow(stack, Rule());
        AddRow(stack, _normalMapBox);
        AddRow(stack, _normalButton);
        AddRow(stack, _helpButton);
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
        _lightZBar.ValueChanged += (_, _) =>
        {
            ApplyLightZFromUi();
            _drawing.Invalidate();
        };
        _animateBox.CheckedChanged += (_, _) => _clock.Enabled = _animateBox.Checked;
        _resetButton.Click += (_, _) =>
        {
            _scene.ReplaceOrientation(Matrix3.Identity);
            _drawing.Invalidate();
        };
        _surfaceList.SelectedIndexChanged += (_, _) =>
        {
            if (_surfaceList.SelectedIndex >= 0)
            {
                _selectedSurface = _surfaceList.SelectedIndex;
                ShowSelectedAppearance();
            }
        };
        _solidRadio.CheckedChanged += (_, _) => ApplyColorMode();
        _textureRadio.CheckedChanged += (_, _) => ApplyColorMode();
        _objectColorButton.Click += (_, _) => ChooseObjectColor();
        _textureButton.Click += (_, _) => LoadTexture();
        _normalMapBox.CheckedChanged += (_, _) =>
        {
            _scene.UseNormalMap = _normalMapBox.Checked;
            _drawing.Invalidate();
        };
        _normalButton.Click += (_, _) => LoadNormalMap();
        _lightColorButton.Click += (_, _) => ChooseLightColor();
        _helpButton.Click += (_, _) => ShowHelp();
    }

    /// płaski biały przycisk z szarą ramką i zapasem pod ogonki
    private static void StyleFlat(Button button)
    {
        button.Font = new Font("Segoe UI", 10f);
        button.FlatStyle = FlatStyle.Flat;
        button.BackColor = PageColor;
        button.ForeColor = InkColor;
        button.TextAlign = ContentAlignment.MiddleCenter;
        button.Padding = new Padding(8, 10, 8, 14);
        button.Height = ButtonHeight(button);
        button.FlatAppearance.BorderColor = LineColor;
        button.FlatAppearance.BorderSize = 1;
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

    /// ustawia pole wyboru albo radio na kolor panelu
    private static void StyleOption(ButtonBase box)
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

    /// przesuwa całą krzywą światła na wysokość z suwaka
    // suwak ma zakres −200…200, a scena liczy w jednostkach, stąd dzielenie przez 100
    private void ApplyLightZFromUi()
    {
        _scene.LightPath.SetZ(_lightZBar.Value / 100f);
        _lightZLabel.Text = $"z światła: {_scene.LightPath.Z:0.00}";
    }

    /// posuwa lampę wzdłuż krzywej i zawija parametr po pełnym obiegu
    private void AdvanceLight()
    {
        _scene.LightParameter += 0.008f;
        if (_scene.LightParameter > 1f)
        {
            _scene.LightParameter -= 1f;
        }

        _drawing.Invalidate();
    }

    /// spacja włącza i wyłącza animację lampy
    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Space)
        {
            _animateBox.Checked = !_animateBox.Checked;
        }
        else if (e.KeyCode == Keys.F1)
        {
            ShowHelp();
        }
    }

    /// pokazuje instrukcję w osobnym oknie
    private void ShowHelp()
    {
        using var help = new HelpForm();
        help.ShowDialog(this);
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
                var appearance = new SurfaceAppearance
                {
                    SolidColor = colors[index % colors.Length],
                    Texture = DefaultImages.BrickColor
                };
                _scene.Surfaces.Add(new BezierSurface(grids[index], appearance));
                _surfaceList.Items.Add($"Powierzchnia {index + 1}");
            }
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, "Plik powierzchni");
        }

        _scene.NormalMap = DefaultImages.BrickNormal;
        _lightColorButton.BackColor = BitmapImages.ToMedia(_scene.LightColor);
        if (_surfaceList.Items.Count > 0)
        {
            _surfaceList.SelectedIndex = 0;
        }

        ShowSelectedAppearance();
    }

    /// ustawia radio i tło przycisku według wybranej powierzchni
    private void ShowSelectedAppearance()
    {
        if (_selectedSurface < 0 || _selectedSurface >= _scene.Surfaces.Count)
        {
            return;
        }

        SurfaceAppearance appearance = _scene.Surfaces[_selectedSurface].Appearance;
        _updatingUi = true;
        _solidRadio.Checked = !appearance.UseTexture;
        _textureRadio.Checked = appearance.UseTexture;
        _objectColorButton.BackColor = BitmapImages.ToMedia(appearance.SolidColor);
        _updatingUi = false;
    }

    /// przełącza wybraną powierzchnię między kolorem a teksturą
    private void ApplyColorMode()
    {
        if (_updatingUi || _selectedSurface >= _scene.Surfaces.Count)
        {
            return;
        }

        _scene.Surfaces[_selectedSurface].Appearance.UseTexture = _textureRadio.Checked;
        _drawing.Invalidate();
    }

    /// zmienia kolor światła il
    private void ChooseLightColor()
    {
        using var dialog = new ColorDialog { Color = _lightColorButton.BackColor, FullOpen = true };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        _scene.LightColor = new Vector3Color(dialog.Color.R / 255f, dialog.Color.G / 255f, dialog.Color.B / 255f);
        _lightColorButton.BackColor = dialog.Color;
        _drawing.Invalidate();
    }

    /// zmienia jednolity kolor wybranej powierzchni i wyłącza teksturę
    private void ChooseObjectColor()
    {
        if (_selectedSurface >= _scene.Surfaces.Count)
        {
            return;
        }

        using var dialog = new ColorDialog { Color = _objectColorButton.BackColor, FullOpen = true };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        _scene.Surfaces[_selectedSurface].Appearance.SolidColor =
            new Vector3Color(dialog.Color.R / 255f, dialog.Color.G / 255f, dialog.Color.B / 255f);
        _objectColorButton.BackColor = dialog.Color;
        _solidRadio.Checked = true;
        _drawing.Invalidate();
    }

    /// wczytuje obraz jako teksturę wybranej powierzchni
    private void LoadTexture()
    {
        if (_selectedSurface >= _scene.Surfaces.Count)
        {
            return;
        }

        using var dialog = new OpenFileDialog { Filter = "Obrazy|*.png;*.jpg;*.jpeg;*.bmp" };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        _scene.Surfaces[_selectedSurface].Appearance.Texture = BitmapImages.Load(dialog.FileName);
        _textureRadio.Checked = true;
        _drawing.Invalidate();
    }

    /// podmienia wspólną mapę normalnych i ją włącza
    private void LoadNormalMap()
    {
        using var dialog = new OpenFileDialog { Filter = "Obrazy|*.png;*.jpg;*.jpeg;*.bmp" };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        _scene.NormalMap = BitmapImages.Load(dialog.FileName);
        _normalMapBox.Checked = true;
        _drawing.Invalidate();
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

        DrawLight(e.Graphics, map);
    }

    /// wypełnia trójkąty do bitmapy i kładzie ją pod liniami
    // kolor liczy wzór światła w każdym pikselu, o wygranej decyduje większe z
    private void DrawFilled(Graphics graphics, ScreenMap map, int width, int height)
    {
        _frame.Resize(width, height);
        _frame.Clear(unchecked((int)0xFFFFFFFF));
        _rasterizer.DrawMesh(_scene, map, _frame, _scene.CurrentLightPosition);
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

    /// rysuje drogę światła, jej punkty kontrolne i samą lampę
    // krzywa jest w układzie okna, więc nie mnożymy jej przez obrót bryły
    private void DrawLight(Graphics graphics, ScreenMap map)
    {
        using var curvePen = new Pen(Color.DarkOrange, 2f);
        using var handleBrush = new SolidBrush(Color.OrangeRed);
        using var lampBrush = new SolidBrush(Color.Gold);
        Vector2 previous = map.ToScreen(BezierCurve.Point(_scene.LightPath.Points, 0f));
        const int samples = 80;
        for (int i = 1; i <= samples; i++)
        {
            float t = i / (float)samples;
            Vector2 next = map.ToScreen(BezierCurve.Point(_scene.LightPath.Points, t));
            graphics.DrawLine(curvePen, previous.X, previous.Y, next.X, next.Y);
            previous = next;
        }

        for (int i = 0; i < 6; i++)
        {
            Vector2 screen = map.ToScreen(_scene.LightPath.Points[i]);
            graphics.FillRectangle(handleBrush, screen.X - 4f, screen.Y - 4f, 8f, 8f);
        }

        Vector2 lamp = map.ToScreen(_scene.CurrentLightPosition);
        graphics.FillEllipse(lampBrush, lamp.X - 6f, lamp.Y - 6f, 12f, 12f);
    }

    /// zaczyna ciągnięcie punktu światła albo obrót sceny
    private void OnMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button is not (MouseButtons.Left or MouseButtons.Right))
        {
            return;
        }

        _lastMouse = e.Location;
        ScreenMap map = ScreenMap.Fit(
            Math.Max(1, _drawing.ClientSize.Width),
            Math.Max(1, _drawing.ClientSize.Height));
        _lightPointIndex = HitLightPoint(map, e.Location);
        if (_lightPointIndex >= 0 && e.Button == MouseButtons.Left)
        {
            _draggingLight = true;
            _drawing.Capture = true;
            return;
        }

        int? surface = SurfacePicker.FrontSurface(_scene.Mesh, map, e.X, e.Y);
        if (surface is int index && index < _surfaceList.Items.Count)
        {
            _surfaceList.SelectedIndex = index;
        }

        _turning = true;
        _drawing.Capture = true;
    }

    /// przesuwa punkt krzywej albo dokłada mały obrót i odrysowuje scenę
    private void OnMouseMove(object? sender, MouseEventArgs e)
    {
        float dx = e.X - _lastMouse.X;
        float dy = e.Y - _lastMouse.Y;
        _lastMouse = e.Location;
        if (_draggingLight)
        {
            ScreenMap map = ScreenMap.Fit(
                Math.Max(1, _drawing.ClientSize.Width),
                Math.Max(1, _drawing.ClientSize.Height));
            Vector3 view = map.ToView(e.X, e.Y, _scene.LightPath.Z);
            _scene.LightPath.MoveXy(_lightPointIndex, view.X, view.Y);
            _drawing.Invalidate();
            return;
        }

        if (!_turning || (dx == 0f && dy == 0f))
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

    /// kończy obrót albo ciągnięcie punktu po puszczeniu przycisku
    private void EndTurn()
    {
        _turning = false;
        _draggingLight = false;
        _lightPointIndex = -1;
        _drawing.Capture = false;
    }

    /// szuka punktu kontrolnego światła pod kursorem, w promieniu 10 pikseli
    private int HitLightPoint(ScreenMap map, Point cursor)
    {
        int found = -1;
        float best = 10f * 10f;
        for (int i = 0; i < 6; i++)
        {
            Vector2 screen = map.ToScreen(_scene.LightPath.Points[i]);
            float dx = screen.X - cursor.X;
            float dy = screen.Y - cursor.Y;
            float distance = dx * dx + dy * dy;
            if (distance <= best)
            {
                best = distance;
                found = i;
            }
        }

        return found;
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
        _clock.Dispose();
        _bitmap?.Dispose();
        base.OnFormClosed(e);
    }
}
