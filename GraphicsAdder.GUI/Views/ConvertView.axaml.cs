using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace GraphicsAdder.Views
{
    public class ConvertView : UserControl
    {
        public ConvertView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
