namespace TopPicksPlus.Artwork;

/// <summary>
/// What a drawing call gives back: either PNG bytes, or "failed" with a short reason. Drawing never throws (D15); the caller uses the plain
/// backdrop and sets <c>artworkFailed</c> when <see cref="Failed"/> is true.
/// </summary>
public sealed class ArtworkResult
{
    private ArtworkResult(byte[]? bytes, string? failureReason)
    {
        Bytes = bytes;
        FailureReason = failureReason;
    }

    /// <summary>True when nothing was drawn. <see cref="Bytes"/> is then null.</summary>
    public bool Failed => Bytes == null;

    /// <summary>The encoded picture, or null when <see cref="Failed"/>.</summary>
    public byte[]? Bytes { get; }

    /// <summary>Why it failed (for the log), or null when it worked.</summary>
    public string? FailureReason { get; }

    internal static ArtworkResult Drawn(byte[] bytes) => new ArtworkResult(bytes, null);

    internal static ArtworkResult Fail(string reason) => new ArtworkResult(null, reason);
}
