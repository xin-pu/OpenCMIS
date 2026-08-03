using System.Reflection;
using OpenCMIS.Module.Core.Hci;
using OpenCMIS.Protocol.Abstractions;
using OpenCMIS.Protocol.Abstractions.Models;
using OpenCMIS.Shared;
using OpenCMIS.Transport.Abstractions;
using OpenCMIS.UI.WPF.Models;
using OpenCMIS.UI.WPF.ViewModels;
using Xunit;

namespace OpenCMIS.UI.WPF.Tests
{
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
            Assert.Equal(0x10,         single.StartAddress);
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
            Assert.Equal(0xA0,         single.StartAddress);
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

            Assert.Equal(0x7E,         result[0].StartAddress);
            Assert.Equal([0x11, 0x22], result[0].Data);

            Assert.Equal(0x80,         result[1].StartAddress);
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
            var spy    = new SpyRegisterAccess();
            var device = new SpyCmisDevice(spy);
            var vm     = new PageEditorViewModel();
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
            Assert.Contains(spy.Writes,
                            w =>
                                    w.Bank == 0 && w.Page == 0 && w.StartAddress == 0x05);
        }

        [Fact]
        public async Task Write_routes_upper_bytes_to_selected_bank_page()
        {
            var spy    = new SpyRegisterAccess();
            var device = new SpyCmisDevice(spy);
            var vm     = new PageEditorViewModel();
            vm.SetDevice(device);

            LoadPageBuffer(vm, CreateEmptyPage());

            // Edit a byte in upper range
            SetBufferByte(vm, 0xA0, 0xCD);
            SetProperty(vm, "BankNumber", "02");
            SetProperty(vm, "PageNumber", "11");

            var writeTask = InvokeWritePageAsync(vm);
            await writeTask;

            // Assert: the high-byte edit was written to selected bank/page (2, 0x11)
            Assert.Contains(spy.Writes,
                            w =>
                                    w.Bank == 2 && w.Page == 0x11 && w.StartAddress == 0xA0);
        }

        [Fact]
        public void ReadRange_invalid_bank_shows_error()
        {
            var vm = new PageEditorViewModel();
            SetProperty(vm, "BankNumber",   "GG");
            SetProperty(vm, "PageNumber",   "00");
            SetProperty(vm, "StartAddress", "80");
            SetProperty(vm, "ReadLength",   "10");

            // Execute ReadRangeAsync via reflection (no device, so it will
            // bail out at validation before touching _device).
            InvokeReadRangeAsync(vm).GetAwaiter().GetResult();

            Assert.Contains("Invalid bank number", GetStatusMessage(vm));
        }

        [Fact]
        public void ReadRange_invalid_page_shows_error()
        {
            var vm = new PageEditorViewModel();
            SetProperty(vm, "BankNumber",   "00");
            SetProperty(vm, "PageNumber",   "ZZ");
            SetProperty(vm, "StartAddress", "80");
            SetProperty(vm, "ReadLength",   "10");

            InvokeReadRangeAsync(vm).GetAwaiter().GetResult();

            Assert.Contains("Invalid page number", GetStatusMessage(vm));
        }

        [Fact]
        public void ReadRange_invalid_start_address_shows_error()
        {
            var vm = new PageEditorViewModel();
            SetProperty(vm, "BankNumber",   "00");
            SetProperty(vm, "PageNumber",   "00");
            SetProperty(vm, "StartAddress", "1G");
            SetProperty(vm, "ReadLength",   "10");

            InvokeReadRangeAsync(vm).GetAwaiter().GetResult();

            Assert.Contains("Invalid start address", GetStatusMessage(vm));
        }

        [Fact]
        public void ReadRange_invalid_length_shows_error()
        {
            var vm = new PageEditorViewModel();
            SetProperty(vm, "BankNumber",   "00");
            SetProperty(vm, "PageNumber",   "00");
            SetProperty(vm, "StartAddress", "80");
            SetProperty(vm, "ReadLength",   "XX");

            InvokeReadRangeAsync(vm).GetAwaiter().GetResult();

            Assert.Contains("Invalid read length", GetStatusMessage(vm));
        }

        [Fact]
        public void ReadRange_zero_length_shows_error()
        {
            var vm = new PageEditorViewModel();
            SetProperty(vm, "BankNumber",   "00");
            SetProperty(vm, "PageNumber",   "00");
            SetProperty(vm, "StartAddress", "80");
            SetProperty(vm, "ReadLength",   "00");

            InvokeReadRangeAsync(vm).GetAwaiter().GetResult();

            Assert.Contains("1–FF", GetStatusMessage(vm));
        }

        [Fact]
        public void ReadRange_exceeds_page_boundary_shows_error()
        {
            var vm = new PageEditorViewModel();
            SetProperty(vm, "BankNumber",   "00");
            SetProperty(vm, "PageNumber",   "00");
            SetProperty(vm, "StartAddress", "F0");
            SetProperty(vm, "ReadLength",   "20"); // 0xF0 + 0x20 = 0x110 > 256

            InvokeReadRangeAsync(vm).GetAwaiter().GetResult();

            Assert.Contains("exceeds page boundary", GetStatusMessage(vm));
        }

        [Fact]
        public async Task ReadRange_lower_only_reads_from_common_page_zero()
        {
            var spy    = new SpyRegisterAccess();
            var device = new SpyCmisDevice(spy);
            var vm     = new PageEditorViewModel();
            vm.SetDevice(device);
            LoadPageBuffer(vm, CreateEmptyPage());
            SetProperty(vm, "BankNumber",   "02");
            SetProperty(vm, "PageNumber",   "11");
            SetProperty(vm, "StartAddress", "10");
            SetProperty(vm, "ReadLength",   "08");

            await InvokeReadRangeAsync(vm);

            // Should have read from (0, 0, 0x10, 8), NOT from (2, 0x11)
            Assert.Contains(spy.Reads,
                            r =>
                                    r.Bank == 0 && r.Page == 0 && r.StartAddress == 0x10 && r.Length == 8);
            Assert.DoesNotContain(spy.Reads, r => r.Bank != 0);
        }

        [Fact]
        public async Task ReadRange_upper_only_reads_from_selected_bank_page()
        {
            var spy    = new SpyRegisterAccess();
            var device = new SpyCmisDevice(spy);
            var vm     = new PageEditorViewModel();
            vm.SetDevice(device);
            LoadPageBuffer(vm, CreateEmptyPage());
            SetProperty(vm, "BankNumber",   "03");
            SetProperty(vm, "PageNumber",   "1F");
            SetProperty(vm, "StartAddress", "A0");
            SetProperty(vm, "ReadLength",   "10");

            await InvokeReadRangeAsync(vm);

            Assert.Contains(spy.Reads,
                            r =>
                                    r.Bank == 3 && r.Page == 0x1F && r.StartAddress == 0xA0 && r.Length == 0x10);
        }

        [Fact]
        public async Task ReadRange_crossing_boundary_reads_both_pages()
        {
            var spy    = new SpyRegisterAccess();
            var device = new SpyCmisDevice(spy);
            var vm     = new PageEditorViewModel();
            vm.SetDevice(device);
            LoadPageBuffer(vm, CreateEmptyPage());
            SetProperty(vm, "BankNumber",   "01");
            SetProperty(vm, "PageNumber",   "05");
            SetProperty(vm, "StartAddress", "70");
            SetProperty(vm, "ReadLength",   "20"); // 0x70..0x8F, crosses 0x80

            await InvokeReadRangeAsync(vm);

            // Lower part from common page
            Assert.Contains(spy.Reads,
                            r =>
                                    r.Bank == 0 && r.Page == 0 && r.StartAddress == 0x70 && r.Length == 0x10);

            // Upper part from selected bank/page
            Assert.Contains(spy.Reads,
                            r =>
                                    r.Bank == 1 && r.Page == 5 && r.StartAddress == 0x80 && r.Length == 0x10);
        }

        [Fact]
        public async Task ReadRange_preserves_unread_bytes_from_previous_load()
        {
            // Load a full page with known values (all 0xAA)
            var original = new byte[256];
            Array.Fill(original, (byte) 0xAA);

            var spy    = new SpyRegisterAccess();
            var device = new SpyCmisDevice(spy);
            var vm     = new PageEditorViewModel();
            vm.SetDevice(device);
            LoadPageBuffer(vm, original);

            // Range read only 0x80..0x87 (8 bytes). Spy returns zeroes.
            SetProperty(vm, "BankNumber",   "00");
            SetProperty(vm, "PageNumber",   "00");
            SetProperty(vm, "StartAddress", "80");
            SetProperty(vm, "ReadLength",   "08");

            await InvokeReadRangeAsync(vm);

            // Outside the range: preserved from original load (0xAA, not 0x00)
            Assert.Equal(0xAA, GetBufferByte(vm, 0x00));
            Assert.Equal(0xAA, GetBufferByte(vm, 0x7F));
            Assert.Equal(0xAA, GetBufferByte(vm, 0x88));
            Assert.Equal(0xAA, GetBufferByte(vm, 0xFF));

            // Inside the range: updated by the spy read (0x00)
            Assert.Equal(0x00, GetBufferByte(vm, 0x80));
            Assert.Equal(0x00, GetBufferByte(vm, 0x87));
        }

        [Fact]
        public void ReadRange_without_prior_page_load_shows_error()
        {
            var spy    = new SpyRegisterAccess();
            var device = new SpyCmisDevice(spy);
            var vm     = new PageEditorViewModel();
            vm.SetDevice(device);

            // No LoadPageBuffer — simulate first use
            SetProperty(vm, "BankNumber",   "00");
            SetProperty(vm, "PageNumber",   "00");
            SetProperty(vm, "StartAddress", "80");
            SetProperty(vm, "ReadLength",   "08");

            InvokeReadRangeAsync(vm).GetAwaiter().GetResult();

            Assert.Contains("Load a full page first", GetStatusMessage(vm));

            // No reads should have been issued against hardware
            Assert.Empty(spy.Reads);
        }

        private static byte GetBufferByte(PageEditorViewModel vm, int address)
        {
            var field = typeof(PageEditorViewModel)
                   .GetField("_pageBuffer",
                             BindingFlags.NonPublic |
                             BindingFlags.Instance)!;
            var buffer = (MsaPageBuffer) field.GetValue(vm)!;
            return buffer.GetByte(address);
        }

        private static void LoadPageBuffer(PageEditorViewModel vm, byte[] data)
        {
            var buffer = new MsaPageBuffer();
            buffer.Load(data);
            var field = typeof(PageEditorViewModel)
                   .GetField("_pageBuffer",
                             BindingFlags.NonPublic |
                             BindingFlags.Instance)!;
            field.SetValue(vm, buffer);
            typeof(PageEditorViewModel)
                   .GetProperty("IsLoaded",
                                BindingFlags.Public |
                                BindingFlags.Instance)!
                   .SetValue(vm, true);
        }

        private static void SetBufferByte(PageEditorViewModel vm, int address, byte value)
        {
            var field = typeof(PageEditorViewModel)
                   .GetField("_pageBuffer",
                             BindingFlags.NonPublic |
                             BindingFlags.Instance)!;
            var buffer = (MsaPageBuffer) field.GetValue(vm)!;
            buffer.SetByte(address, value);
        }

        private static void SetProperty(PageEditorViewModel vm, string name, string value)
        {
            typeof(PageEditorViewModel)
                   .GetProperty(name,
                                BindingFlags.Public |
                                BindingFlags.Instance)!
                   .SetValue(vm, value);
        }

        private static Task InvokeWritePageAsync(PageEditorViewModel vm)
        {
            var method = typeof(PageEditorViewModel)
                   .GetMethod("WritePageAsync",
                              BindingFlags.NonPublic |
                              BindingFlags.Instance)!;
            return (Task) method.Invoke(vm, null)!;
        }

        private static Task InvokeReadRangeAsync(PageEditorViewModel vm)
        {
            var method = typeof(PageEditorViewModel)
                   .GetMethod("ReadRangeAsync",
                              BindingFlags.NonPublic |
                              BindingFlags.Instance)!;
            return (Task) method.Invoke(vm, null)!;
        }

        private static string GetStatusMessage(PageEditorViewModel vm)
        {
            return (string) typeof(PageEditorViewModel)
                           .GetProperty("StatusMessage",
                                        BindingFlags.Public |
                                        BindingFlags.Instance)!
                           .GetValue(vm)!;
        }

        private static byte[] CreateEmptyPage()
        {
            var page = new byte[256];
            Array.Fill(page, (byte) 0x00);
            return page;
        }

        private sealed class SpyRegisterAccess : IRegisterAccess
        {
            public List<(byte Bank, byte Page, byte StartAddress, byte[] Data)> Writes { get; } = [];
            public List<(byte Bank, byte Page, byte StartAddress, int Length)>  Reads  { get; } = [];

            public Task<byte> ReadByteAsync(byte page, byte address)
            {
                return Task.FromResult((byte) 0);
            }

            public Task WriteByteAsync(byte page, byte address, byte value)
            {
                Writes.Add((0, page, address, [value]));
                return Task.CompletedTask;
            }

            public Task<byte[]> ReadBlockAsync(byte page, byte startAddress, int length)
            {
                Reads.Add((0, page, startAddress, length));
                return Task.FromResult(new byte[length]);
            }

            public Task<byte[]> ReadBlockAsync(byte bank, byte page, byte startAddress, int length)
            {
                Reads.Add((bank, page, startAddress, length));
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
            public DeviceInfo          DeviceInfo     { get; } = new () {Id = "spy", Name = "Spy"};
            public bool                IsConnected    => true;
            public IRegisterAccess     RegisterAccess { get; } = registerAccess;
            public IHciMemoryAccessor? HciAccess      => null;

            public Task<ModuleInfo> GetModuleInfoAsync()
            {
                throw new NotSupportedException();
            }

            public Task<ModuleStatus> GetStatusAsync()
            {
                throw new NotSupportedException();
            }

            public Task SetStateAsync(ModuleState state)
            {
                throw new NotSupportedException();
            }

            public Task<ModuleIdentity> ReadModuleIdentityAsync()
            {
                throw new NotSupportedException();
            }

            public Task<ModuleMonitors> ReadModuleMonitorsAsync(int laneCount = 4)
            {
                throw new NotSupportedException();
            }

            public Task<List<LaneStatus>> ReadLaneStatusAsync(int laneCount = 4)
            {
                throw new NotSupportedException();
            }

            public Task<ModuleDashData> ReadModuleDashDataAsync(int laneCount = 4)
            {
                throw new NotSupportedException();
            }

            public Task CloseAsync()
            {
                return Task.CompletedTask;
            }
        }
    }
}
