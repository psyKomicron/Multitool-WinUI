using Microsoft.UI.Xaml.Controls;

using Multitool.Interop;
using Multitool.Interop.Power;

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Timers;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MultitoolWinUI.Pages.Power
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class PowerPage : Page, INotifyPropertyChanged
    {
        private readonly PowerController controller = new();
        private bool _buttonsEnabled;

        public PowerPage()
        {
            InitializeComponent();
        }

        #region properties

        public bool ButtonsEnabled
        {
            get => _buttonsEnabled;
            set
            {
                _buttonsEnabled = value;
                NotifyPropertyChanged();
            }
        }

        #endregion

        public event PropertyChangedEventHandler PropertyChanged;

        #region controller methods

        private void Shutdown()
        {
            try
            {
                controller.Shutdown();
            }
            catch (OperationFailedException ex)
            {
                App.TraceError(ex);
                //App.MainWindow.DisplayMessage("Error", "Operation failed", "Unable to shutdown the system. The operation failed.");
            }
        }

        private void Restart()
        {
            try
            {
                controller.Restart();
            }
            catch (OperationFailedException ex)
            {
                App.TraceError(ex);
                //App.MainWindow.DisplayMessage("Error", "Operation failed", "Unable to restart the system. The operation failed");
            }
        }

        private void Lock()
        {
            try
            {
                controller.Lock();
            }
            catch (OperationFailedException ex)
            {
                App.TraceError(ex);
                //App.MainWindow.DisplayMessage("Error", "Operation failed", "Unable to lock the system. The operation failed");
            }
        }

        private void Sleep()
        {
            try
            {
                controller.Suspend();
            }
            catch (OperationFailedException ex)
            {
                App.TraceError(ex);
                //App.MainWindow.DisplayMessage("Error", "Operation failed", "Unable to suspend the system. The operation failed");
            }
        }

        private void Hibernate()
        {
            try
            {
                controller.Hibernate();
            }
            catch (OperationFailedException ex)
            {
                App.TraceError(ex);
            }
        }

        #endregion

        #region window methods

        private void NotifyPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        #endregion

        #region events

        private void TimerPicker_Elapsed(object sender, ElapsedEventArgs e)
        {
            _ = DispatcherQueue.TryEnqueue(() =>
            {
                if (SelectionComboBox.SelectedItem is ComboBoxItem item)
                {
                    switch (item.Tag)
                    {
                        case "lock":
                            Lock();
                            break;
                        case "sleep":
                            Sleep();
                            break;
                        case "hiber":
                            Hibernate();
                            break;
                        case "shut":
                            Shutdown();
                            break;
                        case "restart":
                            Restart();
                            break;
                    }
                }
                else
                {
#if !DEBUG
                    throw new FormatException("You need to set an action");
#else
                    //App.MainWindow.DisplayMessage("Error", "User input required", "You need to set an action for when the timer ends");
                    App.TraceWarning("User input required. You need to set an action for when the timer ends");
#endif
                }
            });
        }

        #endregion
    }
}
