using System.Collections.ObjectModel;
using CommunityToolkit.Maui.Views;
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
    // The video player (the web's /estudiar/video/{id}). The page drives the
    // MediaElement; this holds the lesson's data and saves the position.
    [QueryProperty(nameof(VideoId), "videoId")]
    public partial class StudyVideoViewModel(IStudyApi studyApi, IEnvironmentService environment) : BaseViewModel
    {
        private readonly IStudyApi _studyApi = studyApi;
        private readonly IEnvironmentService _environment = environment;

        public long VideoId { get; set; }

        // Where to start once the media is open (the saved position, 0 after a full watch).
        public int StartPosition { get; private set; }

        // Raised for a tap on a chapter; the page seeks the MediaElement.
        public event Action<int>? SeekRequested;

        public ObservableCollection<string> KeyTerms { get; } = [];
        public ObservableCollection<StudyVideoChapterItem> Chapters { get; } = [];

        [ObservableProperty] private bool _isLoaded;
        [ObservableProperty] private string _topicText = string.Empty;
        [ObservableProperty] private string _title = string.Empty;
        [ObservableProperty] private string? _subtitle;
        [ObservableProperty] private bool _hasSubtitle;
        [ObservableProperty] private string? _poster;
        [ObservableProperty] private MediaSource? _source;
        [ObservableProperty] private bool _canPlay;
        [ObservableProperty] private bool _isLocked;
        [ObservableProperty] private bool _noFile;
        [ObservableProperty] private bool _hasKeyTerms;
        [ObservableProperty] private bool _hasChapters;
        [ObservableProperty] private string _practiceText = string.Empty;

        [ObservableProperty] private bool _hasNext;
        [ObservableProperty] private string _nextTitle = string.Empty;
        [ObservableProperty] private string _nextMeta = string.Empty;
        [ObservableProperty] private string? _nextImage;

        private long _nextId;
        private int _topicNumber;
        private string _topicName = string.Empty;

        public async Task LoadAsync()
        {
            if (IsLoaded || IsBusy) return;
            IsBusy = true;

            try
            {
                var v = await _studyApi.GetVideoAsync(VideoId);

                _topicNumber = v.TopicNumber;
                _topicName = v.TopicName;
                TopicText = $"{string.Format(AppResources.StudyTopicFormat, v.TopicNumber)} · {v.TopicName}";
                Title = $"{v.Code} {v.Title}";
                Subtitle = v.Subtitle;
                HasSubtitle = !string.IsNullOrWhiteSpace(v.Subtitle);
                Poster = ImageUrls.Absolute(_environment, v.ImageUrl);
                PracticeText = string.Format(AppResources.StudyPracticeTopicFormat, v.TopicNumber);

                IsLocked = v.Locked;
                var url = ImageUrls.Absolute(_environment, v.VideoUrl);
                NoFile = !v.Locked && url is null;
                CanPlay = !v.Locked && url is not null;
                StartPosition = v.Completed ? 0 : v.PositionSeconds;
                if (CanPlay)
                    Source = MediaSource.FromUri(url!);

                KeyTerms.Clear();
                foreach (var term in v.KeyTerms)
                    KeyTerms.Add(term);
                HasKeyTerms = KeyTerms.Count > 0;

                Chapters.Clear();
                foreach (var c in v.Chapters)
                    Chapters.Add(new StudyVideoChapterItem(c.Title, c.Start));
                HasChapters = Chapters.Count > 0;

                HasNext = v.Next is not null;
                if (v.Next is { } next)
                {
                    _nextId = next.Id;
                    NextTitle = $"{next.Code} {next.Title}";
                    NextMeta = StudyText.Clock(next.DurationSeconds) + (next.Locked ? $" · {AppResources.StudyStateLocked}" : string.Empty);
                    NextImage = ImageUrls.Absolute(_environment, next.ImageUrl);
                }

                IsLoaded = true;
            }
            catch (Exception)
            {
                await UserMessageHelper.ShowErrorAsync(AppResources.UnknownError);
            }
            finally
            {
                IsBusy = false;
            }
        }

        // Best effort: a failed save is retried by the next one with a newer position.
        public async Task SaveProgressAsync(int seconds, bool completed)
        {
            if (!CanPlay) return;
            try
            {
                await _studyApi.SaveProgressAsync(VideoId, new StudyProgressRequestDto(seconds, completed));
            }
            catch (Exception)
            {
            }
        }

        [RelayCommand]
        private void SeekToChapter(StudyVideoChapterItem chapter) => SeekRequested?.Invoke(chapter.Start);

        [RelayCommand]
        private Task OpenNextAsync() =>
            Shell.Current.GoToAsync(nameof(StudyVideoPage), new Dictionary<string, object> { ["videoId"] = _nextId });

        [RelayCommand]
        private Task PracticeAsync() =>
            Shell.Current.GoToAsync(nameof(TopicSessionPage), new Dictionary<string, object>
            {
                ["topicNumber"] = _topicNumber,
                ["topicName"] = _topicName,
                ["completed"] = false,
            });
    }

    public class StudyVideoChapterItem(string title, int start)
    {
        public string Title { get; } = title;
        public int Start { get; } = start;
        public string TimeText { get; } = StudyText.Clock(start);
    }
}
