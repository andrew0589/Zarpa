namespace NavigationES.Shared.Dtos
{
    // Null = the user has not chosen a qualification yet.
    public record SelectedLicenseDto(long? LicenseId);

    public record SelectLicenseRequestDto(long LicenseId);
}
