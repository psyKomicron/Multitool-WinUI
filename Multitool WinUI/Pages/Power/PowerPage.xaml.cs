using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using Multitool.NTInterop;
using Multitool.NTInterop.Power;

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Timers;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MultitoolWinUI.Pages.Power
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class PowerPage : Page, INotifyPropertyChanged, IDisposable
    {
        private PowerController controller = new();
        private TimeSpan remainingTimeSpan;
        private TimeSpan originalTimeSpan;
        private Timer timer;
        private DispatcherQueueTimer animationTimer;
        private ElapsedEventHandler timerHandler;

        private bool _buttonsEnabled = true;
        private bool _isReadOnly;
        private int _hours;
        private int _minutes;
        private int _seconds;

        public PowerPage()
        {
            InitializeComponent();
        }

        #region properties

        public bool IsReadOnly
        {
            get => _isReadOnly;
            set
            {
                _isReadOnly = value;
                NotifyPropertyChanged();
            }
        }

        public bool ButtonsEnabled
        {
            get => _buttonsEnabled;
            set
            {
                _buttonsEnabled = value;
                NotifyPropertyChanged();
            }
        }

        public int Hours
        {
            get => _hours;
            set
            {
                if (value != _hours)
                {
                    _hours = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public int Minutes
        {
            get => _minutes;
            set
            {
                if (value != _minutes)
                {
                    _minutes = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public int Seconds
        {
            get => _seconds;
            set
            {
                if (value != _seconds)
                {
                    _seconds = value;
                    NotifyPropertyChanged();
                }
            }
        }

        #endregion

        public event PropertyChangedEventHandler PropertyChanged;

        public void Dispose()
        {
            timer.Dispose();
        }

        #region controller methods

        private void Shutdown(object sender, ElapsedEventArgs e)
        {
            PrepareWindow();

            try
            {
                controller.Shutdown();
            }
            catch (OperationFailedException ex)
            {
                Trace.WriteLine(ex.ToString());
                App.MainWindow.DisplayMessage("Unable to shutdown the system. The operation failed.");
            }
        }

        private void Restart(object sender, ElapsedEventArgs e)
        {
            PrepareWindow();

            try
            {
                controller.Restart();
            }
            catch (OperationFailedException ex)
            {
                Trace.WriteLine(ex.ToString());
                App.MainWindow.DisplayMessage("Unable to restart the system. The operation failed");
            }
        }

        private void Lock(object sender, ElapsedEventArgs e)
        {
            PrepareWindow();

            try
            {
                controller.Lock();
            }
            catch (OperationFailedException ex)
            {
                Trace.WriteLine(ex.ToString());
                App.MainWindow.DisplayMessage("Unable to lock the system. The operation failed");
            }
        }

        private void Sleep(object sender, ElapsedEventArgs e)
        {
            PrepareWindow();

            try
            {
                controller.Suspend();
            }
            catch (OperationFailedException ex)
            {
                Trace.WriteLine(ex.ToString());
                App.MainWindow.DisplayMessage("Unable to suspend the system. The operation failed");
            }
        }

        private void Hibernate(object sender, ElapsedEventArgs e)
        {
            PrepareWindow();

            try
            {
                controller.Hibernate();
            }
            catch (OperationFailedException ex)
            {
                Trace.WriteLine(ex.ToString());
                App.MainWindow.DisplayMessage("Unable to hibernate the system. The operation failed");
            }
        }

        #endregion

        #region window methods

        private void UpdateTimer(TimeSpan span)
        {
            if (IsLoaded)
            {
                Hours = span.Hours;
                Minutes = span.Minutes;
                Seconds = span.Seconds;
            }
        }

        private void PrepareWindow()
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                ButtonsEnabled = true;
                IsReadOnly = false;
            });
            
            timer.Stop();
            timer.Close();
            animationTimer.Stop();
        }

        private void StartTimer(ElapsedEventHandler function, TimeSpan span)
        {
            try
            {
                remainingTimeSpan = span;
                originalTimeSpan = span;
                UpdateTimer(remainingTimeSpan);

                if (span.TotalSeconds == 0)
                {
                    throw new FormatException("Input for power action cannot be empty");
                }

                timer = new Timer(span.TotalMilliseconds)
                {
                    AutoReset = false,
                };
                timer.Elapsed += function;
                timerHandler = function;

                animationTimer = DispatcherQueue.CreateTimer();
                animationTimer.Interval = new TimeSpan(1 * TimeSpan.TicksPerSecond); // 1s
                animationTimer.IsRepeating = true;
                animationTimer.Tick += AnimationTimer_Tick;
                animationTimer.Start();

                timer.Start();

                ButtonsEnabled = false;
                IsReadOnly = true;
            }
            catch (FormatException e)
            {
                Trace.WriteLine(nameof(PowerPage) + " -> " + e.Message);
            }
        }

        private void NotifyPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        #endregion

        #region events

        private void AnimationTimer_Tick(DispatcherQueueTimer sender, object args)
        {
            TimeSpan span = remainingTimeSpan.Subtract(new TimeSpan(1 * TimeSpan.TicksPerSecond));
            UpdateTimer(span);
            remainingTimeSpan = span;
        }

        private void InputTimerPicker_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.PropertyName))
            {
                Trace.WriteLine("Property changed: " + e.PropertyName);
            }
        }

        #region window events

        private void LockButton_Click(object sender, RoutedEventArgs e)
        {
            StartTimer(Lock, new TimeSpan(Hours, Minutes, Seconds));
        }

        private void SleepButton_Click(object sender, RoutedEventArgs e)
        {
            StartTimer(Sleep, new TimeSpan(Hours, Minutes, Seconds));
        }

        private void RestartButton_Click(object sender, RoutedEventArgs e)
        {
            StartTimer(Restart, new TimeSpan(Hours, Minutes, Seconds));
        }

        private void ShutdownButton_Click(object sender, RoutedEventArgs e)
        {
            StartTimer(Shutdown, new TimeSpan(Hours, Minutes, Seconds));
        }

        private void HibernateButton_Click(object sender, RoutedEventArgs e)
        {
            StartTimer(Hibernate, new TimeSpan(Hours, Minutes, Seconds));
        }

        private void RestartTimerButton_Click(object sender, RoutedEventArgs e)
        {
            if (timer != null)
            {
                timer.Stop();
                timer.Close();
                animationTimer.Stop();
                StartTimer(timerHandler, originalTimeSpan);
            }
        }

        private void StopTimerButton_Click(object sender, RoutedEventArgs e)
        {
            if (timer != null)
            {
                timer.Stop();
                timer.Close();
                animationTimer.Stop();
                UpdateTimer(originalTimeSpan);
                ButtonsEnabled = true;
            }
        }

        private void PauseTimerButton_Click(object sender, RoutedEventArgs e)
        {
            if (timer != null)
            {
                timer.Stop();
                timer.Close();
                animationTimer.Stop();
                UpdateTimer(originalTimeSpan);
                ButtonsEnabled = true;
            }
        }

        #endregion

        #region timer input events

        private void HoursTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (int.TryParse(HoursTextBox.Text, out int res))
            {
                if (res > 0)
                {
                    Hours = res;
                }
                else
                {
                    Hours = 0;
                    HoursTextBox.SelectionStart = HoursTextBox.Text.Length;
                    HoursTextBox.SelectionLength = 0;
                }
            }
        }

        private void MinutesTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (int.TryParse(MinutesTextBox.Text, out int res))
            {
                if (res > 0)
                {
                    Minutes = res;
                }
                else
                {
                    Minutes = 0;
                    MinutesTextBox.SelectionStart = MinutesTextBox.Text.Length;
                    MinutesTextBox.SelectionLength = 0;
                }
            }
        }

        private void SecondsTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (int.TryParse(SecondsTextBox.Text, out int res))
            {
                if (res > 0)
                {
                    Seconds = res;
                }
                else
                {
                    Seconds = 0;
                    SecondsTextBox.SelectionStart = SecondsTextBox.Text.Length;
                    SecondsTextBox.SelectionLength = 0;
                }
            }
        }

        private void HoursUpButton_Click(object sender, RoutedEventArgs e)
        {
            Hours++;
        }

        private void HoursDownButton_Click(object sender, RoutedEventArgs e)
        {
            if (Hours > 0)
            {
                Hours--;
            }
        }

        private void MinutesUpButton_Click(object sender, RoutedEventArgs e)
        {
            Minutes++;
        }

        private void MinutesDownButton_Click(object sender, RoutedEventArgs e)
        {
            if (Minutes > 0)
            {
                Minutes--;
            }
        }

        private void SecondsUpButton_Click(object sender, RoutedEventArgs e)
        {
            Seconds++;
        }

        private void SecondsDownButton_Click(object sender, RoutedEventArgs e)
        {
            if (Seconds > 0)
            {
                Seconds--;
            }
        }

        private void HoursTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(HoursTextBox.Text) || HoursTextBox.Text == "0")
            {
                HoursTextBox.Text = string.Empty;
            }
        }

        private void MinutesTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(MinutesTextBox.Text) || MinutesTextBox.Text == "0")
            {
                MinutesTextBox.Text = string.Empty;
            }
        }

        private void SecondsTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SecondsTextBox.Text) || SecondsTextBox.Text == "0")
            {
                SecondsTextBox.Text = string.Empty;
            }
        }

        #endregion

        #endregion
    }
}
