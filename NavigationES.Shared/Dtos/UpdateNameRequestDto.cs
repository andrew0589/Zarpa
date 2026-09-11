namespace NavigationES.Shared.Dtos
{
    // Perfil → Datos personales → Nombre. The API answers with a refreshed
    // AuthResponseDto: the JWT carries the first name, so the token is reissued.
    public record UpdateNameRequestDto(string Name);
}
