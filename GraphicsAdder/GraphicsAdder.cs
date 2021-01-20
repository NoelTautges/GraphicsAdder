﻿using AssetsTools.NET;
using AssetsTools.NET.Extra;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using uTinyRipper;
using uTinyRipper.Classes.Shaders;
using UnityObject = uTinyRipper.Classes.Object;
using UnityShader = uTinyRipper.Classes.Shader;

namespace GraphicsAdder
{
    static class GraphicsAdder
    {
        public const int shaderPlatform = (int)ShaderGpuProgramType55.GLCore41;
        public const bool spoilers = true;

        public static IEnumerable<(T item, int index)> WithIndex<T>(this IEnumerable<T> source)
        {
            return source.Select((item, index) => (item, index));
        }

        static AssetTypeValueField[] GetArray<T>(AssetTypeValueField template, T[] array)
        {
            return array.Select(value =>
            {
                var val = ValueBuilder.DefaultValueFieldFromArrayTemplate(template);
                val.GetValue().Set(value);
                return val;
            }).ToArray();
        }

        static void ConvertFile(AssetsManager am, SerializedFile file, string destPath)
        {
            var inst = am.LoadAssetsFile(file.FilePath, false);
            am.LoadClassDatabaseFromPackage(inst.file.typeTree.unityVersion);
            var replacers = new List<AssetsReplacer>();
            var shaderIndex = 0;

            foreach (UnityObject asset in file.FetchAssets())
            {
                Console.Write($" - Looking for shader...\r");

                if (asset.ClassID != ClassIDType.Shader)
                    continue;

                shaderIndex += 1;

                var shader = (UnityShader)asset;
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

                    if (direct3DIndex != -1)
                        break;
                }

                platforms.SetChildrenList(platforms.GetChildrenList().Concat(GetArray(platforms, new int[] { 15 })).ToArray());

                var offsets = new List<uint>();
                var compressedLengths = new List<uint>();
                var decompressedLengths = new List<uint>();
                var cache = new GLSLCache(file.Version);
                var subShaders = shader.ParsedForm.SubShaders.Length;

                using (var memStream = new MemoryStream())
                {
                    // This code is very version-specific and only works between Unity versions 5.5-2019.3
                    foreach (var blob in shader.Blobs)
                    {
                        blob.Write(file.Layout, memStream, out uint[] existingOffsets, out uint[] existingCompressedLengths, out uint[] existingDecompressedLengths);
                        offsets.AddRange(existingOffsets);
                        compressedLengths.AddRange(existingCompressedLengths);
                        decompressedLengths.AddRange(existingDecompressedLengths);
                    }

                    foreach (var (subShader, subIndex) in shader.ParsedForm.SubShaders.WithIndex())
                    {
                        foreach (var (pass, passIndex) in subShader.Passes.WithIndex())
                        {
                            var subPrograms = pass.ProgVertex.SubPrograms.Length;
                            for (int i = 0; i < subPrograms; i++)
                            {
                                var message = $" - Converting shader {(spoilers ? shader.ParsedForm.Name : shaderIndex)} - subshader {subIndex + 1}/{subShaders} - pass {passIndex + 1}/{subShader.Passes.Length} - program {i + 1}/{subPrograms}";
                                if (message.Length <= Console.WindowWidth)
                                    Console.Write(message);
                                else
                                    Console.Write($"{message.Substring(0, Console.WindowWidth - 3)}...");
                                Console.Write(new string(' ', Console.WindowWidth - Console.CursorLeft - 1));
                                Console.CursorLeft = 0;

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

                                var vertexGLSL = cache.GetGLSL(vertex, vertexSubProgram.BlobIndex);
                                var fragmentGLSL = cache.GetGLSL(fragment, fragmentSubProgram.BlobIndex);
                                var completedProgram = string.Join(
                                    "\n",
                                    "#ifdef VERTEX",
                                    vertexGLSL,
                                    "#endif",
                                    "#ifdef FRAGMENT",
                                    fragmentGLSL,
                                    "#endif");

                                vertex.ProgramData = Encoding.UTF8.GetBytes(completedProgram);
                                vertex.ProgramType = shaderPlatform;
                                vertex.BindChannels.Channels = new ShaderBindChannel[0];
                                vertex.TextureParameters = new TextureParameter[0];
                                vertex.StructParameters = new StructParameter[0];
                                vertex.ConstantBuffers = new ConstantBuffer[0];
                                vertex.ConstantBufferBindings = new BufferBinding[0];

                                fragment.ProgramData = new byte[0];
                                fragment.ProgramType = shaderPlatform;
                                fragment.BindChannels.Channels = new ShaderBindChannel[0];
                                fragment.TextureParameters = new TextureParameter[0];
                                fragment.StructParameters = new StructParameter[0];
                                fragment.ConstantBuffers = new ConstantBuffer[0];
                                fragment.ConstantBufferBindings = new BufferBinding[0];

                                var replicaPass = replicaBaseField.Get("m_ParsedForm").Get("m_SubShaders").Get("Array").children[subIndex].Get("m_Passes").Get("Array").children[passIndex];
                                var realPass = baseField.Get("m_ParsedForm").Get("m_SubShaders").Get("Array").children[subIndex].Get("m_Passes").Get("Array").children[passIndex];

                                var serializedVertex = replicaPass.Get("progVertex").Get("m_SubPrograms").Get("Array").children[i];
                                serializedVertex.Get("m_GpuProgramType").GetValue().Set(shaderPlatform);
                                serializedVertex.Get("m_Channels").Get("m_Channels").Get("Array").SetChildrenList(new AssetTypeValueField[0]);
                                serializedVertex.Get("m_TextureParams").Get("Array").SetChildrenList(new AssetTypeValueField[0]);
                                serializedVertex.Get("m_ConstantBuffers").Get("Array").SetChildrenList(new AssetTypeValueField[0]);
                                serializedVertex.Get("m_ConstantBufferBindings").Get("Array").SetChildrenList(new AssetTypeValueField[0]);
                                var vertices = realPass.Get("progVertex").Get("m_SubPrograms").Get("Array");
                                vertices.SetChildrenList(vertices.GetChildrenList().Concat(new AssetTypeValueField[] { serializedVertex }).ToArray());

                                var serializedFragment = replicaPass.Get("progFragment").Get("m_SubPrograms").Get("Array").children[bestFragmentIndex];
                                serializedFragment.Get("m_GpuProgramType").GetValue().Set(shaderPlatform);
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
                    offsets.AddRange(newOffsets);
                    compressedLengths.AddRange(newCompressedLengths);
                    decompressedLengths.AddRange(newDecompressedLengths);

                    var offsetsField = baseField.Get("offsets").Get("Array");
                    baseField.Get("offsets").Get("Array").SetChildrenList(GetArray(offsetsField, offsets.ToArray()));
                    baseField.Get("compressedLengths").Get("Array").SetChildrenList(GetArray(offsetsField, compressedLengths.ToArray()));
                    baseField.Get("decompressedLengths").Get("Array").SetChildrenList(GetArray(offsetsField, decompressedLengths.ToArray()));
                    baseField.Get("compressedBlob").Get("Array").SetChildrenList(GetArray(baseField.Get("compressedBlob").Get("Array"), memStream.ToArray()));
                }

                var newBytes = baseField.WriteToByteArray();
                replacers.Add(new AssetsReplacerFromMemory(0, shaderInfo.index, (int)shaderInfo.curFileType, 0xFFFF, newBytes));

                if (subShaders > 0)
                    Console.WriteLine();
            }

            if (File.Exists(destPath))
                File.Delete(destPath);
            var writer = new AssetsFileWriter(File.OpenWrite(destPath));
            inst.file.Write(writer, 0, replacers, 0);
            writer.Close();
        }

        static void Main(string[] args)
        {
            var am = new AssetsManager();
            try
            {
                am.LoadClassPackage("classdata.tpk");
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("classdata.tpk not found! Did you delete it?");
                Console.WriteLine("Press any key to quit...");
                Console.ReadKey();
                Environment.Exit(1);
            }

            var outPath = @"OuterWilds_Data_replacement";
            if (!Directory.Exists(outPath))
                Directory.CreateDirectory(outPath);

            var inPath = @"C:\Program Files (x86)\Steam\steamapps\common\Outer Wilds\OuterWilds_Data";
            var structure = GameStructure.Load(new List<string> { inPath });

            foreach (var file in structure.FileCollection.SerializedFiles)
            {
                if (file.Name.IndexOf("level") != -1)
                {
                    Console.WriteLine($"Skipping level file {file.Name}");
                    continue;
                }

                Console.WriteLine($"Reading asset bundle {file.Name}");
                var destPath = Path.Combine(outPath, Path.GetRelativePath(inPath, file.FilePath));
                Directory.CreateDirectory(Path.GetDirectoryName(destPath));
                ConvertFile(am, file, destPath);
            }

            Console.WriteLine("\n");
            Console.WriteLine("Done!");
            Console.WriteLine($"Copy the files from {outPath} to {inPath}, but make backups first or you'll need to reinstall the game to get back. :)");
            Console.WriteLine("Press any key to quit...");
            Console.ReadKey();
        }
    }
}