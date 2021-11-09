using Microsoft.UI.Dispatching;

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MultitoolWinUI.Models
{
    public class Model : INotifyPropertyChanged
    {
        protected Model(DispatcherQueue dispatcherQueue)
        {
            CurrentDispatcherQueue = dispatcherQueue;
        }

        public DispatcherQueue CurrentDispatcherQueue { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaiseNotifyPropertyChanged([CallerMemberName] string propName = "")
        {
            if (CurrentDispatcherQueue != null)
            {
                _ = CurrentDispatcherQueue.TryEnqueue(() => PropertyChanged?.Invoke(this, new(propName)));
            }
            else
            {
                PropertyChanged?.Invoke(this, new(propName));
            }
        }
    }
}