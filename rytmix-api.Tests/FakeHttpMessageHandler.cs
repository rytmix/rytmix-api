using System.Net;

namespace rytmix_api.Tests;

/// <summary>
/// A test double for <see cref="HttpMessageHandler"/> that returns a canned
/// response instead of hitting the network — so we can unit-test
/// <c>JamendoService</c>'s parsing/mapping without a real Jamendo call. Also
/// records how many requests it received, to assert that blank queries never
/// reach the wire.
/// </summary>
internal sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _statusCode;
    private readonly string _jsonBody;

    public int CallCount { get; private set; }
    public Uri? LastRequestUri { get; private set; }

    public FakeHttpMessageHandler(string jsonBody, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        _jsonBody = jsonBody;
        _statusCode = statusCode;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        CallCount++;
        LastRequestUri = request.RequestUri;

        var response = new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(_jsonBody, System.Text.Encoding.UTF8, "application/json"),
        };
        return Task.FromResult(response);
    }
}
