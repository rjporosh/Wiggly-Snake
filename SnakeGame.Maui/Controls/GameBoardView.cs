using Microsoft.Maui.Graphics;

namespace WigglySnake.Controls;

/// <summary>
/// Draws the play field itself: the rounded dark panel, the checkerboard cells,
/// the gradient-tinted snake body, the glowing head with eyes, and the bouncy
/// fruit emoji. Kept as a single lightweight <see cref="GraphicsView"/> so the
/// same drawing code renders identically on phone, tablet, TV and desktop, in
/// both portrait and landscape — it simply squares itself to the available space.
/// </summary>
public sealed class GameBoardView : GraphicsView, IDrawable
{
    public static readonly BindableProperty EngineProperty =
        BindableProperty.Create(nameof(Engine), typeof(GameEngine), typeof(GameBoardView),
            propertyChanged: OnEngineChanged);

    public GameEngine? Engine
    {
        get => (GameEngine?)GetValue(EngineProperty);
        set => SetValue(EngineProperty, value);
    }

    // Palette lifted 1:1 from wwwroot/css/app.css so the native board matches
    // the original web design.
    private static readonly Color BoardBg = Color.FromArgb("#142A4C");
    private static readonly Color CellLight = Color.FromRgba(255, 255, 255, 15);   // rgba(255,255,255,.06)
    private static readonly Color CellDark = Color.FromRgba(255, 255, 255, 5);    // rgba(255,255,255,.02)
    private static readonly Color SnakeHead = Color.FromArgb("#A8FF60");
    private static readonly Color SnakeHeadCore = Color.FromArgb("#D3FF9C");
    private static readonly Color BoardBorder = Color.FromArgb("#FFFFFF");
    private static readonly Color EyeDark = Color.FromArgb("#16233F");

    public GameBoardView()
    {
        Drawable = this;
        BackgroundColor = Colors.Transparent;
    }

