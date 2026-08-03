using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenCMIS.Protocol.Abstractions;
using OpenCMIS.UI.WPF.Models;

namespace OpenCMIS.UI.WPF.ViewModels
{
    public class PageEditorViewModel : ObservableObject
    {
        private ICmisDevice?   _device;
        private MsaPageBuffer? _pageBuffer;

        [ObservableProperty]
        private string _bankNumber = "0";

        [ObservableProperty]
        private string _pageNumber = "0";

        [ObservableProperty]
        private string _startAddress = "80";

        [ObservableProperty]
        private string _readLength = "128";

        [ObservableProperty]
        private ObservableCollection<HexRowViewModel> _hexRows = [];

        [ObservableProperty]
        private string _pageInfo = "No data loaded";

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private bool _isLoaded;

        public void SetDevice(ICmisDevice? device)
        {
            _device     = device;
            _pageBuffer = null;
            HexRows.Clear();
            PageInfo      = "No data loaded";
            StatusMessage = string.Empty;
            IsLoaded      = false;
        }

        /// <summary>
        ///     Splits a write segment if it crosses the upper-page boundary
        ///     (address 0x80 = byte 128 in the combined 256-byte buffer).
        ///     Lower portion goes to common page (0,0); upper portion goes
        ///     to the selected bank/page.
        /// </summary>
        internal static IEnumerable<MsaWriteSegment> SplitAtPageBoundary(MsaWriteSegment segment)
        {
            var endAddr = segment.StartAddress + segment.Data.Length;

            // Segment is entirely in lower page (0x00-0x7F)
            if (endAddr <= 0x80)
            {
                yield return segment;

                yield break;
            }

            // Segment is entirely in upper page (0x80-0xFF)
            if (segment.StartAddress >= 0x80)
            {
                yield return segment;

                yield break;
            }

            // Segment crosses the boundary — split at 0x80
            var lowerLength = 0x80                - segment.StartAddress;
            var upperLength = segment.Data.Length - lowerLength;

            yield return new (
                    segment.StartAddress,
                    segment.Data.Take(lowerLength));

            yield return new (
                    0x80,
                    segment.Data.Skip(lowerLength).Take(upperLength));
        }

