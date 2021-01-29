using Avalonia.Controls;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShaderView.GUI.ViewModels
{
    public class ShaderBrowserViewModel : ViewModelBase
    {
        public async void OpenFolder(Window window)
        {
            var dialog = new OpenFolderDialog()
            {
                Title = "Choose Data Folder"
            };
            var result = await dialog.ShowAsync(window);

            if (result == "")
            {
                return;
            }

            FolderOpened = true;
            var name = Path.GetFileName(result);
            window.Title = "ShaderView: " + (name is null ? result : name);
        }

        public void CloseFolder(Window window)
        {
            FolderOpened = false;
            window.Title = "ShaderView";
        }

        private bool folderOpened = false;
        public bool FolderOpened
        {
            get => folderOpened;
            set => this.RaiseAndSetIfChanged(ref folderOpened, value);
        }
    }
}
