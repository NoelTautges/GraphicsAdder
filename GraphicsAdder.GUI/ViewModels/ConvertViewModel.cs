using Avalonia.Controls;
using GraphicsAdder.GUI.Models;
using GraphicsAdder.GUI.Services;
using ReactiveUI;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Input;

namespace GraphicsAdder.GUI.ViewModels
{
    public class ConvertViewModel : ViewModelBase
    {
        private SettingsJson settingsJson;
        private GraphicsConverter graphicsConverter;

        public ConvertViewModel(SettingsJson settingsJson, GraphicsConverter graphicsConverter)
        {
            this.settingsJson = settingsJson;
            Settings = this.settingsJson.LoadSettings();
            this.WhenAnyValue(x => x.Settings.SourcePath, x => x.Settings.SeparateDestination, x => x.Settings.DestinationPath)
                .Subscribe(_ => this.settingsJson.SaveSettings(Settings));

            this.graphicsConverter = graphicsConverter;
            ConversionProgress = conversionProgress = new ConversionProgress();
        }

        public async void OpenFolderPicker(Window window, string origin)
        {
            var dialog = new OpenFolderDialog();
            dialog.Title = $"Choose {origin} Folder"
            var result = await dialog.ShowAsync(window);

            if (origin == "Source")
            {
                Settings.SourcePath = result;
            }
            else
            {
                Settings.DestinationPath = result;
            }
        }

        public void ChooseSourcePath(Window window) => OpenFolderPicker(window, "Source");
        public void ChooseDestinationPath(Window window) => OpenFolderPicker(window, "Destination");

        public void SetEpicGamesPath() => throw new NotImplementedException();
        public void SetSteamPath()
        {
            var path = "";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                path = @"C:\Program Files (x86)\Steam\steamapps\common\Outer Wilds\OuterWilds_Data";
            }
            else
            {
                throw new PlatformNotSupportedException();
            }

            if (Settings.SeparateDestination)
            {
                Settings.DestinationPath = path;
            }
            else
            {
                Settings.SourcePath = path;
            }
        }

        public async void StartConversion()
        {
            var progressCallback = new Progress<ConversionProgress>(e => ConversionProgress = e);
            await Task.Factory.StartNew(() => graphicsConverter.StartConversion(progressCallback, Settings));

            ConversionProgress = new ConversionProgress();

            if (Settings.PlaySound)
            {
                var proc = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        CreateNoWindow = true
                    }
                };

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    proc.StartInfo.FileName = "rundll32.exe";
                    proc.StartInfo.Arguments = "user32.dll,MessageBeep";
                }
                else
                {
                    throw new PlatformNotSupportedException();
                }

                proc.Start();
            }
        }

        public Settings Settings { get; set; }
        private ConversionProgress conversionProgress;
        public ConversionProgress ConversionProgress
        {
            get => conversionProgress;
            set => this.RaiseAndSetIfChanged(ref conversionProgress, value);
        }
    }
}
