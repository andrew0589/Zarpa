namespace Zarpa.Client.Services.Environment
{
    public class ProductionEnvironmentService : IEnvironmentService
    {
        // TODO: replace with the real production API host once it exists.
        public string ApiBaseUrl => "https://api.zarpa.example";
        public string Name => "Production";
    }
}
