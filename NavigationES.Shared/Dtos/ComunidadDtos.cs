namespace NavigationES.Shared.Dtos
{
    public record ComunidadDto(long Id, string Name);

    // Null = the user has not chosen an autonomous community yet.
    public record SelectedComunidadDto(long? ComunidadId);

    public record SelectComunidadRequestDto(long ComunidadId);
}
