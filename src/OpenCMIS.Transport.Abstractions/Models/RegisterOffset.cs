namespace OpenCMIS.Transport.Abstractions
{
    /// <summary>
    ///     Represents an eight-bit register offset.
    /// </summary>
    public readonly record struct RegisterOffset(byte Value)
    {
        public override string ToString()
        {
            return $"0x{Value:X2}";
        }
    }
}
