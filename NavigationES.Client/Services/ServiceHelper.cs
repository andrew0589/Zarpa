namespace NavigationES.Client.Services
{
    public static class ServiceHelper
    {
        private static IServiceProvider? _services;

        public static void Initialize(IServiceProvider services) => _services = services;

        // Use this when the service must exist
        public static T GetRequiredService<T>() where T : notnull
        {
            if (_services is null)
                throw new InvalidOperationException(
                    "Services not initialized. Call ServiceHelper.Initialize in MauiProgram.");
            return _services.GetRequiredService<T>();
        }

        // Optional service lookup
        public static T? GetService<T>() where T : class
            => _services?.GetService<T>();
    }
}
