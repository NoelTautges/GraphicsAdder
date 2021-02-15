using Avalonia.Collections;
using Avalonia.Controls;
using GraphicsAdder.Common;
using ReactiveUI;
using ShaderView.Models;
using ShaderView.Services;
using System.Text;
using UnityVersion = uTinyRipper.Version;
using uTinyRipper.Classes.Shaders;

namespace ShaderView.ViewModels
{
    public class ShaderBrowserViewModel : ViewModelBase
    {

        public ShaderBrowserViewModel(ShaderLoader shaderLoader)
        {
            this.shaderLoader = shaderLoader;
            shaderCache = new GLSLCache(UnityVersion.MaxVersion);
        }

        public async void OpenFolder(Window window)
        {
            var dialog = new OpenFolderDialog()
            {
                Title = "Choose Data Folder"
            };
            var folder = await dialog.ShowAsync(window);

            if (folder == "")
            {
                return;
            }

            CloseFolder(window);
            FolderOpened = true;
            window.Title = $"ShaderView: {folder}";

            var (version, contents) = shaderLoader.LoadShaders(folder);
            shaderCache = new GLSLCache(version);
            ContentsList.AddRange(contents);
        }

        public void ContentsList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            var selected = e.AddedItems[0];

            if (selected is not SubProgramListing)
            {
                ProgramSelected = false;
                SelectedProgram = new SubProgramListing();
                return;
            }

            ProgramSelected = true;
            SelectedProgram = (SubProgramListing)selected;
            ProgramHasDXBC = SelectedProgram.DirectXIndex != -1;
            ProgramHasGLSL = SelectedProgram.OpenGLIndex != -1;

            if (ProgramHasDXBC)
            {
                DisplayUnprocessedConvertedGLSL();
            }
            else if (ProgramHasGLSL)
            {
                DisplayOriginalGLSL();
            }
            else
            {
                ProgramText = "No displayable platforms available!";
            }
        }

        private ShaderSubProgram GetCurrentSubProgram(int platformIndex)
        {
            return SelectedProgram.Blobs[platformIndex].SubPrograms[SelectedProgram.BlobIndex];
        }

        public void DisplayConvertedGLSL(bool unprocessed = false)
        {
            var shader = ContentsList[SelectedProgram.ShaderIndex];
            ProgramText = shaderCache.GetGLSL(GetCurrentSubProgram(SelectedProgram.DirectXIndex), shader.Name, SelectedProgram.BlobIndex, unprocessed);
        }

        public void DisplayProcessedConvertedGLSL() => DisplayConvertedGLSL();

        public void DisplayUnprocessedConvertedGLSL() => DisplayConvertedGLSL(true);

        public void DisplayOriginalGLSL() => ProgramText = Encoding.UTF8.GetString(GetCurrentSubProgram(SelectedProgram.OpenGLIndex).ProgramData);

        public void CloseFolder(Window window)
        {
            window.Title = "ShaderView";
            FolderOpened = false;
            ContentsList.Clear();
            ProgramSelected = false;
            SelectedProgram = new();
            ProgramText = "";
            ProgramHasDXBC = false;
            ProgramHasGLSL = false;
        }

        private ShaderLoader shaderLoader;
        private GLSLCache shaderCache;

        private bool folderOpened = false;
        public bool FolderOpened
        {
            get => folderOpened;
            set => this.RaiseAndSetIfChanged(ref folderOpened, value);
        }
        private AvaloniaList<ComponentListing> contentsList = new();
        public AvaloniaList<ComponentListing> ContentsList
        {
            get => contentsList;
            set => this.RaiseAndSetIfChanged(ref contentsList, value);
        }
        private bool programSelected = false;
        public bool ProgramSelected
        {
            get => programSelected;
            set => this.RaiseAndSetIfChanged(ref programSelected, value);
        }
        private SubProgramListing selectedProgram = new SubProgramListing();
        public SubProgramListing SelectedProgram
        {
            get => selectedProgram;
            set => this.RaiseAndSetIfChanged(ref selectedProgram, value);
        }
        private bool programHasGLSL = false;
        public bool ProgramHasGLSL
        {
            get => programHasGLSL;
            set => this.RaiseAndSetIfChanged(ref programHasGLSL, value);
        }
        private bool programHasDXBC = false;
        public bool ProgramHasDXBC
        {
            get => programHasDXBC;
            set => this.RaiseAndSetIfChanged(ref programHasDXBC, value);
        }
        private string programText = "";
        public string ProgramText
        {
            get => programText;
            set => this.RaiseAndSetIfChanged(ref programText, value);
        }
    }
}
