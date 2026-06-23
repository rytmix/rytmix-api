namespace rytmix_api.Models;

/// <summary>
/// The track shape Rytmix's own API returns to clients — deliberately a small,
/// stable subset of Jamendo's much larger response.
///
/// The frontend only ever sees this DTO (never raw Jamendo JSON), which keeps
/// the contract clean and lets us swap the upstream music provider later
/// without breaking any client.
/// </summary>
/// <param name="Id">Jamendo track id (used to fetch a single track later).</param>
/// <param name="Title">Track title.</param>
/// <param name="Artist">Performing artist's name.</param>
/// <param name="Duration">Length in whole seconds.</param>
/// <param name="ArtworkUrl">Album/cover image URL (may be empty if upstream omits it).</param>
/// <param name="StreamUrl">Playable audio URL the &lt;audio&gt; element loads.</param>
public record TrackDto(
    string Id,
    string Title,
    string Artist,
    int Duration,
    string ArtworkUrl,
    string StreamUrl);
