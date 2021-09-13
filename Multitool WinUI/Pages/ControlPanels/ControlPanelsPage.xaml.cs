using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

using Multitool.FileSystem;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

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
        private const string customSettingsPathFileName = "custom_settings.xml";

        private Dictionary<string, Tuple<Uri, bool>> pathes = new();
        private FileSystemWatcher watcher;

        public ControlPanelsPage()
        {
            InitializeComponent();
            Task.Run(() => LoadCustoms());
        }

        #region private methods

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
                xmlDocument.AppendChild(xmlDocument.CreateElement("pathes"));
                xmlDocument.Save(path);
                App.MainWindow.DisplayMessage("Custom settings file created (since none was found)");
            }
            catch (XmlException e)
            {
                Trace.WriteLine(e);
                App.MainWindow.DisplayMessage("Unable to load custom settings pathes. " + e.Message);
                return;
            }

            List<UIElement> frameworkElements = new();
            LoadNewElements(xmlDocument.DocumentElement);

            watcher = WatcherFactory.CreateWatcher(ApplicationData.Current.LocalFolder.Path, new()
            {
                ChangedHandler = OnFileChange
            });
            watcher.EnableRaisingEvents = true;

            _ = DispatcherQueue.TryEnqueue(() =>
            {
                for (int i = 0; i < frameworkElements.Count; i++)
                {
                    ItemsWrapGrid.Children.Add(frameworkElements[i]);
                }
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

        private SettingPathView FindButton(string name)
        {
            UIElementCollection collection = pathes[name].Item2 ? PinnedItemsWrapGrid.Children : ItemsWrapGrid.Children;
            for (int i = 0; i < collection.Count; i++)
            {
                if ((collection[i] as SettingPathView).ButtonName == name)
                {
                    return collection[i] as SettingPathView;
                }
            }
            return null;
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

        private void LoadNewElements(XmlElement root)
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
                                    _ = DispatcherQueue.TryEnqueue(() =>
                                    {
                                        UIElement el = FindButton(name, pathes[name].Item2);
                                        if (!pathes[name].Item2)
                                        {
                                            _ = ItemsWrapGrid.Children.Remove(el);
                                            PinnedItemsWrapGrid.Children.Add(CreateElements(name, pathes[name].Item1, pathes[name].Item2));
                                        }
                                        else
                                        {
                                            _ = PinnedItemsWrapGrid.Children.Remove(el);
                                            ItemsWrapGrid.Children.Add(CreateElements(name, pathes[name].Item1, pathes[name].Item2));
                                        }
                                        pathes[name] = new(pathes[name].Item1, pinned);
                                    });
                                }
                            }
                            else
                            {
                                Debug.WriteLine("Adding '" + name + "'. " + (pinned ? "Pinned" : "Not pinned"));
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
                            Trace.WriteLine(ex);
                            App.MainWindow.DisplayMessage("Unable to parse changes from the .XML settings file.");
                        }
                    }
                }
            }
        }

        private void RemoveOldElements(XmlElement root)
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
                    Trace.WriteLine("Removed " + names[i] + " from settings pathes");
                }
                else
                {
                    Trace.WriteLine("Unable to remove " + names[i] + " from settings pathes");
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
                        PinnedItemsWrapGrid.Children.Remove(FindButton(names[i]));
                    }
                    else
                    {
                        ItemsWrapGrid.Children.Remove(FindButton(names[i]));
                    }
                }
            });
        }

        #endregion

        #region event handlers

        #region control

        private async void SettingPathButton_Clicked(SettingPathView sender, RoutedEventArgs args)
        {
            if (!await Launcher.LaunchUriAsync(sender.SettingUri))
            {
                Trace.WriteLine("Unable to launch: " + sender.SettingUri.AbsoluteUri);
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
            //pathes[sender.ButtonName] = new(pathes[sender.ButtonName].Item1, pinned);
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
                Trace.WriteLine("Unable to pin/unpin element (name: " + sender.ButtonName + ").\n" + ex.ToString());
                if (pinned)
                {
                    ItemsWrapGrid.Children.Remove(sender);
                    PinnedItemsWrapGrid.Children.Add(sender);
                }
                else
                {
                    PinnedItemsWrapGrid.Children.Remove(sender);
                    ItemsWrapGrid.Children.Add(sender);
                }
                App.MainWindow.DisplayMessage("Failed to save the element state (pinned/unpinned).");
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
            throw new NotImplementedException();
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
                    suggestions.Add(pair.Value.ToString());
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
                        Trace.WriteLine("Unable to launch: " + tuple.Item1.AbsoluteUri);
                    }
                }
            }
        }

        #endregion

        private void OnFileChange(object sender, FileSystemEventArgs e)
        {
            if (e.Name == customSettingsPathFileName)
            {
                try
                {
                    Debug.WriteLine("Parsing xml");
                    XmlDocument doc = new();
                    doc.Load(e.FullPath);
                    XmlElement root = doc.DocumentElement;

                    LoadNewElements(root);
                    RemoveOldElements(root);
                    Debug.WriteLine("Parsed xml");
                }
                catch (XmlException ex)
                {
                    Trace.WriteLine("XmlException: Unable to parse changes from the .XML settings file.\n" + ex);
                    App.MainWindow.DisplayMessage("Unable to parse changes from the .XML settings file.");
                }
                catch (IOException ex)
                {
                    Trace.WriteLine("IOException: Unable to parse changes from the .XML settings file.\n" + ex);
                    
                    if (ex.HResult != -2147024864)
                    {
                        App.MainWindow.DisplayMessage("Unable to parse changes from the .XML settings file.");
                    }
                }
            }
        }

        #endregion
    }
}
