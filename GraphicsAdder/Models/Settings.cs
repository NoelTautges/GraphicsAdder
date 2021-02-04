using ReactiveUI;

namespace GraphicsAdder.Models
{
    public class SettingsInternal
    {
        public string SourcePath = "";
        public bool SeparateDestination = false;
        public string DestinationPath = "";
        public bool PlaySound = true;
    }

    public class Settings : ReactiveObject
    {
        public SettingsInternal SettingsInternal { get; set; } = new SettingsInternal();
        public string SourcePath
        {
            get => SettingsInternal.SourcePath;
            set => this.RaiseAndSetIfChanged(ref SettingsInternal.SourcePath, value);
        }
        public bool SeparateDestination
        {
            get => SettingsInternal.SeparateDestination;
            set => this.RaiseAndSetIfChanged(ref SettingsInternal.SeparateDestination, value);
        }
        public string DestinationPath
        {
            get => SettingsInternal.DestinationPath;
            set => this.RaiseAndSetIfChanged(ref SettingsInternal.DestinationPath, value);
        }
        public bool PlaySound
        {
            get => SettingsInternal.PlaySound;
            set => this.RaiseAndSetIfChanged(ref SettingsInternal.PlaySound, value);
        }
    }
}
