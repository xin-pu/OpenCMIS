using OpenCMIS.Protocol.Abstractions;
using OpenCMIS.Protocol.Abstractions.Models;
using OpenCMIS.Shared;
using OpenCMIS.Transport.Abstractions;
using OpenCMIS.UI.WPF.Models;
using OpenCMIS.UI.WPF.ViewModels;
using Xunit;

namespace OpenCMIS.UI.WPF.Tests;

public sealed class PageEditorViewModelTests
{
    [Fact]
    public void SplitAtPageBoundary_entirely_lower_returns_unchanged()
    {
        var segment = new MsaWriteSegment(0x10, [0xAA, 0xBB]);

        var result = PageEditorViewModel
            .SplitAtPageBoundary(segment)
            .ToList();

        var single = Assert.Single(result);
        Assert.Equal(0x10, single.StartAddress);
        Assert.Equal([0xAA, 0xBB], single.Data);
    }

    [Fact]
    public void SplitAtPageBoundary_entirely_upper_returns_unchanged()
    {
        var segment = new MsaWriteSegment(0xA0, [0xCC, 0xDD]);

        var result = PageEditorViewModel
            .SplitAtPageBoundary(segment)
            .ToList();

        var single = Assert.Single(result);
        Assert.Equal(0xA0, single.StartAddress);
        Assert.Equal([0xCC, 0xDD], single.Data);
    }

    [Fact]
    public void SplitAtPageBoundary_crossing_boundary_splits_at_0x80()
    {
        // Addresses 0x7E, 0x7F (lower) + 0x80, 0x81 (upper)
        var segment = new MsaWriteSegment(0x7E, [0x11, 0x22, 0x33, 0x44]);

        var result = PageEditorViewModel
            .SplitAtPageBoundary(segment)
            .ToList();

        Assert.Equal(2, result.Count);

        Assert.Equal(0x7E, result[0].StartAddress);
        Assert.Equal([0x11, 0x22], result[0].Data);

        Assert.Equal(0x80, result[1].StartAddress);
        Assert.Equal([0x33, 0x44], result[1].Data);
    }

    [Fact]
    public void SplitAtPageBoundary_ending_at_0x80_is_lower()
    {
        var segment = new MsaWriteSegment(0x7F, [0xFF]);

        var result = PageEditorViewModel
            .SplitAtPageBoundary(segment)
            .ToList();

        var single = Assert.Single(result);
        Assert.Equal(0x7F, single.StartAddress);
        // endAddr = 0x7F + 1 = 0x80, which is NOT <= 0x80? 
        // Actually endAddr = 0x80, the condition is `endAddr <= 0x80` → true → lower
        Assert.Equal([0xFF], single.Data);
    }

    [Fact]
    public void SplitAtPageBoundary_starting_at_0x80_is_upper()
    {
        var segment = new MsaWriteSegment(0x80, [0xAA]);

        var result = PageEditorViewModel
            .SplitAtPageBoundary(segment)
            .ToList();

        var single = Assert.Single(result);
        Assert.Equal(0x80, single.StartAddress);
    }

    [Fact]
    public async Task Write_routes_lower_bytes_to_common_page_zero()
    {
        var spy = new SpyRegisterAccess();
        var device = new SpyCmisDevice(spy);
        var vm = new PageEditorViewModel();
        vm.SetDevice(device);

        // Simulate loading a page — load data into the buffer via internal access
        LoadPageBuffer(vm, CreateEmptyPage());
        // Edit a byte in lower range
        SetBufferByte(vm, 0x05, 0xAB);
        // Must also set Bank/Page numbers
        SetProperty(vm, "BankNumber", "01");
        SetProperty(vm, "PageNumber", "10");

        var writeTask = InvokeWritePageAsync(vm);
        await writeTask;

        // Assert: the low-byte edit was written to common page (0,0)
        Assert.Contains(spy.Writes, w =>
            w.Bank == 0 && w.Page == 0 && w.StartAddress == 0x05);
    }

