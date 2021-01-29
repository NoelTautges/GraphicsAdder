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
        private int shaders = 1;
        public int Shaders
        {
            get => shaders;
            set => this.RaiseAndSetIfChanged(ref shaders, value);
        }
        private int currentShader = 0;
        public int CurrentShader
        {
            get => shaders;
            set => this.RaiseAndSetIfChanged(ref currentShader, value);
        }
        private int subShaders = 1;
        public int SubShaders
        {
            get => shaders;
            set => this.RaiseAndSetIfChanged(ref subShaders, value);
        }
        private int currentSubShader = 0;
        public int CurrentSubShader
        {
            get => shaders;
            set => this.RaiseAndSetIfChanged(ref currentSubShader, value);
        }
        private int passes = 1;
        public int Passes
        {
            get => shaders;
            set => this.RaiseAndSetIfChanged(ref passes, value);
        }
        private int currentPass = 0;
        public int CurrentPass
        {
            get => shaders;
            set => this.RaiseAndSetIfChanged(ref currentPass, value);
        }
        private int programs = 1;
        public int Programs
        {
            get => shaders;
            set => this.RaiseAndSetIfChanged(ref programs, value);
        }
        private int currentProgram = 0;
        public int CurrentProgram
        {
            get => shaders;
            set => this.RaiseAndSetIfChanged(ref currentProgram, value);
        }
    }
}
