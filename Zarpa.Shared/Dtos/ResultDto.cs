namespace Zarpa.Shared.Dtos
{
    public record ResultDto(bool IsSuccess, string? ErrorMessage, string? SuccessMessage = null, object[]? MessageParams = null)
    {
        public static ResultDto Success() => new(true, null);
        public static ResultDto Success(string successMessage) => new(true, null, successMessage);
        public static ResultDto Success(string successMessage, params object[] parameters) => new(true, null, successMessage, parameters);
        public static ResultDto Failure(string? errorMessage) => new(false, errorMessage);
    }
}
