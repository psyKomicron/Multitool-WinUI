using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MultitoolWinUI.Pages.Test
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class TestPage : Page
    {
        public TestPage()
        {
            this.InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            /*
            try
            {
                string value = ValueTextBox.Text;
                string[] values = value.Split(' ');
                List<byte> chars = new();
                foreach (string v in values)
                {
                    chars.Add((byte)int.Parse(v));
                }
                ReadOnlySpan<byte> span = new(chars.ToArray());

                ResultTextBlock.Text = Encoding.UTF8.GetString(span);
            }
            catch (Exception ex)
            {
                ResultTextBlock.Text = ex.ToString();
            }*/
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {/*
            try
            {
                string value = ValueTextBox.Text;
                ResultTextBlock.Text = Encoding.UTF8.GetBytes(value).ToString();
            }
            catch (Exception ex)
            {
                ResultTextBlock.Text = ex.ToString();
            }
            */
        }

        private void PageBackgroundColorPicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
        {
            Background = new SolidColorBrush(args.NewColor);
        }
    }
}
