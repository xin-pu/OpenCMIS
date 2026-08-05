using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using OpenCMIS.CDB.Abstractions;
using OpenCMIS.CDB.Core;
using OpenCMIS.Protocol.Abstractions;
using OpenCMIS.Shared;

namespace OpenCMIS.UI.WPF.ViewModels
{
    public partial class CdbEditorViewModel : ObservableObject
    {
        private ICmisDevice? _device;
        private ConfigurationDataBlock? _cdb;
        private byte[]? _rawCdbBytes;

        // ── Header properties ──

        [ObservableProperty]
        private string _cdbVersion = "--";

        [ObservableProperty]
        private string _cdbFlags = "--";

        [ObservableProperty]
        private string _cdbTotalLength = "--";

        [ObservableProperty]
        private int _cdbFieldCount;

        [ObservableProperty]
        private string _cdbChecksumStored = "--";

        [ObservableProperty]
        private string _cdbChecksumComputed = "--";

        [ObservableProperty]
        private bool _isChecksumValid;

        [ObservableProperty]
        private string _checksumStatusIcon = "";

        // ── Fields ──

        [ObservableProperty]
        private ObservableCollection<CdbFieldViewModel> _fields = [];

        [ObservableProperty]
        private string _cdbInfo = "Not loaded";

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        public void SetDevice(ICmisDevice? device)
        {
            _device = device;
        }

        // ── Read ──

        [RelayCommand]
        private async Task ReadCdbAsync()
        {
            if (_device == null)
                return;

            try
            {
                var reader = new CdbReader();
                _cdb = await reader.ReadAsync(_device);

                PopulateFromCdb(_cdb);

                // Fill header info
                CdbVersion     = $"{_cdb.Version.Major}.{_cdb.Version.Minor}";
                CdbFlags        = $"0x{_cdb.Header.Flags:X2}";
                CdbTotalLength  = $"{_cdb.Header.Length} bytes";
                CdbFieldCount   = _cdb.Fields.Count;
                CdbChecksumStored = $"0x{_cdb.Checksum:X4}";

                // Build raw bytes for checksum and export
                RebuildRawBytes();

                CdbInfo       = $"Fields: {_cdb.Fields.Count}, Checksum: 0x{_cdb.Checksum:X4}";
                StatusMessage = "CDB loaded.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
        }

        private void PopulateFromCdb(ConfigurationDataBlock cdb)
        {
            Fields.Clear();
            foreach (var field in cdb.Fields)
                Fields.Add(CdbFieldViewModel.FromCdbField(field, OnFieldChanged));
        }

        // ── Field change callback (from CdbFieldViewModel edits) ──

        private void OnFieldChanged()
        {
            RebuildRawBytes();
        }

        // ── Checksum ──

        private void RebuildRawBytes()
        {
            if (_cdb == null)
            {
                _rawCdbBytes = null;
                return;
            }

            // Apply all VM fields back to the model
            foreach (var vm in Fields)
                vm.ApplyToField(_cdb);

            // Re-serialize to get updated raw bytes
            _rawCdbBytes = CdbWriter.SerializeCdb(_cdb);

            // Recompute checksum: CRC-16 over all bytes except last 2 (checksum placeholder)
            var bodyLength = _rawCdbBytes.Length - 2;
            var body       = new byte[bodyLength];
            Array.Copy(_rawCdbBytes, 0, body, 0, bodyLength);

            var computed = CrcCalculator.CalculateCrc16(body);
            CdbChecksumComputed = $"0x{computed:X4}";

            // Compare computed vs stored
            var stored = _cdb.Checksum;
            IsChecksumValid      = computed == stored;
            ChecksumStatusIcon   = IsChecksumValid ? "\uE8FB" : "\uE711"; // Checkmark : Cancel
            CdbTotalLength       = $"{_rawCdbBytes.Length} bytes";
        }

        // ── Validate ──

        [RelayCommand]
        private void ValidateCdb()
        {
            if (_cdb == null)
            {
                StatusMessage = "No CDB loaded. Read first.";
                return;
            }

            var validator = new CdbValidator();
            if (validator.Validate(_cdb))
            {
                StatusMessage = IsChecksumValid
                    ? "CDB is valid. Ready to write."
                    : "CDB structure valid, but checksum mismatch — will be corrected on write.";
            }
            else
            {
                StatusMessage = "CDB validation failed. Check field values.";
            }
        }

        // ── Write ──

        [RelayCommand]
        private async Task WriteCdbAsync()
        {
            if (_device == null || _cdb == null)
                return;

            try
            {
                // Sync all VM fields to model
                foreach (var vm in Fields)
                    vm.ApplyToField(_cdb);

                var validator = new CdbValidator();
                if (!validator.Validate(_cdb))
                {
                    StatusMessage = "CDB validation failed. Fix errors before writing.";
                    return;
                }

                var writer = new CdbWriter();
                await writer.WriteAsync(_device, _cdb);

                // Refresh after write to get the actual stored checksum
                RebuildRawBytes();
                StatusMessage = "CDB written successfully.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Write error: {ex.Message}";
            }
        }

        // ── Export Binary ──

        [RelayCommand]
        private void ExportBinary()
        {
            if (_rawCdbBytes == null)
            {
                StatusMessage = "No CDB data to export.";
                return;
            }

            var dialog = new SaveFileDialog
            {
                Filter     = "CDB Binary (*.cdb)|*.cdb|All Files (*.*)|*.*",
                DefaultExt = ".cdb",
                FileName   = "module.cdb"
            };

            if (dialog.ShowDialog() == true)
            {
                File.WriteAllBytes(dialog.FileName, _rawCdbBytes);
                StatusMessage = $"Exported to {Path.GetFileName(dialog.FileName)}";
            }
        }

        // ── Export JSON ──

        [RelayCommand]
        private void ExportJson()
        {
            if (_cdb == null)
            {
                StatusMessage = "No CDB data to export.";
                return;
            }

            var dialog = new SaveFileDialog
            {
                Filter     = "JSON (*.json)|*.json|All Files (*.*)|*.*",
                DefaultExt = ".json",
                FileName   = "module.json"
            };

            if (dialog.ShowDialog() == true)
            {
                var export = new CdbExportModel
                {
                    Header = new CdbExportHeader
                    {
                        Version     = _cdb.Header.Version,
                        Flags       = _cdb.Header.Flags,
                        TotalLength = _cdb.Header.Length
                    },
                    Checksum = _cdb.Checksum,
                    Fields = _cdb.Fields.Select(f => new CdbExportField
                    {
                        Id    = f.Id,
                        Type  = f.Type.ToString(),
                        Value = ConvertFieldValueForJson(f)
                    }).ToList()
                };

                var json = JsonSerializer.Serialize(export,
                    new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(dialog.FileName, json);
                StatusMessage = $"Exported to {Path.GetFileName(dialog.FileName)}";
            }
        }

        // ── Import ──

        [RelayCommand]
        private void ImportFile()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "CDB Files (*.cdb;*.json)|*.cdb;*.json|All Files (*.*)|*.*"
            };

            if (dialog.ShowDialog() != true) return;

            try
            {
                var ext = Path.GetExtension(dialog.FileName).ToLowerInvariant();
                if (ext == ".json")
                    ImportFromJson(dialog.FileName);
                else
                    ImportFromBinary(dialog.FileName);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Import error: {ex.Message}";
            }
        }

