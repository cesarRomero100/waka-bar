namespace WakaBar.Core;

/// <summary>The server answered with a non-success status. The UI shows its own localized text per status.</summary>
public sealed class WakaTimeApiException : Exception
{
    public WakaTimeApiException(int statusCode)
        : base(statusCode == 401 ? "Invalid token." : $"Server returned HTTP {statusCode}.")
    {
        StatusCode = statusCode;
    }

    public int StatusCode { get; }

    public bool IsUnauthorized => StatusCode == 401;

    /// <summary>Since November 1, 2026 WakaTime refuses reads with an API key: only OAuth tokens can read stats.</summary>
    public bool IsForbidden => StatusCode == 403;
}
