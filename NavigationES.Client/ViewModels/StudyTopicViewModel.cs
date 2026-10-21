using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NavigationES.ApiClient;
using NavigationES.Client.Pages;
using NavigationES.Client.Resources.Languages;
using NavigationES.Client.Services.Environment;
using NavigationES.Client.Utilities;
using NavigationES.Shared.Dtos;

namespace NavigationES.Client.ViewModels
{
    // One chapter of the Estudiar tab (the web's /estudiar/tema/{n}): cover, progress
    // and the list of its video lessons.
    [QueryProperty(nameof(TopicNumber), "topicNumber")]
    public partial class StudyTopicViewModel(IStudyApi studyApi, IEnvironmentService environment) : BaseViewModel
    {
        private readonly IStudyApi _studyApi = studyApi;
        private readonly IEnvironmentService _environment = environment;

        public int TopicNumber { get; set; }

        public ObservableCollection<StudyVideoRowItem> Videos { get; } = [];

        [ObservableProperty] private string _eyebrow = string.Empty;
        [ObservableProperty] private string _name = string.Empty;
        [ObservableProperty] private string? _description;
        [ObservableProperty] private bool _hasDescription;
        [ObservableProperty] private string? _image;
        [ObservableProperty] private string _facts = string.Empty;
        [ObservableProperty] private double _progress;
        [ObservableProperty] private bool _hasResume;
        [ObservableProperty] private string _resumeText = string.Empty;
        [ObservableProperty] private string _practiceText = string.Empty;
        [ObservableProperty] private bool _isEmpty;
        [ObservableProperty] private bool _loadFailed;

        private long _resumeVideoId;
        private bool _loading;

        public async Task LoadAsync()
        {
            if (_loading) return;
            _loading = true;
            IsBusy = Videos.Count == 0;

            try
            {
                var topic = await _studyApi.GetTopicAsync(TopicNumber);

                Eyebrow = string.Format(AppResources.StudyTopicFormat, topic.Number);
                Name = topic.Name;
                Description = topic.Description;
                HasDescription = !string.IsNullOrWhiteSpace(topic.Description);
                Image = ImageUrls.Absolute(_environment, topic.ImageUrl);
                PracticeText = string.Format(AppResources.StudyPracticeTopicFormat, topic.Number);

                var completed = topic.Videos.Count(v => v.Completed);
                Facts = $"{StudyText.Videos(topic.Videos.Count)} · {StudyText.Minutes(topic.Videos.Sum(v => v.DurationSeconds))} · " +
                        string.Format(AppResources.StudyWatchedFormat, completed);
                Progress = StudyText.Fraction(completed, topic.Videos.Count);
                IsEmpty = topic.Videos.Count == 0;

                var resume = topic.Videos.FirstOrDefault(v => !v.Completed && v.PositionSeconds > 0 && !v.Locked)
                             ?? topic.Videos.FirstOrDefault(v => !v.Completed && !v.Locked);
                HasResume = resume is not null;
                if (resume is not null)
                {
                    _resumeVideoId = resume.Id;
                    ResumeText = $"{(resume.PositionSeconds > 0 ? AppResources.StudyContinue : AppResources.StudyStart)} · {resume.Code}";
                }

                // Rebuilt on every appearing so the watched/in-progress states are current;
                // the list is short (a handful of subsections per topic).
                Videos.Clear();
                foreach (var v in topic.Videos)
                    Videos.Add(new StudyVideoRowItem(v, _environment));
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
                IsBusy = false;
            }
        }

        [RelayCommand]
        private Task ResumeAsync() => OpenVideoIdAsync(_resumeVideoId);

        [RelayCommand]
        private Task OpenVideoAsync(StudyVideoRowItem video) => OpenVideoIdAsync(video.Id);

        private static Task OpenVideoIdAsync(long id) =>
            Shell.Current.GoToAsync(nameof(StudyVideoPage), new Dictionary<string, object> { ["videoId"] = id });

        // Same entry as the Tests tab's topic squares.
        [RelayCommand]
        private Task PracticeAsync() =>
            Shell.Current.GoToAsync(nameof(TopicSessionPage), new Dictionary<string, object>
            {
                ["topicNumber"] = TopicNumber,
                ["topicName"] = Name,
                ["completed"] = false,
            });
    }

    public class StudyVideoRowItem
    {
        public long Id { get; }
        public string Code { get; }
        public string Title { get; }
        public string? Subtitle { get; }
        public bool HasSubtitle { get; }
        public string? Image { get; }
        public string DurationText { get; }
        public double Progress { get; }
        public string StateText { get; }
        public Color StateColor { get; }
        public bool IsFree { get; }
        public bool IsDraft { get; }
        public bool IsLocked { get; }
        // Row tint, as on the web: green once watched to the end, red until then.
        public Color RowBackground { get; }
        public Color RowBorder { get; }
        public bool ShowCheck { get; }

        public StudyVideoRowItem(StudyVideoSummaryDto dto, IEnvironmentService environment)
        {
            Id = dto.Id;
            Code = dto.Code;
            Title = dto.Title;
            Subtitle = dto.Subtitle;
            HasSubtitle = !string.IsNullOrWhiteSpace(dto.Subtitle);
            Image = ImageUrls.Absolute(environment, dto.ImageUrl);
            DurationText = StudyText.Clock(dto.DurationSeconds);
            Progress = dto.Completed ? 1 : StudyText.Fraction(dto.PositionSeconds, dto.DurationSeconds);
            IsFree = dto.IsFree;
            IsDraft = !dto.IsPublished;
            IsLocked = dto.Locked;
            ShowCheck = dto.Completed && !dto.Locked;
            (RowBackground, RowBorder) = dto.Completed
                ? (Color.FromArgb("#D3EFDD"), Color.FromArgb("#5DBB86"))
                : (Color.FromArgb("#FDEEEE"), Color.FromArgb("#F0BDBD"));

            (StateText, StateColor) =
                dto.Locked ? (AppResources.StudyStateLocked, Color.FromArgb("#5E5E5E"))
                : dto.Completed ? (AppResources.StudyStateWatched, Color.FromArgb("#157347"))
                : dto.PositionSeconds > 0 ? ($"{AppResources.StudyStateInProgress} · {(int)Math.Round(Progress * 100)} %", Color.FromArgb("#0B3D5C"))
                : (AppResources.StudyStateNotWatched, Color.FromArgb("#5E5E5E"));
        }
    }
}
