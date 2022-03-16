using Microsoft.UI;
using Microsoft.UI.Xaml.Media;

using System;
using System.ComponentModel;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MultitoolWinUI.Models
{
    public class SpotlightItem : INotifyPropertyChanged
    {
        private static readonly Brush unselectedBrush = new SolidColorBrush(Colors.Transparent);
        private static readonly Brush selectedBrush = new SolidColorBrush(Colors.Red);
        private bool isSelected;
         
        public SpotlightItem(string fileName, string path)
        {
            FileName = fileName;
            ImageSource = new(path);
            Path = path;
        }

        public string FileName { get; }
        public Brush ImageBorder => IsSelected ? selectedBrush : unselectedBrush;
        public Uri ImageSource { get; }
        public bool IsSelected
        { 
            get => isSelected;
            set
            {
                isSelected = value;
                PropertyChanged?.Invoke(this, new(nameof(IsSelected)));
                PropertyChanged?.Invoke(this, new(nameof(ImageBorder)));
            }
        }
        public string Path { get; }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
