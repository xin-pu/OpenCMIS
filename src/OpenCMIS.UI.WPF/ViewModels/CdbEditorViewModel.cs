using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenCMIS.CDB.Abstractions;
using OpenCMIS.CDB.Core;
using OpenCMIS.Protocol.Abstractions;
using OpenCMIS.Shared;

namespace OpenCMIS.UI.WPF.ViewModels
{
	public partial class CdbEditorViewModel : ObservableObject
	{
        private ICmisDevice? _device;

        [ObservableProperty]
        private ObservableCollection<CdbFieldViewModel> _fields = [];

        [ObservableProperty]
        private string _cdbInfo = "Not loaded";

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        private ConfigurationDataBlock? _cdb;

        public void SetDevice(ICmisDevice? device)
        {
            _device = device;
        }

        [RelayCommand]
        private async Task ReadCdbAsync()
        {
            if (_device == null)
                return;

            try
            {
                var reader = new CdbReader();
                _cdb = await reader.ReadAsync(_device);

                Fields.Clear();
                foreach (var field in _cdb.Fields)
                    Fields.Add(new CdbFieldViewModel
                               {
                                   Id    = field.Id,
                                   Type  = field.Type.ToString(),
                                   Value = field.Value?.ToString() ?? ""
                               });

                CdbInfo       = $"Fields: {_cdb.Fields.Count}, Checksum: 0x{_cdb.Checksum:X4}";
                StatusMessage = "CDB loaded.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task WriteCdbAsync()
        {
            if (_device == null || _cdb == null)
                return;

            try
            {
                // Update fields from view model
                var fieldList = _cdb.Fields.ToList();
                for (var i = 0; i < Fields.Count && i < fieldList.Count; i++)
                {
                    var vm    = Fields[i];
                    var field = fieldList[i];

                    if (field.Type == CdbFieldType.Byte)
                        field.Value = byte.TryParse(vm.Value, out var b) ? b : field.Value;
                    else
                        field.Value = vm.Value;
                }

                _cdb.Fields = fieldList;

                var writer    = new CdbWriter();
                var validator = new CdbValidator();

                if (!validator.Validate(_cdb))
                {
                    StatusMessage = "CDB validation failed.";
                    return;
                }

                await writer.WriteAsync(_device, _cdb);
                StatusMessage = "CDB written successfully.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
        }
    }

	public partial class CdbFieldViewModel : ObservableObject
	{
        [ObservableProperty]
        private string _id = string.Empty;

        [ObservableProperty]
        private string _type = string.Empty;

        [ObservableProperty]
        private string _value = string.Empty;
    }
}
