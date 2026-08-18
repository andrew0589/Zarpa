using CommunityToolkit.Mvvm.ComponentModel;

namespace Zarpa.Client.ViewModels
{
    [QueryProperty(nameof(ExplanationText), "explanationText")]
    [QueryProperty(nameof(ImageUrl), "imageUrl")]
    public partial class ExplanationViewModel : BaseViewModel
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasText))]
        private string? _explanationText;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasImage))]
        private string? _imageUrl;

        public bool HasText => !string.IsNullOrWhiteSpace(ExplanationText);
        public bool HasImage => !string.IsNullOrWhiteSpace(ImageUrl);
    }
}
