using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;

using MultitoolWinUI.Models;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MultitoolWinUI.Pages.MusicPlayer
{
    public sealed partial class MusicFileView : UserControl, INotifyPropertyChanged
    {
        private readonly MusicFileModel model;

        public MusicFileView(MusicFileModel model)
        {
            this.model = model;
            this.model.PropertyChanged += Model_PropertyChanged;
            InitializeComponent();
        }

        #region properties
        public string Album => model.Album;

        public string Artist => model.Artist;

        public string Comment { get; set; }

        public Visibility CommentVisibility => !string.IsNullOrEmpty(Comment) ? Visibility.Visible : Visibility.Collapsed;

        public string FileName => model.Name;

        public TimeSpan FileLength => model.AudioLength;

        public string FullPath => model.Path;

        public string Length => model.Length;

        public MusicFileModel Model => model;

        public string Path => model.Path;

        public int PlayCount => model.PlayCount;

        //!string.IsNullOrEmpty(model.Title) ? model.Title : model.Name
        public string Title
        {
            get
            {
                if (!string.IsNullOrEmpty(model.Title))
                {
                    return model.Title;
                }
                else if (!string.IsNullOrEmpty(model.Name))
                {
                    return model.Name;
                }
                else
                {
                    return model.Path;
                }
            }
        }

        public BitmapImage Thumbnail => model.Thumbnail; 
        #endregion

        public event PropertyChangedEventHandler PropertyChanged;

        private void Model_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }

        private async void MenuFlyoutOpen_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await Launcher.LaunchFileAsync(model.File);
            }
            catch (Exception ex)
            {
                App.TraceError(ex, $"Could not open {FileName}");
            }
        }
    }
}
