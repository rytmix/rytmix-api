using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using rytmix_api.Controllers;
using rytmix_api.Models;
using rytmix_api.Services;

namespace rytmix_api.Tests;

/// <summary>
/// Unit tests for <see cref="TracksController"/>, using a hand-written stub
/// <see cref="IJamendoService"/> (no mocking library). Covers the empty-query
/// path: a blank <c>q</c> must short-circuit to 400 without touching the service.
/// </summary>
public class TracksControllerTests
{
    private sealed class StubJamendoService : IJamendoService
    {
        public int CallCount { get; private set; }
        public IReadOnlyList<TrackDto> ToReturn { get; init; } = [];
        public Exception? ToThrow { get; init; }

        public Task<IReadOnlyList<TrackDto>> SearchTracksAsync(
            string query, CancellationToken cancellationToken = default)
        {
            CallCount++;
            if (ToThrow is not null)
            {
                throw ToThrow;
            }
            return Task.FromResult(ToReturn);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Search_BlankQuery_ReturnsBadRequest_WithoutCallingService(string? q)
    {
        var stub = new StubJamendoService();
        var controller = new TracksController(stub);

        var result = await controller.Search(q, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(0, stub.CallCount);
    }

    [Fact]
    public async Task Search_ValidQuery_ReturnsOkWithTracks()
    {
        var track = new TrackDto("1", "Title", "Artist", 100, "art", "stream");
        var stub = new StubJamendoService { ToReturn = [track] };
        var controller = new TracksController(stub);

        var result = await controller.Search("hello", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var tracks = Assert.IsAssignableFrom<IReadOnlyList<TrackDto>>(ok.Value);
        Assert.Single(tracks);
        Assert.Equal(1, stub.CallCount);
    }

    [Fact]
    public async Task Search_ServiceThrowsJamendoException_Returns502BadGateway()
    {
        var stub = new StubJamendoService
        {
            ToThrow = new JamendoServiceException("upstream boom"),
        };
        var controller = new TracksController(stub);

        var result = await controller.Search("hello", CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status502BadGateway, objectResult.StatusCode);
        Assert.Equal(1, stub.CallCount);
    }
}
