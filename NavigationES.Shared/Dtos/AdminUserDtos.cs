namespace NavigationES.Shared.Dtos
{
    // One row of the admin Usuarios tab. All timestamps are UTC; the activity ones
    // are written by the API's UserActivityMiddleware (null = never seen since
    // tracking started, or account created before it existed for CreatedAt).
    public record AdminUserDto(
        long Id,
        string Name,
        string Email,
        bool IsEmailVerified,
        bool IsAdmin,
        // Has an email+password credential (social-only accounts do not).
        bool HasPassword,
        // Linked social providers: "Google", "Apple", "Facebook".
        List<string> Providers,
        string? SelectedLicenseCode,
        int TopicSessionCount,
        int ExamSessionCount,
        DateTime? CreatedAt,
        DateTime? LastActiveAt,
        // "web", "android", "ios", "windows" or "app" (older app builds).
        string? LastActiveClient,
        // When an administrator sent the inactivity warning email, if ever.
        DateTime? InactivityWarningSentAt);

    public record AdminUserListDto(List<AdminUserDto> Items, int TotalCount, int Page, int PageSize);
}
