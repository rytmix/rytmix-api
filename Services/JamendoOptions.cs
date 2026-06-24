namespace rytmix_api.Services;

/// <summary>
/// Strongly-typed Jamendo configuration, bound from the "Jamendo" config
/// section in <c>Program.cs</c>.
///
/// The <see cref="ClientId"/> is a secret: it is read from configuration
/// (env var <c>Jamendo__ClientId</c> on Render, or gitignored
/// <c>appsettings.Development.json</c> / user-secrets locally) and is NEVER
/// committed or sent to the frontend — that is the whole point of proxying
/// Jamendo through this API.
/// </summary>
public sealed class JamendoOptions
{
    public const string SectionName = "Jamendo";

    /// <summary>Jamendo API client id (secret — supplied via configuration).</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>Jamendo API base URL; the typed HttpClient's BaseAddress.</summary>
    public string BaseUrl { get; set; } = "https://api.jamendo.com/v3.0/";
}
