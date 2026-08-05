using System.Windows;
using System.Windows.Controls;
using OpenCMIS.Shared;
using OpenCMIS.UI.WPF.ViewModels;

namespace OpenCMIS.UI.WPF.Converters
{
    /// <summary>
    ///     Selects an editor DataTemplate based on a CdbFieldViewModel's FieldType.
    /// </summary>
    public class CdbFieldEditTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? ByteTemplate   { get; set; }
        public DataTemplate? WordTemplate   { get; set; }
        public DataTemplate? DWordTemplate  { get; set; }
        public DataTemplate? StringTemplate { get; set; }

        public override DataTemplate? SelectTemplate(object item,
                                                    DependencyObject container)
        {
            if (item is not CdbEditorViewModel.CdbFieldViewModel vm)
                return null;

            return vm.FieldType switch
            {
                CdbFieldType.Byte   => ByteTemplate,
                CdbFieldType.Word   => WordTemplate,
                CdbFieldType.DWord  => DWordTemplate,
                CdbFieldType.String => StringTemplate,
                _                   => StringTemplate
            };
        }
    }
}
