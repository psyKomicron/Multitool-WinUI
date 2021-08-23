using Microsoft.UI.Dispatching;

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MultitoolWinUI.Models
{
    public class ViewModel : INotifyPropertyChanged
    {
        public static DispatcherQueue CurrentDispatcherQueue { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged([CallerMemberName] string propName = "")
        {
            CurrentDispatcherQueue.TryEnqueue(() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName)));
        }
    }
}