    [Fact]
    public async Task Write_routes_upper_bytes_to_selected_bank_page()
    {
        var spy = new SpyRegisterAccess();
        var device = new SpyCmisDevice(spy);
        var vm = new PageEditorViewModel();
        vm.SetDevice(device);

        LoadPageBuffer(vm, CreateEmptyPage());
        // Edit a byte in upper range
        SetBufferByte(vm, 0xA0, 0xCD);
        SetProperty(vm, "BankNumber", "02");
        SetProperty(vm, "PageNumber", "11");

        var writeTask = InvokeWritePageAsync(vm);
        await writeTask;

        // Assert: the high-byte edit was written to selected bank/page (2, 0x11)
        Assert.Contains(spy.Writes, w =>
            w.Bank == 2 && w.Page == 0x11 && w.StartAddress == 0xA0);
    }

    private static void LoadPageBuffer(PageEditorViewModel vm, byte[] data)
    {
        var buffer = new MsaPageBuffer();
        buffer.Load(data);
        var field = typeof(PageEditorViewModel)
            .GetField("_pageBuffer",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance)!;
        field.SetValue(vm, buffer);
        typeof(PageEditorViewModel)
            .GetProperty("IsLoaded",
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Instance)!
            .SetValue(vm, true);
    }

    private static void SetBufferByte(PageEditorViewModel vm, int address, byte value)
    {
        var field = typeof(PageEditorViewModel)
            .GetField("_pageBuffer",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance)!;
        var buffer = (MsaPageBuffer)field.GetValue(vm)!;
        buffer.SetByte(address, value);
    }

    private static void SetProperty(PageEditorViewModel vm, string name, string value)
    {
        typeof(PageEditorViewModel)
            .GetProperty(name,
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Instance)!
            .SetValue(vm, value);
    }

    private static Task InvokeWritePageAsync(PageEditorViewModel vm)
    {
        var method = typeof(PageEditorViewModel)
            .GetMethod("WritePageAsync",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance)!;
        return (Task)method.Invoke(vm, null)!;
    }

    private static byte[] CreateEmptyPage()
    {
        var page = new byte[256];
        Array.Fill(page, (byte)0x00);
        return page;
    }

    private sealed class SpyRegisterAccess : IRegisterAccess
    {
        public List<(byte Bank, byte Page, byte StartAddress, byte[] Data)> Writes { get; } = [];

        public Task<byte> ReadByteAsync(byte page, byte address) =>
            Task.FromResult((byte)0);

        public Task WriteByteAsync(byte page, byte address, byte value)
        {
            Writes.Add((0, page, address, [value]));
            return Task.CompletedTask;
        }

        public Task<byte[]> ReadBlockAsync(byte page, byte startAddress, int length)
        {
            // Return zeros matching length for read-back
            return Task.FromResult(new byte[length]);
        }

        public Task<byte[]> ReadBlockAsync(byte bank, byte page, byte startAddress, int length)
        {
            return Task.FromResult(new byte[length]);
        }

        public Task WriteBlockAsync(byte page, byte startAddress, byte[] data)
        {
            Writes.Add((0, page, startAddress, data));
            return Task.CompletedTask;
        }

        public Task WriteBlockAsync(byte bank, byte page, byte startAddress, byte[] data)
        {
            Writes.Add((bank, page, startAddress, data));
            return Task.CompletedTask;
        }
    }

    private sealed class SpyCmisDevice(IRegisterAccess registerAccess) : ICmisDevice
    {
        public DeviceInfo DeviceInfo { get; } = new() { Id = "spy", Name = "Spy" };
        public bool IsConnected => true;
        public IRegisterAccess RegisterAccess { get; } = registerAccess;
        public OpenCMIS.Module.Core.Hci.IHciMemoryAccessor? HciAccess => null;

        public Task<ModuleInfo> GetModuleInfoAsync() => throw new NotSupportedException();
        public Task<ModuleStatus> GetStatusAsync() => throw new NotSupportedException();
        public Task SetStateAsync(ModuleState state) => throw new NotSupportedException();
        public Task<ModuleIdentity> ReadModuleIdentityAsync() => throw new NotSupportedException();
        public Task<ModuleMonitors> ReadModuleMonitorsAsync(int laneCount = 4) => throw new NotSupportedException();
        public Task<List<LaneStatus>> ReadLaneStatusAsync(int laneCount = 4) => throw new NotSupportedException();
        public Task<ModuleDashData> ReadModuleDashDataAsync(int laneCount = 4) => throw new NotSupportedException();
        public Task CloseAsync() => Task.CompletedTask;
    }
}
