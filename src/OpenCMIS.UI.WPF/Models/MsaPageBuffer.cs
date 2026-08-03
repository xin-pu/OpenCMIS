namespace OpenCMIS.UI.WPF.Models
{
    public sealed class MsaPageBuffer
    {
        public const int PageLength = 256;

        private byte[]? _original;
        private byte[]? _edited;

        public IReadOnlyList<MsaByteChange> Changes
        {
            get
            {
                EnsureLoaded();
                var changes = new List<MsaByteChange>();
                for (var address = 0; address < PageLength; address++)
                    if (_original![address] != _edited![address])
                    {
                        changes.Add(new (
                                            address,
                                            _original[address],
                                            _edited[address]));
                    }

                return changes;
            }
        }

        public void Load(ReadOnlySpan<byte> data)
        {
            ValidatePageLength(data.Length, nameof(data));
            _original = data.ToArray();
            _edited   = data.ToArray();
        }

        public byte GetByte(int address)
        {
            EnsureLoaded();
            ValidateAddress(address);
            return _edited![address];
        }

        public void SetByte(int address, byte value)
        {
            EnsureLoaded();
            ValidateAddress(address);
            _edited![address] = value;
        }

        public IReadOnlyList<MsaWriteSegment> BuildWriteSegments(bool fullPage)
        {
            EnsureLoaded();
            if (fullPage)
                return [new (0, _edited!)];

            var changes = Changes;
            if (changes.Count == 0)
                return [];

            var segments = new List<MsaWriteSegment>();
            var start    = changes[0].Address;
            var data     = new List<byte> {changes[0].Edited};

            for (var index = 1; index < changes.Count; index++)
            {
                var change = changes[index];
                if (change.Address == start + data.Count)
                {
                    data.Add(change.Edited);
                    continue;
                }

                segments.Add(new ((byte) start, data));
                start = change.Address;
                data  = [change.Edited];
            }

            segments.Add(new ((byte) start, data));
            return segments;
        }

        public bool ApplyVerifiedReadBack(ReadOnlySpan<byte> readBack)
        {
            EnsureLoaded();
            ValidatePageLength(readBack.Length, nameof(readBack));
            if (!readBack.SequenceEqual(_edited))
                return false;

            _original = readBack.ToArray();
            _edited   = readBack.ToArray();
            return true;
        }

        private static void ValidatePageLength(int length, string parameterName)
        {
            if (length != PageLength)
            {
                throw new ArgumentException(
                        $"An MSA page buffer must contain exactly {PageLength} bytes.",
                        parameterName);
            }
        }

        private static void ValidateAddress(int address)
        {
            if ((uint) address >= PageLength)
                throw new ArgumentOutOfRangeException(nameof(address), address, null);
        }

        private void EnsureLoaded()
        {
            if (_original is null || _edited is null)
                throw new InvalidOperationException("Load an MSA page before accessing the buffer.");
        }
    }
}
