namespace NavigationES.Client.Services.Environment
{
    public class DevelopmentEnvironmentService : IEnvironmentService
    {
        // Physical device detection
        public bool IsPhysicalDevice => !DeviceInfo.DeviceType.Equals(DeviceType.Virtual);

        // Emulator URLs
        private const string EmulatorUrl = "http://10.0.2.2:7136/";

        // Physical device URLs
        // Android: localhost tunneled through USB via "adb reverse tcp:7136 tcp:7136" (recreated
        // automatically by the _AdbReverseForLocalApi MSBuild target on every Debug build) —
        // required for Google login, since Google accepts localhost but refuses LAN IPs as redirect URIs.
        // iOS: has no adb-reverse equivalent, so the app reaches the PC over the LAN IP instead;
        // adjust the IP below to your dev machine's LAN address. Google login therefore can't run
        // against the local API on iOS — test that flow against the deployed API (or a public tunnel).
        private static string PhysicalDeviceUrl => DeviceInfo.Platform == DevicePlatform.Android
            ? "http://localhost:7136/"
            : "http://192.168.1.136:7136/";

        public string ApiBaseUrl => IsPhysicalDevice
            ? PhysicalDeviceUrl
            : EmulatorUrl;

        public string Name => IsPhysicalDevice ? "Development-Physical" : "Development-Emulator";
    }
}
