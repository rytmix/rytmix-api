namespace rytmix_api.Services;

/// <summary>
/// Thrown when the upstream Jamendo call fails — a network/timeout error, a
/// malformed payload, or a non-success status from Jamendo itself. The
/// controller catches this and translates it into a 502 (Bad Gateway), so the
/// client gets a clean error instead of a raw exception.
/// </summary>
public sealed class JamendoServiceException : Exception
{
    public JamendoServiceException(string message) : base(message)
    {
    }

    public JamendoServiceException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
