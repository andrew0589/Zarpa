using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NavigationES.ApiClient;
using NavigationES.Client.Resources.Languages;
using NavigationES.Client.Services;
using NavigationES.Client.Utilities;
using NavigationES.Shared.Dtos;

namespace NavigationES.Client.ViewModels
{
    // The website's Perfil page: titulación and comunidad selectors, the personal
    // data card and the "Zona de peligro" with account deletion.
    public partial class ProfileViewModel(
        ILicensesApi licensesApi,
        IComunidadesApi comunidadesApi,
        IAuthApi authApi,
        AuthService authService,
        UserSessionService session,
        SelectedLicenseService selectedLicense) : BaseViewModel
    {
        private readonly ILicensesApi _licensesApi = licensesApi;
        private readonly IComunidadesApi _comunidadesApi = comunidadesApi;
        private readonly IAuthApi _authApi = authApi;
        private readonly AuthService _authService = authService;
        private readonly UserSessionService _session = session;
        private readonly SelectedLicenseService _selectedLicense = selectedLicense;

        public ObservableCollection<LicenseOptionItem> Licenses { get; } = [];
        public ObservableCollection<ComunidadDto> Comunidades { get; } = [];

        [ObservableProperty] private ComunidadDto? _selectedComunidad;

        [ObservableProperty] private string _initial = "?";
        [ObservableProperty] private string? _userName;
        [ObservableProperty] private string? _userEmail;
        [ObservableProperty] private bool _isEmailVerified;
        [ObservableProperty] private bool _loadFailed;

        [ObservableProperty] private bool _confirmingDelete;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DeleteButtonText), nameof(NotDeleting))]
        private bool _deleting;

        [ObservableProperty] private string? _deleteError;

        public string DeleteButtonText => Deleting ? AppResources.Deleting : AppResources.DeleteAccountConfirm;
        public bool NotDeleting => !Deleting;

        // Inline rename (Datos personales → Nombre → Editar).
        [ObservableProperty] private bool _editingName;
        [ObservableProperty] private string _nameDraft = string.Empty;
        [ObservableProperty] private string? _nameError;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SaveNameButtonText), nameof(NotSavingName))]
        private bool _savingName;

        public string SaveNameButtonText => SavingName ? AppResources.SavingName : AppResources.SaveName;
        public bool NotSavingName => !SavingName;

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
                var user = _authService.User;
                UserName = user?.Name;
                UserEmail = user?.Email;
                IsEmailVerified = user?.IsEmailVerified == true;
                Initial = InitialOf(user?.Name);

                // The account's stored selection is the source of truth — it follows the
                // user across the app and the web (same rule as the Tests tab).
                var licenses = await _licensesApi.GetLicensesAsync();
                var selectedLicenseId = (await _licensesApi.GetSelectedLicenseAsync()).LicenseId;

                if (selectedLicenseId is long id && licenses.FirstOrDefault(l => l.Id == id) is { } selected)
                    _selectedLicense.Select(selected.Id, selected.Code);

                // Rebuilding the card list churns platform views mid-navigation (Android:
                // "PlatformView cannot be null here") — update in place when unchanged.
                if (Licenses.Count == licenses.Count && Licenses.All(item => licenses.Any(l => l.Id == item.Id)))
                {
                    foreach (var item in Licenses)
                        item.IsSelected = item.Id == selectedLicenseId;
                }
                else
                {
                    Licenses.Clear();
                    foreach (var license in licenses)
                        Licenses.Add(new LicenseOptionItem(license) { IsSelected = license.Id == selectedLicenseId });
                }

                var comunidades = await _comunidadesApi.GetComunidadesAsync();
                var selectedComunidadId = (await _comunidadesApi.GetSelectedComunidadAsync()).ComunidadId;

                _syncingComunidad = true;
                try
                {
                    if (!Comunidades.Select(c => c.Id).SequenceEqual(comunidades.Select(c => c.Id)))
                    {
                        Comunidades.Clear();
                        foreach (var comunidad in comunidades)
                            Comunidades.Add(comunidad);
                    }

                    SelectedComunidad = Comunidades.FirstOrDefault(c => c.Id == selectedComunidadId);
                }
                finally
                {
                    _syncingComunidad = false;
                }

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
        private async Task SelectLicenseAsync(LicenseOptionItem option)
        {
            if (option.IsSelected) return;

            var previous = Licenses.FirstOrDefault(l => l.IsSelected);

            _selectedLicense.Select(option.Id, option.Code);
            foreach (var license in Licenses)
                license.IsSelected = license.Id == option.Id;

            try
            {
                await _licensesApi.SelectLicenseAsync(new SelectLicenseRequestDto(option.Id));
            }
            catch (Exception)
            {
                // The account copy did not update — undo the optimistic highlight.
                foreach (var license in Licenses)
                    license.IsSelected = license.Id == previous?.Id;
                if (previous is not null)
                    _selectedLicense.Select(previous.Id, previous.Code);
                else
                    _selectedLicense.Clear();

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

        private static string InitialOf(string? name) =>
            string.IsNullOrWhiteSpace(name) ? "?" : name.Trim()[..1].ToUpperInvariant();

        [RelayCommand]
        private void StartEditName()
        {
            NameDraft = UserName ?? string.Empty;
            NameError = null;
            EditingName = true;
        }

        [RelayCommand]
        private void CancelEditName()
        {
            if (SavingName) return;

            EditingName = false;
            NameError = null;
        }

        [RelayCommand]
        private async Task SaveNameAsync()
        {
            if (SavingName) return;

            // Same limit as the API (UserEntity.Name is 50 chars).
            var name = NameDraft.Trim();
            if (name.Length is 0 or > 50)
            {
                NameError = AppResources.NameNotValidError;
                return;
            }

            if (name == UserName)
            {
                EditingName = false;
                return;
            }

            SavingName = true;
            NameError = null;
            try
            {
                var result = await _authApi.UpdateNameAsync(new UpdateNameRequestDto(name));

                if (result.IsSuccess)
                {
                    // The API answers with a refreshed session (the JWT carries the first
                    // name) — store it like a sign-in so the new name survives restarts.
                    _authService.Signin(result.Data);
                    UserName = result.Data.user.Name;
                    Initial = InitialOf(UserName);
                    EditingName = false;

                    await UserMessageHelper.ShowSuccessAsync(AppResources.NameUpdated);
                }
                else
                {
                    NameError = BackendTranslator.Translate(result.ErrorCode);
                }
            }
            catch (Exception)
            {
                NameError = BackendTranslator.Translate(null);
            }
            finally
            {
                SavingName = false;
            }
        }

        [RelayCommand]
        private void StartDelete()
        {
            DeleteError = null;
            ConfirmingDelete = true;
        }

        [RelayCommand]
        private void CancelDelete()
        {
            if (Deleting) return;

            ConfirmingDelete = false;
            DeleteError = null;
        }

        [RelayCommand]
        private async Task DeleteAccountAsync()
        {
            if (Deleting) return;

            Deleting = true;
            DeleteError = null;
            try
            {
                var result = await _authApi.DeleteAccountAsync();

                if (result.IsSuccess)
                {
                    // The account is gone; the local token now points at nothing.
                    SignoutLocally();
                    await Shell.Current.GoToAsync("//SigninPage");
                }
                else
                {
                    DeleteError = BackendTranslator.Translate(result.ErrorMessage);
                }
            }
            catch (Exception)
            {
                DeleteError = BackendTranslator.Translate(null);
            }
            finally
            {
                Deleting = false;
            }
        }

        [RelayCommand]
        private async Task SignoutAsync()
        {
            SignoutLocally();
            await Shell.Current.GoToAsync("//SigninPage");
        }

        private void SignoutLocally()
        {
            _authService.Signout();
            _session.Clear();
            // The license choice lives on the account; the local cache must not leak
            // into whoever signs in next on this device.
            _selectedLicense.Clear();
        }
    }
}
