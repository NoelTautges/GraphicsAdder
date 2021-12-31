using ShaderView.Services;

namespace ShaderView.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public MainWindowViewModel()
        {
            Browser = new ShaderBrowserViewModel();
        }

        public ShaderBrowserViewModel Browser { get; }
    }
}