    private static void OnEngineChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (GameBoardView)bindable;
        if (oldValue is GameEngine oldEngine) oldEngine.Changed -= view.OnEngineChangedRedraw;
        if (newValue is GameEngine newEngine) newEngine.Changed += view.OnEngineChangedRedraw;
    }

    private void OnEngineChangedRedraw() =>
        Dispatcher.Dispatch(Invalidate);

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (Engine is null) return;

        // Square the board to the smaller dimension so it always fits, in
        // either orientation, on anything from a watch face to a TV screen.
        float side = Math.Min(dirtyRect.Width, dirtyRect.Height);
        float originX = dirtyRect.X + (dirtyRect.Width - side) / 2f;
        float originY = dirtyRect.Y + (dirtyRect.Height - side) / 2f;
        var boardRect = new RectF(originX, originY, side, side);

        DrawPanel(canvas, boardRect);

        float padding = side * 0.022f;
        var gridRect = new RectF(
            boardRect.X + padding,
            boardRect.Y + padding,
            boardRect.Width - padding * 2,
            boardRect.Height - padding * 2);

        float cell = gridRect.Width / GameEngine.Cols;

        for (int y = 0; y < GameEngine.Rows; y++)
        {
            for (int x = 0; x < GameEngine.Cols; x++)
            {
                var rect = new RectF(gridRect.X + x * cell, gridRect.Y + y * cell, cell, cell);
                DrawCell(canvas, x, y, rect, cell);
            }
        }
    }

    private static void DrawPanel(ICanvas canvas, RectF boardRect)
    {
        float radius = boardRect.Width * 0.056f;

        canvas.SaveState();
        canvas.FillColor = BoardBg;
        canvas.FillRoundedRectangle(boardRect, radius);

        canvas.StrokeColor = BoardBorder.WithAlpha(0.85f);
        canvas.StrokeSize = Math.Max(3f, boardRect.Width * 0.013f);
        canvas.DrawRoundedRectangle(boardRect, radius);
        canvas.RestoreState();
    }

    private void DrawCell(ICanvas canvas, int x, int y, RectF rect, float cellSize)
    {
        var kind = Engine!.Classify(x, y);

        switch (kind)
        {
            case CellKind.EmptyLight:
                canvas.FillColor = CellLight;
                canvas.FillRectangle(rect);
                break;

            case CellKind.EmptyDark:
                canvas.FillColor = CellDark;
                canvas.FillRectangle(rect);
                break;

            case CellKind.Body:
                DrawBodySegment(canvas, x, y, rect);
                break;

            case CellKind.Head:
                DrawHead(canvas, rect, cellSize);
                break;

            case CellKind.Food:
                DrawFood(canvas, rect, Engine!.FoodEmoji);
                break;
        }
    }

    private void DrawBodySegment(ICanvas canvas, int x, int y, RectF rect)
    {
        double t = Engine!.BodyGradientPosition(x, y);
        // Mirrors the CSS: hue 140 (green) -> 100 (teal-ish), lightness 55% -> 43%.
        double hue = 140 - t * 40;
        double lightness = 55 - t * 12;
        var color = ColorFromHsl(hue, 70, lightness);

        var inset = rect;
        inset.Inflate(-rect.Width * 0.02f, -rect.Height * 0.02f);

        canvas.FillColor = color;
        canvas.FillRoundedRectangle(inset, inset.Width * 0.32f);

        // Soft top-left highlight, mimicking the inset box-shadow in CSS.
        canvas.FillColor = Colors.White.WithAlpha(0.18f);
        var highlight = new RectF(inset.X, inset.Y, inset.Width, inset.Height * 0.45f);
        canvas.FillRoundedRectangle(highlight, inset.Width * 0.3f);
    }

    private void DrawHead(ICanvas canvas, RectF rect, float cellSize)
    {
        var inset = rect;
        inset.Inflate(rect.Width * 0.01f, rect.Height * 0.01f);

        canvas.SaveState();
        canvas.FillColor = SnakeHead.WithAlpha(0.55f);
        canvas.FillRoundedRectangle(inset, inset.Width * 0.42f); // glow base
        canvas.RestoreState();

        var core = rect;
        core.Inflate(-rect.Width * 0.02f, -rect.Height * 0.02f);
        canvas.FillColor = SnakeHeadCore;
        canvas.FillRoundedRectangle(core, core.Width * 0.42f);

        var accent = core;
        accent.Inflate(-core.Width * 0.12f, -core.Height * 0.12f);
        canvas.FillColor = SnakeHead.WithAlpha(0.9f);
        canvas.FillRoundedRectangle(accent, accent.Width * 0.42f);

        DrawEyes(canvas, rect);
    }

    private void DrawEyes(ICanvas canvas, RectF rect)
    {
        float eyeSize = rect.Width * 0.22f;
        canvas.FillColor = EyeDark;

        (float dx1, float dy1, float dx2, float dy2) = Engine!.FacingDirection switch
        {
            Direction.Right => (0.62f, 0.24f, 0.62f, 0.54f),
            Direction.Left => (0.16f, 0.24f, 0.16f, 0.54f),
            Direction.Up => (0.24f, 0.16f, 0.54f, 0.16f),
            _ => (0.24f, 0.62f, 0.54f, 0.62f) // Down
        };

        canvas.FillEllipse(rect.X + rect.Width * dx1, rect.Y + rect.Height * dy1, eyeSize, eyeSize);
        canvas.FillEllipse(rect.X + rect.Width * dx2, rect.Y + rect.Height * dy2, eyeSize, eyeSize);
    }

    private static void DrawFood(ICanvas canvas, RectF rect, string emoji)
    {
        canvas.FontSize = rect.Height * 0.78f;
        canvas.FontColor = Colors.Black; // emoji glyphs render their own colour; this is a safe fallback
        canvas.DrawString(
            emoji,
            rect,
            HorizontalAlignment.Center,
            VerticalAlignment.Center);
    }

    private static Color ColorFromHsl(double h, double s, double l)
    {
        h = ((h % 360) + 360) % 360;
        s /= 100.0;
        l /= 100.0;

        double c = (1 - Math.Abs(2 * l - 1)) * s;
        double x = c * (1 - Math.Abs((h / 60.0 % 2) - 1));
        double m = l - c / 2;

        (double r, double g, double b) = h switch
        {
            < 60 => (c, x, 0.0),
            < 120 => (x, c, 0.0),
            < 180 => (0.0, c, x),
            < 240 => (0.0, x, c),
            < 300 => (x, 0.0, c),
            _ => (c, 0.0, x)
        };

        return new Color((float)(r + m), (float)(g + m), (float)(b + m));
    }
}
