using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Primitives;
using NavigationES.Client.ViewModels;

namespace NavigationES.Client.Pages;

// Drives the MediaElement: resumes at the saved position and reports progress
// every 10 s of playback, on pause, at the end and when leaving the page — the
// same rhythm as the web player (study-player.js).
public partial class StudyVideoPage : ContentPage
{
    private readonly StudyVideoViewModel _viewModel;
    private double _lastSaved;
    private bool _resumed;

    public StudyVideoPage(StudyVideoViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        BindingContext = viewModel;
        _viewModel.SeekRequested += OnSeekRequested;
        SizeChanged += (_, _) => PlayerBox.HeightRequest = Math.Max(Width, 0) * 9 / 16;
    }

    protected async override void OnAppearing()
    {
        base.OnAppearing();

        await _viewModel.LoadAsync();
    }

    protected async override void OnDisappearing()
    {
        base.OnDisappearing();

        if (_viewModel.CanPlay && Player.Position.TotalSeconds > 0)
        {
            Player.Pause();
            await _viewModel.SaveProgressAsync((int)Player.Position.TotalSeconds, false);
        }
    }

    protected override void OnNavigatedFrom(NavigatedFromEventArgs args)
    {
        base.OnNavigatedFrom(args);

        // Leaving for good (back, not a pushed page on top): release the native player,
        // as the MediaElement docs require.
        if (!Navigation.NavigationStack.Contains(this))
        {
            _viewModel.SeekRequested -= OnSeekRequested;
            Player.Handler?.DisconnectHandler();
        }
    }

    private void OnMediaOpened(object? sender, EventArgs e)
    {
        if (_resumed) return;
        _resumed = true;

        var start = _viewModel.StartPosition;
        _lastSaved = start;
        if (start > 0 && start < Player.Duration.TotalSeconds - 5)
            MainThread.BeginInvokeOnMainThread(async () => await Player.SeekTo(TimeSpan.FromSeconds(start)));
    }

    private async void OnPositionChanged(object? sender, MediaPositionChangedEventArgs e)
    {
        var seconds = e.Position.TotalSeconds;
        // A seek backwards or forwards just moves the reference point.
        if (Math.Abs(seconds - _lastSaved) > 30)
        {
            _lastSaved = seconds;
            return;
        }
        if (seconds - _lastSaved >= 10)
        {
            _lastSaved = seconds;
            await _viewModel.SaveProgressAsync((int)seconds, false);
        }
    }

    private async void OnStateChanged(object? sender, MediaStateChangedEventArgs e)
    {
        if (e.NewState == MediaElementState.Paused)
        {
            _lastSaved = Player.Position.TotalSeconds;
            await _viewModel.SaveProgressAsync((int)_lastSaved, false);
        }
    }

    private async void OnMediaEnded(object? sender, EventArgs e) =>
        await _viewModel.SaveProgressAsync((int)Player.Duration.TotalSeconds, true);

    private void OnSeekRequested(int seconds) =>
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            _lastSaved = seconds;
            await Player.SeekTo(TimeSpan.FromSeconds(seconds));
            Player.Play();
        });
}
