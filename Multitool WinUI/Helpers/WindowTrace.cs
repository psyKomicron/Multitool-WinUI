using Microsoft.UI.Xaml.Media;

using Multitool.DAL;

using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

using Windows.UI;

namespace MultitoolWinUI.Helpers
{
    internal class WindowTrace : TraceListener
    {
        private readonly MainWindow window;
        private readonly SolidColorBrush errorBrush;
        private readonly SolidColorBrush warningBrush;
        private readonly SolidColorBrush infoBrush;

        public WindowTrace(MainWindow window) : base()
        {
            this.window = window;
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

        public bool ShowTimestamp { get; set; } = true;

        public int ShowStackTrace { get; set; } = 0;

        #region trace methods
        /// <inheritdoc />
        public override void Write(string message)
        {

        }

        /// <inheritdoc />
        public override void WriteLine(string message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                Write(message + "\n");
            }
        }

        /// <inheritdoc />
        public override void TraceData(TraceEventCache eventCache, string source, TraceEventType eventType, int id, object data)
        {
            _ = Task.Run(() =>
            {
                switch (eventType)
                {
                    case TraceEventType.Error:
                        TraceMessage("Error", data.ToString(), errorBrush, eventCache);
                        break;
                    case TraceEventType.Warning:
                        TraceMessage("Warning", data.ToString(), warningBrush, eventCache);
                        break;
                    case TraceEventType.Information:
                        TraceMessage("Information", data.ToString(), infoBrush, eventCache);
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
                        TraceMessage("Error", data.ToString(), errorBrush, eventCache);
                        break;
                    case TraceEventType.Warning:
                        TraceMessage("Warning", data.ToString(), warningBrush, eventCache);
                        break;
                    case TraceEventType.Information:
                        TraceMessage("Information", data.ToString(), infoBrush, eventCache);
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
                    builder.Append(string.Format(format, args[i])).Append('\n');
                }
                string message = builder.ToString();
                switch (eventType)
                {
                    case TraceEventType.Error:
                        TraceMessage("Error", message, errorBrush, eventCache);
                        break;
                    case TraceEventType.Warning:
                        TraceMessage("Warning", message, warningBrush, eventCache);
                        break;
                    case TraceEventType.Information:
                        TraceMessage("Information", message, infoBrush, eventCache);
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
                        TraceMessage("Error", string.Empty, errorBrush, eventCache);
                        break;
                    case TraceEventType.Warning:
                        TraceMessage("Warning", string.Empty, warningBrush, eventCache);
                        break;
                    case TraceEventType.Information:
                        TraceMessage("Information", string.Empty, infoBrush, eventCache);
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
                        TraceMessage("Error", message, errorBrush, eventCache);
                        break;
                    case TraceEventType.Warning:
                        TraceMessage("Warning", message, warningBrush, eventCache);
                        break;
                    case TraceEventType.Information:
                        TraceMessage("Information", message, infoBrush, eventCache);
                        break;
                }
            });
        }

        #endregion

        #region private methods
        private void TraceMessage(string title, string message, Brush background, TraceEventCache eventCache)
        {
            if (ShowTimestamp && ShowStackTrace == 0)
            {

                window.MessageDisplay.QueueMessage(title, eventCache.DateTime.ToLongTimeString() + " - " + message, background);
            }
            else
            {
                window.MessageDisplay.QueueMessage(title, message, background);
            }
        }
        #endregion
    }
}
