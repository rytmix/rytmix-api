using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using rytmix_api.Models;

namespace rytmix_api.Services;

/// <summary>
/// Default <see cref="IJamendoService"/>. Uses a typed <see cref="HttpClient"/>
/// (configured via <c>IHttpClientFactory</c> in <c>Program.cs</c>) to call
/// Jamendo's <c>/tracks/</c> endpoint server-side, then maps the response into
/// our own <see cref="TrackDto"/>.
/// </summary>
public sealed class JamendoService : IJamendoService
{
    // Jamendo returns 10 results by default; we request a fuller first page.
    // Pagination is out of scope for P1-1, so this stays a fixed page size.
    private const int SearchResultLimit = 20;

    private readonly HttpClient _httpClient;
    private readonly JamendoOptions _options;
    private readonly ILogger<JamendoService> _logger;

    public JamendoService(
        HttpClient httpClient,
        IOptions<JamendoOptions> options,
        ILogger<JamendoService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<TrackDto>> SearchTracksAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        // Blank query → nothing to search. The controller already rejects this
        // with a 400, but we guard here too so the service is safe on its own.
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        // Build "tracks/?client_id=...&format=json&limit=20&search=<query>".
        // The client id is added here, server-side, and never leaves the API.
        var requestUri = QueryHelpers.AddQueryString("tracks/", new Dictionary<string, string?>
        {
            ["client_id"] = _options.ClientId,
            ["format"] = "json",
            ["limit"] = SearchResultLimit.ToString(),
            ["search"] = query,
        });

        JamendoSearchResponse? payload;
        try
        {
            payload = await _httpClient.GetFromJsonAsync<JamendoSearchResponse>(requestUri, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            // Network failure, timeout, or unparseable body — surface a clean
            // error the controller turns into a 502 rather than a 500 stack trace.
            _logger.LogError(ex, "Jamendo search request failed for query {Query}.", query);
            throw new JamendoServiceException("The music service is currently unavailable.", ex);
        }

        // Jamendo answers 200 even when it rejects the request (bad key, quota,
        // etc.); the real outcome is in headers.status.
        if (payload?.Headers is { Status: { } status } &&
            !string.Equals(status, "success", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogError(
                "Jamendo returned a non-success status '{Status}': {Message}",
                status, payload.Headers.ErrorMessage);
            throw new JamendoServiceException("The music service returned an error.");
        }

        if (payload?.Results is null || payload.Results.Count == 0)
        {
            return [];
        }

        // Only return usable tracks: one with no id can't be fetched later, and
        // one with no audio url can't be played — drop those before mapping.
        return payload.Results
            .Where(track => !string.IsNullOrEmpty(track.Id) && !string.IsNullOrEmpty(track.Audio))
            .Select(MapToDto)
            .ToList();
    }

    /// <summary>Maps one Jamendo track onto our DTO, defaulting any missing field.</summary>
    private static TrackDto MapToDto(JamendoTrack track) => new(
        Id: track.Id ?? string.Empty,
        Title: track.Name ?? "Unknown title",
        Artist: track.ArtistName ?? "Unknown artist",
        Duration: track.Duration,
        ArtworkUrl: track.AlbumImage ?? string.Empty,
        StreamUrl: track.Audio ?? string.Empty);
}
