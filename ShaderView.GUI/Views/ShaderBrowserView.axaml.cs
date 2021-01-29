using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ShaderView.GUI.Views
{
    public class ShaderBrowserView : UserControl
    {
        public ShaderBrowserView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
