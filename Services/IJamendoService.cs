using rytmix_api.Models;

namespace rytmix_api.Services;

/// <summary>
/// Server-side gateway to the Jamendo music API. Keeps the Jamendo client id
/// server-side and returns our own <see cref="TrackDto"/> shape, so controllers
/// stay thin and the upstream provider stays swappable.
/// </summary>
public interface IJamendoService
{
    /// <summary>
    /// Searches Jamendo for tracks matching <paramref name="query"/>.
    /// Returns an empty list when the query is blank or nothing matches;
    /// throws <see cref="JamendoServiceException"/> if the upstream call fails.
    /// </summary>
    Task<IReadOnlyList<TrackDto>> SearchTracksAsync(string query, CancellationToken cancellationToken = default);
}
