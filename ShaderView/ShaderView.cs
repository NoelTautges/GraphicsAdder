using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.WindowsAPICodePack.Dialogs;
using Terminal.Gui;
using uTinyRipper;
using uTinyRipper.Classes;
using GraphicsAdder;
using GraphicsAdder.Utils;
using uTinyRipper.Classes.Shaders;

namespace ShaderView
{
    enum ShaderInspectionStatus
    {
        LISTING,
        UNPROCESSED,
        PROCESSED,
    }

    class ShaderView
    {
        static Window menuWin;
        static FrameView shaderPane;
        static ListView shaderList;
        static List<string> shaderNames;
        static Dictionary<string, Shader> shaders;
        static FrameView infoPane;
        static TextView infoText;
        static GLSLCache shaderCache;
        static Shader currentShader;
        static uint currentSubProgram = 0;
        static ShaderInspectionStatus currentStatus = ShaderInspectionStatus.LISTING;

        static void OpenDirectory()
        {
            var dialog = new CommonOpenFileDialog();
            dialog.InitialDirectory = ".";
            dialog.IsFolderPicker = true;

            if (dialog.ShowDialog() != CommonFileDialogResult.Ok)
                return;

            menuWin.Title = $"Shader Browser: {Path.GetFileName(dialog.FileName)}";
            shaders.Clear();

            using (var structure = GameStructure.Load(new List<string> { dialog.FileName }))
            {
                foreach (var (file, fileIndex) in structure.FileCollection.SerializedFiles.WithIndex())
                {
                    if (fileIndex == 0)
                        shaderCache = new GLSLCache(file.Version);

                    foreach (var asset in file.FetchAssets())
                    {
                        if (asset.ClassID != ClassIDType.Shader)
                            continue;

                        var shader = (Shader)asset;
                        shaders.Add($"{shader.ParsedForm.Name} ({shader.PathID})", shader);
                    }
                }
            }

            shaderNames = shaders.Keys.ToList();
            shaderNames.Sort();
            shaderList.SetSource(shaderNames);
            shaderList.OnSelectedChanged();
        }

        static void CloseDirectory()
        {
            menuWin.Title = "Shader Browser";
            shaders.Clear();
            shaderList.SetSource(new List<string>());
            infoText.Text = "";
        }

        static void DisplayShaderListing()
        {
            if (shaderNames is null)
                return;

            currentShader = shaders[shaderNames[shaderList.SelectedItem]];
            currentSubProgram = 0;

            var sb = new StringBuilder();

            foreach (var sub in currentShader.ParsedForm.SubShaders)
            {
                sb.Append("Subshader");
                if (sub.Tags.Tags.Count > 0)
                {
                    sb.Append(" (");
                    foreach (var (tag, tagIndex) in sub.Tags.Tags.WithIndex())
                    {
                        sb.Append($"{tag.Key}={tag.Value}");
                        if (tagIndex < sub.Tags.Tags.Count - 1)
                            sb.Append(", ");
                    }
                    sb.Append(')');
                }
                sb.Append('\n');

                foreach (var (pass, passIndex) in sub.Passes.WithIndex())
                {
                    sb.Append($" - Pass {passIndex}\n");

                    foreach (var (prog, progIndex) in pass.ProgVertex.SubPrograms.WithIndex())
                    {
                        sb.Append($"   - Vertex subprogram {progIndex}");
                        if (prog.GlobalKeywordIndices.Length + (prog.LocalKeywordIndices != null ? prog.LocalKeywordIndices.Length : 0) > 0)
                        {
                            sb.Append(" (");
                            var keywords = prog.GlobalKeywordIndices;
                            if (prog.LocalKeywordIndices != null)
                                keywords = keywords.Concat(prog.LocalKeywordIndices).ToArray();

                            foreach (var (keyword, keywordIndex) in keywords.WithIndex())
                            {
                                sb.Append(pass.NameIndices.FirstOrDefault(name => name.Value == keyword).Key);
                                if (keywordIndex < keywords.Length - 1)
                                    sb.Append(", ");
                            }

                            sb.Append(')');
                        }
                        sb.Append('\n');
                    }

                    foreach (var (prog, progIndex) in pass.ProgFragment.SubPrograms.WithIndex())
                    {
                        sb.Append($"   - Fragment subprogram {progIndex}");
                        if (prog.GlobalKeywordIndices.Length + (prog.LocalKeywordIndices != null ? prog.LocalKeywordIndices.Length : 0) > 0)
                        {
                            sb.Append(" (");
                            var keywords = prog.GlobalKeywordIndices;
                            if (prog.LocalKeywordIndices != null)
                                keywords = keywords.Concat(prog.LocalKeywordIndices).ToArray();

                            foreach (var (keyword, keywordIndex) in keywords.WithIndex())
                            {
                                sb.Append(pass.NameIndices.FirstOrDefault(name => name.Value == keyword).Key);
                                if (keywordIndex < keywords.Length - 1)
                                    sb.Append(", ");
                            }

                            sb.Append(')');
                        }
                        sb.Append('\n');
                    }
                }
            }

            infoPane.Title = "Info";
            infoText.Text = sb.ToString();
        }

