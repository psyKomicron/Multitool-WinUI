using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using System;
using System.IO;
using System.Xml;

using Windows.Storage;
using Windows.Web.Http;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MultitoolWinUI.Pages.ControlPanels
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ControlPanelsFilePage : Page
    {
        private const string customSettingsPathFileName = "custom_settings.xml";

        public ControlPanelsFilePage()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(FilePathTextBox.Text))
            {
                if (File.Exists(FilePathTextBox.Text))
                {
                    try
                    {
                        XmlDocument doc = new();
                        doc.Load(FilePathTextBox.Text);
                        doc.Save(Path.Combine(ApplicationData.Current.LocalFolder.Path, customSettingsPathFileName));

                        _ = Frame.Navigate(typeof(ControlPanelsPage));
                    }
                    catch (XmlException ex)
                    {
                        App.TraceError(ex.ToString());
                    }
                }
            }
        }

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(FileLinkTextBox.Text))
            {
                using HttpClient client = new();
                try
                {
                    HttpResponseMessage response = await client.GetAsync(new(FileLinkTextBox.Text));
                    response.EnsureSuccessStatusCode();
                    string data = await response.Content.ReadAsStringAsync();

                    XmlDocument doc = new();
                    doc.LoadXml(data);
                    doc.Save(Path.Combine(ApplicationData.Current.LocalFolder.Path, customSettingsPathFileName));

                    _ = Frame.Navigate(typeof(ControlPanelsPage));
                }
                catch (Exception ex)
                {
                    App.TraceError(ex.ToString());
                }
            }
        }
    }
}
