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
    // The website's Examen page: the official papers of the selected qualification,
    // filtered server-side by the account's comunidad, grouped by year.
    public partial class ExamsViewModel(
        IExamsApi examsApi,
        ILicensesApi licensesApi,
        IComunidadesApi comunidadesApi,
        SelectedLicenseService selectedLicense) : BaseViewModel
    {
        private static readonly string[] Months =
        [
            "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio",
            "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre",
        ];

        private readonly IExamsApi _examsApi = examsApi;
        private readonly ILicensesApi _licensesApi = licensesApi;
        private readonly IComunidadesApi _comunidadesApi = comunidadesApi;
        private readonly SelectedLicenseService _selectedLicense = selectedLicense;

        public ObservableCollection<ExamYearGroup> Years { get; } = [];

        [ObservableProperty] private string _licenseCode = string.Empty;
        // " — Islas Baleares", or empty while no comunidad is chosen.
        [ObservableProperty] private string _comunidadSuffix = string.Empty;
        [ObservableProperty] private bool _showList;
        [ObservableProperty] private bool _loadFailed;
        [ObservableProperty] private bool _isEmpty;

        public static string MonthName(int month) =>
            month is >= 1 and <= 12 ? Months[month - 1] : month.ToString();

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
        // What the list currently shows; an unchanged answer keeps the platform views
        // (rebuilding them while navigating back crashes Android).
        private string? _signature;

        private async Task LoadCoreAsync(bool showBusyIndicator)
        {
            if (_loading) return;
            _loading = true;
            if (showBusyIndicator) IsBusy = true;

            try
            {
                // The qualification decides which papers apply — without one, the Tests
                // tab is where it gets chosen.
                var licenses = await _licensesApi.GetLicensesAsync();
                var selection = await _licensesApi.GetSelectedLicenseAsync();
                var licenseId = selection.LicenseId ?? _selectedLicense.SelectedLicenseId;
                var license = licenses.FirstOrDefault(l => l.Id == licenseId);
                if (license is null)
                {
                    await UserMessageHelper.ShowWarningAsync(AppResources.SelectLicenseFirst);
                    await Shell.Current.GoToAsync("..");
                    return;
                }
                _selectedLicense.Select(license.Id, license.Code);
                LicenseCode = license.Code;

                var comunidadId = (await _comunidadesApi.GetSelectedComunidadAsync()).ComunidadId;
                string? comunidadName = null;
                if (comunidadId is not null)
                    comunidadName = (await _comunidadesApi.GetComunidadesAsync()).FirstOrDefault(c => c.Id == comunidadId)?.Name;
                ComunidadSuffix = comunidadName is null ? string.Empty : $" — {comunidadName}";

                var exams = await _examsApi.GetExamsAsync(license.Id);

                var signature = string.Join("|", exams.Select(e => $"{e.Id}:{e.Attempted}:{e.Finished}:{e.Passed}"));
                if (signature != _signature)
                {
                    _signature = signature;
                    Years.Clear();
                    foreach (var year in exams.GroupBy(e => e.Year).OrderByDescending(g => g.Key))
                        Years.Add(new ExamYearGroup(year.Key, [.. year.Select(e => new ExamListItem(e))]));
                }

                IsEmpty = exams.Count == 0;
                LoadFailed = false;
                ShowList = true;
            }
            catch (Exception)
            {
                LoadFailed = true;
                ShowList = false;
            }
            finally
            {
                _loading = false;
                if (showBusyIndicator) IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task OpenExamAsync(ExamListItem exam)
        {
            await Shell.Current.GoToAsync(nameof(ExamSessionPage), new Dictionary<string, object>
            {
                ["examId"] = exam.Id,
                ["title"] = exam.SessionTitle,
            });
        }
    }

    public class ExamYearGroup(int year, List<ExamListItem> exams)
    {
        public string YearText { get; } = year.ToString();
        public List<ExamListItem> Exams { get; } = exams;
    }

    public class ExamListItem
    {
        public long Id { get; }
        // "Enero 2024"
        public string MonthYear { get; }
        // "Modelo A" / "Modelo único"
        public string ModelText { get; }
        // "Islas Baleares · 45 preguntas"
        public string Meta { get; }
        // Title of the session page: "Enero 2024 · Modelo A"
        public string SessionTitle { get; }

        public bool HasStatus { get; }
        public string StatusText { get; } = string.Empty;
        public Color StatusBackground { get; } = Colors.Transparent;
        public Color StatusTextColor { get; } = Colors.Transparent;

        public ExamListItem(ExamListItemDto dto)
        {
            Id = dto.Id;
            MonthYear = $"{ExamsViewModel.MonthName(dto.Month)} {dto.Year}";
            ModelText = string.Format(AppResources.ModelFormat, dto.Model ?? AppResources.ModelUnique);
            Meta = string.Format(AppResources.ExamMetaFormat, dto.ComunidadName, dto.QuestionCount);
            SessionTitle = $"{MonthYear} · {ModelText}";

            if (dto.Passed)
            {
                HasStatus = true;
                StatusText = AppResources.StatusApto;
                StatusBackground = Color.FromArgb("#DFF3EC");
                StatusTextColor = Color.FromArgb("#1E7A76");
            }
            else if (dto.Finished)
            {
                HasStatus = true;
                StatusText = AppResources.StatusNoApto;
                StatusBackground = Color.FromArgb("#FBE9E9");
                StatusTextColor = Color.FromArgb("#B23B3B");
            }
            else if (dto.Attempted)
            {
                HasStatus = true;
                StatusText = AppResources.StatusEmpezado;
                StatusBackground = Color.FromArgb("#EDF1F6");
                StatusTextColor = Color.FromArgb("#404040");
            }
        }
    }
}
