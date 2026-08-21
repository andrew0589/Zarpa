namespace NavigationES.Client.Services.Environment
{
    public class ProductionEnvironmentService : IEnvironmentService
    {
        // TODO: replace with the real production API host once it exists.
        public string ApiBaseUrl => "https://api.navigationes.example";
        public string Name => "Production";
    }
}
