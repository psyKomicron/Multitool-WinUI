using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
#if DEBUG
        private readonly Timer timer = new(3000) { AutoReset = true };
#else
        private readonly Timer timer = new(3000) { AutoReset = false };
#endif
        private readonly MainWindow window;
        private readonly DispatcherQueue dispatcher;
        private readonly SolidColorBrush errorBrush;
        private readonly SolidColorBrush warningBrush;
        private readonly SolidColorBrush infoBrush;

        public WindowTrace(MainWindow w) : base("WindowTraceListener")
        {
            window = w;
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

        public Color DefaultColor { get; set; }

        #region trace methods

        /// <inheritdoc />
        public override void Write(string message)
        {
            
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
            window.MessageDisplay.DisplayMessage(title, header, message, background);
        }
        #endregion
    }
}
