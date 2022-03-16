using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Timers;

using Windows.Foundation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MultitoolWinUI.Controls
{
    public sealed partial class TimerPicker : UserControl, INotifyPropertyChanged, IDisposable
    {
        private readonly Timer timer = new() { AutoReset = false };
        private readonly DispatcherQueueTimer animationTimer;
        private TimeSpan remainingTimeSpan;
        private TimeSpan originalTimeSpan;
        private TimeSpan timeSpan;

        private bool _buttonsEnabled = true;
        private bool _isReadOnly;
        private int _hours;
        private int _minutes;
        private int _seconds;

        public TimerPicker()
        {
            InitializeComponent();
            animationTimer = DispatcherQueue.CreateTimer();
            animationTimer.Interval = new TimeSpan(1 * TimeSpan.TicksPerSecond); // 1s
            animationTimer.IsRepeating = true;
            animationTimer.Tick += AnimationTimer_Tick;
            timer.Elapsed += Timer_Elapsed;
        }

        #region events

        /// <inheritdoc/>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Occurs when the timer status has changed (started: True, stopped: False).
        /// </summary>
        public event TypedEventHandler<TimerPicker, bool> StatusChanged;

        /// <summary>
        /// Routed elapsed event.
        /// </summary>
        public event ElapsedEventHandler Elapsed;

        #endregion

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

        public TimeSpan GetValue()
        {
            if (timeSpan.Hours != Hours || timeSpan.Minutes != Minutes || timeSpan.Seconds != Seconds)
            {
                timeSpan = new TimeSpan(Hours, Minutes, Seconds);
            }
            return timeSpan;
        }

        public void Dispose()
        {
            timer.Dispose();
        }

        #region private methods

        private void NotifyPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new(name));
        }

        private void UpdateTimer(TimeSpan span)
        {
            if (IsLoaded)
            {
                Hours = span.Hours;
                Minutes = span.Minutes;
                Seconds = span.Seconds;
            }
        }

        private void PauseTimer()
        {
            if (timer != null)
            {
                timer.Stop();
                animationTimer.Stop();

                ButtonsEnabled = true;
                IsReadOnly = false;

                startTimerButton.Content = new SymbolIcon()
                {
                    Symbol = Symbol.Play
                };
            }
        }

        private void StartTimer()
        {
            ButtonsEnabled = false;
            IsReadOnly = true;

            remainingTimeSpan = GetValue();
            // check value not empty
            if (remainingTimeSpan.TotalMilliseconds == 0)
            {
                throw new FormatException("Input for power action cannot be empty");
            }

            originalTimeSpan = remainingTimeSpan;
            timer.Interval = remainingTimeSpan.TotalMilliseconds;

            timer.Start();
            animationTimer.Start();
            StatusChanged?.Invoke(this, true);

            startTimerButton.Content = new SymbolIcon()
            {
                Symbol = Symbol.Pause
            };
        }

        private void StopTimer()
        {
            timer.Stop();
            animationTimer.Stop();

            ButtonsEnabled = true;
            IsReadOnly = false;
            UpdateTimer(originalTimeSpan);
            startTimerButton.Content = new SymbolIcon()
            {
                Symbol = Symbol.Play
            };

            StatusChanged?.Invoke(this, false);
        }
        #endregion

        #region event handlers

        #region timer control

        #region textboxs

        private void HoursTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (int.TryParse(HoursTextBox.Text, out int res) && res > 0)
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

        private void MinutesTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (int.TryParse(MinutesTextBox.Text, out int res) && res > 0)
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

        private void SecondsTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (int.TryParse(SecondsTextBox.Text, out int res) && res > 0)
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

        #region up/down buttons

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
            if (Minutes + 1 > 59)
            {
                Hours++;
                Minutes = 0;
            }
            else
            {
                Minutes++;
            }
        }

        private void MinutesDownButton_Click(object sender, RoutedEventArgs e)
        {
            if (Minutes > 0)
            {
                Minutes--;
            }
            else if (Hours > 0)
            {
                Hours--;
                Minutes = 59;
            }
        }

        private void SecondsUpButton_Click(object sender, RoutedEventArgs e)
        {
            if (Seconds + 1 > 59)
            {
                Minutes++;
                Seconds = 0;
            }
            else
            {
                Seconds++;
            }
        }

        private void SecondsDownButton_Click(object sender, RoutedEventArgs e)
        {
            if (Seconds > 0)
            {
                Seconds--;
            }
            else if (Minutes > 0)
            {
                Minutes--;
                Seconds = 59;
            }
        }

        #endregion

        #region timer buttons
        private void RestartTimerButton_Click(object sender, RoutedEventArgs e)
        {
            timer.Stop();
            animationTimer.Stop();
            timer.Start();
            animationTimer.Start();
        }

        private void StartTimerButton_Click(object sender, RoutedEventArgs e)
        {
            if (timer.Enabled)
            {
                PauseTimer();
            }
            else
            {
                StartTimer();
            }
        }
        #endregion

        #endregion

        private void AnimationTimer_Tick(DispatcherQueueTimer sender, object args)
        {
            TimeSpan span = remainingTimeSpan.Subtract(new TimeSpan(1 * TimeSpan.TicksPerSecond));
            UpdateTimer(span);
            remainingTimeSpan = span;
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _ = DispatcherQueue.TryEnqueue(() =>
            {
                StopTimer();
            });
            Elapsed?.Invoke(this, e);
        }

        #endregion
    }
}
