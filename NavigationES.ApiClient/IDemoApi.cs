using Refit;
using NavigationES.Shared.Dtos;

namespace NavigationES.ApiClient
{
    // Anonymous: the no-account mini test on /prueba.
    public interface IDemoApi
    {
        [Get("/api/demo")]
        Task<DemoTestDto> GetDemoAsync(string? licencia = null, string? tema = null);
    }
}
