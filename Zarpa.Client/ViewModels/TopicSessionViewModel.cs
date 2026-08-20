using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Zarpa.ApiClient;
using Zarpa.Client.Resources.Languages;
using Zarpa.Client.Services;
using Zarpa.Client.Services.Environment;
using Zarpa.Client.Utilities;
using Zarpa.Shared.Dtos;

namespace Zarpa.Client.ViewModels
{
    [QueryProperty(nameof(TopicNumber), "topicNumber")]
    [QueryProperty(nameof(TopicName), "topicName")]
    public partial class TopicSessionViewModel(
        ISessionsApi sessionsApi,
        SelectedLicenseService selectedLicense,
        IEnvironmentService environmentService) : BaseViewModel
    {
        private readonly ISessionsApi _sessionsApi = sessionsApi;
        private readonly SelectedLicenseService _selectedLicense = selectedLicense;
        private readonly IEnvironmentService _environmentService = environmentService;

        public int TopicNumber { get; set; }

        [ObservableProperty] private string _topicName = string.Empty;
        [ObservableProperty] private string _progressText = string.Empty;
        [ObservableProperty] private double _progress;
        [ObservableProperty] private string _questionText = string.Empty;
        [ObservableProperty] private bool _showQuestion;
        [ObservableProperty] private bool _showFeedback;
        [ObservableProperty] private string _feedbackText = string.Empty;
        [ObservableProperty] private Color _feedbackColor = Colors.Gray;
        [ObservableProperty] private string? _explanation;
        [ObservableProperty] private string? _explanationImageUrl;
        [ObservableProperty] private bool _hasExplanationText;
        [ObservableProperty] private bool _hasExplanationImage;
        // Either text or figure — controls the "Explicación" button.
        [ObservableProperty] private bool _hasExplanation;
        [ObservableProperty] private bool _isCompleted;

        public ObservableCollection<AnswerOptionItem> AnswerOptions { get; } = [];

        private long _sessionId;
        private readonly Queue<SessionQuestionDto> _remaining = new();
        private int _total;
        private int _answered;
        private bool _answerLocked;
        private bool _started;

        public async Task StartAsync()
        {
            // OnAppearing fires again when coming back to this page — start only once.
            if (_started) return;

            if (_selectedLicense.SelectedLicenseId is not long licenseId)
            {
                await Shell.Current.GoToAsync("..");
                return;
            }

            IsBusy = true;
            try
            {
                var session = await _sessionsApi.StartTopicPracticeAsync(new StartTopicPracticeRequestDto(TopicNumber, licenseId));

                _started = true;
                _sessionId = session.SessionId;
                _total = session.TotalQuestions;
                _answered = session.AnsweredCount;

                foreach (var question in session.RemainingQuestions)
                    _remaining.Enqueue(question);

                ShowNextQuestion();
            }
            catch (Exception)
            {
                await UserMessageHelper.ShowErrorAsync(AppResources.UnknownError);
                await Shell.Current.GoToAsync("..");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ShowNextQuestion()
        {
            ShowFeedback = false;
            Explanation = null;
            ExplanationImageUrl = null;
            HasExplanationText = false;
            HasExplanationImage = false;
            HasExplanation = false;
            _answerLocked = false;

            Progress = _total == 0 ? 0 : (double)_answered / _total;

            if (_remaining.Count == 0)
            {
                ShowQuestion = false;
                IsCompleted = true;
                ProgressText = string.Format(AppResources.QuestionProgressFormat, _total, _total);
                return;
            }

            var question = _remaining.Peek();
            QuestionText = question.Text;

            AnswerOptions.Clear();
            for (var i = 0; i < question.Answers.Count; i++)
                AnswerOptions.Add(new AnswerOptionItem(question.Answers[i], (char)('A' + i)));

            ProgressText = string.Format(AppResources.QuestionProgressFormat, _answered + 1, _total);
            ShowQuestion = true;
        }

        [RelayCommand]
        private async Task SelectAnswerAsync(AnswerOptionItem option)
        {
            if (_answerLocked || _remaining.Count == 0) return;
            _answerLocked = true;

            try
            {
                var question = _remaining.Peek();
                var result = await _sessionsApi.SubmitAnswerAsync(_sessionId, new SubmitAnswerRequestDto(question.Id, option.Id));

                foreach (var answer in AnswerOptions)
                {
                    if (answer.Id == result.CorrectAnswerId)
                        answer.MarkCorrect();
                    else if (answer.Id == option.Id)
                        answer.MarkWrong();
                }

                _remaining.Dequeue();
                _answered++;
                Progress = _total == 0 ? 0 : (double)_answered / _total;

                FeedbackText = result.IsCorrect ? AppResources.CorrectFeedback : AppResources.IncorrectFeedback;
                FeedbackColor = result.IsCorrect ? Color.FromArgb("#2AA5A0") : Colors.Red;
                Explanation = result.Explanation;
                ExplanationImageUrl = BuildImageUrl(result.ExplanationImageUrl);
                HasExplanationText = !string.IsNullOrWhiteSpace(result.Explanation);
                HasExplanationImage = ExplanationImageUrl is not null;
                HasExplanation = HasExplanationText || HasExplanationImage;
                ShowFeedback = true;
            }
            catch (Exception)
            {
                // The answer did not reach the server — unlock so the user can retry.
                _answerLocked = false;
                await UserMessageHelper.ShowErrorAsync(AppResources.UnknownError);
            }
        }

        // The DB stores relative paths ("images/questions/x.png"); the Image control
        // needs an absolute URL, whose base differs per environment (emulator/device).
        private string? BuildImageUrl(string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
                return null;
            if (Uri.IsWellFormedUriString(imageUrl, UriKind.Absolute))
                return imageUrl;

            return $"{_environmentService.ApiBaseUrl.TrimEnd('/')}/{imageUrl.TrimStart('/')}";
        }

        [RelayCommand]
        private async Task ShowExplanationAsync()
        {
            var parameters = new Dictionary<string, object>();
            if (Explanation is not null)
                parameters["explanationText"] = Explanation;
            if (ExplanationImageUrl is not null)
                parameters["imageUrl"] = ExplanationImageUrl;

            await Shell.Current.GoToAsync(nameof(Pages.ExplanationPage), parameters);
        }

        [RelayCommand]
        private void NextQuestion() => ShowNextQuestion();

        // Deletes the user's whole history for this topic on the server, then starts
        // the topic again from question 1 with the full question set.
        [RelayCommand]
        private async Task RestartTopicAsync()
        {
            if (_selectedLicense.SelectedLicenseId is not long licenseId) return;

            var confirmed = await Shell.Current.DisplayAlertAsync(
                AppResources.RestartTopic,
                AppResources.RestartTopicConfirmMessage,
                AppResources.Restart,
                AppResources.Cancel);
            if (!confirmed) return;

            IsBusy = true;
            try
            {
                await _sessionsApi.ResetTopicPracticeAsync(new ResetTopicPracticeRequestDto(TopicNumber, licenseId));

                _remaining.Clear();
                _answered = 0;
                _total = 0;
                _started = false;
                IsCompleted = false;

                await StartAsync();
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

        [RelayCommand]
        private async Task BackToTopicsAsync() => await Shell.Current.GoToAsync("..");
    }

    public partial class AnswerOptionItem(SessionAnswerOptionDto dto, char letter) : ObservableObject
    {
        public long Id { get; } = dto.Id;

        // Lettered like the official paper: "A) Pantoques."
        public string Text { get; } = $"{letter}) {dto.Text}";

        [ObservableProperty] private Color _backgroundColor = Colors.White;
        [ObservableProperty] private Color _borderColor = Color.FromArgb("#D8DEE6");

        public void MarkCorrect()
        {
            BackgroundColor = Color.FromArgb("#DFF3EC");
            BorderColor = Color.FromArgb("#2AA5A0");
        }

        public void MarkWrong()
        {
            BackgroundColor = Color.FromArgb("#FBE9E9");
            BorderColor = Color.FromArgb("#D64545");
        }
    }
}
