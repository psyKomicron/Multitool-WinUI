using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using Windows.Foundation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MultitoolWinUI.Controls
{
    public sealed class WindowMessageControl : ContentControl
    {
        #region dependency properties

        public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(nameof(Header), typeof(string), typeof(WindowMessageControl), new PropertyMetadata(string.Empty));
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title), typeof(string), typeof(WindowMessageControl), new PropertyMetadata(string.Empty));
        public static readonly DependencyProperty TitleGlyphProperty = DependencyProperty.Register(nameof(TitleGlyph), typeof(string), typeof(WindowMessageControl), new PropertyMetadata("\xE783"));

        #endregion

        public WindowMessageControl()
        {
            DefaultStyleKey = typeof(WindowMessageControl);
        }

        #region properties

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

        #endregion

        public event TypedEventHandler<WindowMessageControl, RoutedEventArgs> Dismiss;

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (GetTemplateChild("DismissButton") is HyperlinkButton b)
            {
                b.Click += (sender, e) =>
                {
                    Dismiss?.Invoke(this, e);
                };
            }
        }
    }
}
