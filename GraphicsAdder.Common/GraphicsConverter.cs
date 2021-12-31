using AssetsTools.NET;
using AssetsTools.NET.Extra;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using uTinyRipper;
using UnityPlatform = uTinyRipper.Platform;
using UnityVersion = uTinyRipper.Version;
using uTinyRipper.Classes;
using uTinyRipper.Classes.Shaders;
using uTinyRipper.Layout;

namespace GraphicsAdder.Common
{
    public class GraphicsConverter
    {
        private const string SettingsFile = "globalgamemanagers";
        private const int ShaderPlatform = (int)ShaderGpuProgramType55.GLCore41;
        private readonly AssetTypeValueField[] NoChildren = Array.Empty<AssetTypeValueField>();

        private AssetLayout layout;

        public GraphicsConverter(string gameDirectory)
        {
            GetAssetsFiles(gameDirectory);
            if (DataPath == null || EngineSettings == null || AssetsFiles == null)
            {
                throw new Exception($"Cannot load engine settings from {gameDirectory}!");
            }

            AssetsManager.LoadClassPackage("classdata.tpk");
            var unityVersion = EngineSettings.file.typeTree.unityVersion;
            AssetsManager.LoadClassDatabaseFromPackage(unityVersion);
            Version = UnityVersion.Parse(unityVersion);
            layout = new(new LayoutInfo(Version, UnityPlatform.StandaloneWin64Player, TransferInstructionFlags.SerializeGameRelease));
            Cache = new(Version);
        }

        private void GetAssetsFiles(string gameDirectory)
        {
            if (File.Exists(Path.Combine(gameDirectory, "UnityPlayer.dll")))
            {
                try
                {
                    gameDirectory = (from dir in Directory.GetDirectories(gameDirectory) where dir.Contains("_Data") select dir).First();
                }
                catch (InvalidOperationException)
                {
                    return;
                }
            }

            var engineSettings = Path.Combine(gameDirectory, SettingsFile);
            if (!File.Exists(engineSettings))
            {
                return;
            }

            DataPath = Path.GetDirectoryName(engineSettings) ?? gameDirectory;
            EngineSettings = AssetsManager.LoadAssetsFile(engineSettings, true);
            AssetsFiles = EngineSettings.dependencies.Where(d => d != null).ToList();
        }

        private ShaderSubProgramBlob[] UnpackSubProgramBlobs(AssetLayout layout, uint[][] offsets, uint[][] compressedLengths, uint[][] decompressedLengths, byte[] compressedBlob)
        {
            var blobs = new ShaderSubProgramBlob[offsets.Length];
            using var memStream = new MemoryStream(compressedBlob);
            for (int i = 0; i < offsets.Length; i++)
            {
                uint[] blobOffsets = offsets[i];
                uint[] blobCompressedLengths = compressedLengths[i];
                uint[] blobDecompressedLengths = decompressedLengths[i];
                blobs[i].Read(layout, memStream, blobOffsets, blobCompressedLengths, blobDecompressedLengths);
            }
            return blobs;
        }

        private ShaderSubProgramBlob[] UnpackSubProgramBlobs(AssetLayout layout, uint[] offsets, uint[] compressedLengths, uint[] decompressedLengths, byte[] compressedBlob)
        {
            var blobs = new ShaderSubProgramBlob[offsets.Length];
            using var memStream = new MemoryStream(compressedBlob);
            for (int i = 0; i < blobs.Length; i++)
            {
                uint[] blobOffsets = new uint[] { offsets[i] };
                uint[] blobCompressedLengths = new uint[] { compressedLengths[i] };
                uint[] blobDecompressedLengths = new uint[] { decompressedLengths[i] };
                blobs[i].Read(layout, memStream, blobOffsets, blobCompressedLengths, blobDecompressedLengths);
            }
            return blobs;
        }

        public ShaderSubProgramBlob[] GetBlobs(AssetTypeValueField shader)
        {
            var blobs = Array.Empty<ShaderSubProgramBlob>();
            var compressedBlob = shader["compressedBlob"]["Array"].GetChildrenList().Select(b => (byte)b.GetValue().AsInt()).ToArray();
            if (Shader.IsDoubleArray(Version))
            {
                var offsets = AssetsToolsHelper.GetUIntDoubleArray(shader["offsets"]);
                var compressedLengths = AssetsToolsHelper.GetUIntDoubleArray(shader["compressedLengths"]);
                var decompressedLengths = AssetsToolsHelper.GetUIntDoubleArray(shader["decompressedLengths"]);
                blobs = UnpackSubProgramBlobs(layout, offsets, compressedLengths, decompressedLengths, compressedBlob);
            }
            else
            {
                var offsets = AssetsToolsHelper.GetUIntArray(shader["offsets"]);
                var compressedLengths = AssetsToolsHelper.GetUIntArray(shader["compressedLengths"]);
                var decompressedLengths = AssetsToolsHelper.GetUIntArray(shader["decompressedLengths"]);
                blobs = UnpackSubProgramBlobs(layout, offsets, compressedLengths, decompressedLengths, compressedBlob);
            }
            return blobs;
        }

