using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NavigationES.ApiClient;
using NavigationES.Client.Pages;
using NavigationES.Client.Resources.Languages;
using NavigationES.Client.Services;
using NavigationES.Client.Services.Environment;
using NavigationES.Client.Utilities;
using NavigationES.Shared.Dtos;

namespace NavigationES.Client.ViewModels
{
    // The Estudiar tab (the web's /estudiar): "Continuar viendo" plus one card per
    // topic of the user's license. The tab only shows for accounts the API lets in
    // (administrators until Study:Public is on) — see AppShell.
    public partial class StudyViewModel(
        IStudyApi studyApi,
        ILicensesApi licensesApi,
        SelectedLicenseService selectedLicense,
        IEnvironmentService environment) : BaseViewModel
    {
        private readonly IStudyApi _studyApi = studyApi;
        private readonly ILicensesApi _licensesApi = licensesApi;
        private readonly SelectedLicenseService _selectedLicense = selectedLicense;
        private readonly IEnvironmentService _environment = environment;

        public ObservableCollection<StudyChapterItem> Chapters { get; } = [];

        [ObservableProperty] private string _syllabusTitle = string.Empty;
        [ObservableProperty] private string _message = string.Empty;
        [ObservableProperty] private bool _hasMessage;

        [ObservableProperty] private bool _hasContinue;
        [ObservableProperty] private string _continueTitle = string.Empty;
        [ObservableProperty] private string _continueMeta = string.Empty;
        [ObservableProperty] private string? _continueImage;
        [ObservableProperty] private double _continueProgress;
        private long _continueVideoId;

        private bool _loading;

        public Task LoadAsync() => LoadCoreAsync(showBusyIndicator: Chapters.Count == 0);

        [RelayCommand]
        private async Task RefreshAsync()
        {
            try
            {
                await LoadCoreAsync(showBusyIndicator: false);
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        private async Task LoadCoreAsync(bool showBusyIndicator)
        {
            if (_loading) return;
            _loading = true;
            if (showBusyIndicator) IsBusy = true;

            try
            {
                // The server keeps the selection the web and the app share; the local copy
                // covers a selection made offline.
                var licenseId = (await _licensesApi.GetSelectedLicenseAsync()).LicenseId ?? _selectedLicense.SelectedLicenseId;
                if (licenseId is not long id)
                {
                    ShowMessage(AppResources.SelectLicenseFirst);
                    return;
                }

                var overview = await _studyApi.GetOverviewAsync(id);
                ShowMessage(null);

                // In-place update keeps the platform views stable (see TopicPracticeViewModel).
                if (Chapters.Select(c => c.Number).SequenceEqual(overview.Chapters.Select(c => c.Number)))
                {
                    foreach (var (item, dto) in Chapters.Zip(overview.Chapters))
                        item.Update(dto, _environment);
                }
                else
                {
                    Chapters.Clear();
                    foreach (var dto in overview.Chapters)
                        Chapters.Add(new StudyChapterItem(dto, _environment));
                }

                SyllabusTitle = string.Format(AppResources.StudySyllabusFormat, overview.Chapters.Count);

                var next = overview.Continue;
                HasContinue = next is not null;
                if (next is not null)
                {
                    _continueVideoId = next.VideoId;
                    ContinueTitle = $"{next.Code} {next.Title}";
                    ContinueMeta = string.Format(AppResources.StudyRemainingFormat, StudyText.Clock(next.DurationSeconds - next.PositionSeconds));
                    ContinueImage = ImageUrls.Absolute(_environment, next.ImageUrl);
                    ContinueProgress = StudyText.Fraction(next.PositionSeconds, next.DurationSeconds);
                }
            }
            catch (Refit.ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // The section was closed to this account since the tab appeared.
                ShowMessage(AppResources.ComingSoon);
            }
            catch (Exception)
            {
                ShowMessage(AppResources.UnknownError);
            }
            finally
            {
                _loading = false;
                if (showBusyIndicator) IsBusy = false;
            }
        }

        private void ShowMessage(string? message)
        {
            Message = message ?? string.Empty;
            HasMessage = message is not null;
        }

        [RelayCommand]
        private Task ContinueAsync() =>
            Shell.Current.GoToAsync(nameof(StudyVideoPage), new Dictionary<string, object> { ["videoId"] = _continueVideoId });

        [RelayCommand]
        private Task OpenChapterAsync(StudyChapterItem chapter) =>
            Shell.Current.GoToAsync(nameof(StudyTopicPage), new Dictionary<string, object> { ["topicNumber"] = chapter.Number });
    }

    public partial class StudyChapterItem : ObservableObject
    {
        public int Number { get; }
        public string NumberText { get; }
        public string Name { get; }

        [ObservableProperty] private string? _image;
        [ObservableProperty] private string _meta = string.Empty;
        [ObservableProperty] private double _progress;
        [ObservableProperty] private bool _isEmpty;

        public StudyChapterItem(StudyChapterDto dto, IEnvironmentService environment)
        {
            Number = dto.Number;
            NumberText = string.Format(AppResources.StudyTopicFormat, dto.Number);
            Name = dto.Name;
            Update(dto, environment);
        }

        public void Update(StudyChapterDto dto, IEnvironmentService environment)
        {
            Image = ImageUrls.Absolute(environment, dto.ImageUrl);
            IsEmpty = dto.VideoCount == 0;
            Meta = IsEmpty
                ? AppResources.StudyInPreparation
                : $"{StudyText.Videos(dto.VideoCount)} · {StudyText.Minutes(dto.TotalSeconds)}" +
                  (dto.CompletedCount > 0 ? $" · {string.Format(AppResources.StudyWatchedFormat, dto.CompletedCount)}" : string.Empty);
            Progress = StudyText.Fraction(dto.CompletedCount, dto.VideoCount);
        }
    }
}
