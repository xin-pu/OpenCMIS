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
                textBox.SelectAll();
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
