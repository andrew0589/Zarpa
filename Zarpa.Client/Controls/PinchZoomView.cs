namespace Zarpa.Client.Controls;

// Container that lets the user pinch-zoom, pan and double-tap its content.
// Meant for a single Image; keep it inside a clipped parent so the scaled
// content doesn't paint over neighbouring views.
public class PinchZoomView : ContentView
{
    private const double MinScale = 1;
    private const double MaxScale = 4;
    private const double DoubleTapScale = 2.5;

    private double _currentScale = MinScale;
    private double _startScale = MinScale;
    private double _xOffset;
    private double _yOffset;

    public PinchZoomView()
    {
        var pinch = new PinchGestureRecognizer();
        pinch.PinchUpdated += OnPinchUpdated;
        GestureRecognizers.Add(pinch);

        var pan = new PanGestureRecognizer();
        pan.PanUpdated += OnPanUpdated;
        GestureRecognizers.Add(pan);

        var doubleTap = new TapGestureRecognizer { NumberOfTapsRequired = 2 };
        doubleTap.Tapped += OnDoubleTapped;
        GestureRecognizers.Add(doubleTap);
    }

    private void OnPinchUpdated(object? sender, PinchGestureUpdatedEventArgs e)
    {
        if (Content is null) return;

        if (e.Status == GestureStatus.Started)
        {
            _startScale = Content.Scale;
            Content.AnchorX = 0;
            Content.AnchorY = 0;
        }
        else if (e.Status == GestureStatus.Running)
        {
            _currentScale = Math.Clamp(_currentScale + (e.Scale - 1) * _startScale, MinScale, MaxScale);

            // Keep the point under the user's fingers fixed while scaling
            // (standard pinch-to-zoom math from the MAUI docs).
            double renderedX = Content.X + _xOffset;
            double deltaX = renderedX / Width;
            double deltaWidth = Width / (Content.Width * _startScale);
            double originX = (e.ScaleOrigin.X - deltaX) * deltaWidth;

            double renderedY = Content.Y + _yOffset;
            double deltaY = renderedY / Height;
            double deltaHeight = Height / (Content.Height * _startScale);
            double originY = (e.ScaleOrigin.Y - deltaY) * deltaHeight;

            double targetX = _xOffset - originX * Content.Width * (_currentScale - _startScale);
            double targetY = _yOffset - originY * Content.Height * (_currentScale - _startScale);

            Content.TranslationX = Math.Clamp(targetX, -Content.Width * (_currentScale - 1), 0);
            Content.TranslationY = Math.Clamp(targetY, -Content.Height * (_currentScale - 1), 0);
            Content.Scale = _currentScale;
        }
        else if (e.Status == GestureStatus.Completed)
        {
            _xOffset = Content.TranslationX;
            _yOffset = Content.TranslationY;
        }
    }

    private void OnPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        // At 1x there is nothing to pan.
        if (Content is null || _currentScale <= MinScale) return;

        switch (e.StatusType)
        {
            case GestureStatus.Running:
                Content.TranslationX = Math.Clamp(_xOffset + e.TotalX, -Content.Width * (_currentScale - 1), 0);
                Content.TranslationY = Math.Clamp(_yOffset + e.TotalY, -Content.Height * (_currentScale - 1), 0);
                break;
            case GestureStatus.Completed:
                _xOffset = Content.TranslationX;
                _yOffset = Content.TranslationY;
                break;
        }
    }

    private async void OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (Content is null) return;

        if (_currentScale > MinScale)
        {
            await ResetAsync();
            return;
        }

        Content.AnchorX = 0;
        Content.AnchorY = 0;
        _currentScale = DoubleTapScale;

        // Zoom towards the tapped point, clamped so no edge detaches.
        var tap = e.GetPosition(Content);
        double originX = (tap?.X ?? Content.Width / 2) / Content.Width;
        double originY = (tap?.Y ?? Content.Height / 2) / Content.Height;

        _xOffset = Math.Clamp(-originX * Content.Width * (_currentScale - 1), -Content.Width * (_currentScale - 1), 0);
        _yOffset = Math.Clamp(-originY * Content.Height * (_currentScale - 1), -Content.Height * (_currentScale - 1), 0);

        await Task.WhenAll(
            Content.ScaleToAsync(_currentScale, 250, Easing.CubicOut),
            Content.TranslateToAsync(_xOffset, _yOffset, 250, Easing.CubicOut));
    }

    private async Task ResetAsync()
    {
        if (Content is null) return;

        _currentScale = MinScale;
        _xOffset = 0;
        _yOffset = 0;

        await Task.WhenAll(
            Content.ScaleToAsync(MinScale, 250, Easing.CubicOut),
            Content.TranslateToAsync(0, 0, 250, Easing.CubicOut));
    }
}
