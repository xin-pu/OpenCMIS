namespace OpenCMIS.Transport.Abstractions;

/// <summary>
/// Captures a non-fatal adapter discovery failure.
/// </summary>
public sealed record I2cProbeFailure(
    string AdapterId,
    string Candidate,
    string Message);
