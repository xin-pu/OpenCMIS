using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenCMIS.Protocol.Abstractions;

namespace OpenCMIS.UI.WPF.ViewModels
{
    public partial class PageEditorViewModel : ObservableObject
    {
        private ICmisDevice? _device;

        [ObservableProperty]
        private string _pageNumber = "0";

        [ObservableProperty]
        private ObservableCollection<HexRowViewModel> _hexRows = [];

        [ObservableProperty]
        private string _pageInfo = "No data loaded";

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        private byte[] _pageData = [];

        public void SetDevice(ICmisDevice? device)
        {
            _device = device;
            HexRows.Clear();
            _pageData = [];
            PageInfo = "No data loaded";
            StatusMessage = string.Empty;
        }

        [RelayCommand]
        private async Task ReadPageAsync()
        {
            if (_device == null)
            {
                StatusMessage = "No device connected.";
                return;
            }

            if (!byte.TryParse(PageNumber, out var page))
            {
                StatusMessage = "Invalid page number.";
                return;
            }

            try
            {
                // Read full 256 bytes: lower 128 (0x00-0x7F) from page 0, upper 128 (0x80-0xFF) from selected page
                var lowerBlock = await _device.RegisterAccess.ReadBlockAsync(0, 0x00, 128);
                var upperBlock = await _device.RegisterAccess.ReadBlockAsync(page, 0x80, 128);

                _pageData = new byte[256];
                Array.Copy(lowerBlock, 0, _pageData, 0, 128);
                Array.Copy(upperBlock, 0, _pageData, 128, 128);

                BuildHexRows();
                PageInfo = $"Page 0x{page:X2} — 256 bytes loaded";
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

            if (!byte.TryParse(PageNumber, out var page))
            {
                StatusMessage = "Invalid page number.";
                return;
            }

            if (_pageData.Length == 0)
            {
                StatusMessage = "No page data to write. Read a page first.";
                return;
            }

            try
            {
                // Sync edited values back to byte array
                SyncFromGrid();

                // Write upper 128 bytes to selected page
                var upperBlock = new byte[128];
                Array.Copy(_pageData, 128, upperBlock, 0, 128);
                await _device.RegisterAccess.WriteBlockAsync(page, 0x80, upperBlock);

                StatusMessage = $"Page 0x{page:X2} written successfully.";
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
                _pageData = await _device.RegisterAccess.ReadBlockAsync(0, 0x00, 256);
                BuildHexRows();
                PageInfo = "Common Page (0x00) — 256 bytes loaded";
                PageNumber = "0";
                StatusMessage = "Common page read successfully.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Read error: {ex.Message}";
            }
        }

        [RelayCommand]
        private void FillRow(HexRowViewModel row)
        {
            if (row == null) return;

            foreach (var cell in row.Bytes)
            {
                cell.Hex = "00";
            }
            row.RefreshAscii();
        }

        [RelayCommand]
        private void FillAll()
        {
            foreach (var row in HexRows)
            {
                foreach (var cell in row.Bytes)
                {
                    cell.Hex = "00";
                }
                row.RefreshAscii();
            }
            StatusMessage = "All bytes set to 00.";
        }

        [RelayCommand]
        private void FillAllFF()
        {
            foreach (var row in HexRows)
            {
                foreach (var cell in row.Bytes)
                {
                    cell.Hex = "FF";
                }
                row.RefreshAscii();
            }
            StatusMessage = "All bytes set to FF.";
        }

        private void BuildHexRows()
        {
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
                    row.Bytes.Add(new HexByteViewModel
                    {
                        Hex = $"{_pageData[byteIndex]:X2}",
                        OriginalValue = _pageData[byteIndex]
                    });
                }

                row.RefreshAscii();
                HexRows.Add(row);
            }
        }

        private void SyncFromGrid()
        {
            for (var rowIndex = 0; rowIndex < HexRows.Count; rowIndex++)
            {
                var row = HexRows[rowIndex];
                var offset = rowIndex * 16;

                for (var col = 0; col < row.Bytes.Count; col++)
                {
                    var hex = row.Bytes[col].Hex;
                    if (byte.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out var val))
                    {
                        _pageData[offset + col] = val;
                    }
                }
            }
        }
    }

    public partial class HexRowViewModel : ObservableObject
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
            {
                if (byte.TryParse(Bytes[i].Hex, System.Globalization.NumberStyles.HexNumber, null, out var val))
                {
                    chars[i] = val >= 32 && val < 127 ? (char)val : '.';
                }
                else
                {
                    chars[i] = '.';
                }
            }
            Ascii = new string(chars);
        }
    }

    public partial class HexByteViewModel : ObservableObject
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
                {
                    IsModified = OriginalValue != GetByteValue();
                }
            }
        }

        public byte OriginalValue { get; set; }

        public byte GetByteValue()
        {
            return byte.TryParse(_hex, System.Globalization.NumberStyles.HexNumber, null, out var val) ? val : (byte)0;
        }

        private static string NormalizeHex(string value)
        {
            if (string.IsNullOrEmpty(value)) return "00";
            value = value.ToUpperInvariant().Trim();
            // Strip non-hex characters
            value = new string(value.Where(c => (c >= '0' && c <= '9') || (c >= 'A' && c <= 'F')).ToArray());
            if (value.Length == 0) return "00";
            if (value.Length == 1) return "0" + value;
            if (value.Length > 2) return value[..2];
            return value.Length == 2 ? value : "0" + value;
        }
    }
}
