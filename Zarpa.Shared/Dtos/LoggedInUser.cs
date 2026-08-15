namespace Zarpa.Shared.Dtos
{
    public record LoggedInUser(long Id, string Name, string Email, bool IsEmailVerified);
}
