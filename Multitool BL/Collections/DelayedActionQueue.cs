using Microsoft.UI.Dispatching;

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Timers;

using Windows.Foundation;

namespace Multitool.Collections
{
    public class DelayedActionQueue
    {
        private readonly ConcurrentQueue<DispatcherQueueHandler> displayQueue = new();
        private readonly Timer messageTimer = new() { AutoReset = true, Enabled = false };
        private bool silenced;
        private volatile bool busy;

        public DelayedActionQueue() : this(2500)
        {
        }

        public DelayedActionQueue(double interval)
        {
            messageTimer.Interval = interval;
            messageTimer.Elapsed += MessageTimer_Elapsed;
        }

        public DispatcherQueue DispatcherQueue { get; set; }

        public event TypedEventHandler<DelayedActionQueue, ElapsedEventArgs> QueueEmpty;

        public double Delay
        {
            get => messageTimer.Interval;
            set => messageTimer.Interval = value;
        }

        public void QueueAction(DispatcherQueueHandler handler)
        {
            if (!busy)
            {
                busy = true;
                if (DispatcherQueue != null)
                {
                    _ = DispatcherQueue.TryEnqueue(handler);
                }
                else
                {
                    handler();
                }
                messageTimer.Start();
            }
            else
            {
                displayQueue.Enqueue(handler);
            }
        }

        public void Silence()
        {
            silenced = true;
            messageTimer.Stop();
        }

        public void Clear()
        {
            displayQueue.Clear();
            silenced = true;
            messageTimer.Stop();
        }

        private bool CheckForCallbacks()
        {
            if (!displayQueue.IsEmpty)
            {
                if (DispatcherQueue != null && displayQueue.TryDequeue(out DispatcherQueueHandler next))
                {
                    DispatcherQueue.TryEnqueue(next);
                }
#if TRACE
                else
                {
                    Trace.TraceWarning("Unable to dequeue action from display queue");
                }
#endif
                return true;
            }
            else
            {
                return false;
            }
        }

        private void MessageTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (silenced) 
            { 
                return; 
            }
            
            if (!CheckForCallbacks())
            {
                // no messages, close + stop timer
                busy = false;
                messageTimer.Stop();
                if (DispatcherQueue != null)
                {
                    DispatcherQueue.TryEnqueue(() => QueueEmpty?.Invoke(this, e));
                }
                else
                {
                    QueueEmpty?.Invoke(this, e);
                }
            }
        }
    }
}
