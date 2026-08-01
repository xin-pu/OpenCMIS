using OpenCMIS.UI.WPF.Models;
using Xunit;

namespace OpenCMIS.UI.WPF.Tests;

public sealed class MsaPageBufferTests
{
    [Fact]
    public void Returning_byte_to_original_removes_change()
    {
        var buffer = LoadedSequence();

        buffer.SetByte(0x82, 0xFF);
        buffer.SetByte(0x82, 0x82);

        Assert.Empty(buffer.Changes);
    }

    [Fact]
    public void Dirty_bytes_are_grouped_into_contiguous_segments()
    {
        var buffer = new MsaPageBuffer();
        buffer.Load(new byte[256]);
        buffer.SetByte(0x80, 0x11);
        buffer.SetByte(0x81, 0x22);
        buffer.SetByte(0x84, 0x44);

        var segments = buffer.BuildWriteSegments(fullPage: false);

        Assert.Collection(
            segments,
            segment =>
            {
                Assert.Equal(0x80, segment.StartAddress);
                Assert.Equal(new byte[] { 0x11, 0x22 }, segment.Data);
            },
            segment =>
            {
                Assert.Equal(0x84, segment.StartAddress);
                Assert.Equal(new byte[] { 0x44 }, segment.Data);
            });
    }

    [Theory]
    [InlineData(0)]
    [InlineData(255)]
    [InlineData(257)]
    public void Load_rejects_non_page_lengths(int length)
    {
        var buffer = new MsaPageBuffer();

        Assert.Throws<ArgumentException>(() => buffer.Load(new byte[length]));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(256)]
    public void Byte_access_rejects_addresses_outside_page(int address)
    {
        var buffer = LoadedSequence();

        Assert.Throws<ArgumentOutOfRangeException>(() => buffer.GetByte(address));
        Assert.Throws<ArgumentOutOfRangeException>(() => buffer.SetByte(address, 0));
    }

    [Fact]
    public void Full_page_write_is_one_defensive_segment()
    {
        var source = Enumerable.Range(0, 256).Select(value => (byte)value).ToArray();
        var buffer = new MsaPageBuffer();
        buffer.Load(source);

        var segment = Assert.Single(buffer.BuildWriteSegments(fullPage: true));
        source[0] = 0xFF;

        Assert.Equal(0, segment.StartAddress);
        Assert.Equal(256, segment.Data.Length);
        Assert.Equal(0, segment.Data[0]);
    }

    [Fact]
    public void Matching_read_back_replaces_snapshot_and_clears_changes()
    {
        var buffer = LoadedSequence();
        buffer.SetByte(0x80, 0xAA);
        var readBack = Enumerable.Range(0, 256).Select(value => (byte)value).ToArray();
        readBack[0x80] = 0xAA;

        var verified = buffer.ApplyVerifiedReadBack(readBack);

        Assert.True(verified);
        Assert.Empty(buffer.Changes);
        Assert.Equal(0xAA, buffer.GetByte(0x80));
    }

    [Fact]
    public void Mismatched_read_back_preserves_user_changes()
    {
        var buffer = LoadedSequence();
        buffer.SetByte(0x80, 0xAA);
        var readBack = Enumerable.Range(0, 256).Select(value => (byte)value).ToArray();

        var verified = buffer.ApplyVerifiedReadBack(readBack);

        Assert.False(verified);
        var change = Assert.Single(buffer.Changes);
        Assert.Equal(new MsaByteChange(0x80, 0x80, 0xAA), change);
        Assert.Equal(0xAA, buffer.GetByte(0x80));
    }

    private static MsaPageBuffer LoadedSequence()
    {
        var buffer = new MsaPageBuffer();
        buffer.Load(Enumerable.Range(0, 256).Select(value => (byte)value).ToArray());
        return buffer;
    }
}