        static void DisplayShaderListing(ListViewItemEventArgs e)
        {
            DisplayShaderListing();
        }

        static void DisplayShaderText(bool unprocessed)
        {
            if (shaderNames is null)
                return;

            currentStatus = unprocessed ? ShaderInspectionStatus.UNPROCESSED : ShaderInspectionStatus.PROCESSED;
            infoPane.Title = (unprocessed ? "Unprocessed GLSL" : "Processed GLSL") + $" ({currentSubProgram})";
            ShaderSubProgram program;

            try
            {
                program = currentShader.Blobs[0].SubPrograms[currentSubProgram];
            }
            catch (IndexOutOfRangeException)
            {
                infoText.Text = "No subshaders";
                return;
            }

            infoText.Text = shaderCache.GetGLSL(program, currentShader.ParsedForm.Name, currentSubProgram, unprocessed).Replace("\t", "        ");
        }

        static void RedisplayPanel()
        {
            switch (currentStatus)
            {
                case ShaderInspectionStatus.LISTING:
                    DisplayShaderListing();
                    break;
                case ShaderInspectionStatus.UNPROCESSED:
                    DisplayShaderText(true);
                    break;
                case ShaderInspectionStatus.PROCESSED:
                    DisplayShaderText(false);
                    break;
            }
        }

        static void DecrementSubProgram()
        {
            var subPrograms = currentShader.Blobs[0].SubPrograms.Length;
            currentSubProgram = (uint)((currentSubProgram - 1 + subPrograms) % subPrograms);
            RedisplayPanel();
        }

        static void IncrementSubProgram()
        {
            var subPrograms = currentShader.Blobs[0].SubPrograms.Length;
            currentSubProgram = (uint)((currentSubProgram + 1) % subPrograms);
            RedisplayPanel();
        }

        [STAThread]
        static void Main(string[] args)
        {
            shaders = new Dictionary<string, Shader>();

            Application.Init();
            var top = Application.Top;

            var statusBar = new StatusBar(new StatusItem[]
            {
                new StatusItem(Key.Unknown, "~TAB~ Switch Panes", null),
                new StatusItem(Key.CtrlMask | Key.L, "~CTRL-L~ Show Shader Listing", DisplayShaderListing),
                new StatusItem(Key.CtrlMask | Key.U, "~CTRL-U~ Show Unprocessed GLSL", () => DisplayShaderText(true)),
                new StatusItem(Key.CtrlMask | Key.P, "~CTRL-P~ Show Processed GLSL", () => DisplayShaderText(false)),
                new StatusItem(Key.PageUp, "~PGUP~ Previous Subprogram", DecrementSubProgram),
                new StatusItem(Key.PageDown, "~PGDN~ Next Subprogram", IncrementSubProgram),
            });
            top.Add(statusBar);

            menuWin = new Window("Shader Browser")
            {
                X = 0,
                Y = 1,
                Width = Dim.Fill(),
                Height = Dim.Fill(1)
            };
            top.Add(menuWin);

            var menu = new MenuBar(new MenuBarItem[]
            {
                new MenuBarItem("_File", new MenuItem[]
                {
                    new MenuItem("_Open", "", OpenDirectory, null, null, Key.CtrlMask | Key.O),
                    new MenuItem("Close", "", CloseDirectory, null, null, Key.CtrlMask | Key.W),
                    new MenuItem("_Quit", "", Application.RequestStop, null, null, Key.CtrlMask | Key.Q)
                })
            });
            top.Add(menu);

            shaderPane = new FrameView("Shaders")
            {
                X = 0,
                Y = 0,
                Width = Dim.Percent(50),
                Height = Dim.Fill(),
                CanFocus = false
            };
            shaderList = new ListView()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill(),
                CanFocus = true
            };
            shaderList.SelectedItemChanged += DisplayShaderListing;
            shaderList.OpenSelectedItem += DisplayShaderListing;
            shaderPane.Add(shaderList);
            menuWin.Add(shaderPane);

            infoPane = new FrameView("Info")
            {
                X = Pos.Percent(50),
                Y = 0,
                Width = Dim.Percent(50),
                Height = Dim.Fill(),
            };
            infoText = new TextView()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill(),
                ReadOnly = true
            };
            infoPane.Add(infoText);
            menuWin.Add(infoPane);

            Application.Run();
        }
    }
}
