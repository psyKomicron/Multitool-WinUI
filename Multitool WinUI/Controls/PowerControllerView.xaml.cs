using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Multitool.Interop.Power;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MultitoolWinUI.Controls
{
    public sealed partial class PowerControllerView : UserControl
    {
        private readonly PowerController powerController = new();

        public PowerControllerView()
        {
            InitializeComponent();
        }

        private void TimerPickerView_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (selectionComboBox.SelectedItem == null)
            {
                return;
            }

            try
            {
                var selected = ((ComboBoxItem)selectionComboBox.SelectedItem).Tag;
                switch (selected)
                {
                    case "Lock":
                        powerController.Lock();
                        break;
                    case "Sleep":
                        powerController.Suspend();
                        break;
                    case "Restart":
                        powerController.Restart();
                        break;
                    case "Shutdown":
                        powerController.Shutdown();
                        break;
                    case "Hibernate":
                        powerController.Hibernate();
                        break;
                    default:
                        App.TraceWarning("Shutdown option not recognized.");
                        break;
                }
            }
            catch (Exception ex)
            {
                App.TraceError(ex, "Something went wrong !");
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            timerPicker.Elapsed += TimerPickerView_Elapsed;
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            timerPicker.Elapsed -= TimerPickerView_Elapsed;
        }
    }
}
