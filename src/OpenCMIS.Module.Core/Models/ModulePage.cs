namespace OpenCMIS.Module.Core
{
    /// <summary>
    ///     Represents an MSA memory page.
    /// </summary>
    public readonly record struct ModulePage
    {
        public ModulePage(byte value)
                : this(0, value) { }

        public ModulePage(byte bank, byte value)
        {
            Bank  = bank;
            Value = value;
        }

        public byte Bank { get; }

        public byte Value { get; }

        public override string ToString()
        {
            return $"Bank 0x{Bank:X2}, Page 0x{Value:X2}";
        }
    }
}
