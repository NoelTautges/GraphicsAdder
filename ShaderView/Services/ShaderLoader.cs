using AssetsTools.NET;
using AssetsTools.NET.Extra;
using GraphicsAdder.Common;
using ShaderView.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using uTinyRipper.Classes.Shaders;

namespace ShaderView.Services
{
    public static class ShaderLoader
    {
        public static IEnumerable<SubProgramListing> GetSubProgramListings(AssetTypeValueField shader, ShaderSubProgramBlob[] blobs, AssetTypeValueField pass, AssetTypeValueField programGroup, string group, int platforms, int directXIndex, int openGLIndex)
        {
            var subPrograms = programGroup["m_SubPrograms"]["Array"].children;
            foreach (var (prog, progIndex) in subPrograms.WithIndex().Take((int)Math.Floor((double)subPrograms.Length / platforms)))
            {
                var name = new StringBuilder($"{group} subprogram {progIndex} (blob {prog["m_BlobIndex"].value.AsUInt()}");
                var firstKeyword = true;
                if (prog["m_GlobalKeywordIndices"]["Array"].childrenCount > 0 || prog["m_LocalKeywordIndices"]["Array"].childrenCount > 0)
                {
                    name.Append(", keywords ");
                }
                foreach (var keyword in prog["m_GlobalKeywordIndices"]["Array"].children)
                {
                    var keywordVal = keyword.value.AsUInt();
                    if (!firstKeyword)
                    {
                        name.Append(", ");
                    }
                    firstKeyword = false;
                    name.Append(pass["m_NameIndices"]["Array"].children
                        .Where(pair => pair["second"].value.AsInt() == keywordVal)
                        .First()["first"].value.AsString());
                }
                if (prog["m_LocalKeywordIndices"].children != null)
                {
                    foreach (var keyword in prog["m_LocalKeywordIndices"]["Array"].children)
                    {
                        var keywordVal = keyword.value.AsUInt();
                        if (!firstKeyword)
                        {
                            name.Append(", ");
                        }
                        firstKeyword = false;
                        name.Append(pass["m_NameIndices"]["Array"].children
                            .Where(pair => pair["second"].value.AsInt() == keywordVal)
                            .First()["first"].value.AsString());
                    }
                }
                name.Append(")");

                yield return new SubProgramListing(new ShaderContext(shader, pass, prog["m_BlobIndex"].value.AsUInt()))
                {
                    Name = name.ToString(),
                    Blobs = blobs,
                    DirectXIndex = directXIndex,
                    OpenGLIndex = openGLIndex,
                };
            }
        }

        public static List<ComponentListing> LoadShaders(GraphicsConverter converter)
        {
            var components = new List<ComponentListing>();
            var shaders = new List<(long, AssetTypeValueField)>();

            foreach (var file in converter.AssetsFiles)
            {
                foreach (var shader in file.table.GetAssetsOfType((int)AssetClassID.Shader))
                {
                    shaders.Add((shader.index, converter.AssetsManager.GetTypeInstance(file, shader).GetBaseField()));
                }
            }

            shaders.Sort((x, y) => x.Item2["m_ParsedForm"]["m_Name"].value.AsString().CompareTo(y.Item2["m_ParsedForm"]["m_Name"].value.AsString()));

            foreach (var ((pathID, shader), shaderIndex) in shaders.WithIndex())
            {
                var shaderComponent = new ComponentListing()
                {
                    Name = $"{shader["m_ParsedForm"]["m_Name"].value.AsString()} (path {pathID})"
                };
                components.Add(shaderComponent);

                var platforms = shader["platforms"]["Array"].children.Select(c => c.value.AsInt()).ToArray();
                var directXIndex = Array.IndexOf(platforms, (int)GPUPlatform.d3d11);
                var openGLIndex = Array.IndexOf(platforms, (int)GPUPlatform.glcore);
                var blobs = converter.GetBlobs(shader);

                foreach (var (subShader, subShaderIndex) in shader["m_ParsedForm"]["m_SubShaders"]["Array"].children.WithIndex())
                {
                    var subShaderComponent = new ComponentListing()
                    {
                        Name = $"Subshader {subShaderIndex} (LOD {subShader["m_LOD"].value.AsInt()})"
                    };
                    shaderComponent.Children.Add(subShaderComponent);

                    foreach (var (pass, passIndex) in subShader["m_Passes"]["Array"].children.WithIndex())
                    {
                        var stateName = pass["m_State"]["m_Name"].value.AsString();
                        var passComponent = new ComponentListing()
                        {
                            Name = "Pass " + (stateName == "" ? passIndex.ToString() : $"{stateName} ({passIndex})")
                        };
                        subShaderComponent.Children.Add(passComponent);

                        passComponent.Children.AddRange(GetSubProgramListings(shader, blobs, pass, pass["progVertex"], "Vertex", platforms.Length, directXIndex, openGLIndex));
                        passComponent.Children.AddRange(GetSubProgramListings(shader, blobs, pass, pass["progFragment"], "Fragment", platforms.Length, directXIndex, openGLIndex));
                    }
                }
            }

            return components;
        }
    }
}
