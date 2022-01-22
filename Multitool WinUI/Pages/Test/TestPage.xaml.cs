using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

using Multitool.Net.Imaging;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;

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
            InitializeComponent();
        }

        public ObservableCollection<Emote> Emotes { get; set; } = new();

        public string Channel { get; set; }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Trace.TraceError("dsflknbsdflknsdflkn");
            List<byte> data = new();
            string[] input = ChatInput.Text.Split(' ');
            foreach (string c in input)
            {
                byte.TryParse(c, out byte res);
                data.Add(res);
            }
            byte[] vs = data.ToArray();
            Result.Text = Encoding.UTF8.GetString(vs);
        }
    }
}
