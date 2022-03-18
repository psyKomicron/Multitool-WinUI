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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using MultitoolWinUI.Controls;
using Windows.Foundation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MultitoolWinUI.Pages.Widgets
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class WidgetsPage : Page
    {
        //private TwitchIrcClient client;

        public WidgetsPage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            App.MainWindow.Closed += MainWindow_Closed;
        }

        [Setting(typeof(TypeSettingConverter), DefaultInstanciate = false)]
        public Type LastControl { get; set; }

        private void OpenWidget(WidgetView widget, Size widgetSize, Size span, bool closeOthers)
        {
            if (closeOthers)
            {
                foreach (var child in widgetsGrid.Children)
                {
                    if (child is WidgetView widgetView)
                    {
                        widgetView.Close();
                    }
                }
            }

            VariableSizedWrapGrid.SetColumnSpan(widget, (int)span.Width);
            VariableSizedWrapGrid.SetRowSpan(widget, (int)span.Height);
            widget.Height = widgetSize.Height == 0 ? double.NaN : widgetSize.Height; 
            widget.Width = widgetSize.Width == 0 ? double.NaN : widgetSize.Width;

            widget.Open();
#if false
            WidgetsPageNavigationInfo navigationInfo = new(control, new(widget.WidgetName, widget.WidgetIcon));
            App.MainWindow.NavigateTo(typeof(WidgetSelectedPage), navigationInfo); 
#endif
        }

        private void CloseWidget(WidgetView widget, RoutedEventArgs args)
        {
            VariableSizedWrapGrid.SetColumnSpan(widget, 1);
            VariableSizedWrapGrid.SetRowSpan(widget, 1);
            widget.Width = double.NaN;
            widget.Height = double.NaN;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            App.UserSettings.Load(this);
            return;
            /*if (LastControl != null)
            {
                var ctor = LastControl.GetConstructor(Array.Empty<Type>());
                Control control = (Control)ctor.Invoke(Array.Empty<object>());
                SetControl(control, LastControl.Name);
            }*/
        }

        private void OnUnloaded(object sender, RoutedEventArgs e) => App.UserSettings.Save(this);

        private void MainWindow_Closed(object sender, WindowEventArgs args) => App.UserSettings.Save(this);


        private void ColorsButton_Click(WidgetView widget, RoutedEventArgs e) => OpenWidget(widget, new(400, 400), new(2, 3), true);

        private void ColorSpectrumWidgetView_Opened(WidgetView widget, RoutedEventArgs e) => OpenWidget(widget, default, new(2, 3), false);

        private void EmbedsButton_Click(WidgetView widget, RoutedEventArgs e)
        {
#if false
            VariableSizedWrapGrid.SetColumnSpan(widget, 2);
            VariableSizedWrapGrid.SetRowSpan(widget, 3);
            widget.Height = 400;
            widget.Width = 400;
#endif
            App.TraceWarning("Oops, this one is not done yet.");
        }

        private void ImageViewerWidgetView_Opened(WidgetView widget, RoutedEventArgs e) => OpenWidget(widget, new(400, double.NaN), new(2, 1), false);

        private void SpotlightWidgetView_Opened(WidgetView widget, RoutedEventArgs e) => OpenWidget(widget, new(double.NaN, double.NaN), new(3, 2), true);

        private void PowerWidgetView_Opened(WidgetView widget, RoutedEventArgs e) => OpenWidget(widget, new(400, double.NaN), new(2, 1), false);
    }
}