        private void ImportFromBinary(string filePath)
        {
            var data = File.ReadAllBytes(filePath);
            if (data.Length < 8)
            {
                StatusMessage = "Invalid CDB file: too short.";
                return;
            }

            // Parse header
            var totalLength = (ushort)(data[0] | data[1] << 8);
            var version     = data[2];
            var flags       = data[3];
            var checksum    = (ushort)(data[^2] | data[^1] << 8);

            var cdb = new ConfigurationDataBlock
            {
                Header = new CdbHeader { Length = totalLength, Version = version, Flags = flags },
                Version = new CdbVersion
                {
                    Major = (byte)(version >> 4),
                    Minor = (byte)(version & 0x0F)
                },
                Checksum = checksum
            };

            // Parse fields
            var bodyLength = totalLength - 2;
            var offset     = 4;
            while (offset < bodyLength)
            {
                var field = ParseFieldFromBytes(data, ref offset);
                if (field != null)
                    cdb.Fields.Add(field);
                else
                    break;
            }

            _cdb = cdb;
            PopulateFromCdb(_cdb);

            CdbVersion     = $"{_cdb.Version.Major}.{_cdb.Version.Minor}";
            CdbFlags        = $"0x{_cdb.Header.Flags:X2}";
            CdbFieldCount   = _cdb.Fields.Count;
            CdbChecksumStored = $"0x{_cdb.Checksum:X4}";

            RebuildRawBytes();
            StatusMessage = $"Imported from {Path.GetFileName(filePath)}";
        }

