namespace NavigationES.Client.Controls;

// Donut of the topic progress — aciertos (teal), fallos (red), sin responder
// (neutral gray) with a thin white gap between non-empty neighbours: the same
// segments the website draws with its conic-gradient (Temas → "Tu progreso").
public class DonutChart : GraphicsView
{
    public static readonly BindableProperty CorrectProperty =
        BindableProperty.Create(nameof(Correct), typeof(int), typeof(DonutChart), 0, propertyChanged: OnValueChanged);

    public static readonly BindableProperty FailedProperty =
        BindableProperty.Create(nameof(Failed), typeof(int), typeof(DonutChart), 0, propertyChanged: OnValueChanged);

    public static readonly BindableProperty RemainingProperty =
        BindableProperty.Create(nameof(Remaining), typeof(int), typeof(DonutChart), 0, propertyChanged: OnValueChanged);

    public int Correct
    {
        get => (int)GetValue(CorrectProperty);
        set => SetValue(CorrectProperty, value);
    }

    public int Failed
    {
        get => (int)GetValue(FailedProperty);
        set => SetValue(FailedProperty, value);
    }

    public int Remaining
    {
        get => (int)GetValue(RemainingProperty);
        set => SetValue(RemainingProperty, value);
    }

    public DonutChart()
    {
        Drawable = new DonutDrawable(this);
        BackgroundColor = Colors.Transparent;
    }

    private static void OnValueChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((DonutChart)bindable).Invalidate();

    private sealed class DonutDrawable(DonutChart chart) : IDrawable
    {
        private static readonly Color Ok = Color.FromArgb("#2AA5A0");
        private static readonly Color Ko = Color.FromArgb("#D64545");
        private static readonly Color Rest = Color.FromArgb("#AEBACA");

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            var size = Math.Min(dirtyRect.Width, dirtyRect.Height);
            if (size <= 0) return;

            // Ring thickness = 21% of the diameter (the web's inset: 21%).
            var thickness = size * 0.21f;
            var radius = size / 2 - thickness / 2;
            var box = new RectF(dirtyRect.Center.X - radius, dirtyRect.Center.Y - radius, radius * 2, radius * 2);

            canvas.StrokeSize = thickness;
            canvas.StrokeLineCap = LineCap.Butt;

            var total = chart.Correct + chart.Failed + chart.Remaining;
            var segments = new List<(Color Color, double Share)>();
            if (total > 0)
            {
                if (chart.Correct > 0) segments.Add((Ok, 100.0 * chart.Correct / total));
                if (chart.Failed > 0) segments.Add((Ko, 100.0 * chart.Failed / total));
                if (chart.Remaining > 0) segments.Add((Rest, 100.0 * chart.Remaining / total));
            }

            if (segments.Count <= 1)
            {
                canvas.StrokeColor = segments.Count == 0 ? Rest : segments[0].Color;
                canvas.DrawEllipse(box);
                return;
            }

            // MAUI arcs measure degrees counter-clockwise from 3 o'clock; the web's
            // gradient starts at 12 o'clock and runs clockwise — start at 90° and go
            // clockwise. A sliver of a segment keeps at least a third of itself visible.
            var cursor = 0.0;
            foreach (var (color, share) in segments)
            {
                var gap = Math.Min(1.1, share / 3);
                var from = cursor + gap / 2;
                var to = cursor + share - gap / 2;

                canvas.StrokeColor = color;
                canvas.DrawArc(box, (float)(90 - from * 3.6), (float)(90 - to * 3.6), clockwise: true, closed: false);

                cursor += share;
            }
        }
    }
}
