using System.Diagnostics;
using System.Numerics;
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
    private readonly SceneRenderer _renderer;
    private readonly PictureBox _drawing = new SmoothCanvas();
    private readonly System.Windows.Forms.Timer _clock = new();
    private readonly Stopwatch _animationTime = new();
    private float _animationStart;
    private bool _updatingUi;
    private bool _turning;
    private MouseButtons _dragButton;
    private int _lightPointIndex = -1;
    private int _selectedSurface;
    private Point _lastMouse;
    private Vector2 _lightGrabOffset;
    private readonly CheckBox _polygonsBox = new() { Text = "Wieloboki kontrolne" };
    private readonly CheckBox _meshBox = new() { Text = "Siatka trójkątów" };
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
    private readonly TrackBar _lightZBar = new() { Minimum = -200, Maximum = 200, Value = 165, TickStyle = TickStyle.None, AutoSize = true };
    private readonly Label _lightZLabel = new() { AutoSize = true };
    private readonly CheckBox _animateBox = new() { Text = "Animuj światło" };
    private readonly CheckBox _normalMapBox = new() { Text = "Mapa normalnych" };
    private readonly ComboBox _surfaceList = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly RadioButton _solidRadio = new() { Text = "Kolor jednolity", Checked = true };
    private readonly RadioButton _textureRadio = new() { Text = "Tekstura" };
    private readonly Button _resetButton = new() { Text = "Widok z góry" };
    private readonly Button _objectColorButton = new() { Text = "Kolor obiektu" };
    private readonly Button _normalButton = new() { Text = "Wczytaj mapę" };
    private readonly Button _lightColorButton = new() { Text = "Kolor światła" };
    private readonly Button _helpButton = new() { Text = "Pomoc" };

    private static readonly Color PageColor = Color.White;
    private static readonly Color PanelColor = Color.FromArgb(243, 244, 246);
    private static readonly Color LineColor = Color.FromArgb(209, 213, 219);
    private static readonly Color InkColor = Color.FromArgb(17, 24, 39);
    private static readonly Color MutedColor = Color.FromArgb(75, 85, 99);

    /// ustawia okno, wczytuje powierzchnie i tnie je na trójkąty
    public MainForm(IReadOnlyList<ControlGrid> grids)
    {
        Text = "Powierzchnie Béziera";
        Font = new Font("Segoe UI", 10f);
        BackColor = PageColor;
        ClientSize = new Size(1100, 700);
        MinimumSize = new Size(860, 520);
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Font;
        _renderer = new SceneRenderer(_scene);
        BuildLayout();
        SetUpSurfaces(grids);
        _scene.Divisions = _divisionsBar.Value;
        _scene.Draw.ControlPolygons = _polygonsBox.Checked;
        _scene.Draw.TriangleMesh = _meshBox.Checked;
        ApplyShadingFromUi();
        ApplyLightZFromUi();
        _scene.LightParameter = 0.57f;
        _scene.ReplaceOrientation(Matrix3.Multiply(Matrix3.RotationX(0.40f), Matrix3.RotationY(-0.52f)));
        _scene.RebuildMesh();
        RefreshDivisionLabel();
        _clock.Interval = 30;
        _clock.Tick += (_, _) => AdvanceLight();
    }

    /// układa biały obszar rysowania i szary panel, jak w edytorze wielokątów
    private void BuildLayout()
    {
        var page = new Panel { Dock = DockStyle.Fill, BackColor = PageColor, Padding = new Padding(16) };
        var panel = new Panel { Dock = DockStyle.Right, Width = 286, BackColor = PanelColor };
        var tabs = new TabControl { Dock = DockStyle.Fill };
        FlowLayoutPanel viewTab = AddTab(tabs, "Widok");
        FlowLayoutPanel lightTab = AddTab(tabs, "Światło");
        FlowLayoutPanel materialTab = AddTab(tabs, "Materiał");
        panel.Controls.Add(tabs);
        var footer = new Panel { Dock = DockStyle.Bottom, Height = 44, Padding = new Padding(0, 6, 0, 0) };
        _helpButton.Dock = DockStyle.Fill;
        _helpButton.Text = "Pomoc (F1)";
        footer.Controls.Add(_helpButton);
        panel.Controls.Add(footer);
        var columnGap = new Panel { Dock = DockStyle.Right, Width = 12, BackColor = PageColor };
        var frame = new Panel { Dock = DockStyle.Fill, BackColor = LineColor, Padding = new Padding(1) };

        _drawing.Name = "drawingSurface";
        _drawing.Dock = DockStyle.Fill;
        _drawing.BackColor = PageColor;
        _drawing.Paint += OnPaint;
        _drawing.Resize += (_, _) => _drawing.Invalidate();
        _drawing.MouseDown += OnMouseDown;
        _drawing.MouseMove += OnMouseMove;
        _drawing.MouseUp += OnMouseUp;
        _drawing.MouseCaptureChanged += (_, _) =>
        {
            if (!_drawing.Capture)
            {
                EndTurn();
            }
        };
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
        StyleFlat(_normalButton);
        StyleFlat(_lightColorButton);
        StyleFlat(_helpButton);

        _divisionsBar.AccessibleName = "Podziały siatki";
        _diffuseBar.AccessibleName = "Odbicie rozproszone";
        _specularBar.AccessibleName = "Siła połysku";
        _shininessBar.AccessibleName = "Skupienie połysku";
        _lightZBar.AccessibleName = "Wysokość lampy";
        _surfaceList.AccessibleName = "Powierzchnia do zmiany materiału";

        AddRow(viewTab, _divisionsLabel);
        AddRow(viewTab, _divisionsBar);
        AddRow(viewTab, _filledBox);
        AddRow(viewTab, _meshBox);
        AddRow(viewTab, _polygonsBox);
        AddRow(viewTab, _resetButton);
        AddRow(viewTab, Hint("Przeciągnij lewym przyciskiem, aby przechylić scenę. Prawy obraca ją w płaszczyźnie ekranu."));

        AddRow(lightTab, _diffuseLabel);
        AddRow(lightTab, _diffuseBar);
        AddRow(lightTab, _specularLabel);
        AddRow(lightTab, _specularBar);
        AddRow(lightTab, _shininessLabel);
        AddRow(lightTab, _shininessBar);
        AddRow(lightTab, _lightZLabel);
        AddRow(lightTab, _lightZBar);
        AddRow(lightTab, _animateBox);
        AddRow(lightTab, _lightColorButton);
        AddRow(lightTab, Hint("Przeciągnij pomarańczowy uchwyt, aby zmienić tor lampy. Spacja zatrzymuje lub wznawia ruch."));

        AddRow(materialTab, Heading("Powierzchnia"));
        AddRow(materialTab, _surfaceList);
        AddRow(materialTab, _solidRadio);
        AddRow(materialTab, _objectColorButton);
        AddRow(materialTab, _textureRadio);
        AddRow(materialTab, _normalMapBox);
        AddRow(materialTab, _normalButton);
        FitStack(viewTab);
        FitStack(lightTab);
        FitStack(materialTab);

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
        _animateBox.CheckedChanged += (_, _) => SetAnimation(_animateBox.Checked);
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
        _normalMapBox.CheckedChanged += (_, _) =>
        {
            _scene.UseNormalMap = _normalMapBox.Checked;
            _drawing.Invalidate();
        };
        _normalButton.Click += (_, _) => LoadNormalMap();
        _lightColorButton.Click += (_, _) => ChooseLightColor();
        _helpButton.Click += (_, _) => ShowHelp();
    }

    private static FlowLayoutPanel AddTab(TabControl tabs, string title)
    {
        var tab = new TabPage(title) { BackColor = PanelColor };
        var stack = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(12)
        };
        tab.Controls.Add(stack);
        tabs.TabPages.Add(tab);
        stack.Resize += (_, _) => FitStack(stack);
        return stack;
    }

    /// płaski przycisk z zapasem na krój pisma
    private static void StyleFlat(Button button)
    {
        button.Font = new Font("Segoe UI", 10f);
        button.FlatStyle = FlatStyle.Flat;
        button.BackColor = PageColor;
        button.ForeColor = InkColor;
        button.TextAlign = ContentAlignment.MiddleCenter;
        button.Padding = new Padding(6, 2, 6, 3);
        button.Height = TextRenderer.MeasureText(button.Text, button.Font).Height + 14;
        button.FlatAppearance.BorderColor = LineColor;
        button.FlatAppearance.BorderSize = 1;
    }

    /// dokłada kontrolkę pod poprzednią, z odstępem
    private static void AddRow(FlowLayoutPanel stack, Control control)
    {
        control.Margin = new Padding(0, 0, 0, 5);
        stack.Controls.Add(control);
    }

    /// rozciąga wiersze na szerokość panelu, bez zmiany ich wysokości
    private static void FitStack(FlowLayoutPanel stack)
    {
        int width = stack.Width - stack.Padding.Horizontal - SystemInformation.VerticalScrollBarWidth;
        if (width < 40)
        {
            return;
        }

        stack.SuspendLayout();
        foreach (Control control in stack.Controls)
        {
            if (control is Label label)
            {
                label.MaximumSize = new Size(width, 0);
            }
            else
            {
                control.Width = width;
            }
        }

        stack.ResumeLayout();
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
        _diffuseLabel.Text = $"Rozproszenie kd  {_scene.DiffuseWeight:0.00}";
        _specularLabel.Text = $"Siła połysku ks  {_scene.SpecularWeight:0.00}";
        _shininessLabel.Text = $"Skupienie połysku m  {_shininessBar.Value}";
    }

    /// przesuwa całą krzywą światła na wysokość z suwaka
    // suwak ma zakres −200…200, a scena liczy w jednostkach, stąd dzielenie przez 100
    private void ApplyLightZFromUi()
    {
        _scene.LightPath.SetZ(_lightZBar.Value / 100f);
        _lightZLabel.Text = $"z światła: {_scene.LightPath.Z:0.00}";
    }

    private void SetAnimation(bool animate)
    {
        if (animate)
        {
            _animationStart = _scene.LightParameter;
            _animationTime.Restart();
        }
        else if (_animationTime.IsRunning)
        {
            AdvanceLight();
            _animationTime.Stop();
        }

        _clock.Enabled = animate;
    }

    /// pozycja lampy zależy od czasu, więc cięższa klatka nie spowalnia obiegu
    private void AdvanceLight()
    {
        const double cyclesPerSecond = 0.27;
        _scene.LightParameter = (float)((_animationStart + _animationTime.Elapsed.TotalSeconds * cyclesPerSecond) % 1.0);
        _drawing.Invalidate();
    }

    /// obsługuje skróty przed kontrolką, która aktualnie ma fokus
    protected override bool ProcessCmdKey(ref Message message, Keys keyData)
    {
        if (keyData == Keys.Space)
        {
            _animateBox.Checked = !_animateBox.Checked;
            return true;
        }

        if (keyData == Keys.F1)
        {
            ShowHelp();
            return true;
        }

        return base.ProcessCmdKey(ref message, keyData);
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

    /// dodaje wczytane siatki i ich startowe materiały
    private void SetUpSurfaces(IReadOnlyList<ControlGrid> grids)
    {
        var colors = new[]
        {
            new Vector3Color(0.30f, 0.69f, 0.52f),
            new Vector3Color(0.37f, 0.57f, 0.86f)
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

        _scene.NormalMap = DefaultImages.BrickNormal;
        ShowColor(_lightColorButton, BitmapImages.ToMedia(_scene.LightColor));
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
        ShowColor(_objectColorButton, BitmapImages.ToMedia(appearance.SolidColor));
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
        ShowColor(_lightColorButton, dialog.Color);
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
        ShowColor(_objectColorButton, dialog.Color);
        _solidRadio.Checked = true;
        _drawing.Invalidate();
    }

    /// podmienia wspólną mapę normalnych i ją włącza
    private void LoadNormalMap()
    {
        using var dialog = new OpenFileDialog { Title = "Mapa normalnych", Filter = "Obrazy|*.png;*.jpg;*.jpeg;*.bmp" };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            _scene.NormalMap = BitmapImages.LoadNormalMap(dialog.FileName);
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or InvalidDataException or UnauthorizedAccessException
            or System.Runtime.InteropServices.ExternalException or OutOfMemoryException or OverflowException)
        {
            MessageBox.Show(this, $"Nie udało się wczytać obrazu.\n{exception.Message}",
                dialog.Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _normalMapBox.Checked = true;
        _drawing.Invalidate();
    }

    private static void ShowColor(Button button, Color color)
    {
        button.BackColor = color;
        float brightness = 0.2126f * color.R + 0.7152f * color.G + 0.0722f * color.B;
        button.ForeColor = brightness < 130f ? Color.White : InkColor;
    }

    private void OnPaint(object? sender, PaintEventArgs e)
    {
        if (_drawing.ClientSize.Width >= 2 && _drawing.ClientSize.Height >= 2)
        {
            _renderer.Draw(e.Graphics, GetScreenMap());
        }
    }

    private ScreenMap GetScreenMap()
    {
        return ScreenMap.Fit(_drawing.ClientSize.Width, _drawing.ClientSize.Height, _scene.FitRadius);
    }

    /// zaczyna ciągnięcie punktu światła albo obrót sceny
    private void OnMouseDown(object? sender, MouseEventArgs e)
    {
        if (_dragButton != MouseButtons.None || e.Button is not (MouseButtons.Left or MouseButtons.Right))
        {
            return;
        }

        _lastMouse = e.Location;
        _dragButton = e.Button;
        ScreenMap map = GetScreenMap();
        _lightPointIndex = e.Button == MouseButtons.Left ? HitLightPoint(map, e.Location) : -1;
        if (_lightPointIndex >= 0)
        {
            _lightGrabOffset = map.ToScreen(_scene.LightPath.Points[_lightPointIndex]) - new Vector2(e.X, e.Y);
        }

        _drawing.Capture = true;
    }

    /// przesuwa punkt krzywej albo dokłada mały obrót i odrysowuje scenę
    private void OnMouseMove(object? sender, MouseEventArgs e)
    {
        if (_dragButton == MouseButtons.None)
        {
            return;
        }

        if (_lightPointIndex >= 0)
        {
            ScreenMap map = GetScreenMap();
            float x = Math.Clamp(e.X + _lightGrabOffset.X, 0f, map.Width - 1f);
            float y = Math.Clamp(e.Y + _lightGrabOffset.Y, 0f, map.Height - 1f);
            Vector3 view = map.ToView(x, y, _scene.LightPath.Z);
            _scene.LightPath.MoveXy(_lightPointIndex, view.X, view.Y);
            _drawing.Invalidate();
            return;
        }

        int dx = e.X - _lastMouse.X;
        int dy = e.Y - _lastMouse.Y;
        if (!_turning && Math.Abs(dx) < 3 && Math.Abs(dy) < 3)
        {
            return;
        }

        _turning = true;
        _lastMouse = e.Location;
        TurnAxis axis = _dragButton == MouseButtons.Right ? TurnAxis.ScreenZ : TurnAxis.ScreenXAndY;
        Matrix3 increment = TurntableRotation.Increment(dx, dy, axis);
        // z lewej, bo nowy ruch jest względem ekranu, a nie względem już obróconej bryły
        _scene.ReplaceOrientation(Matrix3.Multiply(increment, _scene.Orientation));
        _drawing.Invalidate();
    }

    private void OnMouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button != _dragButton)
        {
            return;
        }

        if (!_turning && _lightPointIndex < 0 && e.Button == MouseButtons.Left)
        {
            int? surface = null;
            if (_scene.Draw.FilledTriangles)
            {
                surface = _renderer.VisibleSurface(e.Location);
            }
            else if (_scene.Draw.TriangleMesh)
            {
                surface = SurfacePicker.FrontSurface(_scene.Mesh, GetScreenMap(), e.X + 0.5f, e.Y + 0.5f);
            }

            if (surface is int index)
            {
                _surfaceList.SelectedIndex = index;
            }
        }

        EndTurn();
    }

    /// kończy obrót albo ciągnięcie punktu po puszczeniu przycisku
    private void EndTurn()
    {
        _turning = false;
        _dragButton = MouseButtons.None;
        _lightPointIndex = -1;
        _drawing.Capture = false;
    }

    /// szuka punktu kontrolnego światła pod kursorem, w promieniu 10 pikseli
    private int HitLightPoint(ScreenMap map, Point cursor)
    {
        int found = -1;
        float best = 10f * 10f;
        for (int i = 0; i < LightCurve.ControlPointCount - 1; i++)
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
        _renderer.Dispose();
        base.OnFormClosed(e);
    }
}