        private void ImportFromJson(string filePath)
        {
            var json   = File.ReadAllText(filePath);
            var export = JsonSerializer.Deserialize<CdbExportModel>(json)
                         ?? throw new InvalidOperationException("Failed to deserialize JSON.");

            var cdb = new ConfigurationDataBlock
            {
                Header = new CdbHeader
                {
                    Length  = export.Header.TotalLength,
                    Version = export.Header.Version,
                    Flags   = export.Header.Flags
                },
                Version = new CdbVersion
                {
                    Major = (byte)(export.Header.Version >> 4),
                    Minor = (byte)(export.Header.Version & 0x0F)
                },
                Checksum = export.Checksum
            };

            foreach (var f in export.Fields)
            {
                var fieldType = Enum.TryParse<CdbFieldType>(f.Type, out var ft) ? ft : CdbFieldType.String;
                cdb.Fields.Add(new CdbField
                {
                    Id    = f.Id,
                    Type  = fieldType,
                    Value = ConvertJsonValue(fieldType, f.Value)
                });
            }

            _cdb = cdb;
            PopulateFromCdb(_cdb);

            CdbVersion     = $"{_cdb.Version.Major}.{_cdb.Version.Minor}";
            CdbFlags        = $"0x{_cdb.Header.Flags:X2}";
            CdbFieldCount   = _cdb.Fields.Count;
            CdbChecksumStored = $"0x{_cdb.Checksum:X4}";

            RebuildRawBytes();
            StatusMessage = $"Imported from {Path.GetFileName(filePath)}";
        }

        // ── Field parsing helpers (mirrors CdbReader.ParseField) ──

        private static CdbField? ParseFieldFromBytes(byte[] data, ref int offset)
        {
            if (offset + 2 > data.Length) return null;

            var type     = (CdbFieldType)data[offset];
            var idLength = data[offset + 1];
            offset += 2;

            if (offset + idLength > data.Length) return null;
            var fieldId = Encoding.ASCII.GetString(data, offset, idLength);
            offset += idLength;

            if (offset + 2 > data.Length) return null;
            var valueLength = (ushort)(data[offset] | data[offset + 1] << 8);
            offset += 2;

            if (offset + valueLength > data.Length) return null;
            var valueBytes = new byte[valueLength];
            Array.Copy(data, offset, valueBytes, 0, valueLength);
            offset += valueLength;

            return new CdbField
            {
                Id    = fieldId,
                Type  = type,
                Value = ConvertByteValue(type, valueBytes)
            };
        }

        private static object ConvertByteValue(CdbFieldType type, byte[] value)
        {
            return type switch
            {
                CdbFieldType.Byte   => value.Length > 0 ? value[0] : 0,
                CdbFieldType.Word   => value.Length >= 2 ? (ushort)(value[0] | value[1] << 8) : 0,
                CdbFieldType.DWord  => value.Length >= 4 ? (uint)(value[0] | value[1] << 8 | value[2] << 16 | value[3] << 24) : 0U,
                CdbFieldType.String => Encoding.ASCII.GetString(value).TrimEnd('\0'),
                _                   => (object)value
            };
        }

        // ── JSON model helpers ──

        private static object? ConvertFieldValueForJson(CdbField field)
        {
            return field.Type switch
            {
                CdbFieldType.Byte   => Convert.ToInt32(field.Value ?? 0),
                CdbFieldType.Word   => Convert.ToInt32(field.Value ?? 0),
                CdbFieldType.DWord  => Convert.ToInt64(field.Value ?? 0U),
                CdbFieldType.String => field.Value?.ToString() ?? "",
                _                   => field.Value
            };
        }

        private static object? ConvertJsonValue(CdbFieldType type, object? jsonValue)
        {
            if (jsonValue is JsonElement je)
            {
                return type switch
                {
                    CdbFieldType.Byte   => je.ValueKind == JsonValueKind.Number ? (byte)je.GetByte() : (byte)0,
                    CdbFieldType.Word   => je.ValueKind == JsonValueKind.Number ? (ushort)je.GetUInt16() : (ushort)0,
                    CdbFieldType.DWord  => je.ValueKind == JsonValueKind.Number ? je.GetUInt32() : 0U,
                    CdbFieldType.String => je.GetString() ?? "",
                    _                   => je.GetString() ?? ""
                };
            }

            return type switch
            {
                CdbFieldType.Byte   => Convert.ToByte(jsonValue ?? 0),
                CdbFieldType.Word   => Convert.ToUInt16(jsonValue ?? 0),
                CdbFieldType.DWord  => Convert.ToUInt32(jsonValue ?? 0U),
                CdbFieldType.String => jsonValue?.ToString() ?? "",
                _                   => jsonValue
            };
        }

        // ═══════════════════════════════════════════════
        //  CdbFieldViewModel (inner class)
        // ═══════════════════════════════════════════════

        public partial class CdbFieldViewModel : ObservableObject
        {
            private readonly CdbField _field;
            private readonly Action?  _onChanged;

            [ObservableProperty]
            private string _id = string.Empty;

            [ObservableProperty]
            private string _typeName = string.Empty;

            [ObservableProperty]
            private string _valueDisplay = string.Empty;

            [ObservableProperty]
            private string _hexPreview = string.Empty;

            [ObservableProperty]
            private CdbFieldType _fieldType;

            // ── Type-specific edit values ──

            [ObservableProperty]
            private byte _byteValue;

