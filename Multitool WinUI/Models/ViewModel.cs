using Microsoft.UI.Dispatching;

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MultitoolWinUI.Models
{
    public class ViewModel : INotifyPropertyChanged
    {
        protected ViewModel(DispatcherQueue dispatcherQueue)
        {
            CurrentDispatcherQueue = dispatcherQueue;
        }

        public DispatcherQueue CurrentDispatcherQueue { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaiseNotifyPropertyChanged([CallerMemberName] string propName = "")
        {
            _ = CurrentDispatcherQueue.TryEnqueue(() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName)));
        }
    }
}