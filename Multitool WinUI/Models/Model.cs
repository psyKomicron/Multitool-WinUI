using Microsoft.UI.Dispatching;

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MultitoolWinUI.Models
{
    public class Model : INotifyPropertyChanged
    {
        protected Model(DispatcherQueue dispatcherQueue)
        {
            DispatcherQueue = dispatcherQueue;
        }

        public DispatcherQueue DispatcherQueue { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaiseNotifyPropertyChanged([CallerMemberName] string propName = "")
        {
            if (DispatcherQueue != null)
            {
                _ = DispatcherQueue.TryEnqueue(() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName))); 
            }
        }
    }
}