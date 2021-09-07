using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

using Multitool.FileSystem;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml;

using Windows.Storage;
using Windows.System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MultitoolWinUI.Pages.ControlPanels
{
    public record CustomSettingPath(string Name, string Path);

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ControlPanelsPage : Page
    {
        private const string settingsUri = "ms-settings:";
        private const string startupUri = "ms-settings:startupapps";
        private const string audioUri = "ms-settings:easeofaccess-audio";
        private const string nightlightUri = "ms-settings:nightlight";
        private const string soundUri = "ms-settings:sound";
        private const string windowsUpdateUri = "ms-settings:windowsupdate";
        private const string customSettingsPathFileName = "custom_settings.xml";

        private Dictionary<string, Uri> pathes = new();
        private FileSystemWatcher watcher;

        public ControlPanelsPage()
        {
            InitializeComponent();
            LoadCustoms();
        }

        private async void LoadCustoms()
        {
            XmlDocument xmlDocument = new();
            string path = Path.Combine(ApplicationData.Current.LocalFolder.Path, customSettingsPathFileName);
            try
            {
                xmlDocument.Load(path);
            }
            catch (FileNotFoundException)
            {
                _ = await ApplicationData.Current.LocalFolder.CreateFileAsync(customSettingsPathFileName, CreationCollisionOption.ReplaceExisting);
                xmlDocument.Load(path);
                App.MainWindow.DisplayMessage("Custom settings file created (since none was found)");
            }
            catch (XmlException e)
            {
                Trace.WriteLine(e);
                App.MainWindow.DisplayMessage("Unable to load custom settings pathes. " + e.Message);
                return;
            }

            List<UIElement> frameworkElements = new();
            XmlNode root = xmlDocument.SelectSingleNode(".//pathes");
            if (root != null)
            {
                foreach (XmlNode node in root)
                {
                    //Debug.WriteLine(attr.Name + ": " + attr.Value);
                    if (node.Attributes["name"].Value != null && node.Attributes["value"].Value != null && !pathes.ContainsKey(node.Attributes["name"].Value))
                    {
                        string name = node.Attributes["name"].Value;
                        string value = node.Attributes["value"].Value;

                        pathes.Add(name, new(value));

                        Button button = new()
                        {
                            Content = new TextBlock()
                            {
                                Text = node.Attributes["name"].Value,
                                TextWrapping = TextWrapping.WrapWholeWords
                            }
                        };
                        button.Click += Button_Click;
                        frameworkElements.Add(button);
                    }
                }
            }

            watcher = WatcherFactory.CreateWatcher(ApplicationData.Current.LocalFolder.Path, new()
            {
                ChangedHandler = OnFileChange
            });
            watcher.EnableRaisingEvents = true;

            _ = DispatcherQueue.TryEnqueue(() =>
            {
                for (int i = 0; i < frameworkElements.Count; i++)
                {
                    PanelsWrapGrid.Children.Add(frameworkElements[i]);
                }
            });
        }

        private void OnFileChange(object sender, FileSystemEventArgs e)
        {
            if (e.Name == customSettingsPathFileName)
            {
                Debug.WriteLine(e.ChangeType);
            }
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                if (pathes.TryGetValue(((TextBlock)button.Content).Text, out Uri uri))
                {
                    if (!await Launcher.LaunchUriAsync(uri))
                    {
                        Trace.WriteLine("Unable to launch: " + uri.AbsoluteUri);
                    }
                }
            }
        }

        private void LoadSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            pathes.Clear();
            LoadCustoms();
        }
    }
}
