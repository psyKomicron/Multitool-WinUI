using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;

using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MultitoolWinUI.Controls
{
    public sealed partial class ColorBrowserControl : UserControl
    {
        public ColorBrowserControl()
        {
            this.InitializeComponent();
            GetColors();
        }

        private void GetColors()
        {
            try
            {
                var props = typeof(Colors).GetProperties();
                Color reflectObjet = new();
                for (int i = 0; i < props.Length; i++)
                {
                    try
                    {
                        if (props[i].CanRead)
                        {
                            object value = props[i].GetValue(reflectObjet);
                            Color color = (Color)Convert.ChangeType(value, typeof(Color));

                            StackPanel panel = new();
                            panel.Children.Add(new TextBlock()
                            {
                                Text = props[i].Name,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Center,
                                TextWrapping = TextWrapping.Wrap,
                            });
                            panel.Children.Add(new Rectangle()
                            {
                                Fill = new SolidColorBrush(color),
                                Height = 50,
                                Width = 50
                            });
                            ContentGrid.Children.Add(panel);
                        }
                    }
                    catch (TargetInvocationException ex)
                    {
                        Trace.TraceError(ex.ToString());
                    }
                    catch (InvalidCastException ex)
                    {
                        Trace.TraceError(ex.ToString());
                    }
                    catch (ArgumentNullException ex)
                    {
                        Trace.TraceError(ex.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                App.TraceWarning("Failed to load colors.");
                Trace.TraceError(ex.ToString());
            }
        }
    }
}
