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
    // The website's Temas page: the topic squares plus the "Tu progreso" donut.
    public partial class TopicPracticeViewModel(ITopicsApi topicsApi, SelectedLicenseService selectedLicense) : BaseViewModel
    {
        private readonly ITopicsApi _topicsApi = topicsApi;
        private readonly SelectedLicenseService _selectedLicense = selectedLicense;

        public ObservableCollection<TopicProgressItem> Topics { get; } = [];

        // Totals over every topic of the selected license — a question's state is
        // decided by its latest answer (correct / wrong / never answered).
        [ObservableProperty] private int _totalQuestions;
        [ObservableProperty] private int _correct;
        [ObservableProperty] private int _failed;
        [ObservableProperty] private int _remaining;
        [ObservableProperty] private string _correctPercentText = "0%";
        [ObservableProperty] private string _statsSummary = string.Empty;
        [ObservableProperty] private bool _loadFailed;

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

                TotalQuestions = result.Topics.Sum(t => t.QuestionCount);
                Correct = result.Topics.Sum(t => t.CorrectCount);
                Failed = result.Topics.Sum(t => t.FailedCount);
                Remaining = TotalQuestions - Correct - Failed;
                var percent = TotalQuestions == 0 ? 0 : (int)Math.Round(100.0 * Correct / TotalQuestions);
                CorrectPercentText = $"{percent}%";
                StatsSummary = string.Format(AppResources.StatsSummaryFormat, Correct + Failed, TotalQuestions);
                LoadFailed = false;
            }
            catch (Exception)
            {
                LoadFailed = true;
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

            // A fully-correct topic opens on the completed screen (with "Reiniciar tema")
            // instead of silently starting a fresh full run — the server would otherwise
            // plan the whole question set again on the start call.
            await Shell.Current.GoToAsync(nameof(TopicSessionPage), new Dictionary<string, object>
            {
                ["topicNumber"] = topic.Number,
                ["topicName"] = topic.Name,
                ["completed"] = topic.IsCompleted,
            });
        }
    }

    // Observable so a reload can refresh the numbers of the SAME item instances
    // instead of rebuilding the list (see the in-place update note above).
    public partial class TopicProgressItem : ObservableObject
    {
        public int Number { get; }
        public string NumberText { get; }
        public string Name { get; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsEmpty), nameof(HasQuestions))]
        private int _questionCount;

        [ObservableProperty] private double _progress;
        [ObservableProperty] private string _percentText = "0%";
        [ObservableProperty] private string _questionCountText = string.Empty;
        [ObservableProperty] private bool _isCompleted;

        public bool IsEmpty => QuestionCount == 0;
        public bool HasQuestions => QuestionCount > 0;

        public TopicProgressItem(TopicProgressDto dto)
        {
            Number = dto.Number;
            NumberText = dto.Number.ToString();
            Name = dto.Name;
            Update(dto);
        }

        public void Update(TopicProgressDto dto)
        {
            QuestionCount = dto.QuestionCount;
            Progress = dto.QuestionCount == 0 ? 0 : (double)dto.CorrectCount / dto.QuestionCount;
            PercentText = $"{(int)Math.Round(Progress * 100)}%";
            QuestionCountText = string.Format(AppResources.QuestionsCountFormat, dto.QuestionCount);
            IsCompleted = dto.QuestionCount > 0 && dto.CorrectCount == dto.QuestionCount;
        }
    }
}
