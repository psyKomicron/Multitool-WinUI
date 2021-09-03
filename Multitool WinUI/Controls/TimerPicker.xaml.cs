using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MultitoolWinUI.Controls
{
    public sealed partial class TimerPicker : UserControl, INotifyPropertyChanged
    {
        private int _hours;
        private int _minutes;
        private int _seconds;
        private TimeSpan timeSpan;

        public TimerPicker()
        {
            InitializeComponent();
        }

        #region properties

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

        #region events

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        public TimeSpan GetValue()
        {
            if (timeSpan.Hours != Hours || timeSpan.Minutes != Minutes || timeSpan.Seconds != Seconds)
            {
                timeSpan = new TimeSpan(Hours, Minutes, Seconds);
            }
            return timeSpan;
        }

        #region private methods

        private void NotifyPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        #endregion

        #region event handlers

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
    }
}
