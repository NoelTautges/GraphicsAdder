using GraphicsAdder.Services;

namespace GraphicsAdder.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public MainWindowViewModel(SettingsJson settingsJson, GraphicsConverter graphicsConverter)
        {
            Conversion = new ConvertViewModel(settingsJson, graphicsConverter);
        }

        public ConvertViewModel Conversion { get; }
    }
}
