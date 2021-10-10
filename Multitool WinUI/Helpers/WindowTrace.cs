using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

using Multitool.DAL;

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

using Windows.UI;

namespace MultitoolWinUI.Helpers
{
    internal class WindowTrace : TraceListener
    {
        private readonly Timer timer = new(3000) { AutoReset = false };
        private readonly MainWindow window;
        private readonly DispatcherQueue dispatcher;
        private readonly SolidColorBrush errorBrush;
        private readonly SolidColorBrush warningBrush;
        private readonly SolidColorBrush infoBrush;
        private bool timerRunning;
        private volatile bool closed;

        public WindowTrace(MainWindow w) : base("WindowTraceListener")
        {
            window = w;
            window.Closed += Window_Closed;
            dispatcher = w.DispatcherQueue;
            try
            {
                errorBrush = new(Tool.GetAppRessource<Color>("SystemAccentColor"));
            }
            catch (SettingNotFoundException e) { Trace.TraceError(e.Message); }
            try
            {
                warningBrush = new(Tool.GetAppRessource<Color>("DevOrange"));
            }
            catch (SettingNotFoundException e) { Trace.TraceError(e.Message); }
            try
            {
                infoBrush = new(Tool.GetAppRessource<Color>("DevBlue"));
            }
            catch (SettingNotFoundException e) { Trace.TraceError(e.Message); }
        }

        #region trace methods

        /// <inheritdoc />
        public override void Write(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                if (dispatcher.TryEnqueue(() =>
                {
                    window.MessageDisplay.Title = string.Empty;
                    window.MessageDisplay.Header = "Message";
                    window.MessageDisplay.Content = message;
                    window.ExceptionPopup.IsOpen = true;
                }))
                {
                    if (timerRunning)
                    {
                        timer.Stop();
                    }
                    timer.Start();
                }
            }
        }

        /// <inheritdoc />
        public override void WriteLine(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                Write(message);
            }
        }

        public static void TraceError(string message, [CallerMemberName] string callerName = null)
        {
            Trace.TraceError(callerName + " -> " + message);
        }

        /// <inheritdoc />
        public override void TraceData(TraceEventCache eventCache, string source, TraceEventType eventType, int id, object data)
        {
            _ = Task.Run(() =>
            {
                switch (eventType)
                {
                    case TraceEventType.Error:
                        TraceMessage("Error", source, data.ToString(), errorBrush);
                        break;
                    case TraceEventType.Warning:
                        TraceMessage("Warning", source, data.ToString(), warningBrush);
                        break;
                    case TraceEventType.Information:
                        TraceMessage("Information", source, data.ToString(), infoBrush);
                        break;
                }
            });
        }

        /// <inheritdoc />
        public override void TraceData(TraceEventCache eventCache, string source, TraceEventType eventType, int id, params object[] data)
        {
            _ = Task.Run(() =>
            {
                switch (eventType)
                {
                    case TraceEventType.Error:
                        TraceMessage("Error", source, data.ToString(), errorBrush);
                        break;
                    case TraceEventType.Warning:
                        TraceMessage("Warning", source, data.ToString(), warningBrush);
                        break;
                    case TraceEventType.Information:
                        TraceMessage("Information", source, data.ToString(), infoBrush);
                        break;
                }
            });
        }

        /// <inheritdoc />
        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string format, params object[] args)
        {
            _ = Task.Run(() =>
            {
                StringBuilder builder = new();
                for (int i = 0; i < args.Length; i++)
                {
                    builder.Append(string.Format(format, args[i]) + "\n");
                }
                string message = builder.ToString();
                switch (eventType)
                {
                    case TraceEventType.Error:
                        TraceMessage("Error", source, message, errorBrush);
                        break;
                    case TraceEventType.Warning:
                        TraceMessage("Warning", source, message, warningBrush);
                        break;
                    case TraceEventType.Information:
                        TraceMessage("Information", source, message, infoBrush);
                        break;
                }
            });
        }

        /// <inheritdoc />
        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id)
        {
            _ = Task.Run(() =>
            {
                switch (eventType)
                {
                    case TraceEventType.Error:
                        TraceMessage("Error", source, string.Empty, errorBrush);
                        break;
                    case TraceEventType.Warning:
                        TraceMessage("Warning", source, string.Empty, warningBrush);
                        break;
                    case TraceEventType.Information:
                        TraceMessage("Information", source, string.Empty, infoBrush);
                        break;
                }
            });
        }

        /// <inheritdoc />
        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
        {
            _ = Task.Run(() =>
            {
                switch (eventType)
                {
                    case TraceEventType.Error:
                        TraceMessage("Error", source, message, errorBrush);
                        break;
                    case TraceEventType.Warning:
                        TraceMessage("Warning", source, message, warningBrush);
                        break;
                    case TraceEventType.Information:
                        TraceMessage("Information", source, message, infoBrush);
                        break;
                }
            });
        }

        #endregion

        #region private methods

        private void TraceMessage(string title, string header, string message, Brush background)
        {
            if (!closed && dispatcher.TryEnqueue(() =>
            {
                if (!closed)
                {
                    window.MessageDisplay.Background = background;
                    window.MessageDisplay.Title = title;
                    window.MessageDisplay.Header = header;
                    window.MessageDisplay.Content = message;
                    window.ExceptionPopup.IsOpen = true;
                }
            }))
            {
                if (timerRunning)
                {
                    timer.Stop();
                }
                timer.Start();
            }
        }

        private void Window_Closed(object sender, WindowEventArgs args)
        {
            closed = true;
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            timerRunning = false;
            _ = dispatcher.TryEnqueue(() => window.ExceptionPopup.IsOpen = false);
        }

        #endregion
    }
}
