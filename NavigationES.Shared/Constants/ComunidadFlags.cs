namespace NavigationES.Shared.Constants;

// Flag image per autonomous community, keyed by the seeded ComunidadAutonoma ID.
// The same file name exists in the web (wwwroot/images/flags) and in the app
// (Resources/Images) — 144×96 PNGs rendered from the Wikimedia Commons SVGs.
public static class ComunidadFlags
{
    private static readonly Dictionary<long, string> Files = new()
    {
        [1] = "flag_andalucia.png",
        [2] = "flag_cantabria.png",
        [3] = "flag_cataluna.png",
        [4] = "flag_ceuta.png",
        [5] = "flag_melilla.png",
        [6] = "flag_madrid.png",
        [7] = "flag_valenciana.png",
        [8] = "flag_galicia.png",
        [9] = "flag_baleares.png",
        [10] = "flag_canarias.png",
        [11] = "flag_pais_vasco.png",
        [12] = "flag_asturias.png",
        [13] = "flag_murcia.png",
    };

    // Null for a community added later without a flag — callers then show the name only.
    public static string? FileName(long? comunidadId) =>
        comunidadId is { } id && Files.TryGetValue(id, out var file) ? file : null;

    // Web path, relative to the app base.
    public static string? WebPath(long? comunidadId) =>
        FileName(comunidadId) is { } file ? $"images/flags/{file}" : null;
}
