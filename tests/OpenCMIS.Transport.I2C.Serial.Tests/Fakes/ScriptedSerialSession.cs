using OpenCMIS.Transport.I2C.Serial.Serial;

namespace OpenCMIS.Transport.I2C.Serial.Tests.Fakes
{
    internal sealed record SerialSessionScript(byte[]     Response,
                                               Exception? WriteException = null,
                                               Exception? ReadException  = null);

    internal sealed class ScriptedSerialSessionFactory(params SerialSessionScript[] scripts) : ISerialSessionFactory
    {
        private readonly Queue<SerialSessionScript> _scripts = new (scripts);

        public int CreateCount { get; private set; }

        public List<byte[]> Writes { get; } = [];

        public ISerialSession Create(string portName, int baudRate)
        {
            CreateCount++;
            if (_scripts.Count == 0)
                throw new InvalidOperationException("No serial session script remains.");

            return new ScriptedSerialSession(_scripts.Dequeue(), Writes);
        }
    }

    internal sealed class ScriptedSerialSession(SerialSessionScript script,
                                                ICollection<byte[]> writes) : ISerialSession
    {
        public bool IsOpen { get; private set; }

        public ValueTask OpenAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IsOpen = true;
            return ValueTask.CompletedTask;
        }

        public ValueTask CloseAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IsOpen = false;
            return ValueTask.CompletedTask;
        }

        public ValueTask WriteAsync(ReadOnlyMemory<byte> data,
                                    CancellationToken    cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (script.WriteException is not null)
                return ValueTask.FromException(script.WriteException);

            writes.Add(data.ToArray());
            return ValueTask.CompletedTask;
        }

        public ValueTask ReadExactlyAsync(Memory<byte>      destination,
                                          CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (script.ReadException is not null)
                return ValueTask.FromException(script.ReadException);

            if (script.Response.Length != destination.Length)
            {
                return ValueTask.FromException(
                        new EndOfStreamException(
                                $"Script contains {script.Response.Length} bytes; requested {destination.Length}."));
            }

            script.Response.CopyTo(destination);
            return ValueTask.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            IsOpen = false;
            return ValueTask.CompletedTask;
        }
    }
}
