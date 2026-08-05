using System.Windows;
using System.Windows.Controls;

namespace OpenCMIS.UI.WPF.Views;

public partial class BitEditorControl : UserControl
{
    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(
            nameof(Value),
            typeof(byte),
            typeof(BitEditorControl),
            new FrameworkPropertyMetadata(
                (byte)0,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnValueChanged));

    public byte Value
    {
        get => (byte)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    private readonly CheckBox[] _bitBoxes = new CheckBox[8];
    private bool _updating;

    public BitEditorControl()
    {
        InitializeComponent();
        BuildBitToggles();
    }

    private void BuildBitToggles()
    {
        for (var bit = 7; bit >= 0; bit--)
        {
            var weight = 1 << bit;

            var stack = new StackPanel
            {
                Width = 36,
                Margin = new Thickness(bit == 7 ? 0 : 2, 0, bit == 0 ? 0 : 2, 0)
            };

            var checkBox = new CheckBox
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Content = bit.ToString(),
                FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = (System.Windows.Media.Brush)Application.Current.TryFindResource("OpenCmisTextBrush")!,
                Tag = bit
            };
            checkBox.Checked += OnBitToggled;
            checkBox.Unchecked += OnBitToggled;

            var weightLabel = new TextBlock
            {
                Text = weight.ToString(),
                FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                FontSize = 9,
                Foreground = (System.Windows.Media.Brush)Application.Current.TryFindResource("OpenCmisMutedTextBrush")!,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            stack.Children.Add(checkBox);
            stack.Children.Add(weightLabel);

            _bitBoxes[bit] = checkBox;
            BitItems.Items.Add(stack);
        }

        RefreshDisplay();
    }

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((BitEditorControl)d).RefreshDisplay();
    }

    private void RefreshDisplay()
    {
        var val = Value;
        HexLabel.Text = $"0x{val:X2}";
        DecLabel.Text = $"({val})";

        if (_updating) return;
        _updating = true;

        for (var bit = 0; bit < 8; bit++)
        {
            if (_bitBoxes[bit] != null)
                _bitBoxes[bit].IsChecked = (val & (1 << bit)) != 0;
        }

        _updating = false;
    }

    private void OnBitToggled(object sender, RoutedEventArgs e)
    {
        if (_updating) return;

        var newVal = (byte)Value;
        for (var bit = 0; bit < 8; bit++)
        {
            if (_bitBoxes[bit]?.IsChecked == true)
                newVal |= (byte)(1 << bit);
            else
                newVal &= (byte)~(1 << bit);
        }

        Value = newVal;
    }
}
