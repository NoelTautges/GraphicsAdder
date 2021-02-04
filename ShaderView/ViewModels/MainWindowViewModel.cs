using ShaderView.Services;

namespace ShaderView.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public MainWindowViewModel(ShaderLoader shaderLoader)
        {
            Browser = new ShaderBrowserViewModel(shaderLoader);
        }

        public ShaderBrowserViewModel Browser { get; }
    }
}
