using System;
using System.Threading;

using Windows.Foundation;

namespace Multitool.Threading
{
    public class ListenableCancellationTokenSource : CancellationTokenSource
    {
        public event TypedEventHandler<ListenableCancellationTokenSource, EventArgs> Cancelled;

        public void InvokeCancel()
        {
            if (Token.IsCancellationRequested)
            {
                Cancelled?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
