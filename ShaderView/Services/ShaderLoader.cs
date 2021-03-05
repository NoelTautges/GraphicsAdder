using GraphicsAdder.Common;
using ShaderView.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using uTinyRipper;
using UnityVersion = uTinyRipper.Version;
using uTinyRipper.Classes;
using uTinyRipper.Classes.Shaders;
using System.Text;

namespace ShaderView.Services
{
    public class ShaderLoader
    {
        public (UnityVersion, List<ComponentListing>) LoadShaders(string folder)
        {
            var structure = GameStructure.Load(new List<string> { folder });
            var components = new List<ComponentListing>();
            var version = UnityVersion.MaxVersion;
            var shaders = new List<Shader>();

            foreach (var file in structure.FileCollection.SerializedFiles)
            {
                version = file.Version;

                foreach (var asset in file.FetchAssets())
                {
                    if (asset.ClassID != ClassIDType.Shader)
                    {
                        continue;
                    }

                    shaders.Add((Shader)asset);
                }
            }

            shaders.Sort((x, y) => x.ParsedForm.Name.CompareTo(y.ParsedForm.Name));

            foreach (var (shader, shaderIndex) in shaders.WithIndex())
            {
                var shaderComponent = new ComponentListing()
                {
                    Name = $"{shader.ParsedForm.Name} (path {shader.PathID})"
                };
                components.Add(shaderComponent);

                var platforms = shader.Platforms;
                var directXIndex = platforms.IndexOf(GPUPlatform.d3d11);
                var openGLIndex = platforms.IndexOf(GPUPlatform.glcore);

                foreach (var (subShader, subShaderIndex) in shader.ParsedForm.SubShaders.WithIndex())
                {
                    var subShaderComponent = new ComponentListing()
                    {
                        Name = $"Subshader {subShaderIndex} (LOD {subShader.LOD})"
                    };
                    shaderComponent.Children.Add(subShaderComponent);

                    foreach (var (pass, passIndex) in subShader.Passes.WithIndex())
                    {
                        var passComponent = new ComponentListing()
                        {
                            Name = "Pass " + (pass.State.Name == "" ? passIndex.ToString() : $"{pass.State.Name} ({passIndex})")
                        };
                        subShaderComponent.Children.Add(passComponent);

                        var vertexPrograms = pass.ProgVertex.SubPrograms;
                        foreach (var (prog, progIndex) in vertexPrograms.WithIndex())
                        {
                            if (progIndex % platforms.Length != 0)
                            {
                                continue;
                            }

                            var name = new StringBuilder();
                            name.Append($"Vertex subprogram {progIndex} (blob {prog.BlobIndex}");
                            if (prog.GlobalKeywordIndices.Length > 0)
                            {
                                name.Append(", keywords ");
                                foreach (var (keyword, keywordIndex) in prog.GlobalKeywordIndices.WithIndex())
                                {
                                    if (keywordIndex > 0)
                                    {
                                        name.Append(", ");
                                    }
                                    name.Append(pass.NameIndices.Where(pair => pair.Value == keyword).Select(pair => pair.Key).First());
                                }
                            }
                            name.Append(")");

                            passComponent.Children.Add(new SubProgramListing(new ShaderContext(shader, pass, prog.BlobIndex))
                            {
                                Name = name.ToString(),
                                Blobs = shader.Blobs,
                                DirectXIndex = directXIndex,
                                OpenGLIndex = openGLIndex,
                            });
                        }
                        var fragmentPrograms = pass.ProgFragment.SubPrograms;
                        foreach (var (prog, progIndex) in fragmentPrograms.WithIndex())
                        {
                            if (progIndex % platforms.Length != 0)
                            {
                                continue;
                            }

                            var name = new StringBuilder();
                            name.Append($"Fragment subprogram {progIndex} (blob {prog.BlobIndex}");
                            if (prog.GlobalKeywordIndices.Length > 0)
                            {
                                name.Append(", keywords ");
                                foreach (var (keyword, keywordIndex) in prog.GlobalKeywordIndices.WithIndex())
                                {
                                    if (keywordIndex > 0)
                                    {
                                        name.Append(", ");
                                    }
                                    name.Append(pass.NameIndices.Where(pair => pair.Value == keyword).Select(pair => pair.Key).First());
                                }
                            }
                            name.Append(")");

                            passComponent.Children.Add(new SubProgramListing(new ShaderContext(shader, pass, prog.BlobIndex))
                            {
                                Name = name.ToString(),
                                Blobs = shader.Blobs,
                                DirectXIndex = directXIndex,
                                OpenGLIndex = openGLIndex,
                            });
                        }
                    }
                }
            }

            return (version, components);
        }
    }
}
