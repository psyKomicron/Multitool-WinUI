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
using MultitoolWinUI.Pages.Power;
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

        private void OpenWidget(Control control, WidgetView widget, Size widgetSize, Size span, bool closeOthers)
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
            /*if (widgetSize.Height > 0)
            {
                widget.Height = widgetSize.Height; 
            }
            if (widgetSize.Width > 0)
            {
                widget.Width = widgetSize.Width; 
            }*/
            widget.Height = widgetSize.Height; 
            widget.Width = widgetSize.Width;
            if (control != null)
            {
                widget.AddControl(control); 
                LastControl = control.GetType();
            }
            else
            {
                widget.Open();
            }
#if false
            WidgetsPageNavigationInfo navigationInfo = new(control, new(widget.WidgetName, widget.WidgetIcon));
            App.MainWindow.NavigateTo(typeof(WidgetSelectedPage), navigationInfo); 
#endif
        }

        private void CloseControl(WidgetView widget, Size widgetSize)
        {
            VariableSizedWrapGrid.SetColumnSpan(widget, 1);
            VariableSizedWrapGrid.SetRowSpan(widget, 1);
            widget.Width = widgetSize.Width;
            widget.Height = widgetSize.Height;
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

        private void ColorsButton_Click(WidgetView widget, RoutedEventArgs e)
        {
            ColorBrowserControl control = new()
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            OpenWidget(control, widget, new(400, 400), new(2, 3), true);
        }

        private void ColorSpectrumButton_Click(WidgetView widget, RoutedEventArgs e)
        {
            ColorPicker picker = new()
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Height = 400
            };
            OpenWidget(picker, widget, new(400, 400), new(2, 2), true);
        }

        private void EmbedsButton_Click(WidgetView widget, RoutedEventArgs e)
        {
#if true
            VariableSizedWrapGrid.SetColumnSpan(widget, 2);
            VariableSizedWrapGrid.SetRowSpan(widget, 3);
            widget.Height = 400;
            widget.Width = 400;
#else
            SetControl(new EmbedFetcherControl(), sender as WidgetView);
#endif
        }

        private void ImageButton_Click(WidgetView widget, RoutedEventArgs e)
        {
#if true
            VariableSizedWrapGrid.SetColumnSpan(widget, 2);
            VariableSizedWrapGrid.SetRowSpan(widget, 3);
            widget.Height = 400;
            widget.Width = 400;
#else
            SetControl(new ImageTester(), sender as WidgetView); 
#endif
        }

        private void SpotlightButton_Click(WidgetView widget, RoutedEventArgs e)
        {
            OpenWidget(new SpotlightImporter(), widget, new(600, 400), new(3, 3), true);
        }

        private void SpotlightWidgetView_Closed(WidgetView sender, RoutedEventArgs args)
        {
            CloseControl(sender, new(double.NaN, double.NaN));
        }

        private void PowerWidgetView_Clicked(WidgetView widget, RoutedEventArgs e)
        {
#if true
            OpenWidget(null, widget, new(400, double.NaN), new(2, 1), false);
#else
            OpenWidget(new TimerPicker(), widget, new(400, double.NaN), new(2, 1), false); 
#endif
        }

        private void PowerWidgetView_Close(WidgetView widget, RoutedEventArgs args) => CloseControl(widget, new(double.NaN, double.NaN));
    }
}
