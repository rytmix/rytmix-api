using Microsoft.AspNetCore.Mvc;
using rytmix_api.Models;
using rytmix_api.Services;

namespace rytmix_api.Controllers;

/// <summary>
/// Public, read-only music endpoints backed by the Jamendo proxy. No auth —
/// browsing/searching music is open; only the later write endpoints
/// (playlists, favorites) require a JWT.
/// </summary>
[ApiController]
[Route("api/tracks")]
public class TracksController : ControllerBase
{
    private readonly IJamendoService _jamendo;

    public TracksController(IJamendoService jamendo)
    {
        _jamendo = jamendo;
    }

    /// <summary>
    /// GET /api/tracks/search?q=...
    /// Searches Jamendo and returns our <see cref="TrackDto"/> list.
    /// </summary>
    /// <returns>
    /// 200 with the (possibly empty) results · 400 if <paramref name="q"/> is
    /// missing/blank · 502 if the upstream Jamendo call fails.
    /// </returns>
    [HttpGet("search")]
    [ProducesResponseType(typeof(IReadOnlyList<TrackDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<IReadOnlyList<TrackDto>>> Search(
        [FromQuery] string? q,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return BadRequest(new { error = "Query parameter 'q' is required." });
        }

        try
        {
            var tracks = await _jamendo.SearchTracksAsync(q, cancellationToken);
            return Ok(tracks);
        }
        catch (JamendoServiceException ex)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { error = ex.Message });
        }
    }
}
