using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ShaderView.ViewModels;

namespace ShaderView.Views
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

            // I know this is bad practice but I keep getting a "Unable to find suitable setter or adder for property SelectionChanged" when adding a listener in the axaml
            this.FindControl<TreeView>("contentsList").SelectionChanged += ContentsList_SelectionChanged;
        }

        private void ContentsList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (DataContext is null)
            {
                return;
            }

            ((ShaderBrowserViewModel)DataContext).ContentsList_SelectionChanged(sender, e);
        }
    }
}
