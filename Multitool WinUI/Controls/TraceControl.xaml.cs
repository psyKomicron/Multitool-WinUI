using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

using MultitoolWinUI.Helpers;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MultitoolWinUI.Controls
{
    public sealed partial class TraceControl : UserControl
    {
        private readonly ConcurrentQueue<DispatcherQueueHandler> displayQueue = new();
        private readonly Timer messageTimer = new() { AutoReset = false, Interval = 3000 };
        private volatile bool closed;

        public TraceControl()
        {
            InitializeComponent();
            if (DispatcherQueue != null)
            {
                DispatcherQueue.ShutdownStarting += DispatcherQueue_ShutdownStarting;
            }
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        #region properties

        #region dependency properties
        public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(nameof(Header), typeof(string), typeof(TraceControl), new(string.Empty));

        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title), typeof(string), typeof(TraceControl), new(string.Empty));

        public static readonly DependencyProperty TitleGlyphProperty = DependencyProperty.Register(nameof(TitleGlyph), typeof(string), typeof(TraceControl), new("\xE783"));

        public static readonly DependencyProperty IsOpenProperty = DependencyProperty.Register(nameof(IsOpen), typeof(bool), typeof(TraceControl), new(false));

        public static readonly DependencyProperty MessageProperty = DependencyProperty.Register(nameof(MessageProperty), typeof(object), typeof(TraceControl), new(null));
        #endregion

        /// <summary>
        /// Glyph to append to the title.
        /// </summary>
        public string TitleGlyph
        {
            get => (string)GetValue(TitleGlyphProperty);
            set => SetValue(TitleGlyphProperty, value);
        }

        /// <summary>
        /// Title of the control.
        /// </summary>
        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        /// <summary>
        /// Header of the optional control's content.
        /// </summary>
        public string Header
        {
            get => (string)GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }

        /// <summary>
        /// Sets if the pop-up is open.
        /// </summary>
        public bool IsOpen
        {
            get => (bool)GetValue(IsOpenProperty);
            set => SetValue(IsOpenProperty, value);
        }

        public object Message
        {
            get => GetValue(MessageProperty);
            set => SetValue(MessageProperty, value);
        }
        #endregion

        public event TypedEventHandler<TraceControl, RoutedEventArgs> Dismissed;

        public void DisplayMessage(string title, string header, object message, Brush background)
        {
            if (displayQueue.IsEmpty)
            {
                if (!closed)
                {
                    _ = DispatcherQueue.TryEnqueue(() => DispatcherQueueCallback(title, header, message, background));
                }
            }
            else
            {
                displayQueue.Enqueue(() => DispatcherQueueCallback(title, header, message, background));
            }
        }

        private void DispatcherQueueCallback(string title, string header, object message, Brush background)
        {
            if (!closed)
            {
                Debug.WriteLine("\tMessage: [" + title + ", " + header + ", " + message.ToString() + "]");
                Background = background;
                Title = title;
                Header = header;
                Message = message;
                try
                {
                    ContentPopup.IsOpen = true;
                }
                catch (AccessViolationException e)
                {
                    Trace.TraceError(e.ToString());
                }
            }
        }

        private void Dump()
        {
            StringBuilder builder = new();
            _ = builder.AppendLine(nameof(WindowTrace) + " trace stack dump:");
            _ = builder.Append("\tQueued callbacks ");
            _ = builder.Append(displayQueue.Count);
            Trace.WriteLine(builder.ToString());
        }

        #region event handlers
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            closed = false;
            Debug.WriteLine("\tPopup width: " + ContentPopup.ActualWidth);
            Debug.WriteLine("\tGrid width: " + Grid.ActualWidth);
            DisplayMessage("Information", "Test", new TextBlock() { Text = "Does it work ?" }, new SolidColorBrush(Colors.White));
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            closed = true;
        }

        private void DispatcherQueue_ShutdownStarting(DispatcherQueue sender, DispatcherQueueShutdownStartingEventArgs args)
        {
            closed = true;
            _ = Task.Run(Dump);
            Trace.WriteLine("Dispatcher shutting down");
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (!displayQueue.IsEmpty)
            {
                if (displayQueue.TryDequeue(out DispatcherQueueHandler next))
                {
                    DispatcherQueue.TryEnqueue(next);
                }
#if TRACE
                else
                {
                    Trace.TraceWarning("Unable to dequeue action from display queue");
                }
#endif
            }
            else
            {
                // no messages, close popup + stop timer
                IsOpen = false;
                messageTimer.Stop();
            }
        }

        private void HyperlinkButton_Click(object sender, RoutedEventArgs e)
        {
            Dismissed?.Invoke(this, e);
        }
        #endregion
    }
}
