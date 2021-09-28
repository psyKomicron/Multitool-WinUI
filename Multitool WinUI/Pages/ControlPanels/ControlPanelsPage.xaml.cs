using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using Multitool.DAL;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

using WinRT;

using Windows.Storage;
using Windows.System;
using Multitool.NTInterop;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml.Navigation;
using System.Runtime.CompilerServices;

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
                xmlDocument.AppendChild(xmlDocument.CreateElement("pathes"));
                xmlDocument.Save(path);

                Trace.TraceInformation("Custom settings file created (since none was found)");
            }
            catch (XmlException e)
            {
                Trace.TraceError(e.ToString());
                //App.MainWindow.DisplayMessage("Error", "Control panels", "Unable to load custom settings pathes. " + e.Message);
                return;
            }

            /*List<UIElement> frameworkElements = new();*/
            LoadNewElements(xmlDocument.DocumentElement);

            watcher = WatcherFactory.CreateWatcher(ApplicationData.Current.LocalFolder.Path, new()
            {
                ChangedHandler = OnFileChange
            });
            watcher.EnableRaisingEvents = false;

            /*_ = DispatcherQueue.TryEnqueue(() =>
            {
                for (int i = 0; i < frameworkElements.Count; i++)
                {
                    ItemsWrapGrid.Children.Add(frameworkElements[i]);
                }
            });*/
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
                Trace.TraceInformation("Unable to pin '" + name + "'");
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
                Trace.TraceInformation("Unable to unpin '" + name + "'");
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
                            Trace.TraceError(ex.ToString());
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
                    Trace.TraceWarning("Removed " + names[i] + " from settings pathes");
                }
                else
                {
                    Trace.TraceWarning("Unable to remove " + names[i] + " from settings pathes");
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
#if false
                else
                {
                    await Task.Run(() =>
                    {
                        XmlDocument currentDoc = new();
                        currentDoc.Load(Path.Combine(ApplicationData.Current.LocalFolder.Path, customSettingsPathFileName));

                        XmlNode importedRoot = importedDoc.SelectSingleNode(".//pathes");
                        XmlNode currentRoot = currentDoc.SelectSingleNode(".//pathes");

                        // update nodes or/and create new ones
                        bool contains = false;
                        foreach (XmlNode node1 in importedRoot)
                        {
                            if (node1.Name == "path" && node1.Attributes != null)
                            {
                                string settingName = node1.Attributes["name"]?.Value;
                                string settingPath = node1.Attributes["value"]?.Value;
                                if (string.IsNullOrWhiteSpace(settingName) || string.IsNullOrWhiteSpace(settingPath))
                                {
                                    Trace.TraceInformation("Skipping node, settingName or settingPath empty");
                                    continue;
                                }

                                foreach (XmlNode node2 in currentRoot)
                                {
                                    if (node2.Name == "path" && node2.Attributes != null)
                                    {
                                        string settingName2 = node2.Attributes["name"]?.Value;
                                        string settingPath2 = node2.Attributes["value"]?.Value;
                                        if (!string.IsNullOrWhiteSpace(settingName2) && !string.IsNullOrWhiteSpace(settingPath2))
                                        {
                                            if (settingName2 == settingName)
                                            {
                                                // nodes are the same
                                                contains = true;
                                                if (replaceExisting || settingPath2 != settingPath)
                                                {
                                                    Trace.TraceInformation("Updating " + settingName);
                                                    node2.Attributes["value"].Value = settingPath2;
                                                }
                                                break;
                                            }
                                        }
                                    }
                                }

                                if (!contains)
                                {
                                    Trace.TraceInformation("Importing [" + node1.Attributes["name"] + ", " + node1.Attributes["value"] + "]");
                                    currentDoc.DocumentElement.AppendChild(currentDoc.ImportNode(node1, true));
                                }
                            }
                        }

                        lock (_lock)
                        {
                            currentDoc.Save(Path.Combine(ApplicationData.Current.LocalFolder.Path, customSettingsPathFileName));
                        }
                    });
                }
#endif
            }

            catch (XmlException e)
            {
                Trace.TraceError(e.ToString());
                Trace.TraceInformation("Importing settings failed");
            }
            catch (NullReferenceException e)
            {
                Trace.TraceError(e.ToString());
                Trace.TraceWarning("NullReferenceException in " + nameof(CopySettingFile));
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
                Trace.TraceWarning("Unable to launch: " + sender.SettingUri.AbsoluteUri);
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
                Trace.TraceWarning("Unable to pin/unpin element (name: " + sender.ButtonName + ").\n" + ex.ToString());
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
            Trace.TraceInformation("Navigating to ControlPanelsFilePage");
        }

        private void SettingsSearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            List<string> suggestions = new();
            Regex query = new("^(" + sender.Text + ")");
            foreach (KeyValuePair<string, Tuple<Uri, bool>> pair in pathes)
            {
                if (query.IsMatch(pair.Key))
                {
                    suggestions.Add(pair.Key);
                }
                if (query.IsMatch(pair.Value.Item1.AbsoluteUri))
                {
                    suggestions.Add(pair.Value.Item1.AbsoluteUri);
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
                        Trace.TraceWarning("Unable to launch: " + tuple.Item1.AbsoluteUri);
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
                    Trace.TraceError("XmlException: Unable to parse changes from the .XML settings file.\n" + ex);
                }
                catch (IOException ex)
                {
                    Trace.TraceError("IOException: Unable to parse changes from the .XML settings file.\n" + ex);
                    
                    if (ex.HResult != -2147024864)
                    {
                        Trace.TraceError("Unable to parse changes from the .XML settings file.");
                    }
                }
            }
        }

        #endregion

        #endregion
    }
}
