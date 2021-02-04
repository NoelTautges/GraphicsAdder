using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using GraphicsAdder.Services;
using GraphicsAdder.ViewModels;
using GraphicsAdder.Views;

namespace GraphicsAdder
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
                var settingsJson = new SettingsJson();
                var graphicsConverter = new GraphicsConverter();

                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel(settingsJson, graphicsConverter),
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
