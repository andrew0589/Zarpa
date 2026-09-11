using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NavigationES.ApiClient;
using NavigationES.Client.Resources.Languages;
using NavigationES.Client.Services.Environment;
using NavigationES.Client.Utilities;
using NavigationES.Shared.Dtos;

namespace NavigationES.Client.ViewModels
{
    // The website's ExamenSesion page: a timed run through one official paper, the
    // answer sheet pushed to the server as it is filled, then the graded review.
    [QueryProperty(nameof(ExamId), "examId")]
    [QueryProperty(nameof(Title), "title")]
    public partial class ExamSessionViewModel(IExamsApi examsApi, IEnvironmentService environmentService) : BaseViewModel
    {
        private static readonly Color KoColor = Color.FromArgb("#D64545");
        private static readonly Color OffBlack = Color.FromArgb("#1F1F1F");

        private readonly IExamsApi _examsApi = examsApi;
        private readonly IEnvironmentService _environmentService = environmentService;

        public long ExamId { get; set; }

        [ObservableProperty] private string _title = AppResources.ExamDefaultTitle;

        // Which of the three screens shows: the load error, the running exam or the result.
        [ObservableProperty] private bool _loadFailed;
        [ObservableProperty] private bool _showExam;
        [ObservableProperty] private bool _showResult;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasError))]
        private string? _error;

        public bool HasError => Error is not null;

        // Running exam
        public ObservableCollection<ExamNavItem> Nav { get; } = [];
        public ObservableCollection<ExamAnswerItem> Answers { get; } = [];

        [ObservableProperty] private string _progressText = string.Empty;
        [ObservableProperty] private string _questionText = string.Empty;
        [ObservableProperty] private string? _questionImageUrl;
        [ObservableProperty] private bool _hasQuestionImage;
        [ObservableProperty] private bool _canGoPrevious;
        [ObservableProperty] private bool _canGoNext;
        [ObservableProperty] private bool _hasTimer;
        [ObservableProperty] private string _timerText = string.Empty;
        [ObservableProperty] private bool _timerLow;
        [ObservableProperty] private bool _showUnsavedWarning;
        [ObservableProperty] private string _unsavedWarningText = string.Empty;

        // Result
        [ObservableProperty] private string _verdictText = string.Empty;
        [ObservableProperty] private Color _verdictBackground = Colors.Transparent;
        [ObservableProperty] private Color _verdictTextColor = Colors.Black;
        [ObservableProperty] private string _correctText = "0";
        [ObservableProperty] private string _wrongText = "0";
        [ObservableProperty] private string _unansweredText = "0";
        [ObservableProperty] private string _totalErrorsText = "0";
        [ObservableProperty] private Color _totalErrorsColor = OffBlack;
        [ObservableProperty] private string _reviewHeading = string.Empty;
        [ObservableProperty] private string _reviewToggleText = string.Empty;
        [ObservableProperty] private bool _noReviewQuestions;

        public ObservableCollection<TopicResultItem> TopicResults { get; } = [];
        public ObservableCollection<ReviewQuestionItem> ReviewQuestions { get; } = [];

        private StartExamSessionDto? _session;
        private List<ExamSessionQuestionDto> _questions = [];
        private readonly Dictionary<long, int?> _chosen = [];
        // Questions whose latest choice has not reached the server yet.
        private readonly HashSet<long> _unsaved = [];
        // Set when a save attempt actually fails, cleared once the backlog drains.
        private bool _saveFailed;
        private bool _flushing;
        private int _flushTicks;
        private int _index;
        private int _remaining;
        private DateTimeOffset? _deadline;
        private IDispatcherTimer? _timer;
        private ExamSessionResultDto? _result;
        private bool _showAllResults;
        private bool _started;
        private bool _finishing;
        private bool _exiting;
        private bool _exited;

        // An attempt is in progress: leaving needs the confirmation (and abandons it).
        public bool IsRunning => _session is not null && _result is null && !_exited;

        public async Task StartAsync()
        {
            // OnAppearing fires again when the app comes back to the foreground —
            // start only once, just wake the countdown up.
            if (_started)
            {
                ResumeTimer();
                return;
            }
            _started = true;

            IsBusy = true;
            try
            {
                _session = await _examsApi.StartExamSessionAsync(ExamId);
                _questions = _session.Questions;
                foreach (var question in _questions)
                    _chosen[question.Id] = question.ChosenIndex;

                // The countdown follows a DEADLINE on the real clock, never a count of
                // timer ticks: the OS stops the timer while the app sleeps, and the server
                // decides expiry from StartedAt — a tick counter would fall behind it.
                if (_session.RemainingSeconds is int seconds)
                {
                    HasTimer = true;
                    _deadline = DateTimeOffset.UtcNow.AddSeconds(seconds);
                    UpdateTimer();
                }

                Nav.Clear();
                foreach (var question in _questions)
                    Nav.Add(new ExamNavItem(question.Id, question.Position) { IsAnswered = question.ChosenIndex is not null });

                _index = 0;
                ShowQuestion();
                ShowExam = true;

                // Runs for untimed licenses too — it also drives the retry of answers
                // that failed to save.
                StartTimer();
            }
            catch (Exception)
            {
                LoadFailed = true;
            }
            finally
            {
                IsBusy = false;
            }
        }

        #region timer
        private void StartTimer()
        {
            if (_timer is not null) return;

            _timer = Application.Current?.Dispatcher.CreateTimer();
            if (_timer is null) return;

            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.IsRepeating = true;
            _timer.Tick += (_, _) => _ = TickAsync();
            _timer.Start();
        }

        // Called when the page goes out of view (app to background, page popped) so a
        // ticking timer cannot outlive it.
        public void PauseTimer() => _timer?.Stop();

        private void ResumeTimer()
        {
            if (IsRunning && _timer is { IsRunning: false })
                _timer.Start();
        }

        private void StopTimer()
        {
            _timer?.Stop();
            _timer = null;
        }

        private async Task TickAsync()
        {
            if (_result is not null || _exited) return;

            if (_deadline is not null)
            {
                UpdateTimer();
                if (_remaining == 0)
                {
                    // Time is up — the paper is handed in as it stands, like the real thing.
                    await FinishAsync();
                    return;
                }
            }

            // Answers the server never received are pushed again every few seconds, so a
            // dropped connection costs nothing once it comes back.
            if (_unsaved.Count > 0 && ++_flushTicks % 5 == 0)
                await FlushAnswersAsync();
        }

        private void UpdateTimer()
        {
            if (_deadline is not DateTimeOffset deadline) return;

            _remaining = Math.Max(0, (int)Math.Ceiling((deadline - DateTimeOffset.UtcNow).TotalSeconds));

            // Above one hour the hours must show — mm:ss alone would render 90 minutes
            // as "30:00" (the hour gets silently dropped).
            var time = TimeSpan.FromSeconds(_remaining);
            TimerText = time.TotalHours >= 1 ? time.ToString(@"h\:mm\:ss") : time.ToString(@"mm\:ss");
            TimerLow = _remaining < 300;
        }
        #endregion

        #region questions
        private void ShowQuestion()
        {
            var question = _questions[_index];

            ProgressText = string.Format(AppResources.QuestionProgressFormat, question.Position, _questions.Count);
            QuestionText = question.Text;
            QuestionImageUrl = ImageUrls.Absolute(_environmentService, question.QuestionImageUrl);
            HasQuestionImage = QuestionImageUrl is not null;

            var chosen = _chosen.GetValueOrDefault(question.Id);
            Answers.Clear();
            for (var i = 1; i <= question.Answers.Count; i++)
                Answers.Add(new ExamAnswerItem(i, question.Answers[i - 1]) { IsSelected = chosen == i });

            foreach (var item in Nav)
                item.IsCurrent = item.QuestionId == question.Id;

            CanGoPrevious = _index > 0;
            CanGoNext = _index < _questions.Count - 1;
        }

        [RelayCommand]
        private void GoToQuestion(ExamNavItem item)
        {
            var index = _questions.FindIndex(q => q.Id == item.QuestionId);
            if (index < 0 || index == _index) return;

            _index = index;
            ShowQuestion();
        }

        [RelayCommand]
        private void Previous()
        {
            if (_index == 0) return;
            _index--;
            ShowQuestion();
        }

        [RelayCommand]
        private void Next()
        {
            if (_index >= _questions.Count - 1) return;
            _index++;
            ShowQuestion();
        }

        // The answer sheet lives locally and is pushed to the server; a save that fails
        // is kept and retried instead of being undone, so a network blip cannot silently
        // erase what the user marked.
        [RelayCommand]
        private async Task SelectAnswerAsync(ExamAnswerItem option)
        {
            if (_result is not null || _session is null) return;

            var question = _questions[_index];
            if (_chosen.GetValueOrDefault(question.Id) == option.Index) return;

            _chosen[question.Id] = option.Index;
            _unsaved.Add(question.Id);
            Error = null;

            foreach (var answer in Answers)
                answer.IsSelected = answer.Index == option.Index;
            var navItem = Nav.FirstOrDefault(n => n.QuestionId == question.Id);
            if (navItem is not null) navItem.IsAnswered = true;

            await FlushAnswersAsync();
        }

        private async Task FlushAnswersAsync()
        {
            if (_flushing || _session is null || _unsaved.Count == 0) return;
            _flushing = true;

            try
            {
                foreach (var questionId in _unsaved.ToArray())
                {
                    if (_chosen.GetValueOrDefault(questionId) is not int choice) continue;

                    try
                    {
                        await _examsApi.SubmitExamAnswerAsync(_session.SessionId, new SubmitExamAnswerRequestDto(questionId, choice));
                        _unsaved.Remove(questionId);
                    }
                    catch (Exception)
                    {
                        // Server unreachable or refusing — stop here and let the timer
                        // try the whole backlog again shortly.
                        _saveFailed = true;
                        UpdateUnsavedWarning();
                        return;
                    }
                }

                _saveFailed = false;
                UpdateUnsavedWarning();
            }
            finally
            {
                _flushing = false;
            }
        }

        // Only after a save has actually failed: every answer sits in _unsaved for the
        // length of its round-trip, so keying this off the backlog alone would make the
        // warning blink on every tap.
        private void UpdateUnsavedWarning()
        {
            ShowUnsavedWarning = _saveFailed && _unsaved.Count > 0;
            UnsavedWarningText = string.Format(
                AppResources.UnsavedWarningFormat,
                _unsaved.Count == 1 ? AppResources.OneAnswer : string.Format(AppResources.AnswersFormat, _unsaved.Count));
        }
        #endregion

        #region finish / exit
        [RelayCommand]
        private async Task ConfirmFinishAsync()
        {
            if (_finishing || _session is null) return;

            var unanswered = _questions.Count(q => _chosen.GetValueOrDefault(q.Id) is null);
            var message = unanswered > 0
                ? string.Format(AppResources.FinishExamUnansweredFormat, unanswered)
                : AppResources.FinishExamMessage;

            if (_unsaved.Count > 0)
            {
                message += "\n\n" + string.Format(
                    AppResources.UnsavedOnFinishFormat,
                    _unsaved.Count == 1 ? AppResources.OneAnswerNotArrived : string.Format(AppResources.AnswersNotArrivedFormat, _unsaved.Count));
            }

            var confirmed = await Shell.Current.DisplayAlertAsync(
                AppResources.FinishExam, message, AppResources.Deliver, AppResources.Cancel);
            if (confirmed) await FinishAsync();
        }

        private async Task FinishAsync()
        {
            if (_finishing || _session is null) return;
            _finishing = true;
            PauseTimer();

            // Last chance for anything that never made it to the server.
            await FlushAnswersAsync();

            try
            {
                _result = await _examsApi.FinishExamSessionAsync(_session.SessionId);
                StopTimer();
                ShowResults(_result);
            }
            catch (Exception)
            {
                Error = AppResources.UnknownError;
                _finishing = false;
                // A failed hand-in must not freeze the exam: the clock keeps running and
                // an expired attempt keeps retrying on its own.
                ResumeTimer();
            }
        }

        // Leaving mid-exam needs a confirmation; a confirmed exit deletes the attempt.
        // Returns true when the navigation may go ahead.
        public async Task<bool> ConfirmExitAsync()
        {
            if (!IsRunning) return true;

            var confirmed = await Shell.Current.DisplayAlertAsync(
                AppResources.ExitExamTitle, AppResources.ExitExamMessage, AppResources.ExitAndDelete, AppResources.ContinueExam);
            if (!confirmed) return false;

            _exited = true;
            StopTimer();

            try
            {
                await _examsApi.AbandonExamSessionAsync(_session!.SessionId);
            }
            catch (Exception)
            {
                // Best effort — an unfinished leftover gets cleaned up on the next start.
            }

            return true;
        }

        // Nav-bar back arrow and the Android hardware back button both land here.
        [RelayCommand]
        private async Task ExitAsync()
        {
            if (_exiting) return;
            _exiting = true;

            try
            {
                if (await ConfirmExitAsync())
                    await Shell.Current.GoToAsync("..");
            }
            finally
            {
                _exiting = false;
            }
        }

        [RelayCommand]
        private async Task BackToExamsAsync() => await Shell.Current.GoToAsync("..");
        #endregion

        #region result
        private void ShowResults(ExamSessionResultDto result)
        {
            ShowExam = false;
            ShowUnsavedWarning = false;
            Error = null;

            VerdictText = result.Passed ? AppResources.Apto : AppResources.NoApto;
            VerdictBackground = Color.FromArgb(result.Passed ? "#DFF3EC" : "#FBE9E9");
            VerdictTextColor = Color.FromArgb(result.Passed ? "#1E7A76" : "#B23B3B");

            CorrectText = result.Correct.ToString();
            WrongText = result.Wrong.ToString();
            UnansweredText = result.Unanswered.ToString();
            TotalErrorsText = result.MaxTotalErrors is int max ? $"{result.TotalErrors} / {max}" : result.TotalErrors.ToString();
            TotalErrorsColor = result.MaxTotalErrors is int limit && result.TotalErrors > limit ? KoColor : OffBlack;

            TopicResults.Clear();
            foreach (var topic in result.Topics)
                TopicResults.Add(new TopicResultItem(topic));

            _showAllResults = false;
            BuildReview();
            ShowResult = true;
        }

        private void BuildReview()
        {
            if (_result is null) return;

            var questions = _showAllResults
                ? _result.Questions
                : [.. _result.Questions.Where(q => q.ChosenIndex != q.CorrectIndex)];

            ReviewQuestions.Clear();
            foreach (var question in questions)
                ReviewQuestions.Add(new ReviewQuestionItem(question));

            NoReviewQuestions = ReviewQuestions.Count == 0;
            ReviewHeading = _showAllResults ? AppResources.AllQuestions : AppResources.FailedQuestions;
            ReviewToggleText = _showAllResults ? AppResources.ShowOnlyErrors : AppResources.ShowAllQuestions;
        }

        [RelayCommand]
        private void ToggleReview()
        {
            _showAllResults = !_showAllResults;
            BuildReview();
        }
        #endregion
    }

    // One button of the question navigator; colors mirror the web's .qnav-btn states.
    public partial class ExamNavItem(long questionId, int position) : ObservableObject
    {
        private static readonly Color Primary = Color.FromArgb("#0B3D5C");
        private static readonly Color Accent = Color.FromArgb("#2AA5A0");

        public long QuestionId { get; } = questionId;
        public string PositionText { get; } = position.ToString();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(BackgroundColor), nameof(BorderColor), nameof(TextColor), nameof(BorderThickness), nameof(FontAttributes))]
        private bool _isAnswered;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(BackgroundColor), nameof(BorderColor), nameof(TextColor), nameof(BorderThickness), nameof(FontAttributes))]
        private bool _isCurrent;

        public Color BackgroundColor => IsAnswered ? Color.FromArgb("#DFF3EC") : Colors.White;
        public Color BorderColor => IsCurrent ? Primary : IsAnswered ? Accent : Color.FromArgb("#D8DEE6");
        public Color TextColor => IsCurrent ? Primary : IsAnswered ? Color.FromArgb("#1E7A76") : Color.FromArgb("#404040");
        public double BorderThickness => IsCurrent ? 2 : 1.5;
        public FontAttributes FontAttributes => IsCurrent ? FontAttributes.Bold : FontAttributes.None;
    }

    // One answer of the current question; Index is 1-based like the API's ChosenIndex.
    public partial class ExamAnswerItem(int index, string text) : ObservableObject
    {
        public int Index { get; } = index;
        public string Letter { get; } = $"{(char)('A' + index - 1)})";
        public string Text { get; } = text;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(BackgroundColor), nameof(BorderColor), nameof(BorderThickness))]
        private bool _isSelected;

        public Color BackgroundColor => IsSelected ? Color.FromArgb("#EAF6F5") : Colors.White;
        public Color BorderColor => IsSelected ? Color.FromArgb("#2AA5A0") : Color.FromArgb("#D8DEE6");
        public double BorderThickness => IsSelected ? 2 : 1.5;
    }

    // "3. RIPA — 2 de 3 errores permitidos", colored like the web's .topic-result states.
    public class TopicResultItem
    {
        public string Name { get; }
        public string Detail { get; }
        public Color Background { get; }
        public Color Border { get; }
        public Color TextColor { get; }
        public FontAttributes FontAttributes { get; }

        public TopicResultItem(ExamTopicResultDto topic)
        {
            Name = $"{topic.TopicNumber}. {topic.TopicName}";
            Detail = topic.MaxErrors is int max
                ? string.Format(AppResources.TopicErrorsAllowedFormat, topic.Errors, max)
                : topic.Errors == 1 ? AppResources.OneError : string.Format(AppResources.ErrorsFormat, topic.Errors);

            if (!topic.WithinLimit)
            {
                Background = Color.FromArgb("#FBE9E9");
                Border = Color.FromArgb("#D64545");
                TextColor = Color.FromArgb("#B23B3B");
                FontAttributes = FontAttributes.Bold;
            }
            else if (topic.MaxErrors is int limit && topic.Errors == limit)
            {
                Background = Color.FromArgb("#FCF3D7");
                Border = Color.FromArgb("#E0A800");
                TextColor = Color.FromArgb("#8A6D1A");
                FontAttributes = FontAttributes.None;
            }
            else
            {
                Background = Colors.White;
                Border = Color.FromArgb("#D8DEE6");
                TextColor = Color.FromArgb("#404040");
                FontAttributes = FontAttributes.None;
            }
        }
    }

    public class ReviewQuestionItem
    {
        public string PositionText { get; }
        public string Text { get; }
        public bool IsUnanswered { get; }
        public List<ReviewAnswerItem> Answers { get; }

        public ReviewQuestionItem(ExamResultQuestionDto question)
        {
            PositionText = question.Position.ToString();
            Text = question.Text;
            IsUnanswered = question.ChosenIndex is null;
            Answers = [];
            for (var i = 1; i <= question.Answers.Count; i++)
            {
                var state = i == question.CorrectIndex ? ReviewAnswerState.Correct
                    : i == question.ChosenIndex ? ReviewAnswerState.Wrong
                    : ReviewAnswerState.None;
                Answers.Add(new ReviewAnswerItem(i, question.Answers[i - 1], state));
            }
        }
    }

    public enum ReviewAnswerState { None, Correct, Wrong }

    public class ReviewAnswerItem(int index, string text, ReviewAnswerState state)
    {
        public string Letter { get; } = $"{(char)('A' + index - 1)})";
        public string Text { get; } = text;

        public Color Background { get; } = state switch
        {
            ReviewAnswerState.Correct => Color.FromArgb("#DFF3EC"),
            ReviewAnswerState.Wrong => Color.FromArgb("#FBE9E9"),
            _ => Colors.White,
        };

        public Color Border { get; } = state switch
        {
            ReviewAnswerState.Correct => Color.FromArgb("#2AA5A0"),
            ReviewAnswerState.Wrong => Color.FromArgb("#D64545"),
            _ => Color.FromArgb("#D8DEE6"),
        };
    }
}
