using CommunityToolkit.Mvvm.ComponentModel;
using NavigationES.Shared.Constants;
using NavigationES.Shared.Dtos;

namespace NavigationES.Client.ViewModels
{
    // One flag card of the comunidad chooser (Home's first run and Perfil).
    public partial class ComunidadOptionItem(ComunidadDto dto) : ObservableObject
    {
        public long Id { get; } = dto.Id;
        public string Name { get; } = dto.Name;

        // Null for a community without a flag file — the card then shows the name only.
        public string? Flag { get; } = ComunidadFlags.FileName(dto.Id);
        public bool HasFlag => Flag is not null;

        [ObservableProperty] private bool _isSelected;
    }
}