        [RelayCommand]
        private async Task ReadPageAsync()
        {
            if (_device == null)
            {
                StatusMessage = "No device connected.";
                return;
            }

            if (!TryParseHexByte(PageNumber, out var page))
            {
                StatusMessage = "Invalid page number.";
                return;
            }

            if (!TryParseHexByte(BankNumber, out var bank))
            {
                StatusMessage = "Invalid bank number.";
                return;
            }

            try
            {
                // Lower 128 bytes always come from page 0 (common memory)
                var lowerBlock = await _device.RegisterAccess.ReadBlockAsync(
                                         0,
                                         0,
                                         0x00,
                                         128);

                // Upper 128 bytes from selected bank/page
                var upperBlock = await _device.RegisterAccess.ReadBlockAsync(
                                         bank,
                                         page,
                                         0x80,
                                         128);

                var fullPage = new byte[256];
                Array.Copy(lowerBlock, 0, fullPage, 0,   128);
                Array.Copy(upperBlock, 0, fullPage, 128, 128);

                _pageBuffer = new ();
                _pageBuffer.Load(fullPage);
                IsLoaded = true;

                BuildHexRows();
                PageInfo      = $"Bank 0x{bank:X2}, Page 0x{page:X2} — 256 bytes loaded";
                StatusMessage = "Page read successfully.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Read error: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task WritePageAsync()
        {
            if (_device == null)
            {
                StatusMessage = "No device connected.";
                return;
            }

            if (_pageBuffer == null || !IsLoaded)
            {
                StatusMessage = "No page data to write. Read a page first.";
                return;
            }

            if (!TryParseHexByte(PageNumber, out var page))
            {
                StatusMessage = "Invalid page number.";
                return;
            }

            if (!TryParseHexByte(BankNumber, out var bank))
            {
                StatusMessage = "Invalid bank number.";
                return;
            }

            try
            {
                // Sync edited values from grid back to page buffer
                SyncFromGrid();

                // Build dirty contiguous segments (only changed bytes)
                var segments = _pageBuffer.BuildWriteSegments(false);
                if (segments.Count == 0)
                {
                    StatusMessage = "No changes to write.";
                    return;
                }

                var lowerWritten = false;
                var upperWritten = false;

                // Write each dirty segment, routing lower addresses (0x00-0x7F)
                // to common page (0,0) and upper addresses (0x80-0xFF) to the
                // selected bank/page.
                foreach (var segment in segments)
                foreach (var split in SplitAtPageBoundary(segment))
                    if (split.StartAddress < 0x80)
                    {
                        await _device.RegisterAccess.WriteBlockAsync(
                                0,
                                0,
                                split.StartAddress,
                                split.Data);
                        lowerWritten = true;
                    }
                    else
                    {
                        await _device.RegisterAccess.WriteBlockAsync(
                                bank,
                                page,
                                split.StartAddress,
                                split.Data);
                        upperWritten = true;
                    }

                // Read back the changed ranges to verify
                var fullReadBack = new byte[256];
                if (lowerWritten)
                {
                    var lowerBack = await _device.RegisterAccess.ReadBlockAsync(
                                            0,
                                            0,
                                            0x00,
                                            128);
                    Array.Copy(lowerBack, 0, fullReadBack, 0, 128);
                }
                else
                {
                    // Reuse the original lower bytes that were not modified
                    for (var i = 0; i < 128; i++)
                        fullReadBack[i] = _pageBuffer.GetByte(i);
                }

                if (upperWritten)
                {
                    var upperBack = await _device.RegisterAccess.ReadBlockAsync(
                                            bank,
                                            page,
                                            0x80,
                                            128);
                    Array.Copy(upperBack, 0, fullReadBack, 128, 128);
                }
                else
                {
                    for (var i = 128; i < 256; i++)
                        fullReadBack[i] = _pageBuffer.GetByte(i);
                }

                var verified = _pageBuffer.ApplyVerifiedReadBack(fullReadBack);
                if (verified)
                {
                    IsLoaded = true;
                    BuildHexRows();
                    StatusMessage = $"Page written and verified. {segments.Count} segment(s).";
                }
                else
                {
                    StatusMessage =
                            "Write completed but read-back MISMATCH — edits preserved. " +
                            "Re-read the page to confirm hardware state.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Write error: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task ReadCommonPageAsync()
        {
            if (_device == null)
            {
                StatusMessage = "No device connected.";
                return;
            }

            try
            {
                var data = await _device.RegisterAccess.ReadBlockAsync(
                                   0,
                                   0,
                                   0x00,
                                   256);

                _pageBuffer = new ();
                _pageBuffer.Load(data);
                IsLoaded = true;

                BuildHexRows();
                PageInfo      = "Common Page (Bank 0, Page 0) — 256 bytes loaded";
                BankNumber    = "0";
                PageNumber    = "0";
                StatusMessage = "Common page read successfully.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Read error: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task ReadRangeAsync()
        {
            if (!ValidateHexInputs(out var bank,
                                   out var page,
                                   out var start,
                                   out var length))
                return;

            if (length == 0)
            {
                StatusMessage = "Read length must be 1–FF (1–255).";
                return;
            }

            if (start + length > 256)
            {
                StatusMessage = $"Range 0x{start:X2}+0x{length:X2} exceeds page boundary (0xFF).";
                return;
            }

            if (_device == null)
            {
                StatusMessage = "No device connected.";
                return;
            }

            // Range read overlays data onto the existing page buffer.
            // Require a full page load first so unread addresses are not
            // displayed as synthetic zeroes.
            if (_pageBuffer == null)
            {
                StatusMessage = "Load a full page first (Read Page / Common).";
                return;
            }

            try
            {
                // Preserve previously loaded bytes so unread addresses
                // are not replaced with synthetic zeroes.
                var fullPage = new byte[256];
                if (_pageBuffer != null)
                {
                    for (var i = 0; i < 256; i++)
                        fullPage[i] = _pageBuffer.GetByte(i);
                }

                var endAddr = start + length;

                // Lower portion (0x00–0x7F) always from common page (0,0)
                if (start < 0x80)
                {
                    var lowerStart = start;
                    var lowerLen   = Math.Min(endAddr, 0x80) - start;
                    var lowerData = await _device.RegisterAccess.ReadBlockAsync(
                                            0,
                                            0,
                                            lowerStart,
                                            lowerLen);
                    Array.Copy(lowerData, 0, fullPage, lowerStart, lowerLen);
                }

                // Upper portion (0x80–0xFF) from selected bank/page
                if (endAddr > 0x80)
                {
                    var upperStart = Math.Max(start, (byte) 0x80);
                    var upperLen   = endAddr - upperStart;
                    var upperData = await _device.RegisterAccess.ReadBlockAsync(
                                            bank,
                                            page,
                                            upperStart,
                                            upperLen);
                    Array.Copy(upperData, 0, fullPage, upperStart, upperLen);
                }

                _pageBuffer = new ();
                _pageBuffer.Load(fullPage);
                IsLoaded = true;

                BuildHexRows();
                PageInfo = $"Bank 0x{bank:X2}, Page 0x{page:X2} — " +
                           $"Range 0x{start:X2}–0x{start + length - 1:X2} ({length} bytes)";
                StatusMessage = $"Range read: {length} byte(s) from 0x{start:X2}.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Range read error: {ex.Message}";
            }
        }

        [RelayCommand]
        private void FillRow(HexRowViewModel row)
        {
            if (row == null || _pageBuffer == null)
                return;

            var rowIndex = HexRows.IndexOf(row);
            if (rowIndex < 0)
                return;

            var offset = rowIndex * 16;

            foreach (var cell in row.Bytes)
                cell.Hex = "00";

            for (var col = 0; col < 16; col++)
                _pageBuffer.SetByte(offset + col, 0x00);

            row.RefreshAscii();
        }

        [RelayCommand]
        private void FillAll()
        {
            if (_pageBuffer == null)
                return;

            for (var rowIndex = 0; rowIndex < HexRows.Count; rowIndex++)
            {
                var row    = HexRows[rowIndex];
                var offset = rowIndex * 16;
                foreach (var cell in row.Bytes)
                    cell.Hex = "00";
                for (var col = 0; col < 16; col++)
                    _pageBuffer.SetByte(offset + col, 0x00);
                row.RefreshAscii();
            }

            StatusMessage = "All bytes set to 00.";
        }

        [RelayCommand]
        private void FillAllFF()
        {
            if (_pageBuffer == null)
                return;

            for (var rowIndex = 0; rowIndex < HexRows.Count; rowIndex++)
            {
                var row    = HexRows[rowIndex];
                var offset = rowIndex * 16;
                foreach (var cell in row.Bytes)
                    cell.Hex = "FF";
                for (var col = 0; col < 16; col++)
                    _pageBuffer.SetByte(offset + col, 0xFF);
                row.RefreshAscii();
            }

            StatusMessage = "All bytes set to FF.";
        }

        private void BuildHexRows()
        {
            if (_pageBuffer == null)
                return;

            HexRows.Clear();

            for (var rowIndex = 0; rowIndex < 16; rowIndex++)
            {
                var offset = rowIndex * 16;
                var row = new HexRowViewModel
                          {
                              Offset = $"0x{offset:X2}"
                          };

                for (var col = 0; col < 16; col++)
                {
                    var byteIndex = offset + col;
                    var value     = _pageBuffer.GetByte(byteIndex);
                    row.Bytes.Add(new()
                                  {
                                      Hex           = $"{value:X2}",
                                      OriginalValue = value
                                  });
                }

                row.RefreshAscii();
                HexRows.Add(row);
            }
        }

        private void SyncFromGrid()
        {
            if (_pageBuffer == null)
                return;

            for (var rowIndex = 0; rowIndex < HexRows.Count; rowIndex++)
            {
                var row    = HexRows[rowIndex];
                var offset = rowIndex * 16;

                for (var col = 0; col < row.Bytes.Count; col++)
                {
                    var hex = row.Bytes[col].Hex;
                    if (byte.TryParse(hex, NumberStyles.HexNumber, null, out var val))
                        _pageBuffer.SetByte(offset + col, val);
                }
            }
        }

        private static bool TryParseHexByte(string text, out byte value)
        {
            return byte.TryParse(
                    text,
                    NumberStyles.HexNumber,
                    null,
                    out value);
        }

        /// <summary>
        ///     Validates all four hex input fields (Bank, Page, StartAddress,
        ///     ReadLength). Sets StatusMessage on first failure and returns
        ///     false so callers can bail out early.
        /// </summary>
        private bool ValidateHexInputs(out byte bank, out byte page, out byte start, out byte length)
        {
            bank = page = start = length = 0;

            if (!TryParseHexByte(BankNumber, out bank))
            {
                StatusMessage = "Invalid bank number (hex 00–FF).";
                return false;
            }

            if (!TryParseHexByte(PageNumber, out page))
            {
                StatusMessage = "Invalid page number (hex 00–FF).";
                return false;
            }

            if (!TryParseHexByte(StartAddress, out start))
            {
                StatusMessage = "Invalid start address (hex 00–FF).";
                return false;
            }

            if (!TryParseHexByte(ReadLength, out length))
            {
                StatusMessage = "Invalid read length (hex 01–FF).";
                return false;
            }

            return true;
        }
    }

    public class HexRowViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _offset = "0x00";

        [ObservableProperty]
        private string _ascii = "";

        public ObservableCollection<HexByteViewModel> Bytes { get; } = [];

        public void RefreshAscii()
        {
            var chars = new char[16];
            for (var i = 0; i < 16 && i < Bytes.Count; i++)
                if (byte.TryParse(Bytes[i].Hex, NumberStyles.HexNumber, null, out var val))
                    chars[i] = val >= 32 && val < 127 ? (char) val : '.';
                else
                    chars[i] = '.';

            Ascii = new string(chars);
        }
    }

    public class HexByteViewModel : ObservableObject
    {
        private string _hex = "00";

        [ObservableProperty]
        private bool _isModified;

        public string Hex
        {
            get => _hex;
            set
            {
                if (SetProperty(ref _hex, NormalizeHex(value)))
                    IsModified = OriginalValue != GetByteValue();
            }
        }

        public byte OriginalValue { get; set; }

        public byte GetByteValue()
        {
            return byte.TryParse(_hex, NumberStyles.HexNumber, null, out var val) ? val : (byte) 0;
        }

        private static string NormalizeHex(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "00";

            value = value.ToUpperInvariant().Trim();

            // Strip non-hex characters
            value = new (value.Where(c => c >= '0' && c <= '9' || c >= 'A' && c <= 'F').ToArray());
            if (value.Length == 0)
                return "00";
            if (value.Length == 1)
                return "0" + value;
            if (value.Length > 2)
                return value[..2];

            return value.Length == 2 ? value : "0" + value;
        }
    }
}
