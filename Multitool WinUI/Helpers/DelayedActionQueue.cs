using Microsoft.UI.Dispatching;

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Timers;

namespace MultitoolWinUI.Helpers
{
    internal class DelayedActionQueue
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

        public double Delay
        {
            get => messageTimer.Interval;
            set => messageTimer.Interval = value;
        }

        public void QueueAction(DispatcherQueueHandler handler)
        {
            if (!busy)
            {
                if (DispatcherQueue != null)
                {
                    busy = true;
                    _ = DispatcherQueue.TryEnqueue(handler);
                }
                else
                {
                    handler();
                }
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
            if (silenced) { return; }
            if (!CheckForCallbacks())
            {
                // no messages, close + stop timer
                busy = false;
                messageTimer.Stop();
            }
        }
    }
}
