namespace OpenCMIS.Transport.Abstractions
{
    /// <summary>
    ///     Represents a canonical 7-bit I2C device address.
    /// </summary>
    public readonly record struct I2cDeviceAddress
    {
        public I2cDeviceAddress(byte value)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, (byte) 0x7F);
            Value = value;
        }

        public byte Value { get; }

        public static I2cDeviceAddress FromLegacy8Bit(byte value)
        {
            if ((value & 0x01) != 0)
            {
                throw new ArgumentOutOfRangeException(
                        nameof(value),
                        value,
                        "Expected an 8-bit I2C write address.");
            }

            return new ((byte) (value >> 1));
        }

        public byte ToWriteAddress8Bit()
        {
            return (byte) (Value << 1);
        }

        public override string ToString()
        {
            return $"0x{Value:X2}";
        }
    }
}
