using OpenCMIS.Transport.I2C.Cypress;

namespace OpenCMIS.Transport.I2C.Cypress.Tests.Fakes;

internal sealed class MockCypressDeviceApi : ICypressDeviceApi
{
    public List<CypressDeviceDescriptor> Devices { get; } = [];

    public Queue<byte[]> ReadResults { get; } = [];

    public List<CypressTransferCall> Calls { get; } = [];

    public bool TransferResult { get; set; } = true;

    public bool OpenResult { get; set; } = true;

    public Action? OnWrite { get; set; }

    public string? OpenedSerialNumber { get; private set; }

    public int CloseCount { get; private set; }

    public bool IsDisposed { get; private set; }

    public IReadOnlyList<CypressDeviceDescriptor> Discover() => Devices;

    public bool Open(string serialNumber)
    {
        OpenedSerialNumber = serialNumber;
        return OpenResult;
    }

    public bool Read(
        int port,
        int speedKhz,
        byte address8Bit,
        int length,
        out byte[] data)
    {
        Calls.Add(new CypressTransferCall(
            CypressTransferDirection.Read,
            port,
            speedKhz,
            address8Bit,
            [],
            length));
        data = ReadResults.Count > 0 ? ReadResults.Dequeue() : new byte[length];
        return TransferResult;
    }

    public bool Write(
        int port,
        int speedKhz,
        byte address8Bit,
        ReadOnlySpan<byte> data)
    {
        OnWrite?.Invoke();
        Calls.Add(new CypressTransferCall(
            CypressTransferDirection.Write,
            port,
            speedKhz,
            address8Bit,
            data.ToArray(),
            0));
        return TransferResult;
    }

    public void Close()
    {
        CloseCount++;
    }

    public ValueTask DisposeAsync()
    {
        IsDisposed = true;
        return ValueTask.CompletedTask;
    }
}

internal sealed record CypressTransferCall(
    CypressTransferDirection Direction,
    int Port,
    int SpeedKhz,
    byte Address8Bit,
    byte[] Data,
    int Length);

internal enum CypressTransferDirection
{
    Read,
    Write
}
