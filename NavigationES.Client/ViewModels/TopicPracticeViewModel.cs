using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NavigationES.ApiClient;
using NavigationES.Client.Pages;
using NavigationES.Client.Resources.Languages;
using NavigationES.Client.Services;
using NavigationES.Client.Utilities;
using NavigationES.Shared.Dtos;

namespace NavigationES.Client.ViewModels
{
    public partial class TopicPracticeViewModel(ITopicsApi topicsApi, SelectedLicenseService selectedLicense) : BaseViewModel
    {
        private readonly ITopicsApi _topicsApi = topicsApi;
        private readonly SelectedLicenseService _selectedLicense = selectedLicense;

        public ObservableCollection<TopicProgressItem> Topics { get; } = [];

        // Completed questions over total questions, across all topics.
        [ObservableProperty] private double _globalProgress;
        [ObservableProperty] private string _globalProgressText = "0%";

        public Task LoadAsync() => LoadCoreAsync(showBusyIndicator: true);

        // Pull-to-refresh: RefreshView shows its own spinner, so the busy overlay stays off.
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

        private bool _loading;

        private async Task LoadCoreAsync(bool showBusyIndicator)
        {
            if (_loading) return;
            _loading = true;
            if (showBusyIndicator) IsBusy = true;

            try
            {
                // The Tests tab guards navigation, so a selection always exists here.
                if (_selectedLicense.SelectedLicenseId is not long licenseId)
                    return;

                var result = await _topicsApi.GetTopicsAsync(licenseId);

                // In-place update keeps the platform views stable — rebuilding the list
                // while navigating back into this page crashes Android with
                // "PlatformView cannot be null here". Rebuild only when the topic set
                // itself changed (e.g. the user switched license).
                if (Topics.Select(t => t.Number).SequenceEqual(result.Topics.Select(t => t.Number)))
                {
                    foreach (var (item, dto) in Topics.Zip(result.Topics))
                        item.Update(dto);
                }
                else
                {
                    Topics.Clear();
                    foreach (var topic in result.Topics)
                        Topics.Add(new TopicProgressItem(topic));
                }

                // Global progress = the average over ALL topics of the selected license,
                // untouched topics included — so PER's 11 topics divide the same work
                // harder than PNB's 6, and 100% means the whole syllabus is done.
                GlobalProgress = Topics.Count == 0
                    ? 0
                    : Topics.Sum(t => t.Progress) / Topics.Count;
                GlobalProgressText = $"{(int)Math.Round(GlobalProgress * 100)}%";
            }
            catch (Exception)
            {
                await UserMessageHelper.ShowErrorAsync(AppResources.UnknownError);
            }
            finally
            {
                _loading = false;
                if (showBusyIndicator) IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task SelectTopicAsync(TopicProgressItem topic)
        {
            if (topic.QuestionCount == 0)
            {
                await UserMessageHelper.ShowWarningAsync(AppResources.NoQuestionsInTopic);
                return;
            }

            await Shell.Current.GoToAsync(nameof(TopicSessionPage), new Dictionary<string, object>
            {
                ["topicNumber"] = topic.Number,
                ["topicName"] = topic.Name,
            });
        }
    }

    // Observable so a reload can refresh the numbers of the SAME item instances
    // instead of rebuilding the list (see the in-place update note above).
    public partial class TopicProgressItem : ObservableObject
    {
        public int Number { get; }
        public string Name { get; }

        [ObservableProperty] private int _questionCount;
        [ObservableProperty] private double _progress;
        [ObservableProperty] private string _questionCountText = string.Empty;
        [ObservableProperty] private string _aciertosText = string.Empty;
        [ObservableProperty] private string _fallosText = string.Empty;
        [ObservableProperty] private string _progressText = string.Empty;

        public TopicProgressItem(TopicProgressDto dto)
        {
            Number = dto.Number;
            Name = dto.Name;
            Update(dto);
        }

        public void Update(TopicProgressDto dto)
        {
            QuestionCount = dto.QuestionCount;
            Progress = dto.QuestionCount == 0 ? 0 : (double)dto.CorrectCount / dto.QuestionCount;
            QuestionCountText = string.Format(AppResources.QuestionsCountFormat, dto.QuestionCount);
            AciertosText = string.Format(AppResources.AciertosFormat, dto.CorrectCount);
            FallosText = string.Format(AppResources.FallosFormat, dto.FailedCount);
            ProgressText = $"{(int)Math.Round(Progress * 100)}%";
        }
    }
}
