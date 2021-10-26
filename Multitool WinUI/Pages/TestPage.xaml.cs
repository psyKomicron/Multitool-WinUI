using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using Multitool.DAL;

using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MultitoolWinUI.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class TestPage : Page, INotifyPropertyChanged
    {
        private readonly DispatcherTimer chatTimer = new()
        {
            Interval = new(TimeSpan.TicksPerMillisecond * 3),
        };
        private bool saved;

        public event PropertyChangedEventHandler PropertyChanged;

        public TestPage()
        {
            InitializeComponent();

            Chat = new();
            for (int i = 0; i < 30; i++)
            {
                Chat.Add("test " + i);
            }

            Unloaded += OnPageUnloaded;
            Loaded += OnPageLoaded;
            App.MainWindow.Closed += OnMainWindowClose;
            SizeChanged += TestPage_SizeChanged;
            chatTimer.Tick += ChatTimer_Tick;
            Chat.CollectionChanged += Chat_CollectionChanged;
        }

        private void Chat_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            ChatListView.ScrollIntoView(Chat[Chat.Count - 1]);
        }

        #region public

        #region properties
        public Uri WebSourceUri { get; set; }

        public string LastStream { get; set; }

        public ObservableCollection<string> Chat { get; set; }
        #endregion

        #endregion

        #region private
        private void SavePage()
        {
            if (!saved)
            {
                ISettings settings = App.Settings;
                settings.SaveSetting(nameof(TestPage), nameof(LastStream), LastStream);
                saved = true;
            }
        }
        #endregion

        #region event handlers
        private void ChatTimer_Tick(object sender, object e)
        {
            if (Chat.Count > 100)
            {
                Debug.WriteLine("Clearing chat");
                for (int i = 10; i < Chat.Count; i++)
                {
                    Chat.Move(10, i - 10);
                }
            }
            Chat.Add(DateTime.Now.ToString() + " : tick");
        }

        private void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            chatTimer.Start();
        }

        private void OnPageUnloaded(object sender, RoutedEventArgs e)
        {
            SavePage();
        }

        private void OnMainWindowClose(object sender, WindowEventArgs args)
        {
            SavePage();
        }

        private void TestPage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //DispatcherQueue.TryEnqueue(() => PageTrace.Width = e.NewSize.Width);
        }

        private void UriTextBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            LastStream = args.QueryText;
            WebSourceUri = new("https://twitch.tv/" + args.QueryText);
            PropertyChanged?.Invoke(this, new(nameof(WebSourceUri)));
        }
        #endregion
    }
}
