using ReactiveUI;

namespace GraphicsAdder.GUI.Models
{
    public class ConversionProgress : ReactiveObject
    {
        private bool inProgress = false;
        public bool InProgress
        {
            get => inProgress;
            set => this.RaiseAndSetIfChanged(ref inProgress, value);
        }
        private int files = 1;
        public int Files
        {
            get => files;
            set => this.RaiseAndSetIfChanged(ref files, value);
        }
        private int currentFile = 0;
        public int CurrentFile
        {
            get => currentFile;
            set => this.RaiseAndSetIfChanged(ref currentFile, value);
        }
    }
}