        public void ConvertFile(AssetsFileInstance inst, string destPath, bool inPlace)
        {
            var replacers = new List<AssetsReplacer>();

            foreach (var asset in inst.table.GetAssetsOfType((int)AssetClassID.Shader))
            {
                var shader = AssetsManager.GetTypeInstance(inst, asset).GetBaseField();
                var shaderName = shader["m_ParsedForm"]["m_Name"].value.AsString();
                var platforms = shader["platforms"]["Array"];
                var directXIndex = -1;

                if (!shadersOfInterest.Contains(shaderName))
                {
                    continue;
                }

                foreach (var (platform, i) in platforms.GetChildrenList().WithIndex())
                {
                    var value = platform.GetValue().AsInt();

                    switch (value)
                    {
                        case 4:
                            directXIndex = i;
                            break;
                        case 15:
                            return;
                        default:
                            break;
                    }
                }

                platforms.SetChildrenList(platforms.GetChildrenList().Concat(AssetsToolsHelper.GetArrayFromTemplate(platforms, new int[] { 15 })).ToArray());
                var blobs = this.GetBlobs(shader);
                ref var blob = ref blobs[directXIndex];

                var cache = new LanguageCache(Version);
                var subShaders = shader["m_ParsedForm"]["m_SubShaders"]["Array"].childrenCount;

                using var memStream = new MemoryStream();
                memStream.Write(shader["compressedBlob"]["Array"].children.Select(b => (byte)b.value.AsInt()).ToArray());

                foreach (var (subShader, subIndex) in shader["m_ParsedForm"]["m_SubShaders"]["Array"].children.WithIndex())
                {
                    foreach (var (pass, passIndex) in subShader["m_Passes"]["Array"].children.WithIndex())
                    {
                        var ctx = new ShaderContext(shader, pass);
                        cache.ProcessSubPrograms(ctx, blob);

                        var vertices = pass["progVertex"]["m_SubPrograms"]["Array"];
                        var vertexSubPrograms = vertices.children;
                        var fragments = pass["progFragment"]["m_SubPrograms"]["Array"];
                        var fragmentSubPrograms = fragments.children;

                        for (int i = 0; i < vertexSubPrograms.Length; i++)
                        {
                            var vertexSubProgram = vertexSubPrograms[i];
                            var vertexBlobIndex = vertexSubProgram["m_BlobIndex"].value.AsUInt();
                            ref var vertex = ref blob.SubPrograms[vertexBlobIndex];
                            var mostKeywords = -1;
                            var fragmentSubProgram = fragmentSubPrograms[0];
                            var bestFragmentIndex = -1;

                            foreach (var (possibleFragment, fragmentIndex) in fragmentSubPrograms.WithIndex())
                            {
                                var globalKeywords = vertexSubProgram["m_GlobalKeywordIndices"].children.Select(keyword => keyword.value.AsInt()).ToArray();
                                var hasLocalKeywords = SerializedSubProgram.HasLocalKeywordIndices(Version);
                                var localKeywords = hasLocalKeywords ? vertexSubProgram["m_LocalKeywordIndices"].children.Select(keyword => keyword.value.AsInt()).ToArray() : new int[] {};

                                if (possibleFragment["m_GlobalKeywordIndices"].children.Any(keyword => !globalKeywords.Contains(keyword.value.AsInt())) ||
                                    (hasLocalKeywords && possibleFragment["m_LocalKeywordIndices"].children.Any(keyword => !localKeywords.Contains(keyword.value.AsInt()))))
                                {
                                    continue;
                                }

                                var keywords = possibleFragment["m_GlobalKeywordIndices"].childrenCount + (hasLocalKeywords ? possibleFragment["m_LocalKeywordIndices"].childrenCount : 0);
                                if (keywords > mostKeywords)
                                {
                                    mostKeywords = keywords;
                                    fragmentSubProgram = possibleFragment;
                                    bestFragmentIndex = fragmentIndex;
                                }
                            }

                            var fragmentBlobIndex = fragmentSubProgram["m_BlobIndex"].value.AsUInt();
                            ref var fragment = ref blob.SubPrograms[fragmentBlobIndex];

                            var vertexGLSL = cache.GetGLSL(ctx.GetContext(vertex, vertexBlobIndex));
                            var fragmentGLSL = cache.GetGLSL(ctx.GetContext(fragment, fragmentBlobIndex));
                            var completedProgram = string.Join(
                                "\n",
                                "#ifdef VERTEX",
                                vertexGLSL,
                                "#endif",
                                "#ifdef FRAGMENT",
                                fragmentGLSL,
                                "#endif");

                            vertex.ProgramData = Encoding.UTF8.GetBytes(completedProgram);
                            vertex.ProgramType = ShaderPlatform;
                            vertex.BindChannels.Channels = new ShaderBindChannel[0];
                            vertex.TextureParameters = new TextureParameter[0];
                            vertex.StructParameters = new StructParameter[0];
                            vertex.ConstantBuffers = new ConstantBuffer[0];
                            vertex.ConstantBufferBindings = new BufferBinding[0];

                            fragment.ProgramData = new byte[0];
                            fragment.ProgramType = ShaderPlatform;
                            fragment.BindChannels.Channels = new ShaderBindChannel[0];
                            fragment.TextureParameters = new TextureParameter[0];
                            fragment.StructParameters = new StructParameter[0];
                            fragment.ConstantBuffers = new ConstantBuffer[0];
                            fragment.ConstantBufferBindings = new BufferBinding[0];

                            var serializedVertex = vertexSubProgram.Copy();
                            serializedVertex["m_GpuProgramType"].value.Set(ShaderPlatform);
                            serializedVertex["m_Channels"]["Array"].SetChildrenList(NoChildren);
                            serializedVertex["m_TextureParams"]["Array"].SetChildrenList(NoChildren);
                            serializedVertex["m_ConstantBuffers"]["Array"].SetChildrenList(NoChildren);
                            serializedVertex["m_ConstantBufferBindings"]["Array"].SetChildrenList(NoChildren);
                            vertices.SetChildrenList(vertices.children.Concat(new AssetTypeValueField[] { serializedVertex }).ToArray());

                            var serializedFragment = fragmentSubProgram.Copy();
                            serializedFragment["m_GpuProgramType"].value.Set(ShaderPlatform);
                            serializedFragment["m_Channels"]["Array"].SetChildrenList(NoChildren);
                            serializedFragment["m_TextureParams"]["Array"].SetChildrenList(NoChildren);
                            serializedFragment["m_ConstantBuffers"]["Array"].SetChildrenList(NoChildren);
                            serializedFragment["m_ConstantBufferBindings"]["Array"].SetChildrenList(NoChildren);
                            fragments.SetChildrenList(fragments.children.Concat(new AssetTypeValueField[] { serializedFragment }).ToArray());
                        }
                    }
                }

                blob.Write(layout, memStream, out uint[] newOffsets, out uint[] newCompressedLengths, out uint[] newDecompressedLengths);
                var newOffsetsArr = shader["offsets"]["Array"].children[0].Copy();
                AssetsToolsHelper.SetArray(newOffsetsArr, newOffsets);
                shader["offsets"]["Array"].SetChildrenList(shader["offsets"]["Array"].children.Concat(new AssetTypeValueField[] { newOffsetsArr }).ToArray());
                var newCompressedLengthsArr = shader["compressedLengths"]["Array"].children[0].Copy();
                AssetsToolsHelper.SetArray(newCompressedLengthsArr, newCompressedLengths);
                shader["compressedLengths"]["Array"].SetChildrenList(shader["compressedLengths"]["Array"].children.Concat(new AssetTypeValueField[] { newCompressedLengthsArr }).ToArray());
                var newDecompressedLengthsArr = shader["decompressedLengths"]["Array"].children[0].Copy();
                AssetsToolsHelper.SetArray(newDecompressedLengthsArr, newDecompressedLengths);
                shader["decompressedLengths"]["Array"].SetChildrenList(shader["decompressedLengths"]["Array"].children.Concat(new AssetTypeValueField[] { newDecompressedLengthsArr }).ToArray());
                AssetsToolsHelper.SetArray(shader["compressedBlob"], memStream.ToArray());
                AssetsToolsHelper.AddReplacer(replacers, asset, shader);
            }

            if (replacers.Count > 0)
            {
                var writePath = inPlace ? Path.GetTempFileName() : destPath;
                var writer = new AssetsFileWriter(File.OpenWrite(writePath));
                inst.file.Write(writer, 0, replacers, 0);
                writer.Close();
                AssetsManager.UnloadAssetsFile(inst.path);

                if (inPlace)
                {
                    File.Move(writePath, destPath, true);
                }
            }
        }

        public AssetsManager AssetsManager { get; set; } = new();
        public string DataPath { get; set; }
        public AssetsFileInstance EngineSettings { get; set; }
        public List<AssetsFileInstance> AssetsFiles { get; set; }
        public UnityVersion Version { get; set; }
        public LanguageCache Cache { get; set; }
    }
}
