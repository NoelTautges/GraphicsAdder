using AssetsTools.NET;
using AssetsTools.NET.Extra;
using GraphicsAdder.Common;
using GraphicsAdder.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using uTinyRipper;
using uTinyRipper.Classes;
using uTinyRipper.Classes.Shaders;
using Object = uTinyRipper.Classes.Object;

namespace GraphicsAdder.Services
{
    public class GraphicsConverter
    {
        private const int ShaderPlatform = (int)ShaderGpuProgramType55.GLCore41;

        AssetTypeValueField[] GetArray<T>(AssetTypeValueField template, T[] array)
        {
            return array.Select(value =>
            {
                var val = ValueBuilder.DefaultValueFieldFromArrayTemplate(template);
                val.GetValue().Set(value);
                return val;
            }).ToArray();
        }

        void ConcatArray<T>(AssetTypeValueField template, T[] array)
        {
            template = template.Get("Array");
            template.SetChildrenList(template.children.Concat(GetArray(template, array)).ToArray());
        }

        void SetArray<T>(AssetTypeValueField template, T[] array)
        {
            template = template.Get("Array");
            template.SetChildrenList(GetArray(template, array));
        }

        void ConvertFile(AssetsManager am, SerializedFile file, string destPath)
        {
            var inst = am.LoadAssetsFile(file.FilePath, false);
            am.LoadClassDatabaseFromPackage(inst.file.typeTree.unityVersion);
            var replacers = new List<AssetsReplacer>();
            var shadersReplaced = false;

            foreach (var (asset, assetIndex) in file.FetchAssets().WithIndex())
            {
                if (asset.ClassID != ClassIDType.Shader)
                {
                    continue;
                }

                var shader = (Shader)asset;
                var shaderInfo = inst.table.GetAssetInfo(shader.PathID);
                var baseField = am.GetTypeInstance(inst.file, shaderInfo).GetBaseField();
                var replicaBaseField = am.GetTypeInstance(inst.file, shaderInfo).GetBaseField();

                var platforms = baseField.Get("platforms").Get("Array");
                var direct3DIndex = -1;
                foreach (var (platform, index) in platforms.GetChildrenList().WithIndex())
                {
                    var value = platform.GetValue().AsInt();

                    switch (value)
                    {
                        case 4:
                            direct3DIndex = index;
                            break;
                        case 15:
                            return;
                        default:
                            break;
                    }
                }

                platforms.SetChildrenList(platforms.GetChildrenList().Concat(GetArray(platforms, new int[] { 15 })).ToArray());
                shadersReplaced = true;

                var cache = new GLSLCache(file.Version);
                var subShaders = shader.ParsedForm.SubShaders.Length;

                using (var memStream = new MemoryStream())
                {
                    memStream.Write(baseField.Get("compressedBlob").Get("Array").GetChildrenList().Select(b => (byte)b.GetValue().AsInt()).ToArray());

                    foreach (var (subShader, subIndex) in shader.ParsedForm.SubShaders.WithIndex())
                    {
                        foreach (var (pass, passIndex) in subShader.Passes.WithIndex())
                        {
                            var subPrograms = pass.ProgVertex.SubPrograms.Length;
                            for (int i = 0; i < subPrograms; i++)
                            {

                                var vertexSubProgram = pass.ProgVertex.SubPrograms[i];
                                ref var vertex = ref shader.Blobs[direct3DIndex].SubPrograms[vertexSubProgram.BlobIndex];
                                var mostKeywords = -1;
                                var fragmentSubProgram = pass.ProgFragment.SubPrograms[0];
                                var bestFragmentIndex = -1;

                                foreach (var (possibleFragment, fragmentIndex) in pass.ProgFragment.SubPrograms.WithIndex())
                                {
                                    if (possibleFragment.GlobalKeywordIndices.Any(keyword => !vertexSubProgram.GlobalKeywordIndices.Contains(keyword)) ||
                                        (SerializedSubProgram.HasLocalKeywordIndices(file.Version) && possibleFragment.LocalKeywordIndices.Any(keyword => !vertexSubProgram.LocalKeywordIndices.Contains(keyword))))
                                    {
                                        continue;
                                    }

                                    var keywords = possibleFragment.GlobalKeywordIndices.Length + (SerializedSubProgram.HasLocalKeywordIndices(file.Version) ? possibleFragment.LocalKeywordIndices.Length : 0);
                                    if (keywords > mostKeywords)
                                    {
                                        mostKeywords = keywords;
                                        fragmentSubProgram = possibleFragment;
                                        bestFragmentIndex = fragmentIndex;
                                    }
                                }

                                ref var fragment = ref shader.Blobs[direct3DIndex].SubPrograms[fragmentSubProgram.BlobIndex];

                                var vertexGLSL = cache.GetGLSL(vertex, shader.ParsedForm.Name, vertexSubProgram.BlobIndex);
                                var fragmentGLSL = cache.GetGLSL(fragment, shader.ParsedForm.Name, fragmentSubProgram.BlobIndex);
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

                                var replicaPass = replicaBaseField.Get("m_ParsedForm").Get("m_SubShaders").Get("Array").children[subIndex].Get("m_Passes").Get("Array").children[passIndex];
                                var realPass = baseField.Get("m_ParsedForm").Get("m_SubShaders").Get("Array").children[subIndex].Get("m_Passes").Get("Array").children[passIndex];

                                var serializedVertex = replicaPass.Get("progVertex").Get("m_SubPrograms").Get("Array").children[i];
                                serializedVertex.Get("m_GpuProgramType").GetValue().Set(ShaderPlatform);
                                serializedVertex.Get("m_Channels").Get("m_Channels").Get("Array").SetChildrenList(new AssetTypeValueField[0]);
                                serializedVertex.Get("m_TextureParams").Get("Array").SetChildrenList(new AssetTypeValueField[0]);
                                serializedVertex.Get("m_ConstantBuffers").Get("Array").SetChildrenList(new AssetTypeValueField[0]);
                                serializedVertex.Get("m_ConstantBufferBindings").Get("Array").SetChildrenList(new AssetTypeValueField[0]);
                                var vertices = realPass.Get("progVertex").Get("m_SubPrograms").Get("Array");
                                vertices.SetChildrenList(vertices.GetChildrenList().Concat(new AssetTypeValueField[] { serializedVertex }).ToArray());

                                var serializedFragment = replicaPass.Get("progFragment").Get("m_SubPrograms").Get("Array").children[bestFragmentIndex];
                                serializedFragment.Get("m_GpuProgramType").GetValue().Set(ShaderPlatform);
                                serializedFragment.Get("m_Channels").Get("m_Channels").Get("Array").SetChildrenList(new AssetTypeValueField[0]);
                                serializedFragment.Get("m_TextureParams").Get("Array").SetChildrenList(new AssetTypeValueField[0]);
                                serializedFragment.Get("m_ConstantBuffers").Get("Array").SetChildrenList(new AssetTypeValueField[0]);
                                serializedFragment.Get("m_ConstantBufferBindings").Get("Array").SetChildrenList(new AssetTypeValueField[0]);
                                var fragments = realPass.Get("progFragment").Get("m_SubPrograms").Get("Array");
                                fragments.SetChildrenList(fragments.GetChildrenList().Concat(new AssetTypeValueField[] { serializedFragment }).ToArray());
                            }
                        }
                    }

                    shader.Blobs[direct3DIndex].Write(file.Layout, memStream, out uint[] newOffsets, out uint[] newCompressedLengths, out uint[] newDecompressedLengths);

                    ConcatArray(baseField.Get("offsets"), newOffsets);
                    ConcatArray(baseField.Get("compressedLengths"), newCompressedLengths);
                    ConcatArray(baseField.Get("decompressedLengths"), newDecompressedLengths);
                    SetArray(baseField.Get("compressedBlob"), memStream.ToArray());
                }

                var newBytes = baseField.WriteToByteArray();
                replacers.Add(new AssetsReplacerFromMemory(0, shaderInfo.index, (int)shaderInfo.curFileType, 0xFFFF, newBytes));
            }

            if (shadersReplaced)
            {
                if (File.Exists(destPath))
                    File.Delete(destPath);
                var writer = new AssetsFileWriter(File.OpenWrite(destPath));
                inst.file.Write(writer, 0, replacers, 0);
                writer.Close();
            }
        }

        public void StartConversion(IProgress<ConversionProgress> progressCallback, Settings settings)
        {
            var am = new AssetsManager();
            am.LoadClassPackage("classdata.tpk");

            var progress = new ConversionProgress()
            {
                InProgress = true
            };
            progressCallback.Report(progress);
            var structure = GameStructure.Load(new List<string> { settings.SourcePath });
            progress.Files = structure.FileCollection.SerializedFiles.Count;
            var outPath = settings.SeparateDestination ? settings.DestinationPath : settings.SourcePath;

            foreach (var (file, fileIndex) in structure.FileCollection.SerializedFiles.WithIndex())
            {
                progress.CurrentFile = fileIndex;
                progressCallback.Report(progress);

                if (file.Name.IndexOf("level") != -1)
                {
                    continue;
                }

                var destPath = Path.Combine(outPath, Path.GetRelativePath(settings.SourcePath, file.FilePath));
                var destDir = Path.GetDirectoryName(destPath);
                if (destDir is not null)
                {
                    Directory.CreateDirectory(destDir);
                }

                ConvertFile(am, file, destPath);
            }
        }
    }
}
