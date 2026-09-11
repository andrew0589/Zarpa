namespace NavigationES.Client.Controls;

// The web's 1.5px dashed separator (.retro-title, .datos-row, .stats-summary,
// .year-header, .danger-zone). BoxView cannot dash, so it is drawn.
public class DashedRule : GraphicsView
{
    public static readonly BindableProperty ColorProperty =
        BindableProperty.Create(nameof(Color), typeof(Color), typeof(DashedRule), Color.FromArgb("#D8DEE6"),
            propertyChanged: (bindable, _, _) => ((DashedRule)bindable).Invalidate());

    public Color Color
    {
        get => (Color)GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    public DashedRule()
    {
        Drawable = new RuleDrawable(this);
        BackgroundColor = Colors.Transparent;
        HeightRequest = 2;
    }

    private sealed class RuleDrawable(DashedRule rule) : IDrawable
    {
        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            canvas.StrokeColor = rule.Color;
            canvas.StrokeSize = 1.5f;
            canvas.StrokeLineCap = LineCap.Butt;
            // Dash lengths are multiples of the stroke size: 4.5px dashes, 3px gaps —
            // what browsers render for a 1.5px dashed border.
            canvas.StrokeDashPattern = [3, 2];

            var y = dirtyRect.Height / 2;
            canvas.DrawLine(dirtyRect.Left, y, dirtyRect.Right, y);
        }
    }
}
