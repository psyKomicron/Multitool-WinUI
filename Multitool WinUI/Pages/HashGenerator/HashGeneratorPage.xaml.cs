using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using System.Security.Cryptography;
using System.Text;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MultitoolWinUI.Pages.HashGenerator
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class HashGeneratorPage : Page
    {
        public HashGeneratorPage()
        {
            InitializeComponent();
        }

        private void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            StringBuilder buffer = new();
            uint count = (uint)LengthSlider.Value;
            for (int i = 0; i < count; i++)
            {
                buffer.Append((char)RandomNumberGenerator.GetInt32(33, 126));
            }
            ResultTextBlock.Text = buffer.ToString();
        }

        private void LengthTextBox_BeforeTextChanging(TextBox sender, TextBoxBeforeTextChangingEventArgs args)
        {
            if (uint.TryParse(args.NewText, out uint res))
            {
                if (res > LengthSlider.Maximum)
                {
                    args.Cancel = true;
                }
            }
            else
            {
                args.Cancel = true;
            }
        }
    }
}
