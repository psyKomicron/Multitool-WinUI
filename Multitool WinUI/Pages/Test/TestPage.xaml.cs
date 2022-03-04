using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

using Multitool.Data.Settings;
using Multitool.Data.Settings.Converters;
using Multitool.Drawing;
using Multitool.Net.Embeds;
using Multitool.Net.Irc;
using Multitool.Net.Irc.Factories;
using Multitool.Net.Irc.Twitch;
using Multitool.Net.Irc.Security;

using MultitoolWinUI.Models;
using MultitoolWinUI.Pages.Irc;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MultitoolWinUI.Pages.Test
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class TestPage : Page
    {
        //private TwitchIrcClient client;

        public TestPage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            App.MainWindow.Closed += MainWindow_Closed;
        }

        [Setting(typeof(TypeSettingConverter), DefaultInstanciate = false)]
        public Type LastControl { get; set; }

        private void SetControl(Control control)
        {
            if (ControlsGrid.Children.Count > 0)
            {
                ControlsGrid.Children.Clear();
            }
            Grid.SetColumn(control, 0);
            Grid.SetRow(control, 0);
            ControlsGrid.Children.Add(control);
            LastControl = control.GetType();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            App.UserSettings.Load(this);
            if (LastControl != null)
            {
                var ctor = LastControl.GetConstructor(Array.Empty<Type>());
                Control control = (Control)ctor.Invoke(Array.Empty<object>());
                SetControl(control);
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e) => App.UserSettings.Save(this);

        private void MainWindow_Closed(object sender, WindowEventArgs args) => App.UserSettings.Save(this);

        private void ColorsButton_Click(object sender, RoutedEventArgs e)
        {
            ColorBrowserControl control = new()
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            SetControl(control);
        }

        private void ColorSpectrumButton_Click(object sender, RoutedEventArgs e)
        {
            ColorPicker picker = new()
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Height = 400
            };
            SetControl(picker);
        }

        private void EmbedsButton_Click(object sender, RoutedEventArgs e) => SetControl(new EmbedFetcherControl());

        private void ImageButton_Click(object sender, RoutedEventArgs e) => SetControl(new ImageTester());

        private void SpotlightButton_Click(object sender, RoutedEventArgs e) => SetControl(new SpotlightImporter());
    }
}
