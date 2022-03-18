using Microsoft.UI.Xaml.Controls;

using System.Collections.Generic;

namespace MultitoolWinUI.Pages.Widgets
{
    internal record WidgetInfo(string WidgetName, string WidgetIcon);
    internal record WidgetsPageNavigationInfo(Control Control, WidgetInfo Widget);
}
