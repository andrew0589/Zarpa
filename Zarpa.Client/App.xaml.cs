using Zarpa.Client.Services;

namespace Zarpa.Client;

public partial class App : Application
{
    private readonly AuthService _authService;

    public App(AuthService authService)
    {
        InitializeComponent();

        UserAppTheme = AppTheme.Light;

        _authService = authService;
        _authService.Initialize();

#if ANDROID
        // Removes the native Android underline inside text fields (the verification-code
        // entries sit inside rounded Borders and the underline would show through).
        Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping(nameof(Entry), (handler, view) =>
        {
            handler.PlatformView.BackgroundTintList =
                Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
        });
#elif IOS
        // Removes the native iOS text field chrome so only the wrapping Border shows.
        Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping(nameof(Entry), (handler, view) =>
        {
            handler.PlatformView.BorderStyle = UIKit.UITextBorderStyle.None;
            handler.PlatformView.BackgroundColor = UIKit.UIColor.Clear;
        });
#endif
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell());
    }
}
