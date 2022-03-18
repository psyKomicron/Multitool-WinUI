using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

using Multitool.Net.Imaging;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MultitoolWinUI.Controls
{
    public sealed partial class EmoteDisplay : UserControl
    {
        public EmoteDisplay()
        {
            this.InitializeComponent();
        }

        public EmoteDisplay(List<Emote> emotes)
        {

        }

        public ObservableCollection<Emote> Emotes { get; set; }
        public string EmoteProvider { get; set; }
        public double EmoteSize { get; set; } = 30;
        public double EmoteSizeOnHover { get; set; }
        public int MaximumRowsOrColumns { get; set; } = 10;
        public double PanelMinimumHeight { get; set; } = 300;

        private void EmoteGridView_ItemClick(object sender, ItemClickEventArgs e)
        {

        }
    }
}
