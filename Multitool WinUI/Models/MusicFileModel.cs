using Microsoft.UI.Xaml.Media.Imaging;

using System;
using System.ComponentModel;
using System.Globalization;

using Windows.Storage;
using Windows.Storage.FileProperties;

namespace MultitoolWinUI.Models
{
    public class MusicFileModel : INotifyPropertyChanged
    {
        private string album;
        private string artist;
        private string fileName;
        private string length;
        private string title;
        private BitmapImage thumbnail;
        private string path;
        private int playCount;
        private bool selected;

        public MusicFileModel()
        {
        }

        public MusicFileModel(MusicProperties properties)
        {
            Album = properties.Album;
            Artist = properties.Artist;
            //$"{properties.Duration.Minutes}:{properties.Duration.Seconds}"
            Length = properties.Duration.ToString("mm\\:ss", CultureInfo.CurrentCulture);
            AudioLength = properties.Duration;
            Title = properties.Title;
        }

        public string Album
        {
            get => album;
            set
            {
                if (album != null)
                {
                    album = value;
                    Invoke(nameof(Album));
                }
                else
                {
                    album = value;
                }
            }
        }

        public string Artist 
        { 
            get => artist; 
            set 
            {
                if (artist != null)
                {
                    artist = value;
                    Invoke(nameof(Artist));
                }
                else
                {
                    artist = value;
                }
            }
        }

        public string FileName
        {
            get => fileName; 
            set 
            {
                if (fileName != null)
                {
                    fileName = value;
                    Invoke(nameof(FileName));
                }
                else
                {
                    fileName = value;
                }
            } 
        }

        public TimeSpan AudioLength { get; set; }

        public string Length 
        {
            get => length; 
            set 
            {
                if (length != null)
                {
                    length = value;
                    Invoke(nameof(Length));
                }
                else
                {
                    length = value;
                }
            } 
        }

        public string MimeType { get; set; }

        public StorageFile MusicFile { get; set; }

        public string Title
        { 
            get => title; 
            set 
            {
                if (title != null)
                {
                    length = value;
                    Invoke(nameof(Title));
                }
                else
                {
                    title = value;
                }
            } 
        }

        public BitmapImage Thumbnail 
        { 
            get => thumbnail; 
            set
            {
                if (thumbnail != null)
                {
                    thumbnail = value;
                    Invoke(nameof(Thumbnail));
                }
                else
                {
                    thumbnail = value;
                }
            } 
        }

        public string Path
        {
            get => path;
            set
            {
                if (path != null)
                {
                    path = value;
                    Invoke(nameof(Path));
                }
                else
                {
                    path = value;
                }
            }
        }

        public int PlayCount
        {
            get => playCount;
            set
            {
                playCount = value;
                Invoke(nameof(PlayCount));
            }
        }

        public bool Selected
        {
            get => selected;
            set
            {
                selected = value;
                PropertyChanged?.Invoke(this, new(nameof(Selected)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void Invoke(string propName)
        {
            PropertyChanged?.Invoke(this, new(propName));
        }
    }
}
