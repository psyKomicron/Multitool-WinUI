using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using System;

using Windows.Foundation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MultitoolWinUI.Pages.ControlPanels
{
    public sealed partial class SettingPathView : UserControl
    {
        public SettingPathView()
        {
            InitializeComponent();
        }

        public SettingPathView(string name, Uri uri, bool isPinned)
        {
            InitializeComponent();
            SettingUri = uri;
            ButtonName = name;
            IsPinned = isPinned;
            PinButton.Text = IsPinned ? "Unpin" : "Pin";
        }

        #region events

        /// <summary>
        /// Occurs when the item is deleted.
        /// </summary>
        public event TypedEventHandler<SettingPathView, RoutedEventArgs> Deleted;

        /// <summary>
        /// Occurs when the control is clicked.
        /// </summary>
        public event TypedEventHandler<SettingPathView, RoutedEventArgs> Clicked;

        /// <summary>
        /// Occurs when the control is pinned. True if the control has been pinned, false if the control has be unpinned.
        /// </summary>
        public event TypedEventHandler<SettingPathView, bool> Pinned;

        #endregion

        #region properties

        public string ButtonName { get; set; }

        public Uri SettingUri { get; set; }

        public bool IsPinned { get; set; }

        #endregion

        #region event handlers

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            Deleted?.Invoke(this, e);
        }

        private void PinButton_Click(object sender, RoutedEventArgs e)
        {
            IsPinned = !IsPinned;
            PinButton.Text = IsPinned ? "Unpin" : "Pin";
            Pinned?.Invoke(this, IsPinned);
        }

        private void ControlButton_Click(object sender, RoutedEventArgs e)
        {
            Clicked?.Invoke(this, e);
        }

        #endregion
    }
}
