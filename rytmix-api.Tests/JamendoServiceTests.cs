using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using rytmix_api.Services;

namespace rytmix_api.Tests;

/// <summary>
/// Unit tests for <see cref="JamendoService"/> — the Jamendo proxy's parsing,
/// mapping, and error handling, exercised through a fake HTTP handler so no
/// real network call happens. Covers the P1-1 acceptance test: TrackDto
/// mapping + no-results handling (blank-query + upstream error included).
/// </summary>
public class JamendoServiceTests
{
    // A realistic single-result Jamendo /tracks/ response (only the fields we map).
    private const string OneTrackJson = """
    {
      "headers": { "status": "success", "results_count": 1 },
      "results": [
        {
          "id": "1886512",
          "name": "Sunny Afternoon",
          "artist_name": "The Easton Ellises",
          "duration": 217,
          "album_image": "https://usercontent.jamendo.com/cover.jpg",
          "audio": "https://prod-1.storage.jamendo.com/track.mp3"
        }
      ]
    }
    """;

    private const string NoResultsJson = """
    { "headers": { "status": "success", "results_count": 0 }, "results": [] }
    """;

    private const string FailedStatusJson = """
    { "headers": { "status": "failed", "error_message": "Your credential is not valid." }, "results": [] }
    """;

    private static JamendoService CreateService(FakeHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.jamendo.com/v3.0/"),
        };
        var options = Options.Create(new JamendoOptions { ClientId = "test-client-id" });
        return new JamendoService(httpClient, options, NullLogger<JamendoService>.Instance);
    }

    [Fact]
    public async Task SearchTracksAsync_MapsJamendoResponse_ToTrackDto()
    {
        var handler = new FakeHttpMessageHandler(OneTrackJson);
        var service = CreateService(handler);

        var results = await service.SearchTracksAsync("sunny");

        var track = Assert.Single(results);
        Assert.Equal("1886512", track.Id);
        Assert.Equal("Sunny Afternoon", track.Title);
        Assert.Equal("The Easton Ellises", track.Artist);
        Assert.Equal(217, track.Duration);
        Assert.Equal("https://usercontent.jamendo.com/cover.jpg", track.ArtworkUrl);
        Assert.Equal("https://prod-1.storage.jamendo.com/track.mp3", track.StreamUrl);
    }

    [Fact]
    public async Task SearchTracksAsync_SendsClientIdAndSearchQuery_ToJamendo()
    {
        var handler = new FakeHttpMessageHandler(NoResultsJson);
        var service = CreateService(handler);

        await service.SearchTracksAsync("daft punk");

        // The secret client id is added server-side, and the query is forwarded.
        var query = handler.LastRequestUri!.Query;
        Assert.Contains("client_id=test-client-id", query);
        Assert.Contains("search=daft", query); // space url-encoded; prefix is enough
    }

    [Fact]
    public async Task SearchTracksAsync_NoResults_ReturnsEmptyList()
    {
        var handler = new FakeHttpMessageHandler(NoResultsJson);
        var service = CreateService(handler);

        var results = await service.SearchTracksAsync("nonexistent-xyz");

        Assert.Empty(results);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SearchTracksAsync_BlankQuery_ReturnsEmpty_WithoutCallingUpstream(string query)
    {
        var handler = new FakeHttpMessageHandler(OneTrackJson);
        var service = CreateService(handler);

        var results = await service.SearchTracksAsync(query);

        Assert.Empty(results);
        Assert.Equal(0, handler.CallCount); // never hit the network
    }

    [Fact]
    public async Task SearchTracksAsync_UpstreamNonSuccessStatus_Throws()
    {
        var handler = new FakeHttpMessageHandler(FailedStatusJson);
        var service = CreateService(handler);

        await Assert.ThrowsAsync<JamendoServiceException>(
            () => service.SearchTracksAsync("anything"));
    }

    [Fact]
    public async Task SearchTracksAsync_UpstreamHttpError_ThrowsJamendoServiceException()
    {
        var handler = new FakeHttpMessageHandler("Service Unavailable", HttpStatusCode.ServiceUnavailable);
        var service = CreateService(handler);

        await Assert.ThrowsAsync<JamendoServiceException>(
            () => service.SearchTracksAsync("anything"));
    }
}
