using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Zarpa.ApiClient;
using Zarpa.Client.Resources.Languages;
using Zarpa.Client.Services;
using Zarpa.Client.Utilities;
using Zarpa.Shared.Dtos;

namespace Zarpa.Client.ViewModels
{
    public partial class TestsViewModel(ILicensesApi licensesApi, SelectedLicenseService selectedLicense) : BaseViewModel
    {
        private readonly ILicensesApi _licensesApi = licensesApi;
        private readonly SelectedLicenseService _selectedLicense = selectedLicense;

        public ObservableCollection<LicenseOptionItem> Licenses { get; } = [];

        [ObservableProperty] private string? _selectedLicenseName;

        public long? SelectedLicenseId => _selectedLicense.SelectedLicenseId;

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
                var licenses = await _licensesApi.GetLicensesAsync();

                // The account's stored selection is the source of truth (it follows the
                // user across devices); the local cache is a fallback and speeds up
                // pages that need the value synchronously.
                var selection = await _licensesApi.GetSelectedLicenseAsync();
                var storedId = selection.LicenseId ?? _selectedLicense.SelectedLicenseId;

                if (storedId is not null && licenses.All(l => l.Id != storedId))
                {
                    _selectedLicense.Clear();
                    storedId = null;
                }

                if (storedId is long id)
                {
                    var selected = licenses.First(l => l.Id == id);
                    _selectedLicense.Select(selected.Id, selected.Code);
                }

                // Rebuilding the chip list churns platform views mid-navigation (Android:
                // "PlatformView cannot be null here") — update in place when the license
                // set is unchanged, which is the normal case.
                if (Licenses.Count == licenses.Count && Licenses.All(item => licenses.Any(l => l.Id == item.Id)))
                {
                    foreach (var item in Licenses)
                        item.IsSelected = item.Id == storedId;
                }
                else
                {
                    Licenses.Clear();
                    foreach (var license in licenses)
                        Licenses.Add(new LicenseOptionItem(license) { IsSelected = license.Id == storedId });
                }

                SelectedLicenseName = licenses.FirstOrDefault(l => l.Id == storedId)?.Name;
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
        private async Task SelectLicenseAsync(LicenseOptionItem option)
        {
            _selectedLicense.Select(option.Id, option.Code);

            foreach (var license in Licenses)
                license.IsSelected = license.Id == option.Id;

            SelectedLicenseName = option.Name;

            try
            {
                await _licensesApi.SelectLicenseAsync(new SelectLicenseRequestDto(option.Id));
            }
            catch (Exception)
            {
                // Kept locally; the account copy syncs on the next successful selection.
                await UserMessageHelper.ShowErrorAsync(AppResources.UnknownError);
            }
        }
    }

    public partial class LicenseOptionItem(LicenseDto dto) : ObservableObject
    {
        public long Id { get; } = dto.Id;
        public string Code { get; } = dto.Code;
        public string Name { get; } = dto.Name;

        [ObservableProperty] private bool _isSelected;
    }
}
