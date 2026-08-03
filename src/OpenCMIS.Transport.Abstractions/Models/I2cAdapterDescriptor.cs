namespace OpenCMIS.Transport.Abstractions
{
    /// <summary>
    ///     Describes a discoverable I2C adapter endpoint.
    /// </summary>
    public sealed record I2cAdapterDescriptor(string               AdapterId,
                                              string               DeviceId,
                                              string               DisplayName,
                                              I2cConnectionProfile Profile);
}
