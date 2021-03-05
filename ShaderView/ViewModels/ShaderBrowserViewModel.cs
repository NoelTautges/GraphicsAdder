using Avalonia.Collections;
using Avalonia.Controls;
using GraphicsAdder.Common;
using ReactiveUI;
using ShaderView.Models;
using ShaderView.Services;
using System.Text;
using UnityVersion = uTinyRipper.Version;
using uTinyRipper.Classes.Shaders;
using System;

namespace ShaderView.ViewModels
{
    public class ShaderBrowserViewModel : ViewModelBase
    {

        public ShaderBrowserViewModel(ShaderLoader shaderLoader)
        {
            this.shaderLoader = shaderLoader;
            shaderCache = new LanguageCache(UnityVersion.MaxVersion);
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
            shaderCache = new LanguageCache(version);
            ContentsList.AddRange(contents);
        }

        public void ContentsList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            var selected = e.AddedItems[0];

            if (selected is not SubProgramListing)
            {
                ProgramSelected = false;
                return;
            }

            ProgramSelected = true;
            SelectedProgram = (SubProgramListing)selected;
            ProgramHasDXBC = SelectedProgram.DirectXIndex != -1;
            ProgramHasGLSL = SelectedProgram.OpenGLIndex != -1;

            switch (currentLanguage)
            {
                case ShaderLanguage.GLSL_CONVERTED_UNPROCESSED:
                    DisplayUnprocessedConvertedGLSL();
                    break;
                case ShaderLanguage.GLSL_CONVERTED_PROCESSED:
                    DisplayProcessedConvertedGLSL();
                    break;
                case ShaderLanguage.GLSL_ORIGINAL:
                    DisplayOriginalGLSL();
                    break;
            }
        }

        private ShaderSubProgram GetCurrentSubProgram(int platformIndex)
        {
            if (SelectedProgram == null)
            {
                throw new InvalidOperationException();
            }

            return SelectedProgram.Blobs[platformIndex].SubPrograms[SelectedProgram.Context.BlobIndex];
        }

        public void DisplayConvertedGLSL(bool unprocessed = false)
        {
            if (SelectedProgram == null)
            {
                throw new InvalidOperationException();
            }

            ProgramText = shaderCache.GetGLSL(SelectedProgram.Context.GetContext(GetCurrentSubProgram(SelectedProgram.DirectXIndex)), unprocessed);
            currentLanguage = unprocessed ? ShaderLanguage.GLSL_CONVERTED_UNPROCESSED : ShaderLanguage.GLSL_CONVERTED_PROCESSED;
        }

        public void DisplayProcessedConvertedGLSL() => DisplayConvertedGLSL();

        public void DisplayUnprocessedConvertedGLSL() => DisplayConvertedGLSL(true);

        public void DisplayOriginalGLSL()
        {
            ProgramText = Encoding.UTF8.GetString(GetCurrentSubProgram(SelectedProgram.OpenGLIndex).ProgramData);
            currentLanguage = ShaderLanguage.GLSL_ORIGINAL;
        }

        public void CloseFolder(Window window)
        {
            window.Title = "ShaderView";
            FolderOpened = false;
            shaderCache = new LanguageCache(UnityVersion.MaxVersion);
            ContentsList.Clear();
            currentLanguage = ShaderLanguage.GLSL_CONVERTED_PROCESSED;
            ProgramSelected = false;
            SelectedProgram = null;
            ProgramText = "";
            ProgramHasDXBC = false;
            ProgramHasGLSL = false;
        }

        private ShaderLoader shaderLoader;
        private LanguageCache shaderCache;
        private ShaderLanguage currentLanguage = ShaderLanguage.GLSL_CONVERTED_PROCESSED;

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
        private SubProgramListing? selectedProgram;
        public SubProgramListing? SelectedProgram
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
