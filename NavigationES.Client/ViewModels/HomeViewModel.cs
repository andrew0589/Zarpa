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
    // The website's Home page. First run: the qualification comes first, then the
    // community (it decides which real exams the simulations offer); once both are
    // on the account, the two progress cards (temas / exámenes) with their CTAs.
    public partial class HomeViewModel(
        ILicensesApi licensesApi,
        IComunidadesApi comunidadesApi,
        ITopicsApi topicsApi,
        IExamsApi examsApi,
        AuthService authService,
        UserSessionService session,
        SelectedLicenseService selectedLicense) : BaseViewModel
    {
        private readonly ILicensesApi _licensesApi = licensesApi;
        private readonly IComunidadesApi _comunidadesApi = comunidadesApi;
        private readonly ITopicsApi _topicsApi = topicsApi;
        private readonly IExamsApi _examsApi = examsApi;
        private readonly AuthService _authService = authService;
        private readonly UserSessionService _session = session;
        private readonly SelectedLicenseService _selectedLicense = selectedLicense;

        [ObservableProperty] private string _greeting = string.Empty;
        [ObservableProperty] private bool _loadFailed;

        // Which of the three screens shows.
        [ObservableProperty] private bool _showLicenseStep;
        [ObservableProperty] private bool _showComunidadStep;
        [ObservableProperty] private bool _showDashboard;

        public ObservableCollection<LicenseOptionItem> Licenses { get; } = [];
        public ObservableCollection<ComunidadDto> Comunidades { get; } = [];

        [ObservableProperty] private ComunidadDto? _selectedComunidad;

        // Dashboard — Práctica por temas
        [ObservableProperty] private string _progressTagline = string.Empty;
        [ObservableProperty] private string _temasPercentText = "0%";
        [ObservableProperty] private double _temasProgress;
        [ObservableProperty] private int _temasCorrect;
        [ObservableProperty] private int _temasFailed;
        [ObservableProperty] private int _temasRemaining;
        [ObservableProperty] private string _temasSummary = string.Empty;

        // Dashboard — Simulación de examen
        [ObservableProperty] private int _examsPassed;
        [ObservableProperty] private int _examsTotal;
        [ObservableProperty] private int _examsFailed;
        [ObservableProperty] private int _examsPending;
        [ObservableProperty] private double _examsProgress;
        [ObservableProperty] private string _examsSummary = string.Empty;

        private LicenseDto? _license;
        private bool _hasComunidad;
        private string? _comunidadName;

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
        // True while the code (not the user) sets SelectedComunidad, so the change
        // handler does not push the value back to the server.
        private bool _syncingComunidad;

        private async Task LoadCoreAsync(bool showBusyIndicator)
        {
            if (_loading) return;
            _loading = true;
            if (showBusyIndicator) IsBusy = true;

            try
            {
                Greeting = string.Format(AppResources.HelloFormat, _authService.User?.Name);

                var licenses = await _licensesApi.GetLicensesAsync();
                var selection = await _licensesApi.GetSelectedLicenseAsync();
                _license = licenses.FirstOrDefault(l => l.Id == selection.LicenseId);

                // The account's stored selection is the source of truth; the local cache
                // speeds up pages that need the value synchronously.
                if (_license is not null)
                    _selectedLicense.Select(_license.Id, _license.Code);
                else
                    _selectedLicense.Clear();

                // Rebuilding the card list churns platform views mid-navigation (Android:
                // "PlatformView cannot be null here") — update in place when unchanged.
                if (Licenses.Count == licenses.Count && Licenses.All(item => licenses.Any(l => l.Id == item.Id)))
                {
                    foreach (var item in Licenses)
                        item.IsSelected = item.Id == _license?.Id;
                }
                else
                {
                    Licenses.Clear();
                    foreach (var license in licenses)
                        Licenses.Add(new LicenseOptionItem(license) { IsSelected = license.Id == _license?.Id });
                }

                var comunidades = await _comunidadesApi.GetComunidadesAsync();
                var comunidadId = (await _comunidadesApi.GetSelectedComunidadAsync()).ComunidadId;

                _syncingComunidad = true;
                try
                {
                    if (!Comunidades.Select(c => c.Id).SequenceEqual(comunidades.Select(c => c.Id)))
                    {
                        Comunidades.Clear();
                        foreach (var comunidad in comunidades)
                            Comunidades.Add(comunidad);
                    }

                    SelectedComunidad = Comunidades.FirstOrDefault(c => c.Id == comunidadId);
                }
                finally
                {
                    _syncingComunidad = false;
                }

                _hasComunidad = comunidadId is not null;
                _comunidadName = SelectedComunidad?.Name;

                if (_license is not null && _hasComunidad)
                    await LoadProgressAsync();

                LoadFailed = false;
                UpdateStep();
            }
            catch (Exception)
            {
                // A 401 already sent the user to sign-in; anything else shows the error.
                LoadFailed = true;
            }
            finally
            {
                _loading = false;
                if (showBusyIndicator) IsBusy = false;
            }
        }

        private void UpdateStep()
        {
            ShowLicenseStep = _license is null;
            ShowComunidadStep = _license is not null && !_hasComunidad;
            ShowDashboard = _license is not null && _hasComunidad;
        }

        private async Task LoadProgressAsync()
        {
            // Topic progress is per qualification; the simulation progress additionally
            // narrows to the account's community — the exams endpoint filters by it
            // server-side, so this page only aggregates what it gets back.
            var topicsTask = _topicsApi.GetTopicsAsync(_license!.Id);
            var examsTask = _examsApi.GetExamsAsync(_license.Id);

            var topics = (await topicsTask).Topics;
            var temasTotal = topics.Sum(t => t.QuestionCount);
            TemasCorrect = topics.Sum(t => t.CorrectCount);
            TemasFailed = topics.Sum(t => t.FailedCount);
            TemasRemaining = temasTotal - TemasCorrect - TemasFailed;
            var temasPercent = temasTotal == 0 ? 0 : (int)Math.Round(100.0 * TemasCorrect / temasTotal);
            TemasPercentText = $"{temasPercent}%";
            TemasProgress = temasPercent / 100.0;
            TemasSummary = string.Format(AppResources.StatsSummaryFormat, TemasCorrect + TemasFailed, temasTotal);

            var exams = await examsTask;
            ExamsTotal = exams.Count;
            ExamsPassed = exams.Count(e => e.Passed);
            ExamsFailed = exams.Count(e => e.Finished && !e.Passed);
            ExamsPending = ExamsTotal - ExamsPassed - ExamsFailed;
            ExamsProgress = ExamsTotal == 0 ? 0 : (double)ExamsPassed / ExamsTotal;
            ExamsSummary = string.Format(
                AppResources.OfficialExamsOfFormat,
                _comunidadName is null ? _license.Code : $"{_license.Code} · {_comunidadName}");

            ProgressTagline = string.Format(
                AppResources.YourProgressInFormat,
                _comunidadName is null ? _license.Code : $"{_license.Code} — {_comunidadName}");
        }

        [RelayCommand]
        private async Task SelectLicenseAsync(LicenseOptionItem option)
        {
            if (option.IsSelected) return;

            var previous = _license;

            // Optimistic highlight; the account copy follows.
            foreach (var license in Licenses)
                license.IsSelected = license.Id == option.Id;

            try
            {
                await _licensesApi.SelectLicenseAsync(new SelectLicenseRequestDto(option.Id));

                _license = new LicenseDto(option.Id, option.Code, option.Name);
                _selectedLicense.Select(option.Id, option.Code);

                await AfterSelectionAsync();
            }
            catch (Exception)
            {
                // The account copy did not update — undo the optimistic highlight.
                foreach (var license in Licenses)
                    license.IsSelected = license.Id == previous?.Id;

                await UserMessageHelper.ShowErrorAsync(AppResources.UnknownError);
            }
        }

        partial void OnSelectedComunidadChanged(ComunidadDto? oldValue, ComunidadDto? newValue)
        {
            // The Picker resets its selection to null while its items are swapped;
            // only a real user choice reaches the server.
            if (_syncingComunidad || newValue is null || newValue.Id == oldValue?.Id) return;

            _ = PersistComunidadAsync(newValue, oldValue);
        }

        private async Task PersistComunidadAsync(ComunidadDto comunidad, ComunidadDto? previous)
        {
            try
            {
                await _comunidadesApi.SelectComunidadAsync(new SelectComunidadRequestDto(comunidad.Id));

                _hasComunidad = true;
                _comunidadName = comunidad.Name;

                await AfterSelectionAsync();
            }
            catch (Exception)
            {
                // The account copy did not update — undo the optimistic selection.
                _syncingComunidad = true;
                try
                {
                    SelectedComunidad = previous;
                }
                finally
                {
                    _syncingComunidad = false;
                }

                await UserMessageHelper.ShowErrorAsync(AppResources.UnknownError);
            }
        }

        // Once both choices are on the account the dashboard replaces the wizard.
        private async Task AfterSelectionAsync()
        {
            if (_license is not null && _hasComunidad)
            {
                IsBusy = true;
                try
                {
                    await LoadProgressAsync();
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

            UpdateStep();
        }

        [RelayCommand]
        private async Task GoToTopicsAsync() => await Shell.Current.GoToAsync(nameof(TopicPracticePage));

        [RelayCommand]
        private async Task GoToExamsAsync() => await Shell.Current.GoToAsync(nameof(ExamsPage));

        [RelayCommand]
        private async Task SignoutAsync()
        {
            _authService.Signout();
            _session.Clear();
            // The license choice lives on the account; the local cache must not leak
            // into whoever signs in next on this device.
            _selectedLicense.Clear();

            await Shell.Current.GoToAsync("//SigninPage");
        }
    }
}
