using NavigationES.Client.ViewModels.Boarding;

namespace NavigationES.Client.Pages.Boarding;

public partial class VerificationEmailCodePage : ContentPage
{
    private readonly VerificationEmailCodeViewModel _viewModel;

    public VerificationEmailCodePage(VerificationEmailCodeViewModel viewModel)
    {
        InitializeComponent();

        BindingContext = _viewModel = viewModel;
    }

    protected override bool OnBackButtonPressed()
    {
        return true;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await Task.Delay(100);

        Code1Field.Focus();
    }

    void CodeField_TextChanged(object sender, TextChangedEventArgs e)
    {
        var field = (Entry)sender;

        var oldLen = e.OldTextValue?.Length ?? 0;
        var newLen = e.NewTextValue?.Length ?? 0;

        bool isDelete = newLen < oldLen;

        // ----- HANDLE DELETE (BACKSPACE) -----
        if (isDelete)
        {
            // If after delete this field is empty, jump to previous
            if (string.IsNullOrEmpty(field.Text))
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (field == Code6Field)
                        Code5Field.Focus();
                    else if (field == Code5Field)
                        Code4Field.Focus();
                    else if (field == Code4Field)
                        Code3Field.Focus();
                    else if (field == Code3Field)
                        Code2Field.Focus();
                    else if (field == Code2Field)
                        Code1Field.Focus();
                    // Code1 has no previous — stay there
                });
            }

            UpdateCodeAndMaybeValidate();
            return;
        }

        // If user cleared via some other way and new value is empty, just update & exit
        if (string.IsNullOrEmpty(e.NewTextValue))
        {
            UpdateCodeAndMaybeValidate();
            return;
        }

        // ----- HANDLE NORMAL INPUT (FORWARD) -----
        // Ensure only 1 character (in case of paste)
        if (field.Text?.Length > 1)
        {
            field.Text = field.Text.Substring(field.Text.Length - 1, 1);
        }

        // Move to next field
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (field == Code1Field)
                Code2Field.Focus();
            else if (field == Code2Field)
                Code3Field.Focus();
            else if (field == Code3Field)
                Code4Field.Focus();
            else if (field == Code4Field)
                Code5Field.Focus();
            else if (field == Code5Field)
                Code6Field.Focus();
            else if (field == Code6Field)
                Code6Field.Unfocus();
        });

        UpdateCodeAndMaybeValidate(field);
    }

    void UpdateCodeAndMaybeValidate(Entry? lastEditedField = null)
    {
        var code = $"{Code1Field.Text}{Code2Field.Text}{Code3Field.Text}{Code4Field.Text}{Code5Field.Text}{Code6Field.Text}";

        if (BindingContext is VerificationEmailCodeViewModel vm)
        {
            vm.ValidationCode = code;

            bool allFilled = !string.IsNullOrWhiteSpace(Code1Field.Text)
                             && !string.IsNullOrWhiteSpace(Code2Field.Text)
                             && !string.IsNullOrWhiteSpace(Code3Field.Text)
                             && !string.IsNullOrWhiteSpace(Code4Field.Text)
                             && !string.IsNullOrWhiteSpace(Code5Field.Text)
                             && !string.IsNullOrWhiteSpace(Code6Field.Text);

            if (!allFilled)
                return;

            // Require all characters to be digits
            if (!code.All(char.IsDigit))
                return;

            // Require exactly 6 digits
            if (code.Length != 6)
                return;

            // Only auto-execute when editing the last field
            if (lastEditedField == Code6Field)
            {
                _ = vm.ValidateAsync();
            }
        }
    }
}
