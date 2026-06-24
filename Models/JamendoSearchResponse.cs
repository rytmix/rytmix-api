using System.Text.Json.Serialization;

namespace rytmix_api.Models;

// Internal shapes used only to deserialize Jamendo's /tracks/ response. We
// declare just the fields we actually map; everything else is ignored. These
// are never exposed to clients — they get mapped into TrackDto first.
//
// Jamendo returns snake_case JSON, so each property is pinned with an explicit
// [JsonPropertyName] rather than relying on a naming policy.

internal sealed class JamendoSearchResponse
{
    [JsonPropertyName("headers")]
    public JamendoHeaders? Headers { get; set; }

    [JsonPropertyName("results")]
    public List<JamendoTrack>? Results { get; set; }
}

internal sealed class JamendoHeaders
{
    // "success" on a good response; anything else means Jamendo rejected the
    // request (bad key, quota, etc.) even though the HTTP status is 200.
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("error_message")]
    public string? ErrorMessage { get; set; }
}

internal sealed class JamendoTrack
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("artist_name")]
    public string? ArtistName { get; set; }

    [JsonPropertyName("duration")]
    public int Duration { get; set; }

    [JsonPropertyName("album_image")]
    public string? AlbumImage { get; set; }

    [JsonPropertyName("audio")]
    public string? Audio { get; set; }
}
