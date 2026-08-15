namespace Zarpa.Client.Services.Environment
{
    public interface IEnvironmentService
    {
        string ApiBaseUrl { get; }
        string Name { get; }
    }
}
