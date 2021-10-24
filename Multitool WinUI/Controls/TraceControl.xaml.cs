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
        private readonly Timer messageTimer = new() { AutoReset = true, Enabled = false, Interval = 3000 };
        private readonly object _lock = new();
        private volatile bool closed;
        private volatile bool busy;

        public TraceControl()
        {
            InitializeComponent();
            if (DispatcherQueue != null)
            {
                DispatcherQueue.ShutdownStarting += DispatcherQueue_ShutdownStarting;
            }
            messageTimer.Elapsed += Timer_Elapsed;
#if !DEBUG
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
#else
            closed = false;
#endif
        }

        #region properties
#if DEBUG
        public double Interval
        {
            get => messageTimer.Interval;
            set => messageTimer.Interval = value;
        }
        /// <summary>
        /// Glyph to append to the title.
        /// </summary>
        public string TitleGlyph
        {
            get => Icon.Glyph;
            set => Icon.Glyph = value;
        }

        /// <summary>
        /// Title of the control.
        /// </summary>
        public string Title
        {
            get => TitleTextBlock.Text;
            set => TitleTextBlock.Text = value;
        }

        /// <summary>
        /// Header of the optional control's content.
        /// </summary>
        public string Header
        {
            get => HeaderTextBlock.Text;
            set => HeaderTextBlock.Text = value;
        }

        public string Message
        {
            get => ContentTextBlock.Text;
            set => ContentTextBlock.Text = value;
        }
#else
        #region dependency properties
        public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(nameof(Header), typeof(string), typeof(TraceControl), new(string.Empty));

        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title), typeof(string), typeof(TraceControl), new(string.Empty));

        public static readonly DependencyProperty TitleGlyphProperty = DependencyProperty.Register(nameof(TitleGlyph), typeof(string), typeof(TraceControl), new("\xE783"));

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

        public object Message
        {
            get => GetValue(MessageProperty);
            set => SetValue(MessageProperty, value);
        }
#endif

        public bool Sync { get; set; } = true;
        #endregion

        public event TypedEventHandler<TraceControl, Visibility> VisibilityChanged;

        public void QueueMessage(string title, string header, string message, Brush background)
        {
            lock (_lock)
            {
                if (!busy)
                {
                    if (!closed)
                    {
                        busy = true;
                        _ = DispatcherQueue.TryEnqueue(() => DisplayMessage(title, header, message, background));
                    }
                }
                else
                {
                    displayQueue.Enqueue(() => DisplayMessage(title, header, message, background));
                    Debug.WriteLine("\tQueued message [message: " + message + "]");
                }
            }
        }

        #region private methods
        private void Dump()
        {
            StringBuilder builder = new();
            _ = builder.AppendLine(nameof(TraceControl) + " trace stack dump:");
            _ = builder.Append("\tQueued callbacks ");
            _ = builder.Append(displayQueue.Count);
            displayQueue.Clear();
            Trace.WriteLine(builder.ToString());
        }

        private void DisplayMessage(string title, string header, string message, Brush background)
        {
            if (!closed)
            {
                Debug.WriteLine("\tdisplaying : [ " + message + " ]");
                Background = background;
                Title = title;
                Header = header;
                Message = message;
                messageTimer.Start();
                VisibilityChanged?.Invoke(this, Visibility.Visible);
            }
        }

        private bool CheckForCallbacks()
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
                return true;
            }
            else
            {
                return false;
            }
        }

#if DEBUG
        private void SetTitle(string title)
        {
            TitleTextBlock.Text = title;
        }

        private void SetHeader(string header)
        {
            HeaderTextBlock.Text = header;
        }

        private void SetMessage(string message)
        {
            ContentTextBlock.Text = message;
        }
#endif
        #endregion

        #region event handlers
#if false
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine(nameof(TraceControl) + " loaded");
            closed = false;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            closed = true;
            _ = Task.Run(Dump);
        }
#endif

        private void DispatcherQueue_ShutdownStarting(DispatcherQueue sender, DispatcherQueueShutdownStartingEventArgs args)
        {
            closed = true;
            busy = false;
            _ = Task.Run(Dump);
            Trace.WriteLine("Dispatcher shutting down");
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (!CheckForCallbacks())
            {
                // no messages, close + stop timer
                busy = false;
                messageTimer.Stop();
                if (Sync)
                {
                    DispatcherQueue.TryEnqueue(() => VisibilityChanged?.Invoke(this, Visibility.Collapsed));
                }
                else
                {
                    VisibilityChanged?.Invoke(this, Visibility.Collapsed);
                }
            }
        }

        private void HyperlinkButton_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckForCallbacks())
            {
                busy = false;
                VisibilityChanged?.Invoke(this, Visibility.Collapsed);
            }
        }
        #endregion
    }
}