            [ObservableProperty]
            private string _wordHexValue = "0000";

            [ObservableProperty]
            private string _dwordHexValue = "00000000";

            [ObservableProperty]
            private string _stringValue = string.Empty;

            private CdbFieldViewModel(CdbField field, Action? onChanged)
            {
                _field     = field;
                _onChanged = onChanged;
                Id         = field.Id;
                FieldType  = field.Type;
                TypeName   = field.Type.ToString();

                // Initialize edit values from underlying field
                switch (field.Type)
                {
                    case CdbFieldType.Byte:
                        ByteValue = field.Value is byte b ? b : (byte)0;
                        break;
                    case CdbFieldType.Word:
                        WordHexValue = field.Value is ushort w
                            ? w.ToString("X4")
                            : "0000";
                        break;
                    case CdbFieldType.DWord:
                        DwordHexValue = field.Value is uint d
                            ? d.ToString("X8")
                            : "00000000";
                        break;
                    case CdbFieldType.String:
                        StringValue = field.Value?.ToString() ?? "";
                        break;
                }

                UpdateDisplay();
            }

            public static CdbFieldViewModel FromCdbField(CdbField field,
                Action? onChanged = null)
                => new(field, onChanged);

            public void ApplyToField(ConfigurationDataBlock cdb)
            {
                // Sync to the bound field instance, then rebuild raw bytes
            }

            public void ApplyToField()
            {
                _field.Id   = Id;
                _field.Type = FieldType;
                _field.Value = FieldType switch
                {
                    CdbFieldType.Byte   => ByteValue,
                    CdbFieldType.Word   => ParseHexToUshort(WordHexValue),
                    CdbFieldType.DWord  => ParseHexToUint(DwordHexValue),
                    CdbFieldType.String => StringValue,
                    _                   => _field.Value
                };
            }

            // ── Auto-update display on edit ──

            partial void OnByteValueChanged(byte value)
            {
                UpdateDisplay();
                _onChanged?.Invoke();
            }

            partial void OnWordHexValueChanged(string value)
            {
                UpdateDisplay();
                _onChanged?.Invoke();
            }

            partial void OnDwordHexValueChanged(string value)
            {
                UpdateDisplay();
                _onChanged?.Invoke();
            }

            partial void OnStringValueChanged(string value)
            {
                UpdateDisplay();
                _onChanged?.Invoke();
            }

            private void UpdateDisplay()
            {
                switch (FieldType)
                {
                    case CdbFieldType.Byte:
                        ValueDisplay = ByteValue.ToString();
                        HexPreview   = $"{ByteValue:X2}";
                        break;
                    case CdbFieldType.Word:
                        var w = ParseHexToUshort(WordHexValue);
                        ValueDisplay = $"0x{w:X4}";
                        HexPreview   = $"{(w & 0xFF):X2} {(w >> 8 & 0xFF):X2}";
                        break;
                    case CdbFieldType.DWord:
                        var d = ParseHexToUint(DwordHexValue);
                        ValueDisplay = $"0x{d:X8}";
                        HexPreview   = $"{(d & 0xFF):X2} {(d >> 8 & 0xFF):X2} {(d >> 16 & 0xFF):X2} {(d >> 24 & 0xFF):X2}";
                        break;
                    case CdbFieldType.String:
                        ValueDisplay = StringValue;
                        HexPreview   = StringValue.Length > 0
                            ? string.Join(" ",
                                Encoding.ASCII.GetBytes(StringValue)
                                    .Select(b => $"{b:X2}"))
                            : "";
                        break;
                }
            }

            private static ushort ParseHexToUshort(string hex)
            {
                hex = hex.Trim().Replace("0x", "").Replace("0X", "");
                return ushort.TryParse(hex, NumberStyles.HexNumber,
                           CultureInfo.InvariantCulture, out var v)
                    ? v
                    : (ushort)0;
            }

            private static uint ParseHexToUint(string hex)
            {
                hex = hex.Trim().Replace("0x", "").Replace("0X", "");
                return uint.TryParse(hex, NumberStyles.HexNumber,
                           CultureInfo.InvariantCulture, out var v)
                    ? v
                    : 0U;
            }
        }
    }

    // ── JSON export/import model classes ──

    internal sealed class CdbExportModel
    {
        public CdbExportHeader          Header   { get; set; } = new();
        public ushort                   Checksum { get; set; }
        public List<CdbExportField>     Fields   { get; set; } = [];
    }

    internal sealed class CdbExportHeader
    {
        public byte Version     { get; set; }
        public byte Flags       { get; set; }
        public int  TotalLength { get; set; }
    }

    internal sealed class CdbExportField
    {
        public string  Id    { get; set; } = string.Empty;
        public string  Type  { get; set; } = string.Empty;
        public object? Value { get; set; }
    }
}
