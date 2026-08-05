using System.Windows;
using System.Windows.Controls;
using OpenCMIS.UI.WPF.ViewModels;

namespace OpenCMIS.UI.WPF.Views
{
    public partial class PageEditorView : UserControl
    {
        public PageEditorView()
        {
            InitializeComponent();
        }

        private void HexTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                textBox.SelectAll();

                // Notify ViewModel of selected byte for BitEditor
                if (textBox.DataContext is HexRowViewModel row
                    && DataContext is PageEditorViewModel vm)
                {
                    var rowIndex = vm.HexRows.IndexOf(row);
                    if (rowIndex >= 0)
                    {
                        // Find which byte column was clicked by matching the bound cell
                        var colIndex = 0;
                        for (var i = 0; i < row.Bytes.Count; i++)
                        {
                            if (row.Bytes[i].Hex == textBox.Text)
                            {
                                colIndex = i;
                                break;
                            }
                        }

                        var address = rowIndex * 16 + colIndex;
                        vm.SelectByteAt(address);
                    }
                }
            }
        }

        private void HexTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is not TextBox textBox)
                return;
            if (textBox.DataContext is not HexRowViewModel row)
                return;

            row.RefreshAscii();

            // Update modified state for each cell
            foreach (var cell in row.Bytes)
                cell.IsModified = cell.OriginalValue != cell.GetByteValue();
        }
    }
}
