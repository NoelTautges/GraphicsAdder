using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ShaderView.Services;
using ShaderView.ViewModels;
using ShaderView.Views;

namespace ShaderView
{
    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var shaderLoader = new ShaderLoader();

                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel(shaderLoader),
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
