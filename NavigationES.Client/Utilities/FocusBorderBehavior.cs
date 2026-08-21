namespace NavigationES.Client.Utilities
{
    /// <summary>
    /// Highlights the Border wrapping a field while its Entry has focus (thicker,
    /// brand-navy stroke), so the user can see which field is being edited.
    /// </summary>
    public class FocusBorderBehavior : Behavior<Entry>
    {
        private Brush? _normalStroke;
        private double _normalThickness;

        protected override void OnAttachedTo(Entry entry)
        {
            base.OnAttachedTo(entry);
            entry.Focused += OnFocused;
            entry.Unfocused += OnUnfocused;
        }

        protected override void OnDetachingFrom(Entry entry)
        {
            entry.Focused -= OnFocused;
            entry.Unfocused -= OnUnfocused;
            base.OnDetachingFrom(entry);
        }

        private static Border? FindWrappingBorder(Element? element)
        {
            while (element is not null && element is not Border)
                element = element.Parent;
            return element as Border;
        }

        private static Color FocusColor =>
            Application.Current?.Resources.TryGetValue("Primary", out var color) == true && color is Color primary
                ? primary
                : Color.FromArgb("#0B3D5C");

        private void OnFocused(object? sender, FocusEventArgs e)
        {
            if (sender is not Entry entry || FindWrappingBorder(entry.Parent) is not Border border)
                return;

            _normalStroke = border.Stroke;
            _normalThickness = border.StrokeThickness;
            border.Stroke = new SolidColorBrush(FocusColor);
            border.StrokeThickness = 2;
        }

        private void OnUnfocused(object? sender, FocusEventArgs e)
        {
            if (sender is not Entry entry || FindWrappingBorder(entry.Parent) is not Border border)
                return;

            border.Stroke = _normalStroke;
            border.StrokeThickness = _normalThickness;
        }
    }
}
