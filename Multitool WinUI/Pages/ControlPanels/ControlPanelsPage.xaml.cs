using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

using Multitool.Data;

using MultitoolWinUI.Helpers;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

using Windows.Storage;
using Windows.System;
using Windows.Web.Http;

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
        private const string customSettingsPathFileName = "custom_settings.xml";
        private readonly Dictionary<string, Tuple<Uri, bool>> pathes = new();
        private readonly object _lock = new();
        private FileSystemWatcher watcher;

        public ControlPanelsPage()
        {
            InitializeComponent();
            _ = Task.Run(() => LoadCustoms(Path.Combine(ApplicationData.Current.LocalFolder.Path, customSettingsPathFileName)));
        }

        #region private methods
        private async void LoadCustoms(string path)
        {
            XmlDocument xmlDocument = new();
            try
            {
                xmlDocument.Load(path);
            }
            catch (FileNotFoundException)
            {
                _ = await ApplicationData.Current.LocalFolder.CreateFileAsync(customSettingsPathFileName, CreationCollisionOption.ReplaceExisting);
                _ = xmlDocument.AppendChild(xmlDocument.CreateElement("pathes"));
                xmlDocument.Save(path);

                App.TraceInformation("Custom settings file created (since none was found)");
            }
            catch (XmlException e)
            {
                App.TraceError(e);
                //App.MainWindow.DisplayMessage("Error", "Control panels", "Unable to load custom settings pathes. " + e.Message);
                return;
            }

            LoadNewElements(xmlDocument.DocumentElement);

            watcher = WatcherFactory.CreateWatcher(ApplicationData.Current.LocalFolder.Path, new()
            {
                ChangedHandler = OnFileChange
            });
        }

        private UIElement CreateElements(string name, Uri uri, bool isPinned)
        {
            SettingPathView settingPathButton = new(name, uri, isPinned);
            settingPathButton.Clicked += SettingPathButton_Clicked;
            settingPathButton.Deleted += SettingPathButton_Deleted;
            settingPathButton.Pinned += SettingPathButton_Pinned;
            return settingPathButton;
        }

        private SettingPathView FindButton(string name, bool pinned)
        {
            UIElementCollection collection = pinned ? PinnedItemsWrapGrid.Children : ItemsWrapGrid.Children;
            for (int i = 0; i < collection.Count; i++)
            {
                if ((collection[i] as SettingPathView).ButtonName == name)
                {
                    return collection[i] as SettingPathView;
                }
            }
            return null;
        }

        private void Pin(string name)
        {
            UIElement el = FindButton(name, false);
            if (ItemsWrapGrid.Children.Remove(el))
            {
                PinnedItemsWrapGrid.Children.Add(CreateElements(name, pathes[name].Item1, pathes[name].Item2));
            }
            else
            {
                App.TraceInformation("Unable to pin '" + name + "'");
            }
        }

        private void UnPin(string name)
        {
            UIElement el = FindButton(name, true);
            if (PinnedItemsWrapGrid.Children.Remove(el))
            {
                ItemsWrapGrid.Children.Add(CreateElements(name, pathes[name].Item1, pathes[name].Item2));
            }
            else
            {
                App.TraceInformation("Unable to unpin '" + name + "'");
            }
        }

        private int FindButtonIndex(string name)
        {
            UIElementCollection collection = pathes[name].Item2 ? PinnedItemsWrapGrid.Children : ItemsWrapGrid.Children;
            for (int i = 0; i < collection.Count; i++)
            {
                if ((collection[i] as SettingPathView).ButtonName == name)
                {
                    return i;
                }
            }
            return -1;
        }

        private void LoadNewElements(XmlNode root)
        {
            // find new valid nodes
            foreach (XmlNode node in root)
            {
                if (node.Name == "path")
                {
                    if (!string.IsNullOrWhiteSpace(node.Attributes["name"]?.Value) &&
                        !string.IsNullOrWhiteSpace(node.Attributes["value"]?.Value))
                    {
                        string name = node.Attributes["name"].Value;
                        string value = node.Attributes["value"].Value;
                        bool pinned = node.Attributes["pinned"] != null && node.Attributes["pinned"]?.Value == "True";
                        try
                        {
                            Uri newUri = new(value);
                            if (pathes.ContainsKey(name))
                            {
                                if (pathes[name].Item1 != newUri)
                                {
                                    pathes[name] = new(newUri, pathes[name].Item2);
                                }
                                if (pathes[name].Item2 != pinned)
                                {
                                    _ = DispatcherQueue.TryEnqueue(() => Pin(name));
                                    pathes[name] = new(pathes[name].Item1, pinned);
                                }
                            }
                            else
                            {
                                pathes.Add(name, new(newUri, pinned));
                                if (pinned)
                                {
                                    _ = DispatcherQueue.TryEnqueue(() => PinnedItemsWrapGrid.Children.Add(CreateElements(name, newUri, true)));
                                }
                                else
                                {
                                    _ = DispatcherQueue.TryEnqueue(() => ItemsWrapGrid.Children.Add(CreateElements(name, newUri, false)));
                                }
                            }
                        }
                        catch (UriFormatException ex)
                        {
                            App.TraceError(ex);
                        }
                    }
                }
            }
        }

        private void RemoveOldElements(XmlNode root)
        {
            List<string> names = new();
            Dictionary<string, Tuple<Uri, bool>>.KeyCollection keys = pathes.Keys;
            foreach (string key in keys)
            {
                bool contains = false;
                foreach (XmlNode node in root)
                {
                    if (node.Name == "path")
                    {
                        if (node.Attributes["name"]?.Value == key)
                        {
                            contains = true;
                            break;
                        }
                    }
                }

                if (!contains)
                {
                    names.Add(key);
                }
            }

            for (int i = 0; i < names.Count; i++)
            {
#if TRACE
                if (pathes.Remove(names[i]))
                {
                    App.TraceWarning("Removed " + names[i] + " from settings pathes");
                }
                else
                {
                    App.TraceWarning("Unable to remove " + names[i] + " from settings pathes");
                }
#else
                pathes.Remove(names[i]);
#endif
            }
            _ = DispatcherQueue.TryEnqueue(() =>
            {
                for (int i = 0; i < names.Count; i++)
                {
                    // find button to remove
                    if (pathes[names[i]].Item2)
                    {
                        PinnedItemsWrapGrid.Children.Remove(FindButton(names[i], true));
                    }
                    else
                    {
                        ItemsWrapGrid.Children.Remove(FindButton(names[i], false));
                    }
                }
            });
        }

        private async void CopySettingFile(string path, bool replaceExisting = false)
        {
            try
            {
                XmlDocument importedDoc = new();
                importedDoc.Load(path);

                _ = await ApplicationData.Current.LocalFolder.CreateFileAsync(customSettingsPathFileName, CreationCollisionOption.ReplaceExisting);
                lock (_lock)
                {
                    importedDoc.Save(Path.Combine(ApplicationData.Current.LocalFolder.Path, customSettingsPathFileName));
                }
            }

            catch (XmlException e)
            {
                App.TraceError(e);
                App.TraceInformation("Importing settings failed");
            }
            catch (NullReferenceException e)
            {
                App.TraceError(e);
                App.TraceWarning("NullReferenceException in " + nameof(CopySettingFile));
            }
        }
        #endregion

        #region event handlers

        #region navigation

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is string path)
            {
                CopySettingFile(path);
            }
        }

        #endregion

        #region control

        private async void SettingPathButton_Clicked(SettingPathView sender, RoutedEventArgs args)
        {
            if (!await Launcher.LaunchUriAsync(sender.SettingUri))
            {
                App.TraceWarning("Unable to launch: " + sender.SettingUri.AbsoluteUri);
            }
        }

        private void SettingPathButton_Deleted(SettingPathView sender, RoutedEventArgs args)
        {
            int i = FindButtonIndex(sender.ButtonName);
            if (i != -1)
            {
                ItemsWrapGrid.Children.RemoveAt(i);
            }
        }

        private void SettingPathButton_Pinned(SettingPathView sender, bool pinned)
        {
            if (pinned)
            {
                DispatcherQueue.TryEnqueue(() => Pin(sender.ButtonName));
            }
            else
            {
                DispatcherQueue.TryEnqueue(() => UnPin(sender.ButtonName));
            }
            pathes[sender.ButtonName] = new(pathes[sender.ButtonName].Item1, pinned);

            // save changes in XML
            XmlDocument doc = new();
            try
            {
                doc.Load(Path.Combine(ApplicationData.Current.LocalFolder.Path, customSettingsPathFileName));
                XmlElement root = doc.DocumentElement;

                foreach (XmlNode node in root)
                {
                    if (node.Name == "path" && node.Attributes != null && node.Attributes["name"]?.Value == sender.ButtonName)
                    {
                        if (node.Attributes["pinned"] != null)
                        {
                            node.Attributes["pinned"].Value = pinned.ToString();
                        }
                        else // if the node does not have the pinned attribute i create it
                        {
                            XmlAttribute newAttribute = doc.CreateAttribute("pinned");
                            newAttribute.Value = pinned.ToString();
                            node.Attributes.Append(newAttribute);
                        }
                        break;
                    }
                }
                doc.Save(Path.Combine(ApplicationData.Current.LocalFolder.Path, customSettingsPathFileName));
            }
            catch (XmlException ex)
            {
                App.TraceWarning("Unable to pin/unpin element (name: " + sender.ButtonName + ").\n" + ex.ToString());
            }
        }

        #endregion

        #region window
        private void LoadSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            _ = Launcher.LaunchUriAsync(new Uri(ApplicationData.Current.LocalFolder.Path));
        }

        private void AddSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(ControlPanelsFilePage));
            App.TraceInformation("Navigating to ControlPanelsFilePage");
        }

        private void SettingsSearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            List<string> suggestions = new();
            Regex query = new("(" + sender.Text + ")", RegexOptions.IgnoreCase);
            foreach (KeyValuePair<string, Tuple<Uri, bool>> pair in pathes)
            {
                if (query.IsMatch(pair.Key))
                {
                    suggestions.Add(pair.Key);
                }
                else if (query.IsMatch(pair.Value.Item1.AbsoluteUri))
                {
                    suggestions.Add(pair.Key);
                }
            }
            _ = DispatcherQueue.TryEnqueue(() => sender.ItemsSource = suggestions);
        }

        private async void SettingsSearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (!string.IsNullOrWhiteSpace(args.QueryText))
            {
                if (pathes.TryGetValue(args.QueryText, out Tuple<Uri, bool> tuple))
                {
                    if (!await Launcher.LaunchUriAsync(tuple.Item1))
                    {
                        App.TraceWarning("Unable to launch: " + tuple.Item1.AbsoluteUri);
                    }
                }
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            _ = DispatcherQueue.TryEnqueue(() =>
            {
                PinnedItemsWrapGrid.Children.Clear();
                ItemsWrapGrid.Children.Clear();

            });
            this.pathes.Clear();
            XmlDocument doc = new();
            doc.Load(Path.Combine(ApplicationData.Current.LocalFolder.Path, customSettingsPathFileName));
            XmlNode pathes = doc.SelectSingleNode(".//pathes");
            if (pathes != null)
            {
                LoadNewElements(pathes);
            }
        }

        private void ResetSettingsButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            var result = await downloadDialog.ShowAsync();
            switch (result)
            {
                case ContentDialogResult.None:
                case ContentDialogResult.Secondary:
                    App.TraceInformation("Download cancelled.");
                    break;
                case ContentDialogResult.Primary:
                    try
                    {
                        Uri uri = new(downloadUriTextBox.Text);

                        using HttpClient client = new();
                        try
                        {
                            HttpResponseMessage response = await client.GetAsync(uri);
                            response.EnsureSuccessStatusCode();
                            string data = await response.Content.ReadAsStringAsync();

                            XmlDocument doc = new();
                            doc.LoadXml(data);
                            doc.Save(Path.Combine(ApplicationData.Current.LocalFolder.Path, customSettingsPathFileName));

                            _ = Frame.Navigate(typeof(ControlPanelsPage));
                        }
                        catch (Exception ex)
                        {
                            App.TraceError(ex);
                        }
                    }
                    catch (Exception ex)
                    {
                        App.TraceError(ex, "Could not download setting file.");
                    }
                    break;
                default:
                    Trace.TraceWarning("Content dialog result not recognized");
                    break;
            }
        }
        #endregion

        #region file change

        private void OnFileChange(object sender, FileSystemEventArgs e)
        {
            if (e.Name == customSettingsPathFileName)
            {
                try
                {
                    XmlDocument doc = new();
                    lock (_lock)
                    {
                        doc.Load(e.FullPath);
                    }
                    XmlElement root = doc.DocumentElement;

                    LoadNewElements(root);
                    RemoveOldElements(root);
                }
                catch (XmlException ex)
                {
                    App.TraceWarning("XmlException: Unable to parse changes from the .XML settings file.");
                    App.TraceError(ex);
                }
                catch (IOException ex)
                {
                    if (ex.HResult != -0x7FF8FFE0)
                    {
                        App.TraceWarning("Unable to parse changes from the .XML settings file.");
                    }
                    else
                    {
                        App.TraceWarning("IOException: Unable to parse changes from the .XML settings file.");
                        App.TraceError(ex);
                    }
                }
            }
        }

        #endregion

        #endregion
    }
}